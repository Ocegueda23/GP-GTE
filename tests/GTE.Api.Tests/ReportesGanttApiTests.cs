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
/// E2E del R16 (Gantt de actividades). Se omite si no hay LocalDB.
///
/// Corre contra SQL Server real a proposito: el riesgo del reporte no es la logica sino la
/// TRADUCCION de la consulta (ordenar por un bool, un ThenBy sobre una navegacion opcional y
/// leer FechaInicio!.Value en la proyeccion). Nada de eso lo ve el compilador: revienta la
/// primera vez que alguien abre el reporte, que es justo lo que ya paso con el R15.
/// </summary>
public class ReportesGanttApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private sealed record ActividadGantt(
        int IdWorkItem, string Folio, string Tipo, string Titulo, string? Descripcion,
        int IdProyecto, string Proyecto, int? IdAsignado, string? Asignado,
        int IdEstatusWorkItem, string Estatus,
        DateTime FechaInicio, DateTime? FechaFin, DateTime? FechaCompromiso);

    private sealed record PaginaGantt(
        IReadOnlyList<ActividadGantt> Items, int Page, int PageSize, int TotalItems, int TotalPages);

    private sealed record ReporteGantt(
        DateOnly Desde, DateOnly Hasta, PaginaGantt Pagina, int TotalEnProgreso);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] = FabricaApiAutenticada.CadenaLocal
            })
            .Build();
        return new FabricaContexto(configuracion);
    }

    private static async Task<ReporteGantt> PedirAsync(HttpClient cliente, string consulta)
    {
        var respuesta = await cliente.GetAsync($"/api/v1/reportes/gantt-actividades?{consulta}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<ReporteGantt>>(OpcionesJson);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Response);
        return envelope.Response!;
    }

    /// <summary>
    /// Una actividad iniciada y cerrada dentro del rango y otra iniciada sin fecha de fin: el
    /// segundo caso es el que el reporte tiene que dibujar como "en progreso" en vez de omitir.
    /// </summary>
    private static async Task<(int IdCerrada, int IdAbierta, int IdProyecto, int IdAsignado)>
        SembrarActividadesAsync(FabricaContexto fabricaDatos, string sufijo)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();

        var usuario = await contexto.TblUsuario.AsNoTracking().FirstAsync(u => u.Dominio == "aviramontes");
        var proyecto = await contexto.TblProyecto.AsNoTracking().FirstAsync(p => p.Activo);
        var prioridad = await contexto.TblPrioridad.AsNoTracking().Select(p => p.Id).FirstAsync();

        TblWorkItem Nuevo(string titulo, DateTime inicio, DateTime? fin, int idEstatus) => new()
        {
            Folio = $"E2E-GANTT-{sufijo}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            IdTipoWorkItem = 4,
            IdProyecto = proyecto.IdProyecto,
            Titulo = titulo,
            IdEstatusWorkItem = idEstatus,
            IdPrioridad = prioridad,
            IdAsignado = usuario.IdUsuario,
            FechaInicio = inicio,
            FechaFin = fin,
            FechaRegistro = inicio.AddDays(-1),
            UsuarioRegistro = "e2e",
            // TRAMPA EF: el DEFAULT 1 de la columna bit no aplica de forma confiable en los
            // INSERT de EF, asi que Activo se fija a mano en toda alta.
            Activo = true,
        };

        var cerrada = Nuevo("E2E Gantt cerrada", DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-3), 6);
        var abierta = Nuevo("E2E Gantt en progreso", DateTime.Today.AddDays(-5), null, 2);

        contexto.TblWorkItem.AddRange(cerrada, abierta);
        await contexto.SaveChangesAsync();

        return (cerrada.IdWorkItem, abierta.IdWorkItem, proyecto.IdProyecto, usuario.IdUsuario);
    }

    private static async Task BorrarAsync(FabricaContexto fabricaDatos, params int[] ids)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        var items = await contexto.TblWorkItem.Where(w => ids.Contains(w.IdWorkItem)).ToListAsync();
        contexto.TblWorkItem.RemoveRange(items);
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task Gantt_DevuelveActividadesCerradasYEnProgreso()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var fabricaDatos = CrearFabricaDatos();
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var (idCerrada, idAbierta, idProyecto, idAsignado) = await SembrarActividadesAsync(fabricaDatos, "A");

        try
        {
            var desde = DateTime.Today.AddDays(-20).ToString("yyyy-MM-dd");
            var hasta = DateTime.Today.ToString("yyyy-MM-dd");

            // Los tres filtros a la vez: es el caso que pidio el usuario y el que mas facil se
            // rompe si alguno se aplicara sobre una proyeccion en vez de sobre la entidad.
            var reporte = await PedirAsync(cliente,
                $"desde={desde}&hasta={hasta}&idProyecto={idProyecto}&idAsignado={idAsignado}" +
                "&agruparPor=Usuario&page=1&pageSize=200");

            var cerrada = reporte.Pagina.Items.SingleOrDefault(i => i.IdWorkItem == idCerrada);
            var abierta = reporte.Pagina.Items.SingleOrDefault(i => i.IdWorkItem == idAbierta);

            Assert.NotNull(cerrada);
            Assert.NotNull(abierta);
            Assert.NotNull(cerrada!.FechaFin);
            Assert.Null(abierta!.FechaFin);
            Assert.True(reporte.TotalEnProgreso >= 1);

            // Cada barra trae lo que el diagrama tiene que pintar sin volver a preguntar.
            Assert.False(string.IsNullOrWhiteSpace(abierta.Titulo));
            Assert.False(string.IsNullOrWhiteSpace(abierta.Estatus));
            Assert.Equal(idAsignado, abierta.IdAsignado);
            Assert.Equal(idProyecto, abierta.IdProyecto);

            // Todo renglon respeta los tres filtros combinados.
            Assert.All(reporte.Pagina.Items, i =>
            {
                Assert.Equal(idProyecto, i.IdProyecto);
                Assert.Equal(idAsignado, i.IdAsignado);
                Assert.True(i.FechaInicio.Date <= DateTime.Today);
            });
        }
        finally
        {
            await BorrarAsync(fabricaDatos, idCerrada, idAbierta);
        }
    }

    [Fact]
    public async Task Gantt_AgrupaPorProyectoYPorUsuarioSinRomperLaTraduccion()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var desde = DateTime.Today.AddMonths(-2).ToString("yyyy-MM-dd");
        var hasta = DateTime.Today.ToString("yyyy-MM-dd");

        // Ordenar por "sin asignado" (un bool) y luego por el nombre de una navegacion opcional
        // es la parte que EF puede negarse a traducir; se ejercitan las tres agrupaciones.
        foreach (var agrupacion in new[] { "Ninguno", "Proyecto", "Usuario" })
        {
            var reporte = await PedirAsync(cliente, $"desde={desde}&hasta={hasta}&agruparPor={agrupacion}&pageSize=10");
            Assert.Equal(10, reporte.Pagina.PageSize);
            Assert.True(reporte.Pagina.Items.Count <= 10);
        }
    }

    [Fact]
    public async Task Gantt_SinActividadesEnElRango_DevuelveListaVaciaNoError()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");

        // Rango en el futuro: no puede haber trabajo iniciado. La UI muestra su estado vacio,
        // asi que el API tiene que responder 200 con cero renglones, no un error.
        var desde = DateTime.Today.AddYears(5).ToString("yyyy-MM-dd");
        var hasta = DateTime.Today.AddYears(5).AddDays(7).ToString("yyyy-MM-dd");

        var reporte = await PedirAsync(cliente, $"desde={desde}&hasta={hasta}&agruparPor=Proyecto");

        Assert.Empty(reporte.Pagina.Items);
        Assert.Equal(0, reporte.Pagina.TotalItems);
        Assert.Equal(0, reporte.TotalEnProgreso);
    }

    [Fact]
    public async Task Gantt_RangoInvertido_DevuelveErrorDeNegocioNoQuinientos()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var respuesta = await cliente.GetAsync(
            $"/api/v1/reportes/gantt-actividades?desde={DateTime.Today:yyyy-MM-dd}" +
            $"&hasta={DateTime.Today.AddDays(-10):yyyy-MM-dd}");

        Assert.NotEqual(HttpStatusCode.InternalServerError, respuesta.StatusCode);

        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<object>>(OpcionesJson);
        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
    }
}
