using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Revisiones;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class RevisionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IRevisionRepository
{
    public async Task<int> CrearAsync(RevisionNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblRevision
        {
            IdWorkItem = datos.IdWorkItem,
            IdRevisor = datos.IdRevisor,
            Comentarios = datos.Comentarios,
            IdEstatusRevision = EstatusRevision.Pendiente,   // el estatus inicial lo fija el backend
            Corregido = false,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblRevision.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Revision",
            IdRegistro = entidad.IdRevision,
            IdEstatus = EstatusRevision.Pendiente,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Revision", entidad.IdRevision, "CREAR",
            $"WorkItem {datos.IdWorkItem}", cancellationToken);
        return entidad.IdRevision;
    }

    public async Task<EstadoRevision?> ObtenerEstadoAsync(
        int idRevision, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRevision.AsNoTracking()
            .Where(r => r.IdRevision == idRevision)
            .Select(r => new EstadoRevision(
                r.IdRevision, r.IdWorkItem, r.IdEstatusRevision, r.Corregido, r.IdRevisor, r.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task EstablecerCorregidoAsync(
        int idRevision, bool corregido, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = await contexto.TblRevision
            .FirstOrDefaultAsync(r => r.IdRevision == idRevision, cancellationToken)
            ?? throw new InvalidOperationException($"Revision {idRevision} no existe.");

        entidad.Corregido = corregido;
        entidad.FechaCorreccion = corregido ? DateTime.Now : null;
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;

        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Revision", idRevision,
            corregido ? "CORREGIR" : "REABRIR", null, cancellationToken);
    }

    public async Task<int> ContarPendientesAsync(int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRevision.AsNoTracking()
            .CountAsync(r => r.IdWorkItem == idWorkItem && !r.Corregido && r.Activo, cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(
        int idRevision, string accion, CancellationToken cancellationToken = default)
    {
        await RegistrarBitacoraAsync("Revision", idRevision, accion, null, cancellationToken);
    }
}
