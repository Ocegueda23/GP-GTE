using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.Interfaces;
using GTE.Domain.Solicitudes;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class SolicitudQueryService(FabricaContexto fabrica) : ISolicitudQueryService
{
    private static readonly int[] EstatusPendientesTriage =
    [
        EstatusSolicitud.Enviada, EstatusSolicitud.EnAnalisis, EstatusSolicitud.Aprobada
    ];

    public async Task<PagedResult<SolicitudResponse>> ObtenerTriageAsync(
        FiltroTriage filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);

        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(s => EstatusPendientesTriage.Contains(s.IdEstatus));
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(s => estatus.Contains(s.IdEstatus));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(s =>
                (s.Folio != null && s.Folio.Contains(texto))
                || s.Titulo.Contains(texto)
                || s.Solicitante.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var ordenada = filtro.OrdenarPor switch
        {
            "folio" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.Folio)
                : consulta.OrderBy(s => s.Folio),
            "titulo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.Titulo)
                : consulta.OrderBy(s => s.Titulo),
            "solicitante" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.Solicitante)
                : consulta.OrderBy(s => s.Solicitante),
            "tipo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.Tipo)
                : consulta.OrderBy(s => s.Tipo),
            "prioridad" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.Prioridad)
                : consulta.OrderBy(s => s.Prioridad),
            "diasEspera" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.DiasEspera)
                : consulta.OrderBy(s => s.DiasEspera),
            "estatus" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(s => s.IdEstatus)
                : consulta.OrderBy(s => s.IdEstatus),
            "proyecto" => filtro.OrdenDescendente
                ? consulta.OrderBy(s => s.Proyecto == null).ThenByDescending(s => s.Proyecto)
                : consulta.OrderBy(s => s.Proyecto == null).ThenBy(s => s.Proyecto),
            _ => consulta
                .OrderByDescending(s => s.DiasEspera)
                .ThenBy(s => s.IdSolicitud)
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SolicitudResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IReadOnlyList<SolicitudResponse>> ObtenerMiasAsync(
        int idSolicitante, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var solicitudes = await Proyectar(contexto)
            .Where(s => s.IdSolicitanteInterno == idSolicitante)
            .OrderByDescending(s => s.IdSolicitud)
            .ToListAsync(cancellationToken);

        // Los items generados son lo que el solicitante quiere ver: en que se convirtio
        // su peticion. Se cargan en una sola consulta y se agrupan en memoria.
        var ids = solicitudes.Select(s => s.IdSolicitud).ToList();
        var items = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join e in contexto.TblEstatusWorkItem.AsNoTracking() on w.IdEstatusWorkItem equals e.Id
            where w.IdSolicitud != null && ids.Contains(w.IdSolicitud.Value) && w.Activo
            orderby w.IdWorkItem
            select new { w.IdSolicitud, Item = new ItemGeneradoResponse { Folio = w.Folio, Titulo = w.Titulo, Estatus = e.Descripcion } }
            ).ToListAsync(cancellationToken);

        var porSolicitud = items
            .GroupBy(x => x.IdSolicitud!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ItemGeneradoResponse>)g.Select(x => x.Item).ToList());

        foreach (var solicitud in solicitudes)
        {
            if (porSolicitud.TryGetValue(solicitud.IdSolicitud, out var generados))
            {
                solicitud.ItemsGenerados = generados;
            }
        }

        return solicitudes;
    }

    public async Task<int> ContarPendientesTriageAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSolicitud.AsNoTracking()
            .Where(s => s.Activo && EstatusPendientesTriage.Contains(s.IdEstatusSolicitud))
            .CountAsync(cancellationToken);
    }

    public async Task<SolicitudResponse?> ObtenerPorIdAsync(
        int idSolicitud, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var solicitud = await Proyectar(contexto)
            .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud, cancellationToken);
        if (solicitud is null)
        {
            return null;
        }

        solicitud.ItemsGenerados = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join e in contexto.TblEstatusWorkItem.AsNoTracking() on w.IdEstatusWorkItem equals e.Id
            where w.IdSolicitud == idSolicitud && w.Activo
            orderby w.IdWorkItem
            select new ItemGeneradoResponse { Folio = w.Folio, Titulo = w.Titulo, Estatus = e.Descripcion }
            ).ToListAsync(cancellationToken);

        return solicitud;
    }

    private sealed class SolicitudProyeccion : SolicitudResponse
    {
        public int IdSolicitanteInterno { get; set; }
    }

    private static IQueryable<SolicitudProyeccion> Proyectar(DbContextGTE contexto)
    {
        return from s in contexto.TblSolicitud.AsNoTracking()
               join e in contexto.TblEstatusSolicitud.AsNoTracking() on s.IdEstatusSolicitud equals e.Id
               join t in contexto.TblTipoSolicitud.AsNoTracking() on s.IdTipoSolicitud equals t.Id
               join p in contexto.TblPrioridad.AsNoTracking() on s.IdPrioridad equals p.Id
               join u in contexto.TblUsuario.AsNoTracking() on s.IdSolicitante equals u.IdUsuario
               join pr in contexto.TblProyecto.AsNoTracking() on s.IdProyecto equals pr.IdProyecto into proyectos
               from pr in proyectos.DefaultIfEmpty()
               join us in contexto.TblUsuarioSolicitante.AsNoTracking() on s.IdUsuarioSolicitante equals us.IdUsuarioSolicitante into solicitantesExternos
               from us in solicitantesExternos.DefaultIfEmpty()
               where s.Activo
               select new SolicitudProyeccion
               {
                   IdSolicitud = s.IdSolicitud,
                   Folio = s.Folio,
                   Titulo = s.Titulo,
                   Descripcion = s.Descripcion,
                   IdTipoSolicitud = s.IdTipoSolicitud,
                   Tipo = t.Nombre,
                   IdPrioridad = s.IdPrioridad,
                   Prioridad = p.Nombre,
                   IdEstatus = s.IdEstatusSolicitud,
                   Estatus = e.Descripcion,
                   Solicitante = u.Nombre,
                   IdUsuarioSolicitante = s.IdUsuarioSolicitante,
                   UsuarioSolicitante = us != null ? (us.Nombre ?? us.Usuario) : null,
                   IdSolicitanteInterno = s.IdSolicitante,
                   Proyecto = pr != null ? pr.Nombre : null,
                   IdProyecto = s.IdProyecto,
                   FechaDeseada = s.FechaDeseada != null
                       ? s.FechaDeseada.Value.ToDateTime(TimeOnly.MinValue)
                       : null,
                   JustificacionNegocio = s.JustificacionNegocio,
                   FechaRegistro = s.FechaRegistro,
                   DiasEspera = EF.Functions.DateDiffDay(s.FechaRegistro, DateTime.Now)
               };
    }
}
