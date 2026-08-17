using GTE.Application.DTOs.Responses.Reportes;
using GTE.Application.Interfaces;
using GTE.Application.Reportes.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Catalogo de reportes R01-R14 (Doctos/GTE-DocumentoMaestro.md seccion 13): lectura, sin
/// comandos de escritura. Permisos: RPT.Ver (general), RPT.Costos (R08/R09), RPT.Auditoria
/// (R14), RPT.Actividad (reporte previo de actividad diaria). Cada reporte tiene su
/// endpoint de exportacion a Excel gemelo bajo "/exportar" (ClosedXML, mismos filtros).
/// </summary>
[ApiController]
[Route("api/v1/reportes")]
public class ReportesController(IMediator mediator, IExportadorExcel exportador) : ControllerBase
{
    private const string TipoContenidoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("actividad-usuario")]
    public async Task<ActionResult<ApiResponse<ActividadUsuarioResponse>>> ObtenerActividadUsuario(
        [FromQuery] int idUsuario, [FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ObtenerActividadUsuarioQuery(idUsuario, fechaInicio, fechaFin), cancellationToken);
        return Ok(ApiResponse<ActividadUsuarioResponse>.Exito(resultado));
    }

    // ---------- R01 Productividad ----------
    [HttpGet("productividad")]
    public async Task<ActionResult<ApiResponse<ProductividadReporteResponse>>> ObtenerProductividad(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, [FromQuery] int? idEquipo,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerProductividadQuery(desde, hasta, idProyecto, idEquipo), cancellationToken);
        return Ok(ApiResponse<ProductividadReporteResponse>.Exito(resultado));
    }

    [HttpGet("productividad/exportar")]
    public async Task<IActionResult> ExportarProductividad(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, [FromQuery] int? idEquipo,
        CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerProductividadQuery(desde, hasta, idProyecto, idEquipo), cancellationToken);
        var encabezados = new[] { "Usuario", "Items terminados", "Puntos", "% a tiempo", "Eficiencia %" };
        var filas = r.Personas.Select(p => (IReadOnlyList<object?>)
            [p.Usuario, p.ItemsTerminados, p.PuntosTotales, p.PorcentajeATiempo, p.EficienciaPorcentaje]).ToList();
        return ArchivoExcel("Productividad", encabezados, filas);
    }

    // ---------- R02 Horas registradas ----------
    [HttpGet("horas-registradas")]
    public async Task<ActionResult<ApiResponse<HorasRegistradasReporteResponse>>> ObtenerHorasRegistradas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idEquipo, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerHorasRegistradasQuery(desde, hasta, idEquipo), cancellationToken);
        return Ok(ApiResponse<HorasRegistradasReporteResponse>.Exito(resultado));
    }

    [HttpGet("horas-registradas/exportar")]
    public async Task<IActionResult> ExportarHorasRegistradas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idEquipo, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerHorasRegistradasQuery(desde, hasta, idEquipo), cancellationToken);
        var encabezados = new[] { "Usuario", "Fecha", "Minutos", "Ausencia" };
        var filas = r.Usuarios.SelectMany(u => u.Dias.Select(d => (IReadOnlyList<object?>)
            [u.Usuario, d.Fecha, d.Minutos, d.EsAusencia ? "Si" : "No"])).ToList();
        return ArchivoExcel("HorasRegistradas", encabezados, filas);
    }

    // ---------- R03 Retrabajo ----------
    [HttpGet("retrabajo")]
    public async Task<ActionResult<ApiResponse<RetrabajoReporteResponse>>> ObtenerRetrabajo(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRetrabajoQuery(desde, hasta, idProyecto), cancellationToken);
        return Ok(ApiResponse<RetrabajoReporteResponse>.Exito(resultado));
    }

    [HttpGet("retrabajo/exportar")]
    public async Task<IActionResult> ExportarRetrabajo(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerRetrabajoQuery(desde, hasta, idProyecto), cancellationToken);
        var encabezados = new[] { "Usuario", "Proyecto", "Minutos correccion", "Minutos totales", "% retrabajo" };
        var filas = r.Detalle.Select(d => (IReadOnlyList<object?>)
            [d.Usuario, d.Proyecto, d.MinutosCorreccion, d.MinutosTotales, d.Porcentaje]).ToList();
        return ArchivoExcel("Retrabajo", encabezados, filas);
    }

    // ---------- R04 Bugs y defectos ----------
    [HttpGet("bugs-defectos")]
    public async Task<ActionResult<ApiResponse<BugsDefectosReporteResponse>>> ObtenerBugsDefectos(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerBugsDefectosQuery(desde, hasta, idProyecto), cancellationToken);
        return Ok(ApiResponse<BugsDefectosReporteResponse>.Exito(resultado));
    }

    [HttpGet("bugs-defectos/exportar")]
    public async Task<IActionResult> ExportarBugsDefectos(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerBugsDefectosQuery(desde, hasta, idProyecto), cancellationToken);
        var encabezados = new[] { "Proyecto", "Total bugs", "Total items", "Densidad %", "Aging promedio (dias)", "Escapados", "Tasa escape %" };
        var filas = r.Proyectos.Select(p => (IReadOnlyList<object?>)
            [p.Proyecto, p.TotalBugs, p.TotalItems, p.DensidadPorcentaje, p.AgingPromedioDias, p.Escapados, p.TasaEscapePorcentaje]).ToList();
        return ArchivoExcel("BugsDefectos", encabezados, filas);
    }

    // ---------- R05 Releases ----------
    [HttpGet("releases")]
    public async Task<ActionResult<ApiResponse<ReleasesReporteResponse>>> ObtenerReleasesReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerReleasesReporteQuery(desde, hasta, idProyecto), cancellationToken);
        return Ok(ApiResponse<ReleasesReporteResponse>.Exito(resultado));
    }

    [HttpGet("releases/exportar")]
    public async Task<IActionResult> ExportarReleasesReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerReleasesReporteQuery(desde, hasta, idProyecto), cancellationToken);
        var encabezados = new[] { "Version", "Folio", "Proyecto", "Fecha liberacion", "Dias aprobacion", "Items incluidos" };
        var filas = r.Releases.Select(x => (IReadOnlyList<object?>)
            [x.Version, x.Folio, x.Proyecto, x.FechaLiberacion, x.DiasAprobacion, x.ItemsIncluidos]).ToList();
        return ArchivoExcel("Releases", encabezados, filas);
    }

    // ---------- R06 Riesgos ----------
    [HttpGet("riesgos")]
    public async Task<ActionResult<ApiResponse<RiesgosReporteResponse>>> ObtenerRiesgosReporte(
        [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRiesgosReporteQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<RiesgosReporteResponse>.Exito(resultado));
    }

    [HttpGet("riesgos/exportar")]
    public async Task<IActionResult> ExportarRiesgosReporte([FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerRiesgosReporteQuery(idProyecto), cancellationToken);
        var encabezados = new[] { "Proyecto", "Descripcion", "Probabilidad", "Impacto", "Exposicion", "Estatus" };
        var filas = r.Riesgos.Select(x => (IReadOnlyList<object?>)
            [x.Proyecto, x.Descripcion, x.Probabilidad, x.Impacto, x.Exposicion, x.Estatus]).ToList();
        return ArchivoExcel("Riesgos", encabezados, filas);
    }

    // ---------- R07 Solicitantes ----------
    [HttpGet("solicitantes")]
    public async Task<ActionResult<ApiResponse<SolicitantesReporteResponse>>> ObtenerSolicitantesReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerSolicitantesReporteQuery(desde, hasta), cancellationToken);
        return Ok(ApiResponse<SolicitantesReporteResponse>.Exito(resultado));
    }

    [HttpGet("solicitantes/exportar")]
    public async Task<IActionResult> ExportarSolicitantesReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerSolicitantesReporteQuery(desde, hasta), cancellationToken);
        var encabezados = new[] { "Area", "Total solicitudes", "Triage promedio (dias)", "Entrega promedio (dias)" };
        var filas = r.PorArea.Select(x => (IReadOnlyList<object?>)
            [x.Area, x.TotalSolicitudes, x.TiempoTriagePromedioDias, x.TiempoEntregaPromedioDias]).ToList();
        return ArchivoExcel("Solicitantes", encabezados, filas);
    }

    // ---------- R08 Costos ----------
    [HttpGet("costos")]
    public async Task<ActionResult<ApiResponse<CostosReporteResponse>>> ObtenerCostosReporte(
        [FromQuery] int anio, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCostosReporteQuery(anio, idProyecto), cancellationToken);
        return Ok(ApiResponse<CostosReporteResponse>.Exito(resultado));
    }

    [HttpGet("costos/exportar")]
    public async Task<IActionResult> ExportarCostosReporte(
        [FromQuery] int anio, [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerCostosReporteQuery(anio, idProyecto), cancellationToken);
        var encabezados = new[] { "Proyecto", "Mes", "Horas reales", "Costo real" };
        var filas = r.PorProyectoMes.Select(x => (IReadOnlyList<object?>)
            [x.Proyecto, x.Mes, x.HorasReales, x.CostoReal]).ToList();
        return ArchivoExcel("Costos", encabezados, filas);
    }

    // ---------- R09 Rentabilidad ----------
    [HttpGet("rentabilidad")]
    public async Task<ActionResult<ApiResponse<RentabilidadReporteResponse>>> ObtenerRentabilidadReporte(
        [FromQuery] int anio, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRentabilidadReporteQuery(anio), cancellationToken);
        return Ok(ApiResponse<RentabilidadReporteResponse>.Exito(resultado));
    }

    [HttpGet("rentabilidad/exportar")]
    public async Task<IActionResult> ExportarRentabilidadReporte([FromQuery] int anio, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerRentabilidadReporteQuery(anio), cancellationToken);
        var encabezados = new[] { "Proyecto", "Presupuesto autorizado", "Costo real", "% consumido", "Semaforo" };
        var filas = r.Proyectos.Select(x => (IReadOnlyList<object?>)
            [x.Proyecto, x.MontoAutorizado, x.CostoReal, x.PorcentajeConsumido, x.Semaforo]).ToList();
        return ArchivoExcel("Rentabilidad", encabezados, filas);
    }

    // ---------- R10 SLA ----------
    [HttpGet("sla")]
    public async Task<ActionResult<ApiResponse<SlaReporteResponse>>> ObtenerSlaReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerSlaReporteQuery(desde, hasta), cancellationToken);
        return Ok(ApiResponse<SlaReporteResponse>.Exito(resultado));
    }

    [HttpGet("sla/exportar")]
    public async Task<IActionResult> ExportarSlaReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerSlaReporteQuery(desde, hasta), cancellationToken);
        var encabezados = new[] { "Agente", "Total tickets", "Dentro de SLA", "% cumplimiento", "Incumplimientos" };
        var filas = r.PorAgente.Select(x => (IReadOnlyList<object?>)
            [x.Usuario, x.TotalTickets, x.DentroSla, x.CumplimientoPorcentaje, x.Incumplimientos]).ToList();
        return ArchivoExcel("SLA", encabezados, filas);
    }

    // ---------- R11 KPIs / DORA ----------
    [HttpGet("kpis-historicos")]
    public async Task<ActionResult<ApiResponse<KpisHistoricosReporteResponse>>> ObtenerKpisHistoricosReporte(
        [FromQuery] int anio, [FromQuery] int? anioComparativo, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerKpisHistoricosQuery(anio, anioComparativo), cancellationToken);
        return Ok(ApiResponse<KpisHistoricosReporteResponse>.Exito(resultado));
    }

    // ---------- R12 Carga de trabajo ----------
    [HttpGet("carga-trabajo")]
    public async Task<ActionResult<ApiResponse<CargaTrabajoReporteResponse>>> ObtenerCargaTrabajoReporte(
        [FromQuery] int? idEquipo, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCargaTrabajoReporteQuery(idEquipo), cancellationToken);
        return Ok(ApiResponse<CargaTrabajoReporteResponse>.Exito(resultado));
    }

    [HttpGet("carga-trabajo/exportar")]
    public async Task<IActionResult> ExportarCargaTrabajoReporte([FromQuery] int? idEquipo, CancellationToken cancellationToken)
    {
        var r = await mediator.Send(new ObtenerCargaTrabajoReporteQuery(idEquipo), cancellationToken);
        var encabezados = new[] { "Usuario", "WIP", "% ocupacion" };
        var filas = r.Personas.Select(x => (IReadOnlyList<object?>)
            [x.Usuario, x.Wip, x.PorcentajeOcupacion]).ToList();
        return ArchivoExcel("CargaTrabajo", encabezados, filas);
    }

    // ---------- R13 Flujo (CFD) ----------
    [HttpGet("flujo")]
    public async Task<ActionResult<ApiResponse<FlujoReporteResponse>>> ObtenerFlujoReporte(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerFlujoReporteQuery(desde, hasta, idProyecto), cancellationToken);
        return Ok(ApiResponse<FlujoReporteResponse>.Exito(resultado));
    }

    // ---------- R14 Auditoria ----------
    [HttpGet("auditoria")]
    public async Task<ActionResult<ApiResponse<GTE.Application.Common.PagedResult<AuditoriaItemResponse>>>> ObtenerAuditoriaReporte(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] string? usuario, [FromQuery] string? entidad,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(
            new ObtenerAuditoriaReporteQuery(desde, hasta, usuario, entidad, page, pageSize), cancellationToken);
        return Ok(ApiResponse<GTE.Application.Common.PagedResult<AuditoriaItemResponse>>.Exito(resultado));
    }

    private FileContentResult ArchivoExcel(string nombre, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas)
    {
        var contenido = exportador.GenerarLibro(nombre, encabezados, filas);
        return File(contenido, TipoContenidoXlsx, $"{nombre}.xlsx");
    }
}
