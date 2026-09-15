using GTE.Domain.Operacion;
using GTE.Domain.Soporte;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Api.Tests;

/// <summary>
/// Regresion del R15: fuerza la traduccion a SQL de sus tres consultas SIN tocar la BD
/// (ToQueryString no abre conexion), por eso corre en cualquier maquina, a diferencia del
/// resto de las pruebas de integracion.
///
/// Existe porque un fallo de traduccion de EF NO lo ve el compilador: revienta en tiempo de
/// ejecucion la primera vez que alguien abre el reporte. Ya paso: las secciones de Tickets e
/// Incidentes usaban un record posicional dentro del Select de un GroupBy que entra a un join,
/// EF perdia el tipo y el R15 fallaba entero con "No se pudo cargar el reporte".
///
/// Estas consultas son un ESPEJO de las de ReportesQueryService (que son privadas): si alla se
/// cambia la forma de un join o de una proyeccion intermedia, hay que reflejarlo aqui o la
/// prueba deja de cubrir lo que cree cubrir.
/// </summary>
public class TraduccionConsultasR15Tests
{
    private static DbContextGTE Contexto()
    {
        var opciones = new DbContextOptionsBuilder<DbContextGTE>()
            .UseSqlServer("Server=noexiste;Database=bdsGTE;Trusted_Connection=True")
            .Options;
        return new DbContextGTE(opciones);
    }

    [Fact]
    public void WorkItemsConTiempoRegistrado()
    {
        using var contexto = Contexto();
        var inicio = DateTime.Today.AddMonths(-1);
        var fin = DateTime.Today;

        var registrado = contexto.TblRegistroTiempo.AsNoTracking()
            .Where(t => t.Activo)
            .GroupBy(t => t.IdWorkItem)
            .Select(g => new { IdWorkItem = g.Key, Minutos = (int?)g.Sum(t => t.Minutos) });

        var consulta =
            from w in contexto.TblWorkItem.AsNoTracking()
            join v in contexto.VwBandejaTrabajo.AsNoTracking() on w.IdWorkItem equals v.IdWorkItem
            join r in registrado on w.IdWorkItem equals r.IdWorkItem into registrosItem
            from r in registrosItem.DefaultIfEmpty()
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItem.Terminado
                && w.FechaFin != null && w.FechaFin >= inicio && w.FechaFin <= fin
            orderby w.FechaFin descending, w.IdWorkItem descending
            select new
            {
                w.IdWorkItem, w.Folio, w.Titulo,
                MinutosInvertidos = v.MinutosInvertidos ?? 0,
                MinutosRegistrados = r.Minutos ?? 0,
                TieneRegistro = r.Minutos != null,
            };

        var sql = consulta.Take(10).ToQueryString();

        // El SUM del LEFT JOIN tiene que llegar coalescido: sin esto la consulta traduce bien
        // pero revienta al MATERIALIZAR los items sin registros de tiempo, con
        // "Nullable object must have a value". Ya paso en produccion.
        Assert.Contains("COALESCE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RelojEstatusDTO
    {
        public int IdRegistro { get; set; }
        public int? Minutos { get; set; }
    }

    private static IQueryable<RelojEstatusDTO> RelojEstatus(
        DbContextGTE contexto, string proceso, int idEstatus)
        => contexto.TblHistorialEstatus.AsNoTracking()
            .Where(h => h.Proceso == proceso && h.IdEstatus == idEstatus && h.MinutosLaborales != null)
            .GroupBy(h => h.IdRegistro)
            .Select(g => new RelojEstatusDTO
            {
                IdRegistro = g.Key,
                Minutos = g.Sum(h => h.MinutosLaborales!.Value),
            });

    [Fact]
    public void TicketsConRelojDeEstatus()
    {
        using var contexto = Contexto();
        var inicio = DateTime.Today.AddMonths(-1);
        var fin = DateTime.Today;
        var reloj = RelojEstatus(contexto, "Ticket", EstatusTicket.EnAtencion);

        var consulta =
            from t in contexto.TblTicket.AsNoTracking()
            join r in reloj on t.IdTicket equals r.IdRegistro into relojes
            from r in relojes.DefaultIfEmpty()
            where t.Activo
                && (t.IdEstatusTicket == EstatusTicket.Resuelto || t.IdEstatusTicket == EstatusTicket.Cerrado)
                && t.FechaResolucion != null && t.FechaResolucion >= inicio && t.FechaResolucion <= fin
            orderby t.FechaResolucion descending, t.IdTicket descending
            select new
            {
                t.IdTicket, t.Folio, t.Titulo,
                Categoria = t.IdCategoriaTicketNavigation != null ? t.IdCategoriaTicketNavigation.Nombre : null,
                Prioridad = t.IdPrioridadNavigation.Nombre,
                Estatus = t.IdEstatusTicketNavigation.Descripcion,
                Solicitante = t.IdSolicitanteNavigation.Nombre,
                Asignado = t.IdAsignadoNavigation != null ? t.IdAsignadoNavigation.Nombre : null,
                MinutosEnAtencion = r.Minutos ?? 0,
            };

        var sql = consulta.Take(10).ToQueryString();
        Assert.False(string.IsNullOrWhiteSpace(sql));
    }

    [Fact]
    public void IncidentesConRelojDeEstatus()
    {
        using var contexto = Contexto();
        var inicio = DateTime.Today.AddMonths(-1);
        var fin = DateTime.Today;
        var reloj = RelojEstatus(contexto, "Incidente", EstatusIncidente.EnAtencion);

        var consulta =
            from i in contexto.TblIncidente.AsNoTracking()
            join r in reloj on i.IdIncidente equals r.IdRegistro into relojes
            from r in relojes.DefaultIfEmpty()
            where i.Activo
                && (i.IdEstatusIncidente == EstatusIncidente.Resuelto || i.IdEstatusIncidente == EstatusIncidente.Cerrado)
                && i.FechaResolucion != null && i.FechaResolucion >= inicio && i.FechaResolucion <= fin
            orderby i.FechaResolucion descending, i.IdIncidente descending
            select new
            {
                i.IdIncidente, i.Folio, i.Titulo,
                Severidad = i.IdSeveridadNavigation.Nombre,
                Proyecto = i.IdProyectoNavigation.Nombre,
                Estatus = i.IdEstatusIncidenteNavigation.Descripcion,
                MinutosEnAtencion = r.Minutos ?? 0,
            };

        var sql = consulta.Take(10).ToQueryString();
        Assert.False(string.IsNullOrWhiteSpace(sql));
    }
}
