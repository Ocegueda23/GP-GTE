namespace GTE.Application.Interfaces;

/// <summary>Mensaje de notificacion agnostico del canal.</summary>
public record MensajeNotificacion(
    string Titulo,
    string Contenido,
    string? Url,
    IReadOnlyList<string> Destinatarios);

/// <summary>
/// Canal de salida de notificaciones. Implementaciones: InApp (SignalR),
/// Correo (Graph/SMTP), Teams, WhatsApp, Slack.
/// </summary>
public interface ICanalNotificacion
{
    string NombreCanal { get; }

    Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancellationToken = default);
}
