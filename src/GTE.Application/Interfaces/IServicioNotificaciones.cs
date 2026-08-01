namespace GTE.Application.Interfaces;

/// <summary>
/// Orquesta el alta de notificaciones In-App: escribe tblNotificacion y empuja en vivo
/// por INotificadorTiempoReal. Unico canal implementado hoy (ver PENDIENTES.md sobre
/// ICanalNotificacion, reservado para Correo/Teams/WhatsApp cuando existan).
/// </summary>
public interface IServicioNotificaciones
{
    Task NotificarAsync(
        IReadOnlyList<int> idsUsuarios,
        string titulo,
        string? mensaje,
        string? entidad,
        int? idEntidad,
        string? url,
        CancellationToken cancellationToken = default);
}
