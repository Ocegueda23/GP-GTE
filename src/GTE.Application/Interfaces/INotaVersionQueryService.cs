using GTE.Application.DTOs.Responses.NotasVersion;

namespace GTE.Application.Interfaces;

public interface INotaVersionQueryService
{
    /// <summary>
    /// Historial que ve el usuario final: solo notas publicadas y activas, de la mas reciente a
    /// la mas vieja, con su arbol de renglones.
    /// </summary>
    Task<IReadOnlyList<NotaVersionResponse>> ObtenerPublicadasAsync(CancellationToken cancellationToken = default);

    /// <summary>Listado de administracion: incluye borradores.</summary>
    Task<IReadOnlyList<NotaVersionListaResponse>> ObtenerTodasAsync(CancellationToken cancellationToken = default);

    Task<NotaVersionResponse?> ObtenerPorIdAsync(int idNotaVersion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoCambioVersionResponse>> ObtenerTiposCambioAsync(CancellationToken cancellationToken = default);
}
