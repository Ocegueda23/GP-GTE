using GTE.Application.Interfaces;
using GTE.Domain.Interfaces;
using GTE.Domain.Notificaciones;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Canal InApp (siempre activo: escribe tblNotificacion y empuja en vivo) mas los canales
/// externos registrados por DI (hoy solo Correo, ver CanalCorreoSmtp; Teams/WhatsApp siguen
/// sin implementacion, ver ICanalNotificacion). Sin preferencias de canal/evento por
/// usuario (fuera de alcance, ver PENDIENTES.md): todo destinatario con correo capturado
/// recibe tambien el correo si el canal esta habilitado.
/// </summary>
public class ServicioNotificaciones(
    INotificacionRepository repositorio,
    INotificacionQueryService consultas,
    INotificadorTiempoReal notificador,
    IEnumerable<ICanalNotificacion> canales,
    FabricaContexto fabrica) : IServicioNotificaciones
{
    public async Task NotificarAsync(
        IReadOnlyList<int> idsUsuarios,
        string titulo,
        string? mensaje,
        string? entidad,
        int? idEntidad,
        string? url,
        CancellationToken cancellationToken = default)
    {
        foreach (var idUsuario in idsUsuarios)
        {
            var idNotificacion = await repositorio.CrearAsync(
                new NotificacionNueva(idUsuario, titulo, mensaje, entidad, idEntidad, url), cancellationToken);

            var dto = await consultas.ObtenerPorIdAsync(idNotificacion, cancellationToken);
            if (dto is not null)
            {
                await notificador.NotificarUsuarioAsync(idUsuario, dto, cancellationToken);
            }
        }

        var canalCorreo = canales.FirstOrDefault(c => c.NombreCanal == "Correo");
        if (canalCorreo is not null)
        {
            await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
            var correos = await contexto.TblUsuario.AsNoTracking()
                .Where(u => idsUsuarios.Contains(u.IdUsuario) && u.Correo != null && u.Correo != "")
                .Select(u => u.Correo!)
                .ToListAsync(cancellationToken);

            if (correos.Count > 0)
            {
                await canalCorreo.EnviarAsync(
                    new MensajeNotificacion(titulo, mensaje ?? string.Empty, url, correos), cancellationToken);
            }
        }
    }
}
