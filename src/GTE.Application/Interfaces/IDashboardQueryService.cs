using GTE.Application.DTOs.Responses.Dashboard;

namespace GTE.Application.Interfaces;

/// <summary>
/// Consultas del Dashboard Ejecutivo de Metricas. El "alcance" (que usuarios entran en el
/// resumen/carga de trabajo/rankings) lo resuelve cada metodo combinando los flags de
/// permiso ya evaluados por el Handler (Application) con la jerarquia real en BD
/// (tblEquipo.IdLider / tblUsuario.IdJefe) -- ver comentario de <see cref="ObtenerDashboardAsync"/>.
/// </summary>
public interface IDashboardQueryService
{
    /// <summary>
    /// Payload agregado principal (empleado del mes, resumen ejecutivo, carga de trabajo,
    /// rankings, comparativos) para el periodo y filtros dados.
    /// </summary>
    /// <param name="idUsuarioActual">Usuario que consulta (siempre ve, como minimo, su propia informacion).</param>
    /// <param name="tieneAlcanceGlobal">true si tiene DASH.Ejecutivo (ve todos los usuarios activos).</param>
    /// <param name="tieneAlcanceDepartamento">true si tiene DASH.VerDepartamento (ve su area/departamento).</param>
    Task<DashboardResponse> ObtenerDashboardAsync(
        int idUsuarioActual,
        bool tieneAlcanceGlobal,
        bool tieneAlcanceDepartamento,
        int anio,
        int mes,
        int? idProyecto,
        int? idArea,
        int? idUsuarioFoco,
        CancellationToken cancellationToken = default);

    Task<IndicadoresEmpleadoResponse?> ObtenerIndicadoresEmpleadoAsync(
        int idUsuarioActual,
        bool tieneAlcanceGlobal,
        bool tieneAlcanceDepartamento,
        int idUsuarioConsultado,
        int anio,
        int mes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TendenciaResponse>> ObtenerTendenciasAsync(
        int idUsuarioActual,
        bool tieneAlcanceGlobal,
        bool tieneAlcanceDepartamento,
        int anio,
        int? idProyecto,
        int? idArea,
        CancellationToken cancellationToken = default);

    Task<FiltroCatalogosDashboardResponse> ObtenerFiltrosAsync(
        int idUsuarioActual,
        bool tieneAlcanceGlobal,
        bool tieneAlcanceDepartamento,
        CancellationToken cancellationToken = default);
}
