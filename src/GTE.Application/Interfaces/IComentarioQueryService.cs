using GTE.Application.DTOs.Responses.Comentarios;

namespace GTE.Application.Interfaces;

public interface IComentarioQueryService
{
    /// <summary>Orden cronologico ascendente; el front arma el hilo con IdComentarioPadre.</summary>
    Task<IReadOnlyList<ComentarioResponse>> ObtenerPorEntidadAsync(
        string entidad, int idEntidad, CancellationToken cancellationToken = default);

    Task<ComentarioResponse?> ObtenerPorIdAsync(int idComentario, CancellationToken cancellationToken = default);
}
