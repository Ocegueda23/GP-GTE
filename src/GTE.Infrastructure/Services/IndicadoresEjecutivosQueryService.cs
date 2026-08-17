using GTE.Application.DTOs.Responses.IndicadoresEjecutivos;
using GTE.Application.Interfaces;
using GTE.Domain.DashboardEjecutivo;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Dashboard Ejecutivo P18 (Doctos/GTE-DocumentoMaestro.md 3.10/5.10) -- vista de
/// equipo/proyecto (DORA, costo, rentabilidad, OKR), distinta del Dashboard de colaborador
/// individual (<see cref="DashboardQueryService"/>). Todo se calcula en tiempo real contra
/// DbContextGTE, mismo criterio que el dashboard de colaborador (sin job/snapshot para lo
/// que se puede calcular al vuelo); la unica pieza que SI usa snapshot es KPIs
/// personalizados (tblKpiDefinicion/tblKpiValor), materializados de noche por
/// <see cref="SnapshotKpiJob"/> via Hangfire -- para eso se construyo esa tabla.
///
/// Simplificaciones deliberadas de esta primera pasada (documentadas tambien en
/// Doctos/PENDIENTES.md):
/// - DORA "Lead Time for Changes" (merge de PR -> despliegue PROD) no tiene fuente de datos
///   real todavia (integracion Git, resto de Fase 3, sin consumidor) -- se reporta
///   explicitamente "sin datos" en vez de inventar un numero.
///   "Deployment Frequency" y "Change Failure Rate" SI se calculan reales via
///   tblDespliegue/tblRelease/tblIncidente.
/// - SLA/CSAT son transversales a toda la mesa de ayuda: tblTicket no tiene FK de
///   proyecto en el modelo, asi que no se pueden acotar por alcance Departamento/proyecto;
///   siempre se calculan globales.
/// - "Top riesgos" consulta tblRiesgo real (workflow ya sembrado), pero esa tabla no
///   tiene CRUD/UI propia todavia (A5 sigue pendiente) -- normalmente vendra vacia hasta
///   que exista una forma de capturar riesgos.
/// - Retrabajo cuenta tiempo en items tipo Correccion; "reaperturas" (veces que un item
///   regreso a En Proceso) no se cuenta todavia -- no existe ese contador en el modelo.
/// - OKR: se reusa <see cref="IOkrQueryService"/> tal cual (idProyecto/idEquipo/anio); sin
///   alcance Departamento, sin filtro de proyecto/equipo explicito, se listan TODOS los
///   objetivos del anio (esa interfaz no soporta una lista de proyectos visibles).
/// </summary>
public class IndicadoresEjecutivosQueryService(
    FabricaContexto fabrica, ICalendarioLaboral calendario, ICosteoQueryService costeo, IOkrQueryService okr)
    : IIndicadoresEjecutivosQueryService
{
    private const int EstatusWorkItemTerminado = 6;
    private const int TipoWorkItemCorreccion = 9;
    private const int EstatusDespliegueExitoso = 2;
    private const int EstatusReleaseLiberado = 4;
    private const int EstatusSprintActivo = 2;
    private const int EstatusRiesgoCerrado = 4;
    private const int VentanaDiasChangeFailure = 7;
    private const int TopRiesgos = 10;

    public async Task<IndicadoresEjecutivosResponse> ObtenerAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal,
        int anio, int mes, int? idEquipo, int? idProyecto,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var proyectosVisibles = await ResolverProyectosVisiblesAsync(contexto, idUsuarioActual, tieneAlcanceGlobal, cancellationToken);
        if (idProyecto.HasValue)
        {
            proyectosVisibles = proyectosVisibles is null
                ? [idProyecto.Value]
                : (proyectosVisibles.Contains(idProyecto.Value) ? [idProyecto.Value] : []);
        }

        var (desde, hasta) = RangoMes(anio, mes);
        var idHorarioDefault = await ResolverHorarioDefaultAsync(contexto, cancellationToken);

        var leadCycle = await ObtenerLeadCycleTimeAsync(contexto, proyectosVisibles, idEquipo, desde, hasta, idHorarioDefault, cancellationToken);
        var dora = await ObtenerDoraAsync(contexto, proyectosVisibles, desde, hasta, cancellationToken);
        var entregaATiempo = await ObtenerEntregaATiempoAsync(contexto, proyectosVisibles, idEquipo, desde, hasta, cancellationToken);
        var eficiencia = await ObtenerEficienciaAsync(contexto, proyectosVisibles, idEquipo, desde, hasta, cancellationToken);
        var retrabajo = await ObtenerRetrabajoAsync(contexto, proyectosVisibles, idEquipo, desde, hasta, cancellationToken);
        var productividad = await ObtenerProductividadAsync(contexto, proyectosVisibles, idEquipo, desde, hasta, cancellationToken);
        var sla = await ObtenerSlaAsync(contexto, desde, hasta, cancellationToken);
        var proyectos = await ObtenerSemaforoProyectosAsync(contexto, proyectosVisibles, desde, hasta, anio, cancellationToken);
        var kpis = await ObtenerKpisPersonalizadosAsync(contexto, anio, cancellationToken);
        var riesgos = await ObtenerTopRiesgosAsync(contexto, proyectosVisibles, cancellationToken);
        var burndown = idEquipo.HasValue
            ? await ObtenerBurndownSprintActivoAsync(contexto, idEquipo.Value, cancellationToken)
            : null;
        var objetivosOkr = await okr.ObtenerObjetivosAsync(idProyecto, idEquipo, anio, cancellationToken);

        return new IndicadoresEjecutivosResponse
        {
            Alcance = tieneAlcanceGlobal ? "Global" : "Departamento",
            Anio = anio,
            Mes = mes,
            LeadCycleTime = leadCycle,
            Dora = dora,
            EntregaATiempo = entregaATiempo,
            EficienciaPorcentaje = eficiencia,
            Retrabajo = retrabajo,
            Productividad = productividad,
            Sla = sla,
            Proyectos = proyectos,
            Okr = objetivosOkr,
            KpisPersonalizados = kpis,
            TopRiesgos = riesgos,
            BurndownSprintActivo = burndown,
        };
    }

    public async Task<string?> ObtenerLayoutAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblDashboardLayoutUsuario
            .Where(l => l.IdUsuario == idUsuario)
            .Select(l => l.LayoutJson)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<List<int>?> ResolverProyectosVisiblesAsync(
        DbContextGTE contexto, int idUsuarioActual, bool tieneAlcanceGlobal, CancellationToken ct)
    {
        if (tieneAlcanceGlobal) return null;

        var idAreaUsuario = await contexto.TblUsuario
            .Where(u => u.IdUsuario == idUsuarioActual)
            .Select(u => u.IdPuestoNavigation != null ? u.IdPuestoNavigation.IdArea : null)
            .FirstOrDefaultAsync(ct);

        if (idAreaUsuario is null) return [];

        return await contexto.TblProyecto
            .Where(p => p.Activo && p.IdResponsable != null
                && p.IdResponsableNavigation!.IdPuestoNavigation != null
                && p.IdResponsableNavigation!.IdPuestoNavigation!.IdArea == idAreaUsuario)
            .Select(p => p.IdProyecto)
            .ToListAsync(ct);
    }

    private static async Task<int> ResolverHorarioDefaultAsync(DbContextGTE contexto, CancellationToken ct)
        => await contexto.TblHorario
            .Where(h => h.Activo)
            .OrderBy(h => h.IdHorario)
            .Select(h => h.IdHorario)
            .FirstOrDefaultAsync(ct);

    private static (DateTime Desde, DateTime Hasta) RangoMes(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        return (desde, desde.AddMonths(1).AddTicks(-1));
    }

    private async Task<LeadCycleTimeResponse> ObtenerLeadCycleTimeAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, int? idEquipo,
        DateTime desde, DateTime hasta, int idHorarioDefault, CancellationToken ct)
    {
        var items = await (
            from w in contexto.TblWorkItem
            join v in contexto.VwBandejaTrabajo on w.IdWorkItem equals v.IdWorkItem
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (proyectosVisibles == null || proyectosVisibles.Contains(w.IdProyecto))
                && (idEquipo == null || w.IdEquipo == idEquipo)
            select new
            {
                w.FechaRegistro, FechaFin = w.FechaFin!.Value,
                IdHorarioAsignado = w.IdAsignadoNavigation!.IdHorario, MinutosInvertidos = v.MinutosInvertidos ?? 0,
            }).ToListAsync(ct);

        if (items.Count == 0)
        {
            return new LeadCycleTimeResponse();
        }

        var horasLeadTime = new List<double>(items.Count);
        foreach (var item in items)
        {
            var idHorario = item.IdHorarioAsignado ?? idHorarioDefault;
            var minutos = await calendario.CalcularMinutosLaboralesAsync(item.FechaRegistro, item.FechaFin, idHorario, ct);
            horasLeadTime.Add(minutos / 60.0);
        }

        return new LeadCycleTimeResponse
        {
            LeadTimeHorasP50 = Math.Round((decimal)CalculadoraIndicadoresEjecutivos.Percentil(horasLeadTime, 50), 1),
            LeadTimeHorasP85 = Math.Round((decimal)CalculadoraIndicadoresEjecutivos.Percentil(horasLeadTime, 85), 1),
            CycleTimeHorasPromedio = Math.Round(items.Sum(i => i.MinutosInvertidos) / 60m / items.Count, 1),
            ItemsConsiderados = items.Count,
        };
    }

    private async Task<DoraResponse> ObtenerDoraAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var despliegues = await contexto.TblDespliegue
            .Where(d => d.IdEstatusDespliegue == EstatusDespliegueExitoso
                && d.FechaFin != null && d.FechaFin >= desde && d.FechaFin <= hasta
                && d.IdAmbienteNavigation.Nombre.Contains("PROD")
                && (proyectosVisibles == null || proyectosVisibles.Contains(d.IdReleaseNavigation.IdProyecto)))
            .CountAsync(ct);

        var diasPeriodo = (hasta.Date - desde.Date).Days + 1;
        var deploymentsPorSemana = diasPeriodo == 0 ? 0 : despliegues / (diasPeriodo / 7m);

        var releasesLiberados = await contexto.TblRelease
            .Where(r => r.IdEstatusRelease == EstatusReleaseLiberado
                && r.FechaLiberacion != null && r.FechaLiberacion >= desde && r.FechaLiberacion <= hasta
                && (proyectosVisibles == null || proyectosVisibles.Contains(r.IdProyecto)))
            .Select(r => new { r.IdRelease, FechaLiberacion = r.FechaLiberacion!.Value })
            .ToListAsync(ct);

        var fallidos = 0;
        foreach (var release in releasesLiberados)
        {
            var limite = release.FechaLiberacion.AddDays(VentanaDiasChangeFailure);
            var tieneIncidente = await contexto.TblIncidente
                .AnyAsync(i => i.IdReleaseCausante == release.IdRelease
                    && i.FechaOcurrencia >= release.FechaLiberacion && i.FechaOcurrencia <= limite, ct);
            if (tieneIncidente) fallidos++;
        }

        var incidentesResueltos = await contexto.TblIncidente
            .Where(i => i.FechaResolucion != null && i.FechaResolucion >= desde && i.FechaResolucion <= hasta
                && (proyectosVisibles == null || proyectosVisibles.Contains(i.IdProyecto)))
            .Select(i => new { i.FechaOcurrencia, FechaResolucion = i.FechaResolucion!.Value })
            .ToListAsync(ct);

        return new DoraResponse
        {
            DeploymentsPorSemana = Math.Round(deploymentsPorSemana, 2),
            LeadTimeCambiosHoras = null,
            LeadTimeCambiosSinDatos = true,
            ChangeFailureRatePorcentaje = releasesLiberados.Count == 0 ? 0 : Math.Round(fallidos * 100m / releasesLiberados.Count, 1),
            MttrHoras = incidentesResueltos.Count == 0
                ? null
                : Math.Round((decimal)incidentesResueltos.Average(i => (i.FechaResolucion - i.FechaOcurrencia).TotalHours), 1),
            DespliguesConsiderados = despliegues,
            IncidentesConsiderados = incidentesResueltos.Count,
        };
    }

    private async Task<EntregaATiempoResponse> ObtenerEntregaATiempoAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, int? idEquipo, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var terminados = await contexto.TblWorkItem
            .Where(w => w.Activo && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (proyectosVisibles == null || proyectosVisibles.Contains(w.IdProyecto))
                && (idEquipo == null || w.IdEquipo == idEquipo))
            .Select(w => new { w.FechaCompromiso, w.FechaFin })
            .ToListAsync(ct);

        var conCompromiso = terminados.Where(w => w.FechaCompromiso.HasValue).ToList();
        var aTiempo = conCompromiso.Count(w => w.FechaFin <= w.FechaCompromiso);
        var porcentaje = conCompromiso.Count == 0 ? 100m : Math.Round(aTiempo * 100m / conCompromiso.Count, 1);

        return new EntregaATiempoResponse
        {
            Porcentaje = porcentaje,
            Semaforo = CalculadoraIndicadoresEjecutivos.Semaforo(porcentaje),
            TotalConCompromiso = conCompromiso.Count,
        };
    }

    /// <summary>MinutosInvertidos (vwBandejaTrabajo) es el total historico del item, no una
    /// tajada exacta del periodo -- misma limitacion que Retrabajo, aceptable para un
    /// indicador mensual agregado a esta escala.</summary>
    private static async Task<decimal> ObtenerEficienciaAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, int? idEquipo, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var terminados = await (
            from w in contexto.TblWorkItem
            join v in contexto.VwBandejaTrabajo on w.IdWorkItem equals v.IdWorkItem
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && w.MinutosPresupuesto != null
                && (proyectosVisibles == null || proyectosVisibles.Contains(w.IdProyecto))
                && (idEquipo == null || w.IdEquipo == idEquipo)
            select new { Presupuesto = w.MinutosPresupuesto!.Value, Invertido = v.MinutosInvertidos ?? 0 }
            ).ToListAsync(ct);

        if (terminados.Count == 0) return 0;

        var totalInvertido = terminados.Sum(i => i.Invertido);
        if (totalInvertido == 0) return 0;

        return Math.Round(terminados.Sum(i => i.Presupuesto) * 100m / totalInvertido, 1);
    }

    /// <summary>
    /// % de tiempo invertido en items tipo Correccion, acotado a los items Terminados en el
    /// periodo (mismo alcance que Eficiencia). "Reaperturas" queda sin datos, ver nota de la
    /// clase.
    /// </summary>
    private static async Task<RetrabajoResponse> ObtenerRetrabajoAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, int? idEquipo, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var items = await (
            from w in contexto.TblWorkItem
            join v in contexto.VwBandejaTrabajo on w.IdWorkItem equals v.IdWorkItem
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (proyectosVisibles == null || proyectosVisibles.Contains(w.IdProyecto))
                && (idEquipo == null || w.IdEquipo == idEquipo)
            select new { w.IdTipoWorkItem, Invertido = v.MinutosInvertidos ?? 0 }
            ).ToListAsync(ct);

        if (items.Count == 0) return new RetrabajoResponse();

        var totalMinutos = items.Sum(i => i.Invertido);
        if (totalMinutos == 0) return new RetrabajoResponse();

        var minutosCorreccion = items.Where(i => i.IdTipoWorkItem == TipoWorkItemCorreccion).Sum(i => i.Invertido);

        return new RetrabajoResponse { Porcentaje = Math.Round(minutosCorreccion * 100m / totalMinutos, 1) };
    }

    private static async Task<ProductividadResponse> ObtenerProductividadAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, int? idEquipo, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var terminados = await contexto.TblWorkItem
            .Where(w => w.Activo && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && w.PuntosHistoria != null
                && (proyectosVisibles == null || proyectosVisibles.Contains(w.IdProyecto))
                && (idEquipo == null || w.IdEquipo == idEquipo))
            .Select(w => new { w.IdAsignado, Puntos = w.PuntosHistoria!.Value })
            .ToListAsync(ct);

        var personas = terminados.Where(t => t.IdAsignado.HasValue).Select(t => t.IdAsignado!.Value).Distinct().Count();

        return new ProductividadResponse
        {
            PuntosPromedioPorPersona = personas == 0 ? 0 : Math.Round(terminados.Sum(t => t.Puntos) / personas, 1),
            PersonasConsideradas = personas,
        };
    }

    private static async Task<SlaEjecutivoResponse> ObtenerSlaAsync(DbContextGTE contexto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var resueltos = await contexto.TblTicket
            .Where(t => t.Activo && t.FechaResolucion != null && t.FechaResolucion >= desde && t.FechaResolucion <= hasta)
            .Select(t => new { t.IdTicket, t.FechaLimiteResolucion, t.FechaResolucion })
            .ToListAsync(ct);

        var conLimite = resueltos.Where(t => t.FechaLimiteResolucion.HasValue).ToList();
        var dentroDeLimite = conLimite.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion);

        var idsResueltos = resueltos.Select(t => t.IdTicket).ToList();
        var calificaciones = idsResueltos.Count == 0
            ? []
            : await contexto.TblEncuestaSatisfaccion
                .Where(e => idsResueltos.Contains(e.IdTicket))
                .Select(e => (int)e.Calificacion)
                .ToListAsync(ct);

        return new SlaEjecutivoResponse
        {
            CumplimientoPorcentaje = conLimite.Count == 0 ? 100m : Math.Round(dentroDeLimite * 100m / conLimite.Count, 1),
            TicketsConsiderados = conLimite.Count,
            Csat = calificaciones.Count == 0 ? null : Math.Round((decimal)calificaciones.Average(), 1),
            EncuestasConsideradas = calificaciones.Count,
        };
    }

    private async Task<IReadOnlyList<SemaforoProyectoResponse>> ObtenerSemaforoProyectosAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, DateTime desde, DateTime hasta, int anio, CancellationToken ct)
    {
        var proyectos = await contexto.TblProyecto
            .Where(p => p.Activo && (p.IdEstatusProyecto == 2 || p.IdEstatusProyecto == 3 || p.IdEstatusProyecto == 4)
                && (proyectosVisibles == null || proyectosVisibles.Contains(p.IdProyecto)))
            .Select(p => new { p.IdProyecto, p.Clave, p.Nombre })
            .ToListAsync(ct);

        var resultado = new List<SemaforoProyectoResponse>(proyectos.Count);
        foreach (var proyecto in proyectos)
        {
            var terminados = await contexto.TblWorkItem
                .Where(w => w.Activo && w.IdProyecto == proyecto.IdProyecto
                    && w.IdEstatusWorkItem == EstatusWorkItemTerminado
                    && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta)
                .Select(w => new { w.FechaCompromiso, w.FechaFin })
                .ToListAsync(ct);

            var conCompromiso = terminados.Where(t => t.FechaCompromiso.HasValue).ToList();
            var porcentaje = conCompromiso.Count == 0 ? 100m
                : Math.Round(conCompromiso.Count(t => t.FechaFin <= t.FechaCompromiso) * 100m / conCompromiso.Count, 1);

            var costo = await costeo.ObtenerCostoProyectoAsync(proyecto.IdProyecto, anio, ct);

            resultado.Add(new SemaforoProyectoResponse
            {
                IdProyecto = proyecto.IdProyecto,
                Clave = proyecto.Clave,
                Proyecto = proyecto.Nombre,
                Semaforo = CalculadoraIndicadoresEjecutivos.Semaforo(porcentaje),
                EntregaATiempoPorcentaje = porcentaje,
                MontoAutorizado = costo.MontoAutorizado == 0 ? null : costo.MontoAutorizado,
                CostoReal = costo.MontoAutorizado == 0 ? null : costo.CostoReal,
            });
        }

        return resultado;
    }

    private static async Task<IReadOnlyList<KpiPersonalizadoSerieResponse>> ObtenerKpisPersonalizadosAsync(
        DbContextGTE contexto, int anio, CancellationToken ct)
    {
        var definiciones = await contexto.TblKpiDefinicion
            .Where(d => d.Activo)
            .Select(d => new { d.IdKpiDefinicion, d.Clave, d.Nombre, d.Meta, d.Direccion })
            .ToListAsync(ct);

        if (definiciones.Count == 0) return [];

        var idsDefinicion = definiciones.Select(d => d.IdKpiDefinicion).ToList();
        var valores = await contexto.TblKpiValor
            .Where(v => idsDefinicion.Contains(v.IdKpiDefinicion) && v.Alcance == "global" && v.Fecha.Year == anio)
            .OrderBy(v => v.Fecha)
            .Select(v => new { v.IdKpiDefinicion, v.Fecha, v.Valor })
            .ToListAsync(ct);

        return definiciones.Select(d => new KpiPersonalizadoSerieResponse
        {
            Clave = d.Clave,
            Nombre = d.Nombre,
            Meta = d.Meta,
            Direccion = d.Direccion,
            Serie = valores.Where(v => v.IdKpiDefinicion == d.IdKpiDefinicion)
                .Select(v => new PuntoSerieResponse { Fecha = v.Fecha, Valor = v.Valor })
                .ToList(),
        }).ToList();
    }

    private static async Task<IReadOnlyList<RiesgoEjecutivoResponse>> ObtenerTopRiesgosAsync(
        DbContextGTE contexto, List<int>? proyectosVisibles, CancellationToken ct)
    {
        var riesgos = await contexto.TblRiesgo
            .Where(r => r.Activo && r.IdEstatusRiesgo != EstatusRiesgoCerrado
                && (proyectosVisibles == null || proyectosVisibles.Contains(r.IdProyecto)))
            .Select(r => new
            {
                r.IdRiesgo, r.IdProyecto, Proyecto = r.IdProyectoNavigation.Nombre, r.Descripcion,
                r.Probabilidad, r.Impacto, r.Exposicion, Estatus = r.IdEstatusRiesgoNavigation.Descripcion,
            })
            .ToListAsync(ct);

        return riesgos
            .OrderByDescending(r => r.Exposicion ?? (byte)(r.Probabilidad * r.Impacto))
            .Take(TopRiesgos)
            .Select(r => new RiesgoEjecutivoResponse
            {
                IdRiesgo = r.IdRiesgo,
                IdProyecto = r.IdProyecto,
                Proyecto = r.Proyecto,
                Descripcion = r.Descripcion,
                Probabilidad = r.Probabilidad,
                Impacto = r.Impacto,
                Exposicion = r.Exposicion ?? (byte)(r.Probabilidad * r.Impacto),
                Estatus = r.Estatus,
            })
            .ToList();
    }

    private static async Task<BurndownSprintResponse?> ObtenerBurndownSprintActivoAsync(
        DbContextGTE contexto, int idEquipo, CancellationToken ct)
    {
        var sprint = await contexto.TblSprint
            .Where(s => s.Activo && s.IdEquipo == idEquipo && s.IdEstatusSprint == EstatusSprintActivo)
            .Select(s => new { s.IdSprint, s.Nombre, s.IdEquipo, Equipo = s.IdEquipoNavigation.Nombre, s.FechaInicio, s.FechaFin })
            .FirstOrDefaultAsync(ct);

        if (sprint is null) return null;

        var items = await contexto.TblWorkItem
            .Where(w => w.Activo && w.IdSprint == sprint.IdSprint && w.PuntosHistoria != null)
            .Select(w => new { w.PuntosHistoria, w.FechaFin, w.IdEstatusWorkItem })
            .ToListAsync(ct);

        var puntosTotales = items.Sum(i => i.PuntosHistoria!.Value);
        var totalDias = Math.Max(1, (sprint.FechaFin.ToDateTime(TimeOnly.MinValue) - sprint.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days);
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var puntos = new List<PuntoBurndownResponse>();

        for (var fecha = sprint.FechaInicio; fecha <= sprint.FechaFin; fecha = fecha.AddDays(1))
        {
            var diasTranscurridos = (fecha.ToDateTime(TimeOnly.MinValue) - sprint.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days;
            var idealRestante = puntosTotales * (1 - (decimal)diasTranscurridos / totalDias);

            decimal? realRestante = fecha > hoy
                ? null
                : puntosTotales - items
                    .Where(i => i.IdEstatusWorkItem == EstatusWorkItemTerminado
                        && i.FechaFin != null && DateOnly.FromDateTime(i.FechaFin.Value) <= fecha)
                    .Sum(i => i.PuntosHistoria!.Value);

            puntos.Add(new PuntoBurndownResponse
            {
                Fecha = fecha,
                RestanteIdeal = Math.Max(0, idealRestante),
                RestanteReal = realRestante ?? 0,
            });
        }

        return new BurndownSprintResponse
        {
            IdSprint = sprint.IdSprint,
            Nombre = sprint.Nombre,
            IdEquipo = sprint.IdEquipo,
            Equipo = sprint.Equipo,
            FechaInicio = sprint.FechaInicio,
            FechaFin = sprint.FechaFin,
            PuntosTotales = puntosTotales,
            Puntos = puntos,
        };
    }
}
