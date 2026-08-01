using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Notificaciones;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

/// <summary>tblNotificacion no tiene columnas de auditoria (UsuarioRegistro/Activo): su ciclo de vida es solo Leida/FechaLeida.</summary>
public class NotificacionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), INotificacionRepository
{
    public async Task<long> CrearAsync(NotificacionNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblNotificacion
        {
            IdUsuario = datos.IdUsuario,
            Titulo = datos.Titulo,
            Mensaje = datos.Mensaje,
            Entidad = datos.Entidad,
            IdEntidad = datos.IdEntidad,
            Url = datos.Url,
            Leida = false
        };
        contexto.TblNotificacion.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        return entidad.IdNotificacion;
    }

    public async Task MarcarLeidaAsync(
        long idNotificacion, int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblNotificacion.FirstOrDefaultAsync(
            n => n.IdNotificacion == idNotificacion && n.IdUsuario == idUsuario, cancellationToken);

        if (entidad is null || entidad.Leida)
        {
            return;
        }

        entidad.Leida = true;
        entidad.FechaLeida = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task MarcarTodasLeidasAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var pendientes = await contexto.TblNotificacion
            .Where(n => n.IdUsuario == idUsuario && !n.Leida)
            .ToListAsync(cancellationToken);

        var ahora = DateTime.Now;
        foreach (var notificacion in pendientes)
        {
            notificacion.Leida = true;
            notificacion.FechaLeida = ahora;
        }
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
