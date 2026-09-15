using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using GTE.Domain.Calidad;
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
               join s in contexto.TblSeveridad.AsNoTracking() on r.IdSeveridad equals s.Id into severidades
               from s in severidades.DefaultIfEmpty()
               join ej in contexto.TblEjecucionPrueba.AsNoTracking() on r.IdEjecucionPrueba equals ej.IdEjecucionPrueba into ejecuciones
               from ej in ejecuciones.DefaultIfEmpty()
               join cp in contexto.TblCasoPrueba.AsNoTracking() on ej.IdCasoPrueba equals cp.IdCasoPrueba into casos
               from cp in casos.DefaultIfEmpty()
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
                   EsFalsoPositivo = r.EsFalsoPositivo,
                   MotivoDescarte = r.MotivoDescarte,
                   FechaCorreccion = r.FechaCorreccion,
                   FechaRegistro = r.FechaRegistro,
                   IdSeveridad = r.IdSeveridad,
                   Severidad = s != null ? s.Nombre : null,
                   Bloqueante = r.IdSeveridad != null && r.IdSeveridad <= Severidad.S2Alta,
                   IdEjecucionPrueba = r.IdEjecucionPrueba,
                   CasoPrueba = cp != null ? cp.Titulo : null
               };
    }
}
