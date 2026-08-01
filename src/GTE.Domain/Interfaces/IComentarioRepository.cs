using GTE.Domain.Comentarios;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Comentarios.</summary>
public interface IComentarioRepository
{
    Task<int> CrearAsync(ComentarioNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoComentario?> ObtenerEstadoAsync(int idComentario, CancellationToken cancellationToken = default);

    /// <summary>Baja logica. No valida autoria: eso lo decide el handler de Application.</summary>
    Task EliminarAsync(int idComentario, CancellationToken cancellationToken = default);
}
