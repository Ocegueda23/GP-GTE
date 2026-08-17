using GTE.Application.DTOs.Responses.Planeacion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Planeacion;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class PlaneacionQueryService(FabricaContexto fabrica) : IPlaneacionQueryService
{
    private static readonly int[] EstatusAbiertos =
    [
        EstatusWorkItem.Pendiente, EstatusWorkItem.EnProceso, EstatusWorkItem.EnPruebas,
        EstatusWorkItem.Correccion, EstatusWorkItem.Suspendido
    ];

    public async Task<IReadOnlyList<SprintResponse>> ObtenerSprintsAsync(
        int? idEquipo, bool soloAbiertos, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);
        if (idEquipo.HasValue)
        {
            consulta = consulta.Where(s => s.IdEquipo == idEquipo.Value);
        }
        if (soloAbiertos)
        {
            consulta = consulta.Where(s => s.IdEstatus != EstatusSprint.Cerrado);
        }

        return await consulta
            .OrderByDescending(s => s.FechaInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<SprintResponse?> ObtenerSprintAsync(
        int idSprint, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto).FirstOrDefaultAsync(s => s.IdSprint == idSprint, cancellationToken);
    }

    public async Task<BacklogResponse> ObtenerBacklogAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // El filtro va sobre las entidades y la proyeccion al final: EF no traduce
        // filtros sobre proyecciones intermedias complejas (leccion 7.8 del estandar)
        var items = await ConsultaBase(contexto)
            .Where(x => x.Item.IdProyecto == idProyecto
                        && x.Item.IdSprint == null
                        && EstatusAbiertos.Contains(x.Vista.IdEstatusWorkItem))
            .OrderBy(x => x.Item.OrdenBacklog == null)
            .ThenBy(x => x.Item.OrdenBacklog)
            .ThenByDescending(x => x.Vista.IdWorkItem)
            .Select(x => x.Vista)
            .Select(ProyeccionTarjeta)
            .ToListAsync(cancellationToken);

        return new BacklogResponse
        {
            Items = items,
            PuntosTotales = items.Sum(i => i.PuntosHistoria ?? 0)
        };
    }

    /// <summary>
    /// Backlog de todos los proyectos para consulta cruzada (P06 pedido explicito del
    /// usuario): mismo filtro base que ObtenerBacklogAsync pero sin acotar a un proyecto,
    /// mas texto libre sobre folio/titulo/proyecto. Es de solo lectura -- reordenar o
    /// mover a sprint sigue siendo por proyecto (el orden manual y el sprint son conceptos
    /// de un equipo/proyecto, no tiene sentido mezclarlos aqui).
    /// </summary>
    public async Task<BacklogResponse> ObtenerBacklogGlobalAsync(
        string? texto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = ConsultaBase(contexto)
            .Where(x => x.Item.IdSprint == null && EstatusAbiertos.Contains(x.Vista.IdEstatusWorkItem));

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var valor = texto.Trim();
            consulta = consulta.Where(x =>
                x.Vista.Folio.Contains(valor) || x.Vista.Titulo.Contains(valor) || x.Vista.Proyecto.Contains(valor));
        }

        var items = await consulta
            .OrderBy(x => x.Vista.ClaveProyecto)
            .ThenBy(x => x.Item.OrdenBacklog == null)
            .ThenBy(x => x.Item.OrdenBacklog)
            .ThenByDescending(x => x.Vista.IdWorkItem)
            .Select(x => x.Vista)
            .Select(ProyeccionTarjeta)
            .ToListAsync(cancellationToken);

        return new BacklogResponse
        {
            Items = items,
            PuntosTotales = items.Sum(i => i.PuntosHistoria ?? 0)
        };
    }

    public async Task<BacklogResponse> ObtenerItemsDeSprintAsync(
        int idSprint, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var items = await ConsultaBase(contexto)
            .Where(x => x.Item.IdSprint == idSprint)
            .OrderBy(x => x.Item.OrdenBacklog == null)
            .ThenBy(x => x.Item.OrdenBacklog)
            .Select(x => x.Vista)
            .Select(ProyeccionTarjeta)
            .ToListAsync(cancellationToken);

        return new BacklogResponse
        {
            Items = items,
            PuntosTotales = items.Sum(i => i.PuntosHistoria ?? 0)
        };
    }

    /// <summary>idEquipo null = vista consolidada: todos los equipos y usuarios a la vez.</summary>
    public async Task<TableroResponse> ObtenerTableroAsync(
        int? idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        string equipo;
        (int IdSprint, string Nombre)? sprintActivo = null;
        List<(int IdTableroColumna, string Nombre, int IdEstatusWorkItem, int Orden, int? LimiteWip)> columnas;

        if (idEquipo.HasValue)
        {
            equipo = await contexto.TblEquipo.AsNoTracking()
                .Where(e => e.IdEquipo == idEquipo.Value)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync(cancellationToken) ?? "Equipo";

            var sprint = await contexto.TblSprint.AsNoTracking()
                .Where(s => s.IdEquipo == idEquipo.Value && s.IdEstatusSprint == EstatusSprint.Activo && s.Activo)
                .Select(s => new { s.IdSprint, s.Nombre })
                .FirstOrDefaultAsync(cancellationToken);
            if (sprint is not null)
            {
                sprintActivo = (sprint.IdSprint, sprint.Nombre);
            }

            columnas = await (
                from c in contexto.TblTableroColumna.AsNoTracking()
                join t in contexto.TblTablero.AsNoTracking() on c.IdTablero equals t.IdTablero
                where t.IdEquipo == idEquipo.Value && t.Activo && c.Activo
                orderby c.Orden
                select new { c.IdTableroColumna, c.Nombre, c.IdEstatusWorkItem, c.Orden, c.LimiteWip }
                ).Select(c => new ValueTuple<int, string, int, int, int?>(
                    c.IdTableroColumna, c.Nombre, c.IdEstatusWorkItem, c.Orden, c.LimiteWip))
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Vista "todos los equipos": sin TblTablero propio del que leer columnas ni
            // sprint activo unico (cada equipo tiene el suyo) -- se usa el mapeo estandar
            // y no se acota por sprint, para no ocultar trabajo de equipos sin sprint activo.
            equipo = "Todos los equipos";
            columnas = ColumnasTableroEstandar.Columnas
                .Select((c, indice) => (IdTableroColumna: -(indice + 1), c.Nombre, IdEstatusWorkItem: c.IdEstatus, c.Orden, LimiteWip: (int?)null))
                .ToList();
        }

        // Las tarjetas del tablero son los elementos de los proyectos del equipo (o de
        // todos, en la vista consolidada); si hay sprint activo se acota a ese sprint (el
        // tablero es del sprint en curso). RN-PLA-05 (nueva): en la columna Terminado solo
        // entran los items cerrados en el mes en curso -- evita que el tablero acumule
        // meses de historial en esa columna.
        var idSprintActivo = sprintActivo?.IdSprint;
        var inicioMesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var consulta =
            from x in ConsultaBase(contexto)
            join p in contexto.TblProyecto.AsNoTracking() on x.Item.IdProyecto equals p.IdProyecto
            where (idEquipo == null || p.IdEquipo == idEquipo.Value)
                  && (idSprintActivo == null || x.Item.IdSprint == idSprintActivo)
                  && (x.Vista.IdEstatusWorkItem != EstatusWorkItem.Terminado
                      || (x.Item.FechaFin != null && x.Item.FechaFin >= inicioMesActual))
            select new { x.Vista, Orden = x.Item.OrdenBacklog };

        var filas = await consulta.ToListAsync(cancellationToken);

        var proyectar = ProyeccionTarjeta.Compile();
        var tarjetas = filas
            .Select(f => new { Tarjeta = proyectar(f.Vista), f.Orden })
            .ToList();

        return new TableroResponse
        {
            IdEquipo = idEquipo,
            Equipo = equipo,
            IdSprintActivo = sprintActivo?.IdSprint,
            SprintActivo = sprintActivo?.Nombre,
            Columnas = columnas.Select(c => new ColumnaTableroResponse
            {
                IdTableroColumna = c.IdTableroColumna,
                Nombre = c.Nombre,
                IdEstatusWorkItem = c.IdEstatusWorkItem,
                Orden = c.Orden,
                LimiteWip = c.LimiteWip,
                Items = tarjetas
                    .Where(t => t.Tarjeta.IdEstatus == c.IdEstatusWorkItem)
                    .OrderBy(t => t.Orden ?? int.MaxValue)
                    .Select(t => t.Tarjeta)
                    .ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Burndown del sprint: puntos que seguian abiertos al final de cada dia,
    /// reconstruidos del historial de estatus (no de snapshots manuales).
    /// </summary>
    public async Task<IReadOnlyList<PuntoBurndownResponse>> ObtenerBurndownAsync(
        int idSprint, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var sprint = await contexto.TblSprint.AsNoTracking()
            .Where(s => s.IdSprint == idSprint)
            .Select(s => new { s.FechaInicio, s.FechaFin })
            .FirstOrDefaultAsync(cancellationToken);
        if (sprint is null)
        {
            return [];
        }

        var items = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdSprint == idSprint && w.Activo)
            .Select(w => new { w.IdWorkItem, Puntos = w.PuntosHistoria ?? 0 })
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return [];
        }

        var ids = items.Select(i => i.IdWorkItem).ToList();

        // Momento en que cada elemento entro a Terminado por primera vez
        var cierres = await contexto.TblHistorialEstatus.AsNoTracking()
            .Where(h => h.Proceso == "WorkItem" && ids.Contains(h.IdRegistro)
                        && h.IdEstatus == EstatusWorkItem.Terminado)
            .GroupBy(h => h.IdRegistro)
            .Select(g => new { IdWorkItem = g.Key, Fecha = g.Min(h => h.FechaInicio) })
            .ToListAsync(cancellationToken);

        var totalPuntos = items.Sum(i => i.Puntos);
        var dias = sprint.FechaFin.DayNumber - sprint.FechaInicio.DayNumber;
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var resultado = new List<PuntoBurndownResponse>();

        for (var i = 0; i <= dias; i++)
        {
            var dia = sprint.FechaInicio.AddDays(i);
            var cerradosAlDia = cierres
                .Where(c => DateOnly.FromDateTime(c.Fecha) <= dia)
                .Select(c => c.IdWorkItem)
                .ToHashSet();

            resultado.Add(new PuntoBurndownResponse
            {
                Fecha = dia,
                // Los dias futuros no tienen dato real: se dejan en el ultimo valor conocido
                PuntosRestantes = dia > hoy
                    ? (resultado.Count > 0 ? resultado[^1].PuntosRestantes : totalPuntos)
                    : items.Where(x => !cerradosAlDia.Contains(x.IdWorkItem)).Sum(x => x.Puntos),
                PuntosIdeales = dias == 0 ? 0 : Math.Round(totalPuntos * (1m - (decimal)i / dias), 2)
            });
        }

        return resultado;
    }

    private static IQueryable<SprintResponse> Proyectar(DbContextGTE contexto)
    {
        return from s in contexto.TblSprint.AsNoTracking()
               join e in contexto.TblEquipo.AsNoTracking() on s.IdEquipo equals e.IdEquipo
               join es in contexto.TblEstatusSprint.AsNoTracking() on s.IdEstatusSprint equals es.Id
               where s.Activo
               select new SprintResponse
               {
                   IdSprint = s.IdSprint,
                   IdEquipo = s.IdEquipo,
                   Equipo = e.Nombre,
                   Nombre = s.Nombre,
                   Objetivo = s.Objetivo,
                   FechaInicio = s.FechaInicio,
                   FechaFin = s.FechaFin,
                   IdEstatus = s.IdEstatusSprint,
                   Estatus = es.Descripcion,
                   TotalItems = contexto.TblWorkItem.Count(w => w.IdSprint == s.IdSprint && w.Activo),
                   ItemsTerminados = contexto.TblWorkItem.Count(w => w.IdSprint == s.IdSprint && w.Activo
                       && w.IdEstatusWorkItem == EstatusWorkItem.Terminado),
                   PuntosComprometidos = contexto.TblWorkItem
                       .Where(w => w.IdSprint == s.IdSprint && w.Activo)
                       .Sum(w => w.PuntosHistoria ?? 0),
                   PuntosTerminados = contexto.TblWorkItem
                       .Where(w => w.IdSprint == s.IdSprint && w.Activo
                                   && w.IdEstatusWorkItem == EstatusWorkItem.Terminado)
                       .Sum(w => w.PuntosHistoria ?? 0)
               };
    }

    /// <summary>
    /// Vista de bandeja unida a la entidad, SIN proyectar: los filtros y ordenamientos
    /// operan sobre columnas reales para que EF los traduzca a SQL.
    /// </summary>
    private static IQueryable<VistaConItem> ConsultaBase(DbContextGTE contexto)
    {
        return from v in contexto.VwBandejaTrabajo.AsNoTracking()
               join w in contexto.TblWorkItem.AsNoTracking() on v.IdWorkItem equals w.IdWorkItem
               select new VistaConItem { Vista = v, Item = w };
    }

    private sealed class VistaConItem
    {
        public Modelos.bdsGTE.VwBandejaTrabajo Vista { get; init; } = null!;
        public Modelos.bdsGTE.TblWorkItem Item { get; init; } = null!;
    }

    /// <summary>Lambda inline (no metodo estatico) para que EF la traduzca en el Select.</summary>
    private static readonly System.Linq.Expressions.Expression<
        Func<Modelos.bdsGTE.VwBandejaTrabajo, BandejaItemResponse>> ProyeccionTarjeta =
        v => new BandejaItemResponse
        {
            IdWorkItem = v.IdWorkItem,
            Folio = v.Folio,
            Tipo = v.Tipo,
            Titulo = v.Titulo,
            ClaveProyecto = v.ClaveProyecto,
            Proyecto = v.Proyecto,
            IdEstatus = v.IdEstatusWorkItem,
            Estatus = v.Estatus,
            Prioridad = v.Prioridad,
            Complejidad = v.Complejidad,
            Asignado = v.Asignado,
            FechaCompromiso = v.FechaCompromiso,
            EsVencida = v.EsVencida == true,
            PuntosHistoria = v.PuntosHistoria,
            MinutosPresupuesto = v.MinutosPresupuesto,
            MinutosInvertidos = v.MinutosInvertidos,
            RevisionesPendientes = v.RevisionesPendientes ?? 0
        };
}
