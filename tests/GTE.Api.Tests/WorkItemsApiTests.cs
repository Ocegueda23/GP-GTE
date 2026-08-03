using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E del modulo WorkItems por HTTP contra una bdsGTE real (LocalDB): crear con
/// folio propio, RN-REQ-01 (suspension automatica), RN-REQ-03 (cierre sin avance
/// bloqueado), registro de tiempo y cierre. Se omite si no hay LocalDB.
/// </summary>
public class WorkItemsApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] = CadenaLocal
            })
            .Build();
        return new FabricaContexto(configuracion);
    }

    [Fact]
    public async Task VerticalCompleto_CrearIniciarSuspenderRegistrarTerminar()
    {
        if (!BaseDisponible())
        {
            return;
        }

        // lgarcia (Desarrollador) y no aviramontes (Administrador): desde el bypass
        // acotado de cierre (WI.OmitirValidacionCierre, 2026-08-02) Administrador ya
        // no se bloquea en el paso 4, asi que la regla general (RN-REQ-03) necesita
        // una identidad sin ese permiso -- mismo patron que otras pruebas del repo
        // (ArchivosApiTests, ComentariosApiTests) usan "lgarcia" como usuario comun.
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2E{sufijo}";

        // Datos base: proyecto y el usuario que corresponde a la identidad del token
        int idProyecto;
        int idUsuario;
        var usuarioCreado = false;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);

            var usuario = await contexto.TblUsuario.FirstOrDefaultAsync(u => u.Dominio == "lgarcia");
            if (usuario is null)
            {
                usuario = new TblUsuario
                {
                    Dominio = "lgarcia",
                    Nombre = "Usuario E2E",
                    UsuarioRegistro = "e2e",
                    Activo = true
                };
                contexto.TblUsuario.Add(usuario);
                usuarioCreado = true;
            }
            await contexto.SaveChangesAsync();
            idProyecto = proyecto.IdProyecto;
            idUsuario = usuario.IdUsuario;
        }

        var idItemA = 0;
        var idItemB = 0;
        try
        {
            // 1. Crear item A: folio propio de la serie del proyecto, estatus fijado por el backend
            var itemA = await CrearItemAsync(cliente, idProyecto, idUsuario, $"Item A {sufijo}");
            idItemA = itemA.GetProperty("idWorkItem").GetInt32();
            Assert.Equal($"{clave}-0001", itemA.GetProperty("folio").GetString());
            Assert.Equal("Pendiente", itemA.GetProperty("estatus").GetString());

            // 2. Crear item B e iniciarlo
            var itemB = await CrearItemAsync(cliente, idProyecto, idUsuario, $"Item B {sufijo}");
            idItemB = itemB.GetProperty("idWorkItem").GetInt32();
            Assert.Equal($"{clave}-0002", itemB.GetProperty("folio").GetString());
            await CambiarEstatusAsync(cliente, idItemB, "INICIAR", "En Proceso");

            // 3. RN-REQ-01: iniciar A suspende B automaticamente
            await CambiarEstatusAsync(cliente, idItemA, "INICIAR", "En Proceso");
            var detalleB = await ObtenerDetalleAsync(cliente, itemB.GetProperty("folio").GetString()!);
            Assert.Equal("Suspendido", detalleB.GetProperty("estatus").GetString());

            // 4. RN-REQ-03: terminar sin avance registrado se bloquea (400)
            var respuestaCierre = await cliente.PutAsJsonAsync(
                $"/api/v1/workitems/{idItemA}/estatus", new { accion = "TERMINAR" });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaCierre.StatusCode);

            // 5. Registrar tiempo (60 minutos hoy)
            var respuestaTiempo = await cliente.PostAsJsonAsync(
                $"/api/v1/workitems/{idItemA}/tiempo",
                new { fecha = DateOnly.FromDateTime(DateTime.Today), minutos = 60, descripcion = "Avance E2E" });
            respuestaTiempo.EnsureSuccessStatusCode();

            // 6. Terminar A: ahora si procede
            await CambiarEstatusAsync(cliente, idItemA, "TERMINAR", "Terminado");

            // 7. La bandeja default (abiertos) ya no muestra A; con estatus=-1 si aparece
            var abiertos = await ObtenerEnvelopeAsync(cliente,
                $"/api/v1/workitems?texto={clave}&pageSize=50");
            var todos = await ObtenerEnvelopeAsync(cliente,
                $"/api/v1/workitems?texto={clave}&estatus=-1&pageSize=50");
            Assert.Equal(1, abiertos.GetProperty("totalItems").GetInt32());   // solo B (suspendido)
            Assert.Equal(2, todos.GetProperty("totalItems").GetInt32());

            // 8. El tiempo invertido quedo materializado en el historial del item A
            await using var verificacion = fabricaDatos.ConectarContexto<DbContextGTE>();
            var intervalos = await verificacion.TblHistorialEstatus.AsNoTracking()
                .CountAsync(h => h.Proceso == "WorkItem" && h.IdRegistro == idItemA);
            Assert.True(intervalos >= 3);   // ALTA + INICIAR + TERMINAR
        }
        finally
        {
            await LimpiarAsync(fabricaDatos, clave, idProyecto, [idItemA, idItemB], usuarioCreado ? idUsuario : null);
        }
    }

    /// <summary>
    /// RN-REQ-05 (decision del equipo 2026-08-02): una tarea SIN asignar cuenta como
    /// "ajena" igual que una asignada a otra persona -- nadie "toma" trabajo del backlog
    /// solo con INICIAR o registrando tiempo; un Lider/Admin con WI.ModificarAjeno debe
    /// asignarla primero. Reportado por el usuario con una cuenta Desarrollador real
    /// (Antonio.Ochoa) sobre una tarea sin asignar del backlog.
    /// </summary>
    [Fact]
    public async Task ItemSinAsignar_SeTrataComoAjenoParaIniciarYRegistrarTiempo()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteOtro = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"SIN{sufijo}";

        int idProyecto;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto Sin Asignar E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);
            await contexto.SaveChangesAsync();
            idProyecto = proyecto.IdProyecto;
        }

        var idItem = 0;
        try
        {
            var respuestaCrear = await clienteOtro.PostAsJsonAsync("/api/v1/workitems", new
            {
                idProyecto,
                idTipoWorkItem = 3,
                titulo = $"Sin asignar {sufijo}",
                idPrioridad = 3,
                fechaCompromiso = DateTime.Today.AddDays(5)
            });
            respuestaCrear.EnsureSuccessStatusCode();
            var creado = await respuestaCrear.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            idItem = creado!.Response.GetProperty("idWorkItem").GetInt32();
            Assert.False(creado.Response.TryGetProperty("idAsignado", out var idAsignadoProp)
                && idAsignadoProp.ValueKind != JsonValueKind.Null);

            var respuestaIniciar = await clienteOtro.PutAsJsonAsync(
                $"/api/v1/workitems/{idItem}/estatus", new { accion = "INICIAR" });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaIniciar.StatusCode);

            var respuestaTiempo = await clienteOtro.PostAsJsonAsync(
                $"/api/v1/workitems/{idItem}/tiempo",
                new { fecha = DateOnly.FromDateTime(DateTime.Today), minutos = 15, descripcion = "Sin asignar" });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaTiempo.StatusCode);
        }
        finally
        {
            await LimpiarPruebasQaAsync(fabricaDatos, clave, idProyecto, [idItem], [], 0);
        }
    }

    /// <summary>
    /// RN-REQ-05: registrar tiempo en un item ajeno (asignado a otra persona) exige
    /// WI.ModificarAjeno, igual que editar campos o cambiar estatus -- este comando se
    /// quedo sin el gate cuando se agrego a los otros dos (2026-08-02), reportado por
    /// el usuario al probar con una cuenta Desarrollador real.
    /// </summary>
    [Fact]
    public async Task RegistrarTiempo_EnItemAjenoSeBloqueaSinPermiso()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteDueno = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"TMP{sufijo}";
        var dominioOtro = $"otro-dev-e2e-{sufijo}";

        int idProyecto;
        int idUsuarioDueno;
        int idUsuarioOtro;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto Tiempo E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);

            var dueno = await contexto.TblUsuario.FirstAsync(u => u.Dominio == "lgarcia");

            var idRolDesarrollador = await contexto.TblRol
                .Where(r => r.Nombre == "Desarrollador").Select(r => r.IdRol).FirstAsync();
            var otro = new TblUsuario
            {
                Dominio = dominioOtro,
                Nombre = "Otro Dev E2E",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(otro);
            await contexto.SaveChangesAsync();
            contexto.TblUsuarioRol.Add(new TblUsuarioRol
            {
                IdUsuario = otro.IdUsuario,
                IdRol = idRolDesarrollador,
                UsuarioRegistro = "e2e",
                Activo = true
            });
            await contexto.SaveChangesAsync();

            idProyecto = proyecto.IdProyecto;
            idUsuarioDueno = dueno.IdUsuario;
            idUsuarioOtro = otro.IdUsuario;
        }

        var clienteOtro = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, dominioOtro);
        var idItem = 0;
        try
        {
            var item = await CrearItemAsync(clienteDueno, idProyecto, idUsuarioDueno, $"Tiempo ajeno {sufijo}");
            idItem = item.GetProperty("idWorkItem").GetInt32();

            // Otro Desarrollador (sin WI.ModificarAjeno) no puede registrar tiempo aqui
            var respuestaAjena = await clienteOtro.PostAsJsonAsync(
                $"/api/v1/workitems/{idItem}/tiempo",
                new { fecha = DateOnly.FromDateTime(DateTime.Today), minutos = 15, descripcion = "Ajeno" });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaAjena.StatusCode);

            // El dueno si puede
            var respuestaPropia = await clienteDueno.PostAsJsonAsync(
                $"/api/v1/workitems/{idItem}/tiempo",
                new { fecha = DateOnly.FromDateTime(DateTime.Today), minutos = 15, descripcion = "Propio" });
            respuestaPropia.EnsureSuccessStatusCode();
        }
        finally
        {
            await LimpiarPruebasQaAsync(
                fabricaDatos, clave, idProyecto, [idItem], [], idUsuarioOtro);
        }
    }

    /// <summary>
    /// RN-REQ-05: marcar un hallazgo como CORREGIDO en un WorkItem ajeno exige
    /// WI.ModificarAjeno -- mismo hueco que RegistrarTiempoCommand, encontrado al
    /// revisar el resto del modulo (adjuntos, comentarios, revisiones) tras el fix
    /// de tiempo. `CrearRevisionCommand` (reportar) sigue sin gate a proposito
    /// (cualquiera puede reportar un hallazgo, es el rol de revisor); lo que faltaba
    /// era el gate al CERRARLO, que deberia hacerlo quien arreglo el WorkItem.
    /// </summary>
    [Fact]
    public async Task CorregirRevision_MarcarCorregidoEnItemAjenoSeBloqueaSinPermiso()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteDueno = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"HAL{sufijo}";
        var dominioOtro = $"otro-dev-e2e-{sufijo}";

        int idProyecto;
        int idUsuarioDueno;
        int idUsuarioOtro;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto Hallazgo E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);

            var dueno = await contexto.TblUsuario.FirstAsync(u => u.Dominio == "lgarcia");

            var idRolDesarrollador = await contexto.TblRol
                .Where(r => r.Nombre == "Desarrollador").Select(r => r.IdRol).FirstAsync();
            var otro = new TblUsuario
            {
                Dominio = dominioOtro,
                Nombre = "Otro Dev E2E",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(otro);
            await contexto.SaveChangesAsync();
            contexto.TblUsuarioRol.Add(new TblUsuarioRol
            {
                IdUsuario = otro.IdUsuario,
                IdRol = idRolDesarrollador,
                UsuarioRegistro = "e2e",
                Activo = true
            });
            await contexto.SaveChangesAsync();

            idProyecto = proyecto.IdProyecto;
            idUsuarioDueno = dueno.IdUsuario;
            idUsuarioOtro = otro.IdUsuario;
        }

        var clienteOtro = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, dominioOtro);
        var idItem = 0;
        var idsRevisiones = new List<int>();
        try
        {
            var item = await CrearItemAsync(clienteDueno, idProyecto, idUsuarioDueno, $"Hallazgo ajeno {sufijo}");
            idItem = item.GetProperty("idWorkItem").GetInt32();

            // Cualquiera puede REPORTAR el hallazgo (rol de revisor, sin gate a proposito)
            var respuestaHallazgo = await clienteOtro.PostAsJsonAsync(
                $"/api/v1/workitems/{idItem}/revisiones", new { comentarios = "Hallazgo E2E" });
            respuestaHallazgo.EnsureSuccessStatusCode();
            var hallazgo = await respuestaHallazgo.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            var idRevision = hallazgo!.Response.GetProperty("idRevision").GetInt32();
            idsRevisiones.Add(idRevision);

            // Un tercero SIN WI.ModificarAjeno no puede marcarlo corregido (el item es de "lgarcia")
            var respuestaAjena = await clienteOtro.PutAsJsonAsync(
                $"/api/v1/revisiones/{idRevision}/correccion", new { corregido = true });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaAjena.StatusCode);

            // El dueno del WorkItem si puede marcarlo corregido
            var respuestaPropia = await clienteDueno.PutAsJsonAsync(
                $"/api/v1/revisiones/{idRevision}/correccion", new { corregido = true });
            respuestaPropia.EnsureSuccessStatusCode();
        }
        finally
        {
            await LimpiarPruebasQaAsync(
                fabricaDatos, clave, idProyecto, [idItem], idsRevisiones, idUsuarioOtro);
        }
    }

    /// <summary>
    /// Reglas pedidas 2026-08-02 sobre el mini-flujo de QA que ya vive en el estatus
    /// del WorkItem (En Pruebas -&gt; TERMINAR/RECHAZAR_QA): solo quien tiene
    /// WI.AprobarPruebas puede aprobar/rechazar (rol QA por seed), no se puede
    /// aprobar/rechazar el propio elemento, y no se puede rechazar sin un hallazgo
    /// ya registrado. Ver CambiarEstatusWorkItemHandler.ValidarRevisionPruebasAsync
    /// y 15_2026-08-02_INSERT_bdsGTE_PermisoAprobarPruebas.sql.
    /// </summary>
    [Fact]
    public async Task VerticalPruebasQa_AutoaprobacionPermisoYHallazgoSeValidan()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var clienteDev = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"QA{sufijo}";
        var dominioQa = $"qa-e2e-{sufijo}";

        int idProyecto;
        int idUsuarioDev;
        int idUsuarioQa;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto QA E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);

            var dev = await contexto.TblUsuario.FirstAsync(u => u.Dominio == "lgarcia");

            var idRolQa = await contexto.TblRol.Where(r => r.Nombre == "QA").Select(r => r.IdRol).FirstAsync();
            var usuarioQa = new TblUsuario
            {
                Dominio = dominioQa,
                Nombre = "QA E2E",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(usuarioQa);
            await contexto.SaveChangesAsync();
            contexto.TblUsuarioRol.Add(new TblUsuarioRol
            {
                IdUsuario = usuarioQa.IdUsuario,
                IdRol = idRolQa,
                UsuarioRegistro = "e2e",
                Activo = true
            });
            await contexto.SaveChangesAsync();

            idProyecto = proyecto.IdProyecto;
            idUsuarioDev = dev.IdUsuario;
            idUsuarioQa = usuarioQa.IdUsuario;
        }

        var clienteQa = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, dominioQa);
        var idsItems = new List<int>();
        var idsRevisiones = new List<int>();
        try
        {
            // A. Autoaprobacion: QA no puede aprobar/rechazar su PROPIO elemento
            var itemA = await CrearItemAsync(clienteDev, idProyecto, idUsuarioQa, $"Autoaprobacion {sufijo}");
            var idItemA = itemA.GetProperty("idWorkItem").GetInt32();
            idsItems.Add(idItemA);
            await CambiarEstatusAsync(clienteQa, idItemA, "INICIAR", "En Proceso");
            await CambiarEstatusAsync(clienteQa, idItemA, "ENVIAR_PRUEBAS", "En Pruebas");

            var respuestaAutoaprobacion = await clienteQa.PutAsJsonAsync(
                $"/api/v1/workitems/{idItemA}/estatus", new { accion = "TERMINAR" });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaAutoaprobacion.StatusCode);

            // B. Permiso: un Desarrollador sin WI.AprobarPruebas no puede aprobar ni su propio item
            var itemB = await CrearItemAsync(clienteDev, idProyecto, idUsuarioDev, $"SinPermiso {sufijo}");
            var idItemB = itemB.GetProperty("idWorkItem").GetInt32();
            idsItems.Add(idItemB);
            await CambiarEstatusAsync(clienteDev, idItemB, "INICIAR", "En Proceso");
            await CambiarEstatusAsync(clienteDev, idItemB, "ENVIAR_PRUEBAS", "En Pruebas");

            var respuestaSinPermiso = await clienteDev.PutAsJsonAsync(
                $"/api/v1/workitems/{idItemB}/estatus", new { accion = "TERMINAR" });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaSinPermiso.StatusCode);

            // C. Rechazo exige hallazgo: QA (dueño distinto) no puede RECHAZAR_QA sin hallazgo previo
            var itemC = await CrearItemAsync(clienteDev, idProyecto, idUsuarioDev, $"RechazoSinHallazgo {sufijo}");
            var idItemC = itemC.GetProperty("idWorkItem").GetInt32();
            idsItems.Add(idItemC);
            await CambiarEstatusAsync(clienteDev, idItemC, "INICIAR", "En Proceso");
            await CambiarEstatusAsync(clienteDev, idItemC, "ENVIAR_PRUEBAS", "En Pruebas");

            var respuestaRechazoSinHallazgo = await clienteQa.PutAsJsonAsync(
                $"/api/v1/workitems/{idItemC}/estatus", new { accion = "RECHAZAR_QA", motivo = "No cumple" });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaRechazoSinHallazgo.StatusCode);

            var respuestaHallazgo = await clienteQa.PostAsJsonAsync(
                $"/api/v1/workitems/{idItemC}/revisiones", new { comentarios = "No cumple el criterio X" });
            respuestaHallazgo.EnsureSuccessStatusCode();
            var hallazgo = await respuestaHallazgo.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            idsRevisiones.Add(hallazgo!.Response.GetProperty("idRevision").GetInt32());

            // Con el hallazgo ya registrado, el rechazo si procede (motivo obligatorio
            // para RECHAZAR_QA por RequiereMotivo en tblTransicionConfig)
            var respuestaRechazoConHallazgo = await clienteQa.PutAsJsonAsync(
                $"/api/v1/workitems/{idItemC}/estatus", new { accion = "RECHAZAR_QA", motivo = "Confirmado, no cumple" });
            respuestaRechazoConHallazgo.EnsureSuccessStatusCode();
            var envelopeRechazo = await respuestaRechazoConHallazgo.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            Assert.Equal("Correccion", envelopeRechazo!.Response.GetProperty("estatus").GetString());

            // D. Camino feliz: QA (dueño distinto, con permiso, sin pendientes) aprueba
            var itemD = await CrearItemAsync(clienteDev, idProyecto, idUsuarioDev, $"Aprobado {sufijo}");
            var idItemD = itemD.GetProperty("idWorkItem").GetInt32();
            idsItems.Add(idItemD);
            await CambiarEstatusAsync(clienteDev, idItemD, "INICIAR", "En Proceso");
            var respuestaTiempo = await clienteDev.PostAsJsonAsync(
                $"/api/v1/workitems/{idItemD}/tiempo",
                new { fecha = DateOnly.FromDateTime(DateTime.Today), minutos = 30, descripcion = "Avance" });
            respuestaTiempo.EnsureSuccessStatusCode();
            await CambiarEstatusAsync(clienteDev, idItemD, "ENVIAR_PRUEBAS", "En Pruebas");
            await CambiarEstatusAsync(clienteQa, idItemD, "TERMINAR", "Terminado");
        }
        finally
        {
            await LimpiarPruebasQaAsync(fabricaDatos, clave, idProyecto, idsItems, idsRevisiones, idUsuarioQa);
        }
    }

    private static async Task LimpiarPruebasQaAsync(
        FabricaContexto fabrica, string clave, int idProyecto,
        List<int> idsItems, List<int> idsRevisiones, int idUsuarioQa)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var ids = idsItems.Where(i => i > 0).ToArray();

        contexto.TblHistorialEstatus.RemoveRange(
            contexto.TblHistorialEstatus.Where(h => h.Proceso == "Revision" && idsRevisiones.Contains(h.IdRegistro)));
        contexto.TblRevision.RemoveRange(contexto.TblRevision.Where(r => idsRevisiones.Contains(r.IdRevision)));
        contexto.TblRegistroTiempo.RemoveRange(contexto.TblRegistroTiempo.Where(t => ids.Contains(t.IdWorkItem)));
        contexto.TblHistorialEstatus.RemoveRange(
            contexto.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && ids.Contains(h.IdRegistro)));
        contexto.TblHistorialCampo.RemoveRange(
            contexto.TblHistorialCampo.Where(h => h.Entidad == "WorkItem" && ids.Contains(h.IdEntidad)));
        contexto.TblBitacora.RemoveRange(
            contexto.TblBitacora.Where(b => b.Entidad == "WorkItem" && b.IdEntidad != null && ids.Contains(b.IdEntidad.Value)));
        await contexto.SaveChangesAsync();

        contexto.TblWorkItem.RemoveRange(contexto.TblWorkItem.Where(w => ids.Contains(w.IdWorkItem)));
        await contexto.SaveChangesAsync();

        contexto.TblFolio.RemoveRange(contexto.TblFolio.Where(f => f.Serie == clave));
        contexto.TblProyecto.RemoveRange(contexto.TblProyecto.Where(p => p.IdProyecto == idProyecto));
        contexto.TblUsuarioRol.RemoveRange(contexto.TblUsuarioRol.Where(ur => ur.IdUsuario == idUsuarioQa));
        contexto.TblUsuario.RemoveRange(contexto.TblUsuario.Where(u => u.IdUsuario == idUsuarioQa));
        await contexto.SaveChangesAsync();
    }

    private static async Task<JsonElement> CrearItemAsync(
        HttpClient cliente, int idProyecto, int idAsignado, string titulo)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/workitems", new
        {
            idProyecto,
            idTipoWorkItem = 3,   // Historia
            titulo,
            idPrioridad = 3,
            idAsignado,
            fechaCompromiso = DateTime.Today.AddDays(5)
        });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        return envelope.Response;
    }

    private static async Task CambiarEstatusAsync(
        HttpClient cliente, int idWorkItem, string accion, string estatusEsperado)
    {
        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/workitems/{idWorkItem}/estatus", new { accion });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        Assert.Equal(estatusEsperado, envelope.Response.GetProperty("estatus").GetString());
    }

    private static async Task<JsonElement> ObtenerDetalleAsync(HttpClient cliente, string folio)
    {
        var envelope = await cliente.GetFromJsonAsync<Envelope<JsonElement>>(
            $"/api/v1/workitems/{folio}", OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response;
    }

    private static async Task<JsonElement> ObtenerEnvelopeAsync(HttpClient cliente, string url)
    {
        var envelope = await cliente.GetFromJsonAsync<Envelope<JsonElement>>(url, OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response;
    }

    private static async Task LimpiarAsync(
        FabricaContexto fabrica, string clave, int idProyecto, int[] idsItems, int? idUsuarioCreado)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var ids = idsItems.Where(i => i > 0).ToArray();

        contexto.TblRegistroTiempo.RemoveRange(
            contexto.TblRegistroTiempo.Where(t => ids.Contains(t.IdWorkItem)));
        contexto.TblHistorialEstatus.RemoveRange(
            contexto.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && ids.Contains(h.IdRegistro)));
        contexto.TblHistorialCampo.RemoveRange(
            contexto.TblHistorialCampo.Where(h => h.Entidad == "WorkItem" && ids.Contains(h.IdEntidad)));
        contexto.TblBitacora.RemoveRange(
            contexto.TblBitacora.Where(b => b.Entidad == "WorkItem" && b.IdEntidad != null && ids.Contains(b.IdEntidad.Value)));
        await contexto.SaveChangesAsync();

        contexto.TblWorkItem.RemoveRange(contexto.TblWorkItem.Where(w => ids.Contains(w.IdWorkItem)));
        await contexto.SaveChangesAsync();

        contexto.TblFolio.RemoveRange(contexto.TblFolio.Where(f => f.Serie == clave));
        contexto.TblProyecto.RemoveRange(contexto.TblProyecto.Where(p => p.IdProyecto == idProyecto));
        await contexto.SaveChangesAsync();

        if (idUsuarioCreado.HasValue)
        {
            contexto.TblUsuario.RemoveRange(contexto.TblUsuario.Where(u => u.IdUsuario == idUsuarioCreado.Value));
            await contexto.SaveChangesAsync();
        }
    }
}
