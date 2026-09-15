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
        // -1 = Backlog (elementos sin sprint asignado); cualquier otro valor = ese sprint
        if (filtro.IdSprint.HasValue)
        {
            consulta = filtro.IdSprint.Value == -1
                ? consulta.Where(x => x.w.IdSprint == null)
                : consulta.Where(x => x.w.IdSprint == filtro.IdSprint.Value);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(x =>
                x.v.Folio.Contains(texto) || x.v.Titulo.Contains(texto) || x.v.Proyecto.Contains(texto)
                || (x.v.Sprint != null && x.v.Sprint.Contains(texto))
                || (x.v.Sprint == null && "Backlog".Contains(texto, StringComparison.OrdinalIgnoreCase)));
        }
        if (filtro.SoloVencidas)
        {
            consulta = consulta.Where(x => x.v.EsVencida == true);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        // Default (sin ordenarPor): mismo criterio historico -- compromiso mas proximo primero
        // (nulos al final), empate por lo mas reciente creado.
        var ordenada = filtro.OrdenarPor switch
        {
            "folio" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.Folio)
                : consulta.OrderBy(x => x.v.Folio),
            "tipo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.Tipo)
                : consulta.OrderBy(x => x.v.Tipo),
            "titulo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.Titulo)
                : consulta.OrderBy(x => x.v.Titulo),
            "proyecto" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.ClaveProyecto)
                : consulta.OrderBy(x => x.v.ClaveProyecto),
            "asignado" => filtro.OrdenDescendente
                ? consulta.OrderBy(x => x.v.Asignado == null).ThenByDescending(x => x.v.Asignado)
                : consulta.OrderBy(x => x.v.Asignado == null).ThenBy(x => x.v.Asignado),
            "estatus" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.IdEstatusWorkItem)
                : consulta.OrderBy(x => x.v.IdEstatusWorkItem),
            "sprint" => filtro.OrdenDescendente
                ? consulta.OrderBy(x => x.v.Sprint == null).ThenByDescending(x => x.v.Sprint)
                : consulta.OrderBy(x => x.v.Sprint == null).ThenBy(x => x.v.Sprint),
            "prioridad" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(x => x.v.IdPrioridad)
                : consulta.OrderBy(x => x.v.IdPrioridad),
            "presupuesto" => filtro.OrdenDescendente
                ? consulta.OrderBy(x => x.v.MinutosPresupuesto == null).ThenByDescending(x => x.v.MinutosPresupuesto)
                : consulta.OrderBy(x => x.v.MinutosPresupuesto == null).ThenBy(x => x.v.MinutosPresupuesto),
            "invertido" => filtro.OrdenDescendente
                ? consulta.OrderBy(x => x.v.MinutosInvertidos == null).ThenByDescending(x => x.v.MinutosInvertidos)
                : consulta.OrderBy(x => x.v.MinutosInvertidos == null).ThenBy(x => x.v.MinutosInvertidos),
            "compromiso" => filtro.OrdenDescendente
                ? consulta.OrderBy(x => x.v.FechaCompromiso == null).ThenByDescending(x => x.v.FechaCompromiso)
                : consulta.OrderBy(x => x.v.FechaCompromiso == null).ThenBy(x => x.v.FechaCompromiso),
            _ => consulta
                .OrderBy(x => x.v.FechaCompromiso == null)
                .ThenBy(x => x.v.FechaCompromiso)
                .ThenByDescending(x => x.v.IdWorkItem)
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BandejaItemResponse
            {
                IdWorkItem = x.v.IdWorkItem,
                Folio = x.v.Folio,
                Tipo = x.v.Tipo,
                Titulo = x.v.Titulo,
                IdProyecto = x.w.IdProyecto,
                ClaveProyecto = x.v.ClaveProyecto,
                Proyecto = x.v.Proyecto,
                IdEstatus = x.v.IdEstatusWorkItem,
                Estatus = x.v.Estatus,
                Prioridad = x.v.Prioridad,
                Complejidad = x.v.Complejidad,
                IdAsignado = x.v.IdAsignado,
                Asignado = x.v.Asignado,
                Sprint = x.v.Sprint,
                FolioSprint = x.v.FolioSprint,
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

    /// <summary>
    /// Incluye los registros de tiempo de las subtareas (WorkItems con IdPadre = idWorkItem)
    /// ademas de los del propio elemento: antes solo se veian en la pestana Subtareas
    /// (WorkItemHijoResponse.MinutosRegistrados), sin reflejarse en la pestana Tiempo del
    /// padre ni en su "Total" (suma client-side de esta misma lista).
    /// </summary>
    public async Task<IReadOnlyList<RegistroTiempoResponse>> ObtenerTiemposAsync(
        int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from t in contexto.TblRegistroTiempo.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on t.IdUsuario equals u.IdUsuario
            join w in contexto.TblWorkItem.AsNoTracking() on t.IdWorkItem equals w.IdWorkItem
            where (t.IdWorkItem == idWorkItem || w.IdPadre == idWorkItem) && t.Activo
            orderby t.Fecha descending, t.IdRegistroTiempo descending
            select new RegistroTiempoResponse
            {
                IdRegistroTiempo = t.IdRegistroTiempo,
                Fecha = t.Fecha,
                Minutos = t.Minutos,
                Descripcion = t.Descripcion,
                Usuario = u.Nombre,
                FechaRegistro = t.FechaRegistro,
                FolioOrigen = w.IdWorkItem == idWorkItem ? null : w.Folio
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItemHijoResponse>> ObtenerHijosAsync(
        int idWorkItemPadre, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdPadre == idWorkItemPadre)
            .OrderBy(w => w.IdWorkItem)
            .Select(w => new WorkItemHijoResponse
            {
                IdWorkItem = w.IdWorkItem,
                Folio = w.Folio,
                Titulo = w.Titulo,
                IdEstatus = w.IdEstatusWorkItem,
                Estatus = w.IdEstatusWorkItemNavigation.Descripcion,
                Asignado = w.IdAsignadoNavigation != null ? w.IdAsignadoNavigation.Nombre : null,
                MinutosRegistrados = w.TblRegistroTiempo.Where(t => t.Activo).Sum(t => (int?)t.Minutos) ?? 0
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PresupuestoEstimadoResponse?> ObtenerPresupuestoEstimadoAsync(
        int idComplejidad, int? idAsignado, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var complejidad = await contexto.TblComplejidad.AsNoTracking()
            .Where(c => c.IdComplejidad == idComplejidad)
            .Select(c => c.Nombre)
            .FirstOrDefaultAsync(cancellationToken);
        if (complejidad is null)
        {
            return null;
        }

        var niveles = await (
            from m in contexto.TblMatrizPresupuesto.AsNoTracking()
            join n in contexto.TblNivel.AsNoTracking() on m.IdNivel equals n.IdNivel
            where m.IdComplejidad == idComplejidad && m.Activo
            orderby n.Orden
            select new PresupuestoNivelDTO
            {
                IdNivel = n.IdNivel,
                Nivel = n.Nombre,
                Minutos = m.Minutos,
                Puntos = m.Puntos
            }).ToListAsync(cancellationToken);

        // El nivel del asignado es lo que resuelve el renglon de la matriz; sin asignado (o
        // sin nivel capturado) la vista previa se queda en la matriz completa.
        var idNivel = idAsignado.HasValue
            ? await contexto.TblUsuario.AsNoTracking()
                .Where(u => u.IdUsuario == idAsignado.Value)
                .Select(u => u.IdNivel)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var renglon = idNivel.HasValue ? niveles.FirstOrDefault(n => n.IdNivel == idNivel.Value) : null;

        return new PresupuestoEstimadoResponse
        {
            IdComplejidad = idComplejidad,
            Complejidad = complejidad,
            IdAsignado = idAsignado,
            IdNivel = renglon?.IdNivel,
            Nivel = renglon?.Nivel,
            Minutos = renglon?.Minutos,
            Puntos = renglon?.Puntos,
            Niveles = niveles
        };
    }

    private static IQueryable<WorkItemResponse> ProyectarDetalle(DbContextGTE contexto)
    {
        return from v in contexto.VwBandejaTrabajo.AsNoTracking()
               join w in contexto.TblWorkItem.AsNoTracking() on v.IdWorkItem equals w.IdWorkItem
               join us in contexto.TblUsuarioSolicitante.AsNoTracking() on w.IdUsuarioSolicitante equals us.IdUsuarioSolicitante into solicitantesExternos
               from us in solicitantesExternos.DefaultIfEmpty()
               join p in contexto.TblWorkItem.AsNoTracking() on w.IdPadre equals p.IdWorkItem into padres
               from p in padres.DefaultIfEmpty()
               join proy in contexto.TblProyecto.AsNoTracking() on w.IdProyecto equals proy.IdProyecto
               select new WorkItemResponse
               {
                   IdPadre = w.IdPadre,
                   FolioPadre = p != null ? p.Folio : null,
                   TituloPadre = p != null ? p.Titulo : null,
                   IdWorkItem = v.IdWorkItem,
                   Folio = v.Folio,
                   Tipo = v.Tipo,
                   Titulo = v.Titulo,
                   Descripcion = w.Descripcion,
                   CriteriosAceptacion = w.CriteriosAceptacion,
                   IdProyecto = w.IdProyecto,
                   ClaveProyecto = v.ClaveProyecto,
                   Proyecto = v.Proyecto,
                   IdCategoriaProyecto = proy.IdCategoriaProyecto,
                   EsMantenimiento = v.EsMantenimiento,
                   IdEstatus = v.IdEstatusWorkItem,
                   Estatus = v.Estatus,
                   IdPrioridad = v.IdPrioridad,
                   Prioridad = v.Prioridad,
                   IdComplejidad = w.IdComplejidad,
                   Complejidad = v.Complejidad,
                   IdAsignado = v.IdAsignado,
                   Asignado = v.Asignado,
                   Solicitante = v.Solicitante,
                   UsuarioSolicitante = us != null ? (us.Nombre ?? us.Usuario) : null,
                   IdSprint = v.IdSprint,
                   Sprint = v.Sprint,
                   FolioSprint = v.FolioSprint,
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
