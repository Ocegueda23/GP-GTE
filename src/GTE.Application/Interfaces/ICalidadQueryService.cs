using GTE.Application.DTOs.Responses.Calidad;

namespace GTE.Application.Interfaces;

public interface ICalidadQueryService
{
    Task<IReadOnlyList<PlanPruebaResponse>> ObtenerPlanesAsync(
        int? idProyecto, CancellationToken cancellationToken = default);

    Task<PlanPruebaResponse?> ObtenerPlanAsync(int idPlanPrueba, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CicloPruebaResponse>> ObtenerCiclosAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default);

    /// <summary>Casos del plan con el resultado de su ultima ejecucion en el ciclo indicado.</summary>
    Task<IReadOnlyList<CasoPruebaResponse>> ObtenerCasosAsync(
        int idPlanPrueba, int? idCicloPrueba, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrazabilidadResponse>> ObtenerTrazabilidadAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default);
}
