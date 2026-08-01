using GTE.Application.DTOs.Responses.Notificaciones;

namespace GTE.Application.Interfaces;

public interface INotificacionQueryService
{
    Task<IReadOnlyList<NotificacionResponse>> ObtenerPorUsuarioAsync(
        int idUsuario, bool soloNoLeidas, CancellationToken cancellationToken = default);

    Task<NotificacionResponse?> ObtenerPorIdAsync(long idNotificacion, CancellationToken cancellationToken = default);
}
