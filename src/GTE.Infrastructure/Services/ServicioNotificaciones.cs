using GTE.Application.Interfaces;
using GTE.Domain.Interfaces;
using GTE.Domain.Notificaciones;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Unico canal implementado (InApp): escribe tblNotificacion y empuja en vivo. Ver
/// PENDIENTES.md sobre ICanalNotificacion (reservado para Correo/Teams/WhatsApp).
/// </summary>
public class ServicioNotificaciones(
    INotificacionRepository repositorio,
    INotificacionQueryService consultas,
    INotificadorTiempoReal notificador) : IServicioNotificaciones
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
    }
}
