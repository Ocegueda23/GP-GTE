using GTE.Application.Common;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>Lectura del modulo WorkItems: proyecciones directas a DTOs, sin tracking.</summary>
public class WorkItemQueryService(FabricaContexto fabrica) : IWorkItemQueryService
{
    private static readonly int[] EstatusAbiertos =
    [
        EstatusWorkItem.Pendiente, EstatusWorkItem.EnProceso, EstatusWorkItem.EnPruebas,
        EstatusWorkItem.Correccion, EstatusWorkItem.Suspendido
    ];

    public async Task<PagedResult<BandejaItemResponse>> ObtenerBandejaAsync(
        FiltroBandeja filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta =
            from v in contexto.VwBandejaTrabajo.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on v.IdWorkItem equals w.IdWorkItem
            select new { v, w };

        // Semantica heredada del GT: sin filtro = abiertos; [-1] = todos
        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(x => EstatusAbiertos.Contains(x.v.IdEstatusWorkItem));
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(x => estatus.Contains(x.v.IdEstatusWorkItem));
        }

        if (filtro.IdProyecto.HasValue)
        {
            consulta = consulta.Where(x => x.w.IdProyecto == filtro.IdProyecto.Value);
        }
        if (filtro.IdAsignado.HasValue)
        {
            consulta = consulta.Where(x => x.v.IdAsignado == filtro.IdAsignado.Value);
        }
        if (filtro.IdTipoWorkItem.HasValue)
        {
            consulta = consulta.Where(x => x.w.IdTipoWorkItem == filtro.IdTipoWorkItem.Value);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(x =>
                x.v.Folio.Contains(texto) || x.v.Titulo.Contains(texto) || x.v.Proyecto.Contains(texto));
        }
        if (filtro.SoloVencidas)
        {
            consulta = consulta.Where(x => x.v.EsVencida == true);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var items = await consulta
            .OrderBy(x => x.v.FechaCompromiso == null)
            .ThenBy(x => x.v.FechaCompromiso)
            .ThenByDescending(x => x.v.IdWorkItem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BandejaItemResponse
            {
                IdWorkItem = x.v.IdWorkItem,
                Folio = x.v.Folio,
                Tipo = x.v.Tipo,
                Titulo = x.v.Titulo,
                ClaveProyecto = x.v.ClaveProyecto,
                Proyecto = x.v.Proyecto,
                IdEstatus = x.v.IdEstatusWorkItem,
                Estatus = x.v.Estatus,
                Prioridad = x.v.Prioridad,
                Asignado = x.v.Asignado,
                FechaCompromiso = x.v.FechaCompromiso,
                EsVencida = x.v.EsVencida == true,
                PuntosHistoria = x.v.PuntosHistoria,
                MinutosPresupuesto = x.v.MinutosPresupuesto,
                MinutosInvertidos = x.v.MinutosInvertidos,
                RevisionesPendientes = x.v.RevisionesPendientes ?? 0
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BandejaItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<WorkItemResponse?> ObtenerPorIdAsync(int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await ProyectarDetalle(contexto).FirstOrDefaultAsync(
            x => x.IdWorkItem == idWorkItem, cancellationToken);
    }

    public async Task<WorkItemResponse?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await ProyectarDetalle(contexto).FirstOrDefaultAsync(
            x => x.Folio == folio, cancellationToken);
    }

    public async Task<IReadOnlyList<RegistroTiempoResponse>> ObtenerTiemposAsync(
        int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from t in contexto.TblRegistroTiempo.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on t.IdUsuario equals u.IdUsuario
            where t.IdWorkItem == idWorkItem && t.Activo
            orderby t.Fecha descending, t.IdRegistroTiempo descending
            select new RegistroTiempoResponse
            {
                IdRegistroTiempo = t.IdRegistroTiempo,
                Fecha = t.Fecha,
                Minutos = t.Minutos,
                Descripcion = t.Descripcion,
                Usuario = u.Nombre,
                FechaRegistro = t.FechaRegistro
            }).ToListAsync(cancellationToken);
    }

    private static IQueryable<WorkItemResponse> ProyectarDetalle(DbContextGTE contexto)
    {
        return from v in contexto.VwBandejaTrabajo.AsNoTracking()
               join w in contexto.TblWorkItem.AsNoTracking() on v.IdWorkItem equals w.IdWorkItem
               select new WorkItemResponse
               {
                   IdWorkItem = v.IdWorkItem,
                   Folio = v.Folio,
                   Tipo = v.Tipo,
                   Titulo = v.Titulo,
                   Descripcion = w.Descripcion,
                   CriteriosAceptacion = w.CriteriosAceptacion,
                   ClaveProyecto = v.ClaveProyecto,
                   Proyecto = v.Proyecto,
                   EsMantenimiento = v.EsMantenimiento,
                   IdEstatus = v.IdEstatusWorkItem,
                   Estatus = v.Estatus,
                   IdPrioridad = v.IdPrioridad,
                   Prioridad = v.Prioridad,
                   IdComplejidad = w.IdComplejidad,
                   IdAsignado = v.IdAsignado,
                   Asignado = v.Asignado,
                   Solicitante = v.Solicitante,
                   IdSprint = v.IdSprint,
                   Sprint = v.Sprint,
                   PuntosHistoria = v.PuntosHistoria,
                   MinutosPresupuesto = v.MinutosPresupuesto,
                   MinutosInvertidos = v.MinutosInvertidos,
                   FechaCompromiso = v.FechaCompromiso,
                   FechaInicio = v.FechaInicio,
                   FechaFin = v.FechaFin,
                   FechaRegistro = v.FechaRegistro,
                   EsVencida = v.EsVencida == true,
                   RevisionesPendientes = v.RevisionesPendientes ?? 0
               };
    }
}
