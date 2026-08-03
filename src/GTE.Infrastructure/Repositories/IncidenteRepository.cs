using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class IncidenteRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IIncidenteRepository
{
    public async Task<int> CrearAsync(IncidenteNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblIncidente
        {
            Folio = datos.Folio,
            IdProyecto = datos.IdProyecto,
            IdSeveridad = datos.IdSeveridad,
            IdEstatusIncidente = EstatusIncidente.Detectado,   // el estatus inicial lo fija el backend
            Titulo = datos.Titulo,
            Descripcion = datos.Descripcion,
            FechaOcurrencia = datos.FechaOcurrencia,
            FechaDeteccion = datos.FechaDeteccion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblIncidente.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Incidente",
            IdRegistro = entidad.IdIncidente,
            IdEstatus = EstatusIncidente.Detectado,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Incidente", entidad.IdIncidente, "CREAR", datos.Folio, cancellationToken);
        return entidad.IdIncidente;
    }

    public async Task<EstadoIncidente?> ObtenerEstadoAsync(int idIncidente, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblIncidente.AsNoTracking()
            .Where(i => i.IdIncidente == idIncidente)
            .Select(i => new EstadoIncidente(
                i.IdIncidente, i.Folio, i.IdProyecto, i.IdEstatusIncidente, i.IdSeveridad,
                i.Titulo, i.Descripcion, i.CausaRaiz, i.IdWorkItemCorrectivo, i.IdReleaseCausante, i.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerResponsableProyectoAsync(int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => p.IdResponsable)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExisteReleaseEnProyectoAsync(int idRelease, int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRelease.AsNoTracking()
            .AnyAsync(r => r.IdRelease == idRelease && r.IdProyecto == idProyecto, cancellationToken);
    }

    public async Task ActualizarAsync(int idIncidente, IncidenteActualizacion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblIncidente
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken)
            ?? throw new InvalidOperationException($"Incidente {idIncidente} no existe.");

        entidad.Titulo = datos.Titulo;
        entidad.Descripcion = datos.Descripcion;
        entidad.CausaRaiz = datos.CausaRaiz;
        entidad.MinutosIndisponibilidad = datos.MinutosIndisponibilidad;
        entidad.FechaDeteccion = datos.FechaDeteccion;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task CambiarSeveridadAsync(int idIncidente, int idSeveridad, string motivo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblIncidente
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken)
            ?? throw new InvalidOperationException($"Incidente {idIncidente} no existe.");
        entidad.IdSeveridad = idSeveridad;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Incidente", idIncidente, "CAMBIAR_SEVERIDAD", motivo, cancellationToken);
    }

    public async Task VincularCorrectivoAsync(int idIncidente, int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblIncidente
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken)
            ?? throw new InvalidOperationException($"Incidente {idIncidente} no existe.");
        entidad.IdWorkItemCorrectivo = idWorkItem;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Incidente", idIncidente, "VINCULAR_CORRECTIVO", null, cancellationToken);
    }

    public async Task VincularReleaseCausanteAsync(int idIncidente, int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblIncidente
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken)
            ?? throw new InvalidOperationException($"Incidente {idIncidente} no existe.");
        entidad.IdReleaseCausante = idRelease;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Incidente", idIncidente, "VINCULAR_RELEASE_CAUSANTE", null, cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(int idIncidente, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblIncidente
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken)
            ?? throw new InvalidOperationException($"Incidente {idIncidente} no existe.");

        if (accion == AccionesIncidente.Resolver)
        {
            entidad.FechaResolucion = DateTime.Now;
        }

        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Incidente", idIncidente, accion, null, cancellationToken);
    }

    private void MarcarMovimiento(TblIncidente entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
