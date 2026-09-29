using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Reportes;
using GTE.Domain.Reportes;

namespace GTE.Application.Interfaces;

/// <summary>Catalogo de reportes R01-R14 (Doctos/GTE-DocumentoMaestro.md seccion 13).</summary>
public interface IReportesQueryService
{
    /// <summary>Actividad diaria (tblRegistroTiempo) de un usuario en un rango de fechas, con el total de horas reales.</summary>
    Task<ActividadUsuarioResponse> ObtenerActividadUsuarioAsync(
        int idUsuario, DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken = default);

    /// <summary>R01: items terminados, puntos, % a tiempo y eficiencia por persona.</summary>
    Task<ProductividadReporteResponse> ObtenerProductividadAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, int? idEquipo, CancellationToken cancellationToken = default);

    /// <summary>R02: pivote de horas registradas persona x dia, marcando ausencias aprobadas.</summary>
    Task<HorasRegistradasReporteResponse> ObtenerHorasRegistradasAsync(
        DateOnly desde, DateOnly hasta, int? idEquipo, CancellationToken cancellationToken = default);

    /// <summary>R03: % de tiempo invertido en items tipo Correccion, por persona/proyecto.</summary>
    Task<RetrabajoReporteResponse> ObtenerRetrabajoAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R04: densidad, aging y tasa de escape a produccion de bugs por proyecto.</summary>
    Task<BugsDefectosReporteResponse> ObtenerBugsDefectosAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R05: historial de releases, tiempos de aprobacion y frecuencia.</summary>
    Task<ReleasesReporteResponse> ObtenerReleasesAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R06: matriz completa de riesgos (tblRiesgo) por estatus.</summary>
    Task<RiesgosReporteResponse> ObtenerRiesgosAsync(int? idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R07: solicitudes por area con tiempos de triage y entrega.</summary>
    Task<SolicitantesReporteResponse> ObtenerSolicitantesAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);

    /// <summary>R08: costo real (horas x tarifa) por proyecto/mes y por desarrollador.</summary>
    Task<CostosReporteResponse> ObtenerCostosAsync(int anio, int? idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R09: presupuesto autorizado vs costo real acumulado por proyecto.</summary>
    Task<RentabilidadReporteResponse> ObtenerRentabilidadAsync(int anio, CancellationToken cancellationToken = default);

    /// <summary>R10: cumplimiento de SLA por prioridad y por agente, con CSAT.</summary>
    Task<SlaReporteResponse> ObtenerSlaAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);

    /// <summary>R11: series historicas de tblKpiValor con comparativo entre dos anios.</summary>
    Task<KpisHistoricosReporteResponse> ObtenerKpisHistoricosAsync(
        int anio, int? anioComparativo, CancellationToken cancellationToken = default);

    /// <summary>R12: WIP por persona y % de ocupacion vs capacidad de sprint activo.</summary>
    Task<CargaTrabajoReporteResponse> ObtenerCargaTrabajoAsync(int? idEquipo, CancellationToken cancellationToken = default);

    /// <summary>R13: diagrama de flujo acumulado (CFD) de un proyecto, dia por dia.</summary>
    Task<FlujoReporteResponse> ObtenerFlujoAsync(
        DateOnly desde, DateOnly hasta, int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>R14: movimientos de bitacora (tblBitacora) por usuario/entidad/rango, paginado.</summary>
    Task<PagedResult<AuditoriaItemResponse>> ObtenerAuditoriaAsync(
        DateOnly? desde, DateOnly? hasta, string? usuario, string? entidad,
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// R15: detalle renglon por renglon de los work items Terminados en el rango (por FechaFin),
    /// con tiempo invertido, tiempo de espera y tiempo de resolucion en dias naturales y en
    /// tiempo habil del horario del asignado.
    /// </summary>
    Task<ActividadesTerminadasReporteResponse> ObtenerActividadesTerminadasAsync(
        DateOnly desde, DateOnly hasta, int? idEquipo, int? idAsignado, int? idProyecto,
        int? idTipoWorkItem, string? folio, CancellationToken cancellationToken = default);

    /// <summary>
    /// R16: actividades (work items ya iniciados) que se traslapan con el periodo, en forma de
    /// barras para un diagrama de Gantt. Los tres filtros son combinables y ortogonales entre si;
    /// la agrupacion solo cambia el orden de los renglones. Pagina porque un proyecto con miles de
    /// actividades no se puede dibujar completo de un golpe.
    /// </summary>
    Task<GanttActividadesReporteResponse> ObtenerGanttActividadesAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, int? idAsignado,
        AgrupacionGantt agruparPor, int page, int pageSize, CancellationToken cancellationToken = default);
}
