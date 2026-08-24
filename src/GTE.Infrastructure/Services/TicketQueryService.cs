using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Domain.Soporte;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class TicketQueryService(FabricaContexto fabrica) : ITicketQueryService
{
    public async Task<PagedResult<TicketResponse>> ObtenerBandejaAsync(
        FiltroBandejaTicket filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);

        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(t => t.IdEstatus != EstatusTicket.Cerrado);
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(t => estatus.Contains(t.IdEstatus));
        }

        if (filtro.IdAsignado.HasValue)
        {
            consulta = consulta.Where(t => t.IdAsignadoInterno == filtro.IdAsignado.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(t =>
                (t.Folio != null && t.Folio.Contains(texto))
                || t.Titulo.Contains(texto)
                || t.Solicitante.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var ordenada = filtro.OrdenarPor switch
        {
            "folio" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(t => t.Folio)
                : consulta.OrderBy(t => t.Folio),
            "titulo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(t => t.Titulo)
                : consulta.OrderBy(t => t.Titulo),
            "solicitante" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(t => t.Solicitante)
                : consulta.OrderBy(t => t.Solicitante),
            "categoria" => filtro.OrdenDescendente
                ? consulta.OrderBy(t => t.Categoria == null).ThenByDescending(t => t.Categoria)
                : consulta.OrderBy(t => t.Categoria == null).ThenBy(t => t.Categoria),
            "prioridad" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(t => t.Prioridad)
                : consulta.OrderBy(t => t.Prioridad),
            "estatus" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(t => t.IdEstatus)
                : consulta.OrderBy(t => t.IdEstatus),
            "asignado" => filtro.OrdenDescendente
                ? consulta.OrderBy(t => t.Asignado == null).ThenByDescending(t => t.Asignado)
                : consulta.OrderBy(t => t.Asignado == null).ThenBy(t => t.Asignado),
            _ => consulta
                .OrderBy(t => t.FechaLimiteResolucion ?? DateTime.MaxValue)
                .ThenBy(t => t.IdTicket)
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TicketResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IReadOnlyList<TicketResponse>> ObtenerMiosAsync(
        int idSolicitante, IReadOnlyList<int>? estatus, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto).Where(t => t.IdSolicitanteInterno == idSolicitante);

        if (estatus is null || estatus.Count == 0)
        {
            consulta = consulta.Where(t => t.IdEstatus != EstatusTicket.Cerrado);
        }
        else if (!estatus.Contains(-1))
        {
            var estatusArray = estatus.ToArray();
            consulta = consulta.Where(t => estatusArray.Contains(t.IdEstatus));
        }

        return await consulta
            .OrderByDescending(t => t.IdTicket)
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketResponse?> ObtenerPorIdAsync(
        int idTicket, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(t => t.IdTicket == idTicket, cancellationToken);
    }

    public async Task<TicketResponse?> ObtenerPorFolioAsync(
        string folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(t => t.Folio == folio, cancellationToken);
    }

    private sealed class TicketProyeccion : TicketResponse
    {
        public int IdSolicitanteInterno { get; set; }
        public int? IdAsignadoInterno { get; set; }
    }

    private static IQueryable<TicketProyeccion> Proyectar(DbContextGTE contexto)
    {
        return from t in contexto.TblTicket.AsNoTracking()
               join e in contexto.TblEstatusTicket.AsNoTracking() on t.IdEstatusTicket equals e.Id
               join p in contexto.TblPrioridad.AsNoTracking() on t.IdPrioridad equals p.Id
               join s in contexto.TblUsuario.AsNoTracking() on t.IdSolicitante equals s.IdUsuario
               join c in contexto.TblCategoriaTicket.AsNoTracking() on t.IdCategoriaTicket equals c.IdCategoriaTicket into categorias
               from c in categorias.DefaultIfEmpty()
               join a in contexto.TblUsuario.AsNoTracking() on t.IdAsignado equals a.IdUsuario into asignados
               from a in asignados.DefaultIfEmpty()
               join sla in contexto.TblSla.AsNoTracking() on t.IdSla equals sla.IdSla into slas
               from sla in slas.DefaultIfEmpty()
               join wi in contexto.TblWorkItem.AsNoTracking() on t.IdWorkItemDerivado equals wi.IdWorkItem into workitems
               from wi in workitems.DefaultIfEmpty()
               join enc in contexto.TblEncuestaSatisfaccion.AsNoTracking() on t.IdTicket equals enc.IdTicket into encuestas
               from enc in encuestas.DefaultIfEmpty()
               join us in contexto.TblUsuarioSolicitante.AsNoTracking() on t.IdUsuarioSolicitante equals us.IdUsuarioSolicitante into solicitantes
               from us in solicitantes.DefaultIfEmpty()
               join loc in contexto.TblLocacion.AsNoTracking() on t.IdLocacion equals loc.IdLocacion into locaciones
               from loc in locaciones.DefaultIfEmpty()
               where t.Activo
               select new TicketProyeccion
               {
                   IdTicket = t.IdTicket,
                   Folio = t.Folio,
                   Titulo = t.Titulo,
                   Descripcion = t.Descripcion,
                   Categoria = c != null ? c.Nombre : null,
                   Prioridad = p.Nombre,
                   IdEstatus = t.IdEstatusTicket,
                   Estatus = e.Descripcion,
                   IdSolicitante = t.IdSolicitante,
                   Solicitante = s.Nombre,
                   IdSolicitanteInterno = t.IdSolicitante,
                   Asignado = a != null ? a.Nombre : null,
                   IdAsignadoInterno = t.IdAsignado,
                   Sla = sla != null ? sla.Nombre : null,
                   FechaLimiteRespuesta = t.FechaLimiteRespuesta,
                   FechaLimiteResolucion = t.FechaLimiteResolucion,
                   FechaPrimeraRespuesta = t.FechaPrimeraRespuesta,
                   FechaResolucion = t.FechaResolucion,
                   Solucion = t.Solucion,
                   MinutosSolucion = t.MinutosSolucion,
                   UsuarioSolicitante = us != null ? (us.Nombre ?? us.Usuario) : null,
                   Locacion = loc != null ? loc.Locacion : null,
                   IdWorkItemDerivado = t.IdWorkItemDerivado,
                   FolioWorkItemDerivado = wi != null ? wi.Folio : null,
                   FechaRegistro = t.FechaRegistro,
                   Calificacion = enc != null ? (int?)enc.Calificacion : null,
                   ComentarioEncuesta = enc != null ? enc.Comentario : null
               };
    }
}
