using GTE.Application.DTOs.Responses.Entregas;

namespace GTE.Application.Interfaces;

public interface IEntregaQueryService
{
    Task<IReadOnlyList<ReleaseResponse>> ObtenerReleasesAsync(
        int? idProyecto, bool soloAbiertos, CancellationToken cancellationToken = default);

    Task<ReleaseDetalleResponse?> ObtenerDetalleAsync(int idRelease, CancellationToken cancellationToken = default);

    /// <summary>Releases no cerrados de los proyectos donde el usuario es responsable (para Mi dia).</summary>
    Task<IReadOnlyList<ReleaseResponse>> ObtenerRelevantesAsync(
        int idUsuario, CancellationToken cancellationToken = default);

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
