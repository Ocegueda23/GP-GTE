using GTE.Application.DTOs.Responses.Notificaciones;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class NotificacionQueryService(FabricaContexto fabrica) : INotificacionQueryService
{
    public async Task<IReadOnlyList<NotificacionResponse>> ObtenerPorUsuarioAsync(
        int idUsuario, bool soloNoLeidas, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var consulta = contexto.TblNotificacion.AsNoTracking().Where(n => n.IdUsuario == idUsuario);
        if (soloNoLeidas)
        {
            consulta = consulta.Where(n => !n.Leida);
        }

        return await consulta
            .OrderByDescending(n => n.FechaRegistro)
            .Select(n => new NotificacionResponse
            {
                IdNotificacion = n.IdNotificacion,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Entidad = n.Entidad,
                IdEntidad = n.IdEntidad,
                Url = n.Url,
                Leida = n.Leida,
                FechaLeida = n.FechaLeida,
                FechaRegistro = n.FechaRegistro
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificacionResponse?> ObtenerPorIdAsync(
        long idNotificacion, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblNotificacion.AsNoTracking()
            .Where(n => n.IdNotificacion == idNotificacion)
            .Select(n => new NotificacionResponse
            {
                IdNotificacion = n.IdNotificacion,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Entidad = n.Entidad,
                IdEntidad = n.IdEntidad,
                Url = n.Url,
                Leida = n.Leida,
                FechaLeida = n.FechaLeida,
                FechaRegistro = n.FechaRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
