using GTE.Application.DTOs.Responses.Planeacion;

namespace GTE.Application.Interfaces;

public interface IPlaneacionQueryService
{
    Task<IReadOnlyList<SprintResponse>> ObtenerSprintsAsync(
        int? idEquipo, bool soloAbiertos, CancellationToken cancellationToken = default);

    Task<SprintResponse?> ObtenerSprintAsync(int idSprint, CancellationToken cancellationToken = default);

    /// <summary>Backlog del proyecto: elementos abiertos sin sprint, en orden de prioridad manual.</summary>
    Task<BacklogResponse> ObtenerBacklogAsync(int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Backlog de todos los proyectos a la vez, para consulta/busqueda (sin orden manual, es de solo lectura).</summary>
    Task<BacklogResponse> ObtenerBacklogGlobalAsync(string? texto, CancellationToken cancellationToken = default);

    Task<BacklogResponse> ObtenerItemsDeSprintAsync(int idSprint, CancellationToken cancellationToken = default);

    /// <summary>idEquipo null = vista consolidada de todos los equipos.</summary>
    Task<TableroResponse> ObtenerTableroAsync(int? idEquipo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PuntoBurndownResponse>> ObtenerBurndownAsync(
        int idSprint, CancellationToken cancellationToken = default);
}
