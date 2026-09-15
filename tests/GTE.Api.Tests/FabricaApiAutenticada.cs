using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GTE.Api.Tests;

/// <summary>
/// Arranca la API de pruebas con el emisor local de tokens habilitado y entrega
/// clientes ya autenticados. Toda la API exige identidad, asi que las pruebas de
/// integracion deben iniciar sesion como lo haria la interfaz.
/// </summary>
public static class FabricaApiAutenticada
{
    public const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private const string ClaveFirmaPruebas = "clave-de-firma-solo-para-pruebas-integracion-32";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    public static bool BaseDisponible()
    {
        try
        {
            using var conexion = new SqlConnection(CadenaLocal);
            conexion.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static WebApplicationFactory<Program> Configurar(WebApplicationFactory<Program> fabrica)
    {
        return fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.UseSetting("Jwt:Issuer", "gte-api");
            builder.UseSetting("Jwt:Audience", "gte-api");
            builder.UseSetting("Jwt:ClaveFirma", ClaveFirmaPruebas);
            builder.UseSetting("Jwt:Desarrollo:Habilitado", "true");
            builder.UseSetting("Hangfire:Deshabilitado", "true");

            // La cadena NO puede ir por UseSetting: Program.cs agrega appsettings.Local.json
            // cuando el entorno es Development (y las pruebas corren en Development), ese
            // archivo es de cada desarrollador -- apunta a la instancia de SU maquina -- y
            // como se registra despues, GANABA sobre el UseSetting. Resultado: las pruebas
            // decian correr contra LocalDB y en realidad intentaban conectarse a la instancia
            // del otro equipo, fallando con "error 26 - Error Locating Server/Instance".
            // ConfigureAppConfiguration de la fabrica se aplica al final, asi que esto si pisa
            // a appsettings.Local.json.
            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:bdsGTE"] = CadenaLocal,
                    // El almacen por defecto apunta a D:\GTE\Archivos, que no existe en toda
                    // maquina; las pruebas usan una carpeta temporal propia.
                    ["AlmacenArchivos:Ruta"] = RutaAlmacenPruebas.Value,
                });
            });
        });
    }

    /// <summary>
    /// Da al usuario de pruebas el rol indicado si no lo tiene. Idempotente: se puede llamar en
    /// cada prueba sin duplicar filas.
    /// </summary>
    private static async Task AsegurarRolAsync(string dominio, string rol)
    {
        await using var conexion = new SqlConnection(CadenaLocal);
        await conexion.OpenAsync();

        var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO dbo.tblUsuarioRol (IdUsuario, IdRol, UsuarioRegistro, Activo)
            SELECT u.IdUsuario, r.IdRol, N'pruebas-integracion', 1
            FROM dbo.tblUsuario u
            CROSS JOIN dbo.tblRol r
            WHERE u.Dominio = @dominio
              AND r.Nombre = @rol
              AND NOT EXISTS (SELECT 1 FROM dbo.tblUsuarioRol ur
                              WHERE ur.IdUsuario = u.IdUsuario AND ur.IdRol = r.IdRol)
            """;
        comando.Parameters.AddWithValue("@dominio", dominio);
        comando.Parameters.AddWithValue("@rol", rol);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>Carpeta temporal para el almacen de archivos durante las pruebas.</summary>
    private static readonly Lazy<string> RutaAlmacenPruebas = new(() =>
    {
        var ruta = Path.Combine(Path.GetTempPath(), "gte-pruebas-archivos");
        Directory.CreateDirectory(ruta);
        return ruta;
    });

    /// <summary>
    /// Cuentas fijas de la suite y el rol que cada una debe tener. Las pruebas daban por hecho
    /// que ya existian con estos roles en la base de cada quien, asi que pasaban o fallaban
    /// segun la maquina; aqui se siembran para que la suite sea reproducible.
    /// Las pruebas que validan el caso "sin roles" usan un dominio aleatorio y no entran aqui.
    /// </summary>
    private static readonly Dictionary<string, string> RolesDePrueba = new()
    {
        ["aviramontes"] = "Administrador",
        ["lgarcia"] = "Desarrollador",
    };

    /// <summary>Cliente con el token de la cuenta indicada ya puesto en el encabezado.</summary>
    public static async Task<HttpClient> CrearClienteAsync(
        WebApplicationFactory<Program> fabrica, string dominio)
    {
        var cliente = Configurar(fabrica).CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/desarrollo/token", new { dominio });
        respuesta.EnsureSuccessStatusCode();

        // El emisor de desarrollo aprovisiona al usuario, pero SIN roles. Las pruebas daban por
        // hecho que la cuenta ya existia con permisos en la base de cada quien, asi que pasaban
        // o fallaban segun la maquina. Se siembra aqui, de forma idempotente, para que la suite
        // no dependa del estado de una BD en particular.
        if (RolesDePrueba.TryGetValue(dominio, out var rol))
        {
            await AsegurarRolAsync(dominio, rol);
        }

        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>(OpcionesJson);
        var token = contenido.GetProperty("response").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("El emisor de desarrollo no devolvio token.");

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    /// <summary>Cliente sin credenciales, para comprobar que la API rechaza el anonimato.</summary>
    public static HttpClient CrearClienteAnonimo(WebApplicationFactory<Program> fabrica)
    {
        return Configurar(fabrica).CreateClient();
    }

    /// <summary>
    /// Complejidad es obligatoria al crear un WorkItem (RN-GTE-015 extendida, 2026-08-07); las
    /// pruebas de integracion de varios modulos solo necesitan un Id valido para pasar la
    /// alta, no verifican el calculo de minutos/puntos -- se reutiliza cualquier fila activa
    /// ya sembrada y, si el ambiente no tiene ninguna, se crea una propia.
    /// </summary>
    public static async Task<int> ObtenerOCrearComplejidadAsync()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:bdsGTE"] = CadenaLocal })
            .Build();
        var fabricaDatos = new FabricaContexto(configuracion);
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();

        var existente = await contexto.TblComplejidad.AsNoTracking()
            .Where(c => c.Activo)
            .Select(c => c.IdComplejidad)
            .FirstOrDefaultAsync();
        if (existente != 0)
        {
            return existente;
        }

        var complejidad = new TblComplejidad
        {
            Nombre = $"E2E-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Orden = 1,
            UsuarioRegistro = "e2e",
            Activo = true
        };
        contexto.TblComplejidad.Add(complejidad);
        await contexto.SaveChangesAsync();
        return complejidad.IdComplejidad;
    }
}
