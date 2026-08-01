using GTE.Domain.Notificaciones;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Notificaciones.</summary>
public interface INotificacionRepository
{
    Task<long> CrearAsync(NotificacionNueva datos, CancellationToken cancellationToken = default);

    /// <summary>Silencioso si la notificacion no existe o no pertenece al usuario.</summary>
    Task MarcarLeidaAsync(long idNotificacion, int idUsuario, CancellationToken cancellationToken = default);

    Task MarcarTodasLeidasAsync(int idUsuario, CancellationToken cancellationToken = default);
}
