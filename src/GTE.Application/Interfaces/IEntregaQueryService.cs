using GTE.Application.DTOs.Responses.Entregas;

namespace GTE.Application.Interfaces;

public interface IEntregaQueryService
{
    /// <summary>
    /// Listado de releases con los filtros de la bandeja (proyecto, estatus y lider
    /// asignado). Un idEstatus explicito manda sobre soloAbiertos.
    /// </summary>
    Task<IReadOnlyList<ReleaseResponse>> ObtenerReleasesAsync(
        int? idProyecto, bool soloAbiertos, int? idEstatus = null, int? idLiderAsignado = null,
        CancellationToken cancellationToken = default);

    Task<ReleaseDetalleResponse?> ObtenerDetalleAsync(int idRelease, CancellationToken cancellationToken = default);

    /// <summary>Releases no cerrados de los proyectos donde el usuario es responsable (para Mi dia).</summary>
    Task<IReadOnlyList<ReleaseResponse>> ObtenerRelevantesAsync(
        int idUsuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elementos que pueden entrar al release: Terminados del proyecto del release y sin
    /// release asignado todavia, ordenados por folio. Deja fuera lo que ya esta en otro
    /// release (o en este) porque un WorkItem pertenece a un solo release a la vez, y trae
    /// el conteo de hallazgos para avisar en la interfaz de lo que RN-GTE-031 va a rechazar.
    /// </summary>
    Task<IReadOnlyList<CandidatoContenidoResponse>> ObtenerCandidatosContenidoAsync(
        int idRelease, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatrizAmbienteResponse>> ObtenerMatrizAmbientesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Notas de version armadas del contenido, agrupadas por tipo de elemento.</summary>
    Task<string> GenerarNotasAsync(int idRelease, CancellationToken cancellationToken = default);

    /// <summary>
    /// Por cada proyecto que el sprint toco: lo Terminado sin release todavia (disponible
    /// o bloqueado por hallazgos pendientes) y el release En Preparacion del proyecto si
    /// ya existe uno. Fuente de verdad para el paso "enviar sprint a release".
    /// </summary>
    Task<CoberturaReleaseSprintResponse> ObtenerCoberturaReleaseSprintAsync(
        int idSprint, CancellationToken cancellationToken = default);
}
