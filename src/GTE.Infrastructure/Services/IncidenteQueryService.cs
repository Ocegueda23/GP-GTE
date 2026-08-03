using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Operacion;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class IncidenteQueryService(FabricaContexto fabrica) : IIncidenteQueryService
{
    public async Task<PagedResult<IncidenteResponse>> ObtenerBandejaAsync(
        FiltroBandejaIncidente filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);

        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(i => i.IdEstatus != EstatusIncidente.Cerrado);
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(i => estatus.Contains(i.IdEstatus));
        }

        if (filtro.IdSeveridad.HasValue)
        {
            consulta = consulta.Where(i => i.IdSeveridad == filtro.IdSeveridad.Value);
        }

        if (filtro.IdProyecto.HasValue)
        {
            consulta = consulta.Where(i => i.IdProyecto == filtro.IdProyecto.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(i =>
                (i.Folio != null && i.Folio.Contains(texto)) || i.Titulo.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var items = await consulta
            .OrderBy(i => i.IdSeveridad)
            .ThenByDescending(i => i.FechaOcurrencia)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IncidenteResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IncidenteResponse?> ObtenerPorFolioAsync(
        string folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(i => i.Folio == folio, cancellationToken);
    }

    public async Task<IncidenteResponse?> ObtenerPorIdAsync(
        int idIncidente, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken);
    }

    private static IQueryable<IncidenteResponse> Proyectar(DbContextGTE contexto)
    {
        return from i in contexto.TblIncidente.AsNoTracking()
               join e in contexto.TblEstatusIncidente.AsNoTracking() on i.IdEstatusIncidente equals e.Id
               join s in contexto.TblSeveridad.AsNoTracking() on i.IdSeveridad equals s.Id
               join p in contexto.TblProyecto.AsNoTracking() on i.IdProyecto equals p.IdProyecto
               join wi in contexto.TblWorkItem.AsNoTracking() on i.IdWorkItemCorrectivo equals wi.IdWorkItem into workitems
               from wi in workitems.DefaultIfEmpty()
               join rel in contexto.TblRelease.AsNoTracking() on i.IdReleaseCausante equals rel.IdRelease into releases
               from rel in releases.DefaultIfEmpty()
               where i.Activo
               select new IncidenteResponse
               {
                   IdIncidente = i.IdIncidente,
                   Folio = i.Folio,
                   Titulo = i.Titulo,
                   Descripcion = i.Descripcion,
                   IdProyecto = i.IdProyecto,
                   Proyecto = p.Nombre,
                   IdSeveridad = i.IdSeveridad,
                   Severidad = s.Nombre,
                   IdEstatus = i.IdEstatusIncidente,
                   Estatus = e.Descripcion,
                   FechaOcurrencia = i.FechaOcurrencia,
                   FechaDeteccion = i.FechaDeteccion,
                   FechaResolucion = i.FechaResolucion,
                   MinutosIndisponibilidad = i.MinutosIndisponibilidad,
                   CausaRaiz = i.CausaRaiz,
                   IdWorkItemCorrectivo = i.IdWorkItemCorrectivo,
                   FolioWorkItemCorrectivo = wi != null ? wi.Folio : null,
                   IdReleaseCausante = i.IdReleaseCausante,
                   VersionReleaseCausante = rel != null ? rel.Version : null,
                   FechaRegistro = i.FechaRegistro
               };
    }
}
