using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Solicitudes;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class SolicitudRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ISolicitudRepository
{
    public async Task<int> CrearAsync(SolicitudNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblSolicitud
        {
            Folio = datos.Folio,
            IdSolicitante = datos.IdSolicitante,
            Titulo = datos.Titulo,
            Descripcion = datos.Descripcion,
            IdTipoSolicitud = datos.IdTipoSolicitud,
            IdPrioridad = datos.IdPrioridad,
            IdEstatusSolicitud = EstatusSolicitud.Borrador,   // el estatus inicial lo fija el backend
            FechaDeseada = datos.FechaDeseada.HasValue ? DateOnly.FromDateTime(datos.FechaDeseada.Value) : null,
            JustificacionNegocio = datos.JustificacionNegocio,
            IdUsuarioSolicitante = datos.IdUsuarioSolicitante,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblSolicitud.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Solicitud",
            IdRegistro = entidad.IdSolicitud,
            IdEstatus = EstatusSolicitud.Borrador,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Solicitud", entidad.IdSolicitud, "CREAR", datos.Folio, cancellationToken);
        return entidad.IdSolicitud;
    }

    public async Task<EstadoSolicitud?> ObtenerEstadoAsync(int idSolicitud, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSolicitud.AsNoTracking()
            .Where(s => s.IdSolicitud == idSolicitud)
            .Select(s => new EstadoSolicitud(
                s.IdSolicitud, s.Folio, s.IdEstatusSolicitud, s.IdProyecto,
                s.IdSolicitante, s.Titulo, s.Activo, s.IdUsuarioSolicitante))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AsignarProyectoAsync(int idSolicitud, int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var proyectoValido = await contexto.TblProyecto.AsNoTracking()
            .AnyAsync(p => p.IdProyecto == idProyecto && p.Activo, cancellationToken);
        if (!proyectoValido)
        {
            throw new InvalidOperationException($"El proyecto {idProyecto} no existe o esta inactivo.");
        }

        var entidad = await contexto.TblSolicitud
            .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud, cancellationToken)
            ?? throw new InvalidOperationException($"Solicitud {idSolicitud} no existe.");
        entidad.IdProyecto = idProyecto;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(int idSolicitud, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblSolicitud
            .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud, cancellationToken)
            ?? throw new InvalidOperationException($"Solicitud {idSolicitud} no existe.");
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Solicitud", idSolicitud, accion, null, cancellationToken);
    }

    private void MarcarMovimiento(TblSolicitud entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
