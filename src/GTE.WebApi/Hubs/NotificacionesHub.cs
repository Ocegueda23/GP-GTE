using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GTE.WebApi.Hubs;

/// <summary>
/// Empuja "notificacion" (por usuario, via Clients.User) y "workItemActualizado" (broadcast,
/// para refrescar tableros abiertos). Los clientes solo reciben, no invocan metodos del hub.
/// </summary>
[Authorize]
public class NotificacionesHub : Hub;
