using GTE.Application.DTOs.Responses.Notificaciones;

namespace GTE.Application.Interfaces;

/// <summary>
/// Empuja eventos en vivo (SignalR). Implementado en GTE.WebApi -- es la unica capa que
/// puede referenciar el Hub sin romper la direccion de dependencias del proyecto.
/// </summary>
public interface INotificadorTiempoReal
{
    Task NotificarUsuarioAsync(
        int idUsuario, NotificacionResponse notificacion, CancellationToken cancellationToken = default);

    Task NotificarWorkItemActualizadoAsync(
        int idWorkItem, int idEstatusNuevo, CancellationToken cancellationToken = default);
}
