using System.Net;
using System.Net.Mail;
using GTE.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GTE.Infrastructure.Services;

/// <summary>Configuracion del canal Correo (seccion "Smtp" de appsettings). Sin credenciales
/// en este entorno: Habilitado queda en false por default y el canal no envia nada.</summary>
public class OpcionesSmtp
{
    public bool Habilitado { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool Ssl { get; set; } = true;
    public string? Usuario { get; set; }
    public string? Password { get; set; }
    public string Remitente { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = "GTE";
}

/// <summary>
/// Canal Correo de ICanalNotificacion via SMTP (System.Net.Mail: alcanza para correo
/// transaccional basico, sin agregar una dependencia nueva al proyecto). Sin
/// Smtp:Habilitado / Smtp:Host configurados se queda callado -- degradado silencioso,
/// no hay credenciales SMTP en este entorno (ver PENDIENTES.md). Teams/WhatsApp siguen
/// sin implementacion (ver ICanalNotificacion).
/// </summary>
public class CanalCorreoSmtp(IOptions<OpcionesSmtp> opciones, ILogger<CanalCorreoSmtp> logger) : ICanalNotificacion
{
    public string NombreCanal => "Correo";

    public async Task EnviarAsync(MensajeNotificacion mensaje, CancellationToken cancellationToken = default)
    {
        var config = opciones.Value;
        if (!config.Habilitado || string.IsNullOrWhiteSpace(config.Host))
        {
            return;
        }

        var destinatarios = mensaje.Destinatarios.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        if (destinatarios.Count == 0)
        {
            return;
        }

        using var correo = new MailMessage
        {
            From = new MailAddress(config.Remitente, config.RemitenteNombre),
            Subject = mensaje.Titulo,
            Body = mensaje.Url is null ? mensaje.Contenido : $"{mensaje.Contenido}\n\n{mensaje.Url}",
            IsBodyHtml = false
        };
        foreach (var destinatario in destinatarios)
        {
            correo.To.Add(destinatario);
        }

        using var cliente = new SmtpClient(config.Host, config.Puerto) { EnableSsl = config.Ssl };
        if (!string.IsNullOrWhiteSpace(config.Usuario))
        {
            cliente.Credentials = new NetworkCredential(config.Usuario, config.Password);
        }

        try
        {
            await cliente.SendMailAsync(correo, cancellationToken);
        }
        catch (Exception ex)
        {
            // Un correo caido no debe tumbar el flujo de negocio que disparo la notificacion:
            // la notificacion InApp ya se escribio antes de llegar aqui.
            logger.LogWarning(ex, "No se pudo enviar el correo de notificacion a {Destinatarios}",
                string.Join(", ", destinatarios));
        }
    }
}
