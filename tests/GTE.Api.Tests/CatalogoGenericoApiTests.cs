using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E del motor de catalogos genericos por HTTP contra una bdsGTE real (LocalDB, con los
/// scripts 37/38 aplicados). Cubre el ciclo completo (alta de catalogo, config de columnas,
/// CRUD de datos, baja logica), el guardado del SQL dinamico blindado (tabla/FK inexistentes
/// se rechazan antes de tocar la BD), el aislamiento por permiso CAT.&lt;CLAVE&gt;.* y el
/// cifrado de columnas (nunca se ve el texto plano salvo descifrado explicito y auditado).
/// Se omite si no hay LocalDB o si los scripts 37/38 no estan aplicados.
/// </summary>
public class CatalogoGenericoApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private sealed record ColumnaConfigDto(
        string NombreColumna, string TipoSql, bool EsNulable, int? LongitudMaxima, bool EsPk, bool EsIdentity,
        string DisplayName, bool EsVisible, bool EsSoloLectura, bool EsRequerido, int OrdinalPos,
        string? TablaFk, string? ColumnaClaveFk, string? ColumnaMostrarFk, bool EsCifrado,
        bool AutoFechaAlta, bool AutoFechaEdicion, bool AutoUsuarioAlta, bool AutoUsuarioEdicion);

    private sealed record ConfiguracionCatalogoDto(
        int IdCatalogo, string Clave, string NombreTabla, string Titulo, List<ColumnaConfigDto> Columnas);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:bdsGTE"] = CadenaLocal })
            .Build();
        return new FabricaContexto(configuracion);
    }

    private static async Task<ConfiguracionCatalogoDto> CrearCatalogoAsync(
        HttpClient cliente, string clave, string nombreTabla, string titulo)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/catalogo-generico/admin", new { clave, nombreTabla, titulo });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<ConfiguracionCatalogoDto>>(OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response!;
    }

    /// <summary>Borra el catalogo, su config de columnas y los permisos CAT.&lt;CLAVE&gt;.* que haya sembrado.</summary>
    private static async Task LimpiarCatalogoAsync(FabricaContexto fabricaDatos, string clave)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        var catalogo = await contexto.TblCatalogoGenerico.FirstOrDefaultAsync(c => c.Clave == clave);
        if (catalogo is not null)
        {
            contexto.TblCatalogoGenericoColumna.RemoveRange(
                contexto.TblCatalogoGenericoColumna.Where(c => c.IdCatalogo == catalogo.IdCatalogo));
            await contexto.SaveChangesAsync();
            contexto.TblCatalogoGenerico.Remove(catalogo);
            await contexto.SaveChangesAsync();
        }

        var clavesPermiso = new[]
        {
            $"CAT.{clave}.Ver", $"CAT.{clave}.Crear", $"CAT.{clave}.Editar",
            $"CAT.{clave}.Eliminar", $"CAT.{clave}.Descifrar"
        };
        var permisos = await contexto.TblPermiso.Where(p => clavesPermiso.Contains(p.Clave)).ToListAsync();
        var idsPermiso = permisos.Select(p => p.IdPermiso).ToList();
        contexto.TblRolPermiso.RemoveRange(contexto.TblRolPermiso.Where(rp => idsPermiso.Contains(rp.IdPermiso)));
        await contexto.SaveChangesAsync();
        contexto.TblPermiso.RemoveRange(permisos);
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task CicloCompleto_AltaConfiguracionCrudYBajaLogica()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2ECX{sufijo}";
        var idComplejidad = 0;

        try
        {
            // Alta del catalogo: el esquema real reconciliado ya identifica PK/identity.
            var config = await CrearCatalogoAsync(cliente, clave, "tblComplejidad", $"Complejidad E2E {sufijo}");
            Assert.Contains(config.Columnas, c => c.NombreColumna == "IdComplejidad" && c.EsPk && c.EsIdentity);
            Assert.Contains(config.Columnas, c => c.NombreColumna == "Nombre" && !c.EsPk && !c.EsIdentity);

            // Config de columnas: auto-fecha/auto-usuario, y relaja lo que ya trae DEFAULT en BD
            // (si no se relaja, EsRequerido por defecto es "no admite NULL" y bloquearia el alta).
            var columnasEditadas = config.Columnas.Select(c => c.NombreColumna switch
            {
                "FechaRegistro" => c with { AutoFechaAlta = true },
                "UsuarioRegistro" => c with { AutoUsuarioAlta = true },
                "Activo" => c with { EsRequerido = false },
                "IdCategoriaProyecto" => c with { EsVisible = false, EsRequerido = false },
                _ => c
            }).ToList();

            var respuestaConfig = await cliente.PutAsJsonAsync(
                $"/api/v1/catalogo-generico/admin/{clave}/columnas", new { columnas = columnasEditadas });
            respuestaConfig.EnsureSuccessStatusCode();
            var envelopeConfig = await respuestaConfig.Content.ReadFromJsonAsync<Envelope<ConfiguracionCatalogoDto>>(OpcionesJson);
            Assert.True(envelopeConfig!.Response!.Columnas.Single(c => c.NombreColumna == "FechaRegistro").AutoFechaAlta);

            // Alta de un registro: no se manda FechaRegistro/UsuarioRegistro (son auto) ni
            // Activo/IdCategoriaProyecto (opcionales) -- deben quedar resueltos por la BD.
            var nombreInicial = $"Complejidad E2E {sufijo}";
            var respuestaCrear = await cliente.PostAsJsonAsync($"/api/v1/catalogo-generico/{clave}/registros", new
            {
                valores = new Dictionary<string, object?> { ["Nombre"] = nombreInicial, ["Orden"] = 99 }
            });
            respuestaCrear.EnsureSuccessStatusCode();
            var envelopeCrear = await respuestaCrear.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            var filaCreada = envelopeCrear!.Response;
            idComplejidad = filaCreada.GetProperty("IdComplejidad").GetInt32();
            Assert.Equal(nombreInicial, filaCreada.GetProperty("Nombre").GetString());
            Assert.Equal("aviramontes", filaCreada.GetProperty("UsuarioRegistro").GetString());
            Assert.True(filaCreada.GetProperty("Activo").GetBoolean());

            // Listado con filtro de texto libre encuentra el registro recien creado
            var respuestaListar = await cliente.PostAsJsonAsync(
                $"/api/v1/catalogo-generico/{clave}/registros/consulta",
                new { texto = nombreInicial, filtrosColumna = Array.Empty<object>(), pagina = 1, tamanoPagina = 10 });
            respuestaListar.EnsureSuccessStatusCode();
            var envelopeListar = await respuestaListar.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            var items = envelopeListar!.Response.GetProperty("items").EnumerateArray().ToList();
            Assert.Single(items);
            Assert.Equal(idComplejidad, items[0].GetProperty("IdComplejidad").GetInt32());

            // Edicion parcial (solo Nombre)
            var nombreEditado = $"{nombreInicial} editado";
            var respuestaEditar = await cliente.PutAsJsonAsync($"/api/v1/catalogo-generico/{clave}/registros", new
            {
                clavesPk = new Dictionary<string, object?> { ["IdComplejidad"] = idComplejidad },
                valores = new Dictionary<string, object?> { ["Nombre"] = nombreEditado }
            });
            respuestaEditar.EnsureSuccessStatusCode();

            // Baja: la tabla tiene columna Activo -> debe ser logica, no hard delete
            var respuestaEliminar = await cliente.SendAsync(new HttpRequestMessage(
                HttpMethod.Delete, $"/api/v1/catalogo-generico/{clave}/registros")
            {
                Content = JsonContent.Create(new { clavesPk = new Dictionary<string, object?> { ["IdComplejidad"] = idComplejidad } })
            });
            respuestaEliminar.EnsureSuccessStatusCode();

            await using var contextoVerificacion = fabricaDatos.ConectarContexto<DbContextGTE>();
            var fila = await contextoVerificacion.TblComplejidad.AsNoTracking()
                .SingleAsync(c => c.IdComplejidad == idComplejidad);
            Assert.Equal(nombreEditado, fila.Nombre);
            Assert.False(fila.Activo);
        }
        finally
        {
            if (idComplejidad > 0)
            {
                await using var limpieza = fabricaDatos.ConectarContexto<DbContextGTE>();
                limpieza.TblComplejidad.RemoveRange(limpieza.TblComplejidad.Where(c => c.IdComplejidad == idComplejidad));
                await limpieza.SaveChangesAsync();
            }
            await LimpiarCatalogoAsync(fabricaDatos, clave);
        }
    }

    [Fact]
    public async Task SinPermisoDeCatalogo_ConsultaYAdminSeRechazan()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var clienteSinPermiso = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2EPERM{sufijo}";

        try
        {
            // "lgarcia" no tiene rol Administrador, asi que no recibe los permisos CAT.*
            // sembrados al crear el catalogo (solo se asignan a Administrador).
            await CrearCatalogoAsync(clienteAdmin, clave, "tblComplejidad", $"Complejidad Permisos E2E {sufijo}");

            var respuestaConsulta = await clienteSinPermiso.PostAsJsonAsync(
                $"/api/v1/catalogo-generico/{clave}/registros/consulta",
                new { filtrosColumna = Array.Empty<object>(), pagina = 1, tamanoPagina = 10 });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaConsulta.StatusCode);

            var respuestaAdmin = await clienteSinPermiso.PostAsJsonAsync("/api/v1/catalogo-generico/admin", new
            {
                clave = $"{clave}B", nombreTabla = "tblComplejidad", titulo = "No deberia crearse"
            });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaAdmin.StatusCode);
        }
        finally
        {
            await LimpiarCatalogoAsync(fabricaDatos, clave);
        }
    }

    [Fact]
    public async Task CrearCatalogo_TablaInexistente_DevuelveBadRequest()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        // SQL dinamico blindado: el nombre de tabla se valida contra INFORMATION_SCHEMA
        // antes de guardarse -- una tabla inexistente nunca llega a persistirse.
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/catalogo-generico/admin", new
        {
            clave = $"E2ENOEXISTE{sufijo}", nombreTabla = $"tablaQueNoExiste{sufijo}", titulo = "No deberia crearse"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ActualizarConfigColumnas_FkInvalido_DevuelveBadRequest()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2EFK{sufijo}";

        try
        {
            var config = await CrearCatalogoAsync(cliente, clave, "tblComplejidad", $"Complejidad FK E2E {sufijo}");

            // El combo FK tambien se valida en vivo: tabla/columnas inexistentes se rechazan
            // antes de guardarse (mismo criterio que el nombre de tabla del catalogo).
            var columnasConFkInvalido = config.Columnas.Select(c => c.NombreColumna == "IdCategoriaProyecto"
                ? c with { TablaFk = $"tablaFkQueNoExiste{sufijo}", ColumnaClaveFk = "Id", ColumnaMostrarFk = "Nombre" }
                : c).ToList();

            var respuesta = await cliente.PutAsJsonAsync(
                $"/api/v1/catalogo-generico/admin/{clave}/columnas", new { columnas = columnasConFkInvalido });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }
        finally
        {
            await LimpiarCatalogoAsync(fabricaDatos, clave);
        }
    }

    [Fact]
    public async Task Cifrado_ProtegeAlmacenamiento_DescifradoExigePermisoYQuedaAuditado()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var clienteSinPermiso = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var clave = $"E2ECIF{sufijo}";
        // Tabla propia y desechable: evita la longitud/unicidad de columnas reales de bdsGTE
        // (el texto protegido de Data Protection es mas largo que un NVARCHAR(100) tipico).
        var nombreTabla = $"zzzE2ECifrado{sufijo}";
        var idFila = 0;

        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var conexion = contexto.Database.GetDbConnection();
            await conexion.OpenAsync();
            await using var comandoCrear = conexion.CreateCommand();
            comandoCrear.CommandText =
                $"CREATE TABLE dbo.{nombreTabla} (Id INT IDENTITY(1,1) PRIMARY KEY, Valor NVARCHAR(400) NULL)";
            await comandoCrear.ExecuteNonQueryAsync();
        }

        try
        {
            var config = await CrearCatalogoAsync(clienteAdmin, clave, nombreTabla, $"Cifrado E2E {sufijo}");
            var columnasEditadas = config.Columnas.Select(c => c.NombreColumna == "Valor"
                ? c with { EsCifrado = true, EsVisible = true, EsRequerido = false }
                : c).ToList();
            var respuestaConfig = await clienteAdmin.PutAsJsonAsync(
                $"/api/v1/catalogo-generico/admin/{clave}/columnas", new { columnas = columnasEditadas });
            respuestaConfig.EnsureSuccessStatusCode();

            var textoPlano = $"secreto-{sufijo}";
            var respuestaCrear = await clienteAdmin.PostAsJsonAsync($"/api/v1/catalogo-generico/{clave}/registros", new
            {
                valores = new Dictionary<string, object?> { ["Valor"] = textoPlano }
            });
            respuestaCrear.EnsureSuccessStatusCode();
            var envelopeCrear = await respuestaCrear.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            var filaCreada = envelopeCrear!.Response;
            idFila = filaCreada.GetProperty("Id").GetInt32();
            var valorAlmacenado = filaCreada.GetProperty("Valor").GetString();
            Assert.NotEqual(textoPlano, valorAlmacenado); // el alta nunca devuelve el texto plano

            // Sin el permiso dedicado CAT.<CLAVE>.Descifrar: 403
            var respuestaSinPermiso = await clienteSinPermiso.PostAsJsonAsync(
                $"/api/v1/catalogo-generico/{clave}/registros/descifrar",
                new { clavesPk = new Dictionary<string, object?> { ["Id"] = idFila }, nombreColumna = "Valor" });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaSinPermiso.StatusCode);

            // Con permiso: descifrado explicito recupera el texto original y deja bitacora
            var respuestaDescifrar = await clienteAdmin.PostAsJsonAsync(
                $"/api/v1/catalogo-generico/{clave}/registros/descifrar",
                new { clavesPk = new Dictionary<string, object?> { ["Id"] = idFila }, nombreColumna = "Valor" });
            respuestaDescifrar.EnsureSuccessStatusCode();
            var envelopeDescifrar = await respuestaDescifrar.Content.ReadFromJsonAsync<Envelope<string>>(OpcionesJson);
            Assert.Equal(textoPlano, envelopeDescifrar!.Response);

            await using var contextoBitacora = fabricaDatos.ConectarContexto<DbContextGTE>();
            var hayBitacora = await contextoBitacora.TblBitacora.AnyAsync(b =>
                b.Entidad == "CatalogoGenerico.Descifrado" && b.Detalle != null && b.Detalle.Contains(clave));
            Assert.True(hayBitacora);
        }
        finally
        {
            await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
            {
                contexto.TblBitacora.RemoveRange(contexto.TblBitacora.Where(b =>
                    b.Entidad == "CatalogoGenerico.Descifrado" && b.Detalle != null && b.Detalle.Contains(clave)));
                await contexto.SaveChangesAsync();
            }
            await LimpiarCatalogoAsync(fabricaDatos, clave);

            await using var contextoDrop = fabricaDatos.ConectarContexto<DbContextGTE>();
            var conexionDrop = contextoDrop.Database.GetDbConnection();
            await conexionDrop.OpenAsync();
            await using var comandoDrop = conexionDrop.CreateCommand();
            comandoDrop.CommandText = $"DROP TABLE IF EXISTS dbo.{nombreTabla}";
            await comandoDrop.ExecuteNonQueryAsync();
        }
    }
}
