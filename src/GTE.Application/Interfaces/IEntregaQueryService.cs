using GTE.Application.DTOs.Responses.Entregas;

namespace GTE.Application.Interfaces;

public interface IEntregaQueryService
{
    Task<IReadOnlyList<ReleaseResponse>> ObtenerReleasesAsync(
        int? idProyecto, bool soloAbiertos, CancellationToken cancellationToken = default);

    Task<ReleaseDetalleResponse?> ObtenerDetalleAsync(int idRelease, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatrizAmbienteResponse>> ObtenerMatrizAmbientesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Notas de version armadas del contenido, agrupadas por tipo de elemento.</summary>
    Task<string> GenerarNotasAsync(int idRelease, CancellationToken cancellationToken = default);
}
