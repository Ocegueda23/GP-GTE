using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Domain.CentroMando;
using GTE.Domain.Entregas;
using GTE.Domain.Operacion;
using GTE.Domain.Soporte;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Motor de evaluacion del Centro de Mando TI: calcula los indicadores automaticos de cada
/// equipo para un periodo, los normaliza con
/// <see cref="CalculadoraCentroMando"/>, corre el
/// <see cref="DiagnosticoCausaRaiz"/> y persiste evaluacion, detalle, causas y alertas.
///
/// ALCANCE DE UN EQUIPO -- como se decide que trabajo "es" de un responsable:
///   WorkItems  : tblWorkItem.IdEquipo (asignacion directa al equipo).
///   Tickets    : IdAsignado dentro de los miembros del equipo (tblTicket no tiene equipo).
///   Incidentes : por los proyectos del equipo (tblProyecto.IdEquipo), mismo criterio que
///                ya usa IndicadoresEjecutivosQueryService.
///   Releases   : igual, por proyecto del equipo.
///   Tiempo     : tblRegistroTiempo de los miembros del equipo.
///
/// INDICADORES MANUALES: los del catalogo con Origen = Manual no tienen fuente en GTE hoy
/// (respaldos, cobertura de monitoreo, deuda tecnica, capacitacion...). El motor NO les
/// inventa un valor: quedan con SinDatos = 1 y fuera del score. Es la misma disciplina que
/// el Dashboard P18 aplica a DORA "Lead Time for Changes".
///
/// IDEMPOTENTE: recalcular el mismo periodo borra el detalle y las causas previas de esa
/// evaluacion y las reescribe; la fila de tblEvaluacionEquipo se reusa (hay UNIQUE por
/// equipo/anio/mes). Las alertas se deduplican por Clave.
/// </summary>
public class MotorEvaluacionCentroMando(
    FabricaContexto fabrica,
    ICalendarioLaboral calendario,
    AuditContext auditoria,
    ILogger<MotorEvaluacionCentroMando> logger) : IMotorEvaluacionCentroMando
{
    private const int EstatusWorkItemSuspendido = 5;
    private const int EstatusWorkItemTerminado = 6;
    private const int EstatusWorkItemCerrado = 7;
    private const int MinutosPorHora = 60;

    /// <summary>Contexto de un equipo ya resuelto, para no re-consultarlo por indicador.</summary>
    private sealed record ContextoEquipo(
        int IdEquipo,
        string Nombre,
        int? IdLider,
        string? Ambito,
        List<int> Miembros,
        List<int> Proyectos,
        DateTime Desde,
        DateTime Hasta);

    public async Task<int> RecalcularPeriodoAsync(int anio, int mes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var catalogo = await contexto.TblIndicadorGestion.AsNoTracking()
            .Where(i => i.Activo)
            .ToListAsync(cancellationToken);

        if (catalogo.Count == 0)
        {
            logger.LogWarning("Centro de Mando: el catalogo de indicadores esta vacio, no hay nada que calcular.");
            return 0;
        }

        // Solo equipos con lider: el modelo evalua a un responsable, y sin lider no hay a
        // quien atribuirle el resultado.
        var equipos = await contexto.TblEquipo.AsNoTracking()
            .Where(e => e.Activo && e.IdLider != null)
            .Select(e => new { e.IdEquipo, e.Nombre, e.IdLider, e.AmbitoCentroMando })
            .ToListAsync(cancellationToken);

        var (desde, hasta) = RangoMes(anio, mes);
        var evaluados = 0;

        foreach (var equipo in equipos)
        {
            var miembros = await contexto.TblEquipoMiembro.AsNoTracking()
                .Where(m => m.Activo && m.IdEquipo == equipo.IdEquipo)
                .Select(m => m.IdUsuario)
                .ToListAsync(cancellationToken);

            if (equipo.IdLider is { } lider && !miembros.Contains(lider))
            {
                miembros.Add(lider);
            }

            var proyectos = await contexto.TblProyecto.AsNoTracking()
                .Where(p => p.Activo && p.IdEquipo == equipo.IdEquipo)
                .Select(p => p.IdProyecto)
                .ToListAsync(cancellationToken);

            var ctx = new ContextoEquipo(
                equipo.IdEquipo, equipo.Nombre, equipo.IdLider, equipo.AmbitoCentroMando,
                miembros, proyectos, desde, hasta);

            var valores = await CalcularValoresAsync(contexto, ctx, cancellationToken);

            // Al equipo le tocan los indicadores comunes mas los de su ambito tecnico (si tiene).
            var aplicables = catalogo
                .Where(i => i.Ambito == AmbitoCentroMando.Comun
                    || (ctx.Ambito is not null && i.Ambito == ctx.Ambito))
                .ToList();

            var evaluados_ = aplicables
                .Select(i => CalculadoraCentroMando.Evaluar(ADefinicion(i), valores.GetValueOrDefault(i.Clave)))
                .ToList();

            var evaluacion = CalculadoraCentroMando.Combinar(evaluados_);
            var carga = await CalcularCargaAsync(contexto, ctx, cancellationToken);
            var indices = await CalcularIndicesDiagnosticoAsync(contexto, ctx, carga.IndiceCarga, cancellationToken);
            var causas = DiagnosticoCausaRaiz.Diagnosticar(indices);

            await PersistirAsync(contexto, ctx, anio, mes, evaluacion, carga.IndiceCarga, causas, cancellationToken);
            evaluados++;
        }

        logger.LogInformation(
            "Centro de Mando: periodo {Anio}-{Mes:00} recalculado, {Evaluados} equipo(s).", anio, mes, evaluados);

        return evaluados;
    }

    private static DefinicionIndicador ADefinicion(TblIndicadorGestion i) => new(
        i.IdIndicadorGestion, i.Clave, i.Nombre, i.Categoria, i.Ambito, i.Origen, i.Unidad,
        i.Meta, i.UmbralAlerta, i.Direccion, i.Peso, i.PonderaEnScore);

    private static (DateTime Desde, DateTime Hasta) RangoMes(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        return (desde, desde.AddMonths(1).AddTicks(-1));
    }

    // ------------------------------------------------------------------
    // Calculo de los indicadores automaticos
    // ------------------------------------------------------------------

    /// <summary>
    /// Devuelve clave de indicador -> valor crudo. Una clave ausente (o con null) significa
    /// "sin datos": el indicador se reporta como tal y no participa en el score.
    /// </summary>
    private async Task<Dictionary<string, decimal?>> CalcularValoresAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        var valores = new Dictionary<string, decimal?>();

        var wi = await ObtenerWorkItemsAsync(contexto, ctx, ct);
        var tickets = await ObtenerTicketsAsync(contexto, ctx, ct);
        var incidentes = await ObtenerIncidentesAsync(contexto, ctx, ct);

        // ---------- Bloque comun ----------
        valores["com.objetivos"] = await CalcularObjetivosAsync(contexto, ctx, ct);
        valores["com.tiempos"] = PorcentajeATiempo(wi);
        valores["com.retrabajo"] = await CalcularRetrabajoAsync(contexto, ctx, wi, ct);
        valores["com.backlog"] = PromedioDiasAbiertos(wi, ctx.Hasta);
        valores["com.incidentes"] = incidentes.Count;
        valores["com.disponibilidad"] = Disponibilidad(incidentes, ctx);
        valores["com.satisfaccion"] = await CalcularCsatAsync(contexto, tickets, ct);
        valores["com.atencion"] = Atencion(wi, tickets, ctx);

        var (presupuesto, invertido) = await ObtenerMinutosAsync(contexto, ctx, wi, ct);
        valores["com.eficiencia"] = invertido > 0 ? Redondear(presupuesto * 100m / invertido) : null;

        var horasCapacidad = await CalcularHorasCapacidadAsync(contexto, ctx, ct);
        var puntos = wi.Where(w => EsTerminadoEnPeriodo(w, ctx) && w.PuntosHistoria.HasValue)
            .Sum(w => w.PuntosHistoria!.Value);
        valores["com.productividad"] = horasCapacidad > 0 ? Redondear(puntos / horasCapacidad, 3) : null;

        // ---------- Bloque Desarrollo ----------
        if (ctx.Ambito == AmbitoCentroMando.Desarrollo)
        {
            valores["dev.sprint"] = await CalcularCumplimientoSprintAsync(contexto, ctx, ct);
            valores["dev.retrabajo"] = valores["com.retrabajo"];
            valores["dev.entregas.retrasadas"] = await CalcularEntregasRetrasadasAsync(contexto, ctx, ct);
            valores["dev.bugs.criticos"] = incidentes.Count(i =>
                i.IdSeveridad is Severidad.S1Critica or Severidad.S2Alta && i.IdReleaseCausante != null);

            var terminados = wi.Where(w => EsTerminadoEnPeriodo(w, ctx)).ToList();
            var correcciones = terminados.Count(w => w.IdTipoWorkItem == EstatusIncidente.IdTipoWorkItemCorreccion);
            var historias = terminados.Count - correcciones;
            valores["dev.densidad.bugs"] = historias > 0 ? Redondear(correcciones / (historias / 10m)) : null;

            var horasCorreccion = terminados
                .Where(w => w.IdTipoWorkItem == EstatusIncidente.IdTipoWorkItemCorreccion && w.FechaFin.HasValue)
                .Select(w => (w.FechaFin!.Value - w.FechaRegistro).TotalHours)
                .ToList();
            valores["dev.tiempo.bugs"] = horasCorreccion.Count > 0
                ? Redondear((decimal)horasCorreccion.Average())
                : null;

            var minutosCorreccion = await MinutosPorTipoAsync(
                contexto, ctx, EstatusIncidente.IdTipoWorkItemCorreccion, ct);
            valores["dev.mtto.vs.nuevo"] = invertido > 0 ? Redondear(minutosCorreccion * 100m / invertido) : null;
        }

        // ---------- Bloque Infraestructura ----------
        if (ctx.Ambito == AmbitoCentroMando.Infraestructura)
        {
            valores["inf.disponibilidad"] = valores["com.disponibilidad"];
            valores["inf.incidentes.severidad"] = incidentes.Count(i =>
                i.IdSeveridad is Severidad.S1Critica or Severidad.S2Alta);

            var resueltos = incidentes
                .Where(i => i.FechaResolucion.HasValue)
                .Select(i => ((i.FechaResolucion!.Value - (i.FechaDeteccion ?? i.FechaOcurrencia)).TotalHours))
                .Where(h => h >= 0)
                .ToList();
            valores["inf.mttr"] = resueltos.Count > 0 ? Redondear((decimal)resueltos.Average()) : null;

            var detectados = incidentes
                .Where(i => i.FechaDeteccion.HasValue)
                .Select(i => (i.FechaDeteccion!.Value - i.FechaOcurrencia).TotalMinutes)
                .Where(m => m >= 0)
                .ToList();
            valores["inf.mttd"] = detectados.Count > 0 ? Redondear((decimal)detectados.Average()) : null;

            valores["inf.cambios.fallidos"] = await CalcularCambiosFallidosAsync(contexto, ctx, ct);

            var minutosTickets = await MinutosEnTicketsAsync(contexto, ctx, ct);
            valores["inf.tiempo.reactivo"] = invertido > 0
                ? Redondear(minutosTickets * 100m / invertido)
                : null;
        }

        // ---------- Bloque Soporte ----------
        if (ctx.Ambito == AmbitoCentroMando.Soporte)
        {
            var conSla = tickets.Where(t => t.FechaResolucion.HasValue && t.FechaLimiteResolucion.HasValue).ToList();
            valores["sop.sla"] = conSla.Count > 0
                ? Redondear(conSla.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion) * 100m / conSla.Count)
                : null;

            var conRespuesta = tickets.Where(t => t.FechaPrimeraRespuesta.HasValue).ToList();
            valores["sop.primera.respuesta"] = conRespuesta.Count > 0
                ? Redondear((decimal)conRespuesta.Average(t => (t.FechaPrimeraRespuesta!.Value - t.FechaRegistro).TotalMinutes))
                : null;

            var resueltos = tickets.Where(t => t.FechaResolucion.HasValue).ToList();
            valores["sop.tiempo.resolucion"] = resueltos.Count > 0
                ? Redondear((decimal)resueltos.Average(t => (t.FechaResolucion!.Value - t.FechaRegistro).TotalHours))
                : null;

            var abiertos = tickets.Where(t => t.IdEstatusTicket != EstatusTicket.Cerrado
                && t.IdEstatusTicket != EstatusTicket.Resuelto).ToList();
            valores["sop.vencidos"] = abiertos.Count > 0
                ? Redondear(abiertos.Count(t => t.FechaLimiteResolucion < ctx.Hasta) * 100m / abiertos.Count)
                : null;

            valores["sop.reabiertos"] = await CalcularTicketsReabiertosAsync(contexto, tickets, ct);
            valores["sop.csat"] = valores["com.satisfaccion"];
            valores["sop.distribucion"] = DispersionCarga(tickets, ctx.Miembros);
        }

        return valores;
    }

    // ------------------------------------------------------------------
    // Consultas base
    // ------------------------------------------------------------------

    private sealed record WorkItemMinimo(
        int IdWorkItem, int IdTipoWorkItem, int IdEstatusWorkItem, DateTime FechaRegistro,
        DateTime? FechaFin, DateTime? FechaCompromiso, decimal? PuntosHistoria, int? MinutosPresupuesto);

    private static async Task<List<WorkItemMinimo>> ObtenerWorkItemsAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
        => await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdEquipo == ctx.IdEquipo
                && w.FechaRegistro <= ctx.Hasta
                && (w.FechaFin == null || w.FechaFin >= ctx.Desde))
            .Select(w => new WorkItemMinimo(
                w.IdWorkItem, w.IdTipoWorkItem, w.IdEstatusWorkItem, w.FechaRegistro,
                w.FechaFin, w.FechaCompromiso, w.PuntosHistoria, w.MinutosPresupuesto))
            .ToListAsync(ct);

    private sealed record TicketMinimo(
        int IdTicket, int? IdAsignado, int IdEstatusTicket, DateTime FechaRegistro,
        DateTime? FechaPrimeraRespuesta, DateTime? FechaResolucion, DateTime? FechaLimiteResolucion);

    private static async Task<List<TicketMinimo>> ObtenerTicketsAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Miembros.Count == 0)
        {
            return [];
        }

        return await contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.IdAsignado != null && ctx.Miembros.Contains(t.IdAsignado.Value)
                && t.FechaRegistro <= ctx.Hasta
                && (t.FechaResolucion == null || t.FechaResolucion >= ctx.Desde))
            .Select(t => new TicketMinimo(
                t.IdTicket, t.IdAsignado, t.IdEstatusTicket, t.FechaRegistro,
                t.FechaPrimeraRespuesta, t.FechaResolucion, t.FechaLimiteResolucion))
            .ToListAsync(ct);
    }

    private sealed record IncidenteMinimo(
        int IdIncidente, int IdSeveridad, DateTime FechaOcurrencia, DateTime? FechaDeteccion,
        DateTime? FechaResolucion, int? MinutosIndisponibilidad, int? IdReleaseCausante);

    private static async Task<List<IncidenteMinimo>> ObtenerIncidentesAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Proyectos.Count == 0)
        {
            return [];
        }

        return await contexto.TblIncidente.AsNoTracking()
            .Where(i => i.Activo && ctx.Proyectos.Contains(i.IdProyecto)
                && i.FechaOcurrencia >= ctx.Desde && i.FechaOcurrencia <= ctx.Hasta)
            .Select(i => new IncidenteMinimo(
                i.IdIncidente, i.IdSeveridad, i.FechaOcurrencia, i.FechaDeteccion,
                i.FechaResolucion, i.MinutosIndisponibilidad, i.IdReleaseCausante))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------------
    // Formulas
    // ------------------------------------------------------------------

    private static bool EsTerminadoEnPeriodo(WorkItemMinimo w, ContextoEquipo ctx)
        => w.FechaFin.HasValue && w.FechaFin >= ctx.Desde && w.FechaFin <= ctx.Hasta
            && w.IdEstatusWorkItem is EstatusWorkItemTerminado or EstatusWorkItemCerrado;

    private static decimal? PorcentajeATiempo(List<WorkItemMinimo> wi)
    {
        var conCompromiso = wi.Where(w => w.FechaCompromiso.HasValue && w.FechaFin.HasValue).ToList();
        return conCompromiso.Count == 0
            ? null
            : Redondear(conCompromiso.Count(w => w.FechaFin <= w.FechaCompromiso) * 100m / conCompromiso.Count);
    }

    private static decimal? PromedioDiasAbiertos(List<WorkItemMinimo> wi, DateTime corte)
    {
        var abiertos = wi.Where(w => w.FechaFin == null).ToList();
        return abiertos.Count == 0
            ? null
            : Redondear((decimal)abiertos.Average(w => (corte - w.FechaRegistro).TotalDays));
    }

    /// <summary>
    /// Disponibilidad a partir de los minutos de indisponibilidad declarados en los
    /// incidentes del periodo. Sin ningun incidente con ese dato capturado se devuelve null
    /// (sin datos), no 100%: "no hay registro" no es lo mismo que "no hubo caidas".
    /// </summary>
    private static decimal? Disponibilidad(List<IncidenteMinimo> incidentes, ContextoEquipo ctx)
    {
        var conDato = incidentes.Where(i => i.MinutosIndisponibilidad.HasValue).ToList();
        if (conDato.Count == 0)
        {
            return null;
        }

        var minutosPeriodo = (decimal)(ctx.Hasta - ctx.Desde).TotalMinutes;
        if (minutosPeriodo <= 0)
        {
            return null;
        }

        var caidos = conDato.Sum(i => i.MinutosIndisponibilidad!.Value);
        return Redondear(Math.Max(0m, (minutosPeriodo - caidos) * 100m / minutosPeriodo), 3);
    }

    private static decimal? Atencion(List<WorkItemMinimo> wi, List<TicketMinimo> tickets, ContextoEquipo ctx)
    {
        var recibidos = wi.Count(w => w.FechaRegistro >= ctx.Desde)
            + tickets.Count(t => t.FechaRegistro >= ctx.Desde);
        if (recibidos == 0)
        {
            return null;
        }

        var atendidos = wi.Count(w => EsTerminadoEnPeriodo(w, ctx))
            + tickets.Count(t => t.FechaResolucion.HasValue);

        return Redondear(atendidos / (decimal)recibidos, 3);
    }

    /// <summary>
    /// Retrabajo = % de lo terminado en el periodo que ya habia estado terminado antes (es
    /// decir, se reabrio). Se lee de tblHistorialEstatus, que es donde vive el rastro real de
    /// las transiciones.
    /// </summary>
    private static async Task<decimal?> CalcularRetrabajoAsync(
        DbContextGTE contexto, ContextoEquipo ctx, List<WorkItemMinimo> wi, CancellationToken ct)
    {
        var terminados = wi.Where(w => EsTerminadoEnPeriodo(w, ctx)).Select(w => w.IdWorkItem).ToList();
        if (terminados.Count == 0)
        {
            return null;
        }

        // tblHistorialEstatus guarda un tramo por estatus (IdEstatus, FechaInicio, FechaFin).
        // Un tramo en Terminado que YA CERRO (FechaFin != null) significa que el item salio de
        // Terminado, es decir, se reabrio.
        var reabiertos = await contexto.TblHistorialEstatus.AsNoTracking()
            .Where(h => h.Proceso == "WorkItem" && terminados.Contains(h.IdRegistro)
                && h.IdEstatus == EstatusWorkItemTerminado && h.FechaFin != null)
            .Select(h => h.IdRegistro)
            .Distinct()
            .CountAsync(ct);

        return Redondear(reabiertos * 100m / terminados.Count);
    }

    private static async Task<decimal?> CalcularCsatAsync(
        DbContextGTE contexto, List<TicketMinimo> tickets, CancellationToken ct)
    {
        var ids = tickets.Select(t => t.IdTicket).ToList();
        if (ids.Count == 0)
        {
            return null;
        }

        var calificaciones = await contexto.TblEncuestaSatisfaccion.AsNoTracking()
            .Where(e => ids.Contains(e.IdTicket))
            .Select(e => (int)e.Calificacion)
            .ToListAsync(ct);

        return calificaciones.Count == 0 ? null : Redondear((decimal)calificaciones.Average(), 2);
    }

    private static async Task<decimal?> CalcularObjetivosAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        var resultados = await contexto.TblResultadoClave.AsNoTracking()
            .Where(r => r.Activo && r.IdObjetivoOkrNavigation.Activo
                && r.IdObjetivoOkrNavigation.IdEquipo == ctx.IdEquipo
                && r.IdObjetivoOkrNavigation.Anio == ctx.Desde.Year)
            .Select(r => new { r.ValorActual, r.ValorMeta })
            .ToListAsync(ct);

        if (resultados.Count == 0)
        {
            return null;
        }

        var cumplidos = resultados.Count(r => r.ValorMeta != 0 && r.ValorActual >= r.ValorMeta);
        return Redondear(cumplidos * 100m / resultados.Count);
    }

    private static async Task<(decimal Presupuesto, decimal Invertido)> ObtenerMinutosAsync(
        DbContextGTE contexto, ContextoEquipo ctx, List<WorkItemMinimo> wi, CancellationToken ct)
    {
        var presupuesto = wi.Where(w => EsTerminadoEnPeriodo(w, ctx) && w.MinutosPresupuesto.HasValue)
            .Sum(w => (decimal)w.MinutosPresupuesto!.Value);

        if (ctx.Miembros.Count == 0)
        {
            return (presupuesto, 0m);
        }

        var desde = DateOnly.FromDateTime(ctx.Desde);
        var hasta = DateOnly.FromDateTime(ctx.Hasta);

        var invertido = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(r => r.Activo && ctx.Miembros.Contains(r.IdUsuario) && r.Fecha >= desde && r.Fecha <= hasta)
            .SumAsync(r => (int?)r.Minutos, ct) ?? 0;

        return (presupuesto, invertido);
    }

    private static async Task<decimal> MinutosPorTipoAsync(
        DbContextGTE contexto, ContextoEquipo ctx, int idTipoWorkItem, CancellationToken ct)
    {
        if (ctx.Miembros.Count == 0)
        {
            return 0m;
        }

        var desde = DateOnly.FromDateTime(ctx.Desde);
        var hasta = DateOnly.FromDateTime(ctx.Hasta);

        return await (
            from r in contexto.TblRegistroTiempo.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on r.IdWorkItem equals w.IdWorkItem
            where r.Activo && ctx.Miembros.Contains(r.IdUsuario)
                && r.Fecha >= desde && r.Fecha <= hasta
                && w.IdTipoWorkItem == idTipoWorkItem
            select (int?)r.Minutos).SumAsync(ct) ?? 0;
    }

    /// <summary>
    /// Minutos que el equipo dedico a tickets. tblRegistroTiempo se registra contra
    /// WorkItems, asi que se cuenta el tiempo de los WorkItems derivados de un ticket
    /// (tblTicket.IdWorkItemDerivado) -- es el unico puente que existe hoy entre ambos.
    /// </summary>
    private static async Task<decimal> MinutosEnTicketsAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Miembros.Count == 0)
        {
            return 0m;
        }

        var desde = DateOnly.FromDateTime(ctx.Desde);
        var hasta = DateOnly.FromDateTime(ctx.Hasta);

        return await (
            from r in contexto.TblRegistroTiempo.AsNoTracking()
            join t in contexto.TblTicket.AsNoTracking() on r.IdWorkItem equals t.IdWorkItemDerivado
            where r.Activo && t.Activo && ctx.Miembros.Contains(r.IdUsuario)
                && r.Fecha >= desde && r.Fecha <= hasta
            select (int?)r.Minutos).SumAsync(ct) ?? 0;
    }

    private static async Task<decimal?> CalcularCumplimientoSprintAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        var sprints = await contexto.TblSprint.AsNoTracking()
            .Where(s => s.Activo && s.IdEquipo == ctx.IdEquipo
                && s.FechaFin >= DateOnly.FromDateTime(ctx.Desde)
                && s.FechaFin <= DateOnly.FromDateTime(ctx.Hasta))
            .Select(s => s.IdSprint)
            .ToListAsync(ct);

        if (sprints.Count == 0)
        {
            return null;
        }

        var items = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdSprint != null && sprints.Contains(w.IdSprint.Value))
            .Select(w => w.IdEstatusWorkItem)
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            return null;
        }

        var entregadas = items.Count(e => e is EstatusWorkItemTerminado or EstatusWorkItemCerrado);
        return Redondear(entregadas * 100m / items.Count);
    }

    private static async Task<decimal?> CalcularEntregasRetrasadasAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Proyectos.Count == 0)
        {
            return null;
        }

        var releases = await contexto.TblRelease.AsNoTracking()
            .Where(r => r.Activo && ctx.Proyectos.Contains(r.IdProyecto)
                && r.IdEstatusRelease == EstatusRelease.Liberado
                && r.FechaLiberacion != null
                && r.FechaLiberacion >= ctx.Desde && r.FechaLiberacion <= ctx.Hasta)
            .Select(r => new { r.FechaPlan, r.FechaLiberacion })
            .ToListAsync(ct);

        var conPlan = releases.Where(r => r.FechaPlan.HasValue).ToList();
        return conPlan.Count == 0
            ? null
            : Redondear(conPlan.Count(r => DateOnly.FromDateTime(r.FechaLiberacion!.Value) > r.FechaPlan!.Value)
                * 100m / conPlan.Count);
    }

    private static async Task<decimal?> CalcularCambiosFallidosAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Proyectos.Count == 0)
        {
            return null;
        }

        var despliegues = await contexto.TblDespliegue.AsNoTracking()
            .Where(d => ctx.Proyectos.Contains(d.IdReleaseNavigation.IdProyecto)
                && d.FechaInicio >= ctx.Desde && d.FechaInicio <= ctx.Hasta)
            .Select(d => d.IdEstatusDespliegue)
            .ToListAsync(ct);

        return despliegues.Count == 0
            ? null
            : Redondear(despliegues.Count(e => e == EstatusDespliegue.Fallido) * 100m / despliegues.Count);
    }

    private static async Task<decimal?> CalcularTicketsReabiertosAsync(
        DbContextGTE contexto, List<TicketMinimo> tickets, CancellationToken ct)
    {
        var cerrados = tickets
            .Where(t => t.IdEstatusTicket is EstatusTicket.Cerrado or EstatusTicket.Resuelto)
            .Select(t => t.IdTicket)
            .ToList();

        if (cerrados.Count == 0)
        {
            return null;
        }

        // Mismo criterio que en WorkItems: un tramo en Cerrado/Resuelto que ya cerro significa
        // que el ticket salio de ese estatus, es decir, el usuario lo reabrio.
        var reabiertos = await contexto.TblHistorialEstatus.AsNoTracking()
            .Where(h => h.Proceso == "Ticket" && cerrados.Contains(h.IdRegistro)
                && (h.IdEstatus == EstatusTicket.Cerrado || h.IdEstatus == EstatusTicket.Resuelto)
                && h.FechaFin != null)
            .Select(h => h.IdRegistro)
            .Distinct()
            .CountAsync(ct);

        return Redondear(reabiertos * 100m / cerrados.Count);
    }

    /// <summary>
    /// Dispersion de tickets por agente: cuanto se aleja del promedio el agente mas cargado,
    /// en porcentaje. Alta significa que uno absorbe lo que el resto no.
    /// </summary>
    private static decimal? DispersionCarga(List<TicketMinimo> tickets, List<int> miembros)
    {
        if (miembros.Count < 2 || tickets.Count == 0)
        {
            return null;
        }

        var porAgente = miembros
            .Select(m => (decimal)tickets.Count(t => t.IdAsignado == m))
            .ToList();

        var promedio = porAgente.Average();
        if (promedio <= 0)
        {
            return null;
        }

        return Redondear((porAgente.Max() - promedio) * 100m / promedio);
    }

    // ------------------------------------------------------------------
    // Carga de trabajo e indices de diagnostico
    // ------------------------------------------------------------------

    private sealed record CargaEquipo(decimal Disponibles, decimal Asignadas, decimal Ejecutadas, decimal? IndiceCarga);

    private async Task<CargaEquipo> CalcularCargaAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        var horasDisponibles = await CalcularHorasCapacidadAsync(contexto, ctx, ct);

        var minutosAsignados = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdEquipo == ctx.IdEquipo
                && w.FechaRegistro <= ctx.Hasta && (w.FechaFin == null || w.FechaFin >= ctx.Desde))
            .SumAsync(w => (int?)w.MinutosPresupuesto, ct) ?? 0;

        var desde = DateOnly.FromDateTime(ctx.Desde);
        var hasta = DateOnly.FromDateTime(ctx.Hasta);
        var minutosEjecutados = ctx.Miembros.Count == 0
            ? 0
            : await contexto.TblRegistroTiempo.AsNoTracking()
                .Where(r => r.Activo && ctx.Miembros.Contains(r.IdUsuario) && r.Fecha >= desde && r.Fecha <= hasta)
                .SumAsync(r => (int?)r.Minutos, ct) ?? 0;

        var asignadas = Redondear(minutosAsignados / (decimal)MinutosPorHora, 1) ?? 0m;
        var ejecutadas = Redondear(minutosEjecutados / (decimal)MinutosPorHora, 1) ?? 0m;

        var indice = horasDisponibles > 0 ? Redondear(asignadas * 100m / horasDisponibles) : null;
        return new CargaEquipo(horasDisponibles, asignadas, ejecutadas, indice);
    }

    /// <summary>
    /// Capacidad del equipo = minutos laborables reales de cada miembro segun su horario
    /// (ICalendarioLaboral, el motor unico). Nunca una jornada teorica de 8 horas: eso
    /// ignoraria festivos y turnos partidos, y falsearia el indice de carga justo cuando mas
    /// importa.
    /// </summary>
    private async Task<decimal> CalcularHorasCapacidadAsync(
        DbContextGTE contexto, ContextoEquipo ctx, CancellationToken ct)
    {
        if (ctx.Miembros.Count == 0)
        {
            return 0m;
        }

        var horarios = await contexto.TblUsuario.AsNoTracking()
            .Where(u => ctx.Miembros.Contains(u.IdUsuario) && u.IdHorario != null)
            .Select(u => u.IdHorario!.Value)
            .ToListAsync(ct);

        if (horarios.Count == 0)
        {
            return 0m;
        }

        var cache = new Dictionary<int, int>();
        var totalMinutos = 0;

        foreach (var idHorario in horarios)
        {
            if (!cache.TryGetValue(idHorario, out var minutos))
            {
                minutos = await calendario.CalcularMinutosLaboralesAsync(ctx.Desde, ctx.Hasta, idHorario, ct);
                cache[idHorario] = minutos;
            }

            totalMinutos += minutos;
        }

        return Redondear(totalMinutos / (decimal)MinutosPorHora, 1) ?? 0m;
    }

    /// <summary>
    /// Indices del modelo de causa raiz. Los que hoy no tienen fuente en GTE se dejan en
    /// null a proposito: <see cref="DiagnosticoCausaRaiz"/> distingue "dentro de umbral" de
    /// "no medido", y esa diferencia es justamente lo que evita culpar a una persona por
    /// falta de instrumentacion.
    /// </summary>
    private static async Task<IndicesDiagnostico> CalcularIndicesDiagnosticoAsync(
        DbContextGTE contexto, ContextoEquipo ctx, decimal? indiceCarga, CancellationToken ct)
    {
        // Indice de espera: % del tiempo de vida de los items del periodo que estuvieron en
        // un estatus de espera/suspension (bloqueados por un tercero).
        decimal? espera = null;
        var itemsEquipo = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdEquipo == ctx.IdEquipo
                && w.FechaRegistro <= ctx.Hasta && (w.FechaFin == null || w.FechaFin >= ctx.Desde))
            .Select(w => w.IdWorkItem)
            .ToListAsync(ct);

        if (itemsEquipo.Count > 0)
        {
            var tramos = await contexto.TblHistorialEstatus.AsNoTracking()
                .Where(h => h.Proceso == "WorkItem" && itemsEquipo.Contains(h.IdRegistro)
                    && h.MinutosLaborales != null)
                .Select(h => new { h.IdEstatus, Minutos = h.MinutosLaborales!.Value })
                .ToListAsync(ct);

            var total = tramos.Sum(t => t.Minutos);
            if (total > 0)
            {
                // Suspendido es el estatus en que el item quedo detenido esperando a un tercero.
                var bloqueado = tramos
                    .Where(t => t.IdEstatus == EstatusWorkItemSuspendido)
                    .Sum(t => t.Minutos);
                espera = Redondear(bloqueado * 100m / total);
            }
        }

        // Indice de cambio de prioridad: reasignaciones registradas en el historial de campo.
        decimal? cambioPrioridad = null;
        if (itemsEquipo.Count > 0)
        {
            var cambios = await contexto.TblHistorialCampo.AsNoTracking()
                .Where(h => h.Entidad == "WorkItem" && itemsEquipo.Contains(h.IdEntidad)
                    && (h.Campo == "IdPrioridad" || h.Campo == "IdAsignado")
                    && h.Fecha >= ctx.Desde && h.Fecha <= ctx.Hasta)
                .CountAsync(ct);

            var personas = Math.Max(1, ctx.Miembros.Count);
            cambioPrioridad = Redondear(cambios / (decimal)personas, 2);
        }

        return new IndicesDiagnostico(
            Espera: espera,
            CambioPrioridad: cambioPrioridad,
            AlcanceInestable: null,   // requiere marcar el motivo del retrabajo: sin captura hoy
            Carga: indiceCarga,
            TrabajoManual: null,      // requiere inventario de tareas automatizables: sin captura hoy
            DependenciaUnica: null);  // requiere mapa de procedimientos criticos: sin captura hoy
    }

    // ------------------------------------------------------------------
    // Persistencia
    // ------------------------------------------------------------------

    private async Task PersistirAsync(
        DbContextGTE contexto, ContextoEquipo ctx, int anio, int mes,
        EvaluacionCalculada evaluacion, decimal? indiceCarga,
        IReadOnlyList<CausaDetectada> causas, CancellationToken ct)
    {
        var usuario = auditoria.TieneIdentidad ? auditoria.Usuario : "motor-centro-mando";

        var cabecera = await contexto.TblEvaluacionEquipo
            .FirstOrDefaultAsync(e => e.IdEquipo == ctx.IdEquipo && e.Anio == anio && e.Mes == mes, ct);

        if (cabecera is null)
        {
            cabecera = new TblEvaluacionEquipo
            {
                IdEquipo = ctx.IdEquipo,
                Anio = (short)anio,
                Mes = (byte)mes,
                UsuarioRegistro = usuario,
                Activo = true,
            };
            contexto.TblEvaluacionEquipo.Add(cabecera);
        }
        else
        {
            cabecera.UsuarioMovto = usuario;
            cabecera.FechaMovto = DateTime.Now;
        }

        cabecera.IdResponsable = ctx.IdLider;
        cabecera.ScoreGeneral = evaluacion.ScoreGeneral;
        cabecera.Nivel = evaluacion.Nivel;
        cabecera.Semaforo = evaluacion.Semaforo;
        cabecera.IndiceCarga = indiceCarga;
        cabecera.IndicadoresConDato = (short)evaluacion.IndicadoresConDato;
        cabecera.IndicadoresTotales = (short)evaluacion.IndicadoresTotales;
        cabecera.FechaCalculo = DateTime.Now;
        cabecera.Activo = true;

        await contexto.SaveChangesAsync(ct);

        // Recalcular reemplaza el detalle y las causas del periodo, no las acumula.
        var detallePrevio = await contexto.TblEvaluacionEquipoDetalle
            .Where(d => d.IdEvaluacionEquipo == cabecera.IdEvaluacionEquipo)
            .ToListAsync(ct);
        contexto.TblEvaluacionEquipoDetalle.RemoveRange(detallePrevio);

        var causasPrevias = await contexto.TblDiagnosticoCausa
            .Where(d => d.IdEvaluacionEquipo == cabecera.IdEvaluacionEquipo)
            .ToListAsync(ct);
        contexto.TblDiagnosticoCausa.RemoveRange(causasPrevias);

        await contexto.SaveChangesAsync(ct);

        var (anioAnterior, mesAnterior) = mes == 1 ? (anio - 1, 12) : (anio, mes - 1);
        var valoresPrevios = await (
            from d in contexto.TblEvaluacionEquipoDetalle.AsNoTracking()
            join e in contexto.TblEvaluacionEquipo.AsNoTracking()
                on d.IdEvaluacionEquipo equals e.IdEvaluacionEquipo
            where e.IdEquipo == ctx.IdEquipo && e.Anio == anioAnterior && e.Mes == mesAnterior
            select new { d.IdIndicadorGestion, d.Valor })
            .ToDictionaryAsync(x => x.IdIndicadorGestion, x => x.Valor, ct);

        foreach (var indicador in evaluacion.Indicadores)
        {
            contexto.TblEvaluacionEquipoDetalle.Add(new TblEvaluacionEquipoDetalle
            {
                IdEvaluacionEquipo = cabecera.IdEvaluacionEquipo,
                IdIndicadorGestion = indicador.Definicion.IdIndicadorGestion,
                Valor = indicador.Valor,
                ValorNormalizado = indicador.ValorNormalizado,
                Semaforo = indicador.Semaforo,
                SinDatos = indicador.SinDatos,
                ValorPeriodoAnterior = valoresPrevios.GetValueOrDefault(indicador.Definicion.IdIndicadorGestion),
                UsuarioRegistro = usuario,
            });
        }

        foreach (var causa in causas)
        {
            contexto.TblDiagnosticoCausa.Add(new TblDiagnosticoCausa
            {
                IdEvaluacionEquipo = cabecera.IdEvaluacionEquipo,
                Causa = causa.Causa,
                IndiceClave = causa.IndiceClave,
                Valor = causa.Valor,
                Umbral = causa.Umbral,
                Evidencia = causa.Evidencia,
                UsuarioRegistro = usuario,
            });
        }

        await contexto.SaveChangesAsync(ct);
    }

    private static decimal? Redondear(decimal? valor, int decimales = 2)
        => valor.HasValue ? Math.Round(valor.Value, decimales) : null;
}
