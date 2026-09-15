using GTE.Domain.Ausencias;

namespace GTE.Domain.Interfaces;

public interface IAusenciaRepository
{
    Task<int> CrearAsync(AusenciaNueva datos, CancellationToken cancellationToken = default);

    Task ActualizarAsync(AusenciaEdicion datos, CancellationToken cancellationToken = default);

    Task<EstadoAusencia?> ObtenerEstadoAsync(int idAusencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ausencias vigentes (Solicitada/Aprobada) de la persona que se cruzan con el periodo.
    /// <paramref name="idAusenciaExcluir"/> deja fuera la que se esta editando.
    /// </summary>
    Task<IReadOnlyList<TraslapeAusencia>> ObtenerTraslapesAsync(
        int idUsuario,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        int? idAusenciaExcluir = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sella auditoria de movimiento y bitacora despues de una transicion del motor.</summary>
    Task AplicarEfectosTransicionAsync(
        int idAusencia, string accion, CancellationToken cancellationToken = default);

    /// <summary>Usuarios activos que pueden aprobar ausencias (para la notificacion del alta).</summary>
    Task<IReadOnlyList<int>> ObtenerAprobadoresAsync(CancellationToken cancellationToken = default);
}
