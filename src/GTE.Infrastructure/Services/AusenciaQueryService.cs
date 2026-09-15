using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.Interfaces;
using GTE.Domain.Ausencias;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class AusenciaQueryService(FabricaContexto fabrica) : IAusenciaQueryService
{
    /// <summary>Lo que el aprobador tiene que resolver mientras no filtre otra cosa.</summary>
    private static readonly int[] EstatusPendientes = [EstatusAusencia.Solicitada];

    public async Task<PagedResult<AusenciaResponse>> ObtenerBandejaAsync(
        FiltroAusencias filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);

        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(a => EstatusPendientes.Contains(a.IdEstatus));
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(a => estatus.Contains(a.IdEstatus));
        }

        if (filtro.IdUsuario.HasValue)
        {
            consulta = consulta.Where(a => a.IdUsuario == filtro.IdUsuario.Value);
        }

        if (filtro.IdTipoAusencia.HasValue)
        {
            consulta = consulta.Where(a => a.IdTipoAusencia == filtro.IdTipoAusencia.Value);
        }

        // El periodo del filtro atrapa toda ausencia que lo toque, no solo la que empieza dentro.
        if (filtro.Desde.HasValue)
        {
            consulta = consulta.Where(a => a.FechaFin >= filtro.Desde.Value);
        }

        if (filtro.Hasta.HasValue)
        {
            consulta = consulta.Where(a => a.FechaInicio <= filtro.Hasta.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var ordenada = filtro.OrdenarPor switch
        {
            "usuario" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(a => a.Usuario)
                : consulta.OrderBy(a => a.Usuario),
            "tipo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(a => a.Tipo)
                : consulta.OrderBy(a => a.Tipo),
            "estatus" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(a => a.IdEstatus)
                : consulta.OrderBy(a => a.IdEstatus),
            "fechaFin" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(a => a.FechaFin)
                : consulta.OrderBy(a => a.FechaFin),
            "fechaInicio" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(a => a.FechaInicio)
                : consulta.OrderBy(a => a.FechaInicio),
            _ => consulta.OrderBy(a => a.FechaInicio).ThenBy(a => a.IdAusencia)
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AusenciaResponse>
        {
            Items = CalcularDias(items),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IReadOnlyList<AusenciaResponse>> ObtenerMiasAsync(
        int idUsuario, IReadOnlyList<int>? estatus, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto).Where(a => a.IdUsuario == idUsuario);

        if (estatus is null || estatus.Count == 0)
        {
            var vigentes = EstatusAusencia.Vigentes.ToArray();
            consulta = consulta.Where(a => vigentes.Contains(a.IdEstatus));
        }
        else if (!estatus.Contains(-1))
        {
            var estatusArray = estatus.ToArray();
            consulta = consulta.Where(a => estatusArray.Contains(a.IdEstatus));
        }

        var items = await consulta
            .OrderByDescending(a => a.FechaInicio)
            .ThenByDescending(a => a.IdAusencia)
            .ToListAsync(cancellationToken);

        return CalcularDias(items);
    }

    public async Task<AusenciaResponse?> ObtenerPorIdAsync(
        int idAusencia, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var ausencia = await Proyectar(contexto)
            .FirstOrDefaultAsync(a => a.IdAusencia == idAusencia, cancellationToken);
        if (ausencia is not null)
        {
            ausencia.DiasNaturales = ausencia.FechaFin.DayNumber - ausencia.FechaInicio.DayNumber + 1;
        }
        return ausencia;
    }

    public async Task<int> ContarPendientesAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAusencia.AsNoTracking()
            .Where(a => a.Activo && a.IdEstatusAusencia == EstatusAusencia.Solicitada)
            .CountAsync(cancellationToken);
    }

    public async Task<CatalogosAusenciaResponse> ObtenerCatalogosAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var tipos = await contexto.TblTipoAusencia.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new OpcionAusenciaResponse { Id = t.Id, Nombre = t.Nombre })
            .ToListAsync(cancellationToken);

        var estatus = await contexto.TblEstatusAusencia.AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Orden)
            .Select(e => new OpcionAusenciaResponse { Id = e.Id, Nombre = e.Descripcion })
            .ToListAsync(cancellationToken);

        return new CatalogosAusenciaResponse { Tipos = tipos, Estatus = estatus };
    }

    /// <summary>Dias naturales del periodo, extremos incluidos; se calcula ya materializado
    /// para no depender de la traduccion de DateOnly a T-SQL.</summary>
    private static IReadOnlyList<AusenciaResponse> CalcularDias(List<AusenciaResponse> items)
    {
        foreach (var item in items)
        {
            item.DiasNaturales = item.FechaFin.DayNumber - item.FechaInicio.DayNumber + 1;
        }
        return items;
    }

    private static IQueryable<AusenciaResponse> Proyectar(DbContextGTE contexto)
    {
        return from a in contexto.TblAusencia.AsNoTracking()
               join t in contexto.TblTipoAusencia.AsNoTracking() on a.IdTipoAusencia equals t.Id
               join e in contexto.TblEstatusAusencia.AsNoTracking() on a.IdEstatusAusencia equals e.Id
               join u in contexto.TblUsuario.AsNoTracking() on a.IdUsuario equals u.IdUsuario
               where a.Activo
               select new AusenciaResponse
               {
                   IdAusencia = a.IdAusencia,
                   IdUsuario = a.IdUsuario,
                   Usuario = u.Nombre,
                   IdTipoAusencia = a.IdTipoAusencia,
                   Tipo = t.Nombre,
                   IdEstatus = a.IdEstatusAusencia,
                   Estatus = e.Descripcion,
                   FechaInicio = a.FechaInicio,
                   FechaFin = a.FechaFin,
                   Motivo = a.Motivo,
                   FechaRegistro = a.FechaRegistro
               };
    }
}
