using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class RevisionQueryService(FabricaContexto fabrica) : IRevisionQueryService
{
    public async Task<IReadOnlyList<RevisionResponse>> ObtenerPorWorkItemAsync(
        int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .Where(r => r.IdWorkItem == idWorkItem)
            .OrderBy(r => r.Corregido)
            .ThenByDescending(r => r.IdRevision)
            .ToListAsync(cancellationToken);
    }

    public async Task<RevisionResponse?> ObtenerPorIdAsync(
        int idRevision, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(r => r.IdRevision == idRevision, cancellationToken);
    }

    private static IQueryable<RevisionResponse> Proyectar(DbContextGTE contexto)
    {
        return from r in contexto.TblRevision.AsNoTracking()
               join w in contexto.TblWorkItem.AsNoTracking() on r.IdWorkItem equals w.IdWorkItem
               join e in contexto.TblEstatusRevision.AsNoTracking() on r.IdEstatusRevision equals e.Id
               join u in contexto.TblUsuario.AsNoTracking() on r.IdRevisor equals u.IdUsuario
               where r.Activo
               select new RevisionResponse
               {
                   IdRevision = r.IdRevision,
                   IdWorkItem = r.IdWorkItem,
                   FolioWorkItem = w.Folio,
                   Revisor = u.Nombre,
                   Comentarios = r.Comentarios,
                   IdEstatus = r.IdEstatusRevision,
                   Estatus = e.Descripcion,
                   Corregido = r.Corregido,
                   FechaCorreccion = r.FechaCorreccion,
                   FechaRegistro = r.FechaRegistro
               };
    }
}
