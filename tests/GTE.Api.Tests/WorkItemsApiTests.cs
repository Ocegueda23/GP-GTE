using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
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

    private static bool BaseDisponible()
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

        var cliente = fabricaApp.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:bdsGTE", CadenaLocal)).CreateClient();

        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2E{sufijo}";

        // Datos base: proyecto y el usuario que corresponde a la identidad "anonimo"
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

            var usuario = await contexto.TblUsuario.FirstOrDefaultAsync(u => u.Dominio == "anonimo");
            if (usuario is null)
            {
                usuario = new TblUsuario
                {
                    Dominio = "anonimo",
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
