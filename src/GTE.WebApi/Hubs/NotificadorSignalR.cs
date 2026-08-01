using GTE.Application.DTOs.Responses.Notificaciones;
using GTE.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GTE.WebApi.Hubs;

/// <summary>
/// Implementa INotificadorTiempoReal con el Hub de SignalR. Vive en WebApi porque es la
/// unica capa que puede referenciar IHubContext sin que Infrastructure dependa de hosting.
/// </summary>
public class NotificadorSignalR(IHubContext<NotificacionesHub> hub) : INotificadorTiempoReal
{
    public async Task NotificarUsuarioAsync(
        int idUsuario, NotificacionResponse notificacion, CancellationToken cancellationToken = default)
    {
        await hub.Clients.User(idUsuario.ToString())
            .SendAsync("notificacion", notificacion, cancellationToken);
    }

    public async Task NotificarWorkItemActualizadoAsync(
        int idWorkItem, int idEstatusNuevo, CancellationToken cancellationToken = default)
    {
        await hub.Clients.All.SendAsync(
            "workItemActualizado", new { idWorkItem, idEstatusNuevo }, cancellationToken);
    }
}
