using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Reportes;
using GTE.Application.Interfaces;
using GTE.Domain.Entregas;
using GTE.Domain.Exceptions;
using GTE.Domain.Operacion;
using GTE.Domain.Planeacion;
using GTE.Domain.Reportes;
using GTE.Domain.Solicitudes;
using GTE.Domain.Soporte;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class ReportesQueryService(FabricaContexto fabrica, ICalendarioLaboral calendario) : IReportesQueryService
{
    /// <summary>IdEstatusAusencia = Aprobada (seed del script 01, sin clase de constantes propia todavia).</summary>
    private const int EstatusAusenciaAprobada = 2;

    /// <summary>IdEstatusRiesgo (seed del script 01: 1 Identificado, 2 En Mitigacion, 3 Materializado, 4 Cerrado).</summary>
    private const int EstatusRiesgoIdentificado = 1;
    private const int EstatusRiesgoEnMitigacion = 2;
    private const int EstatusRiesgoMaterializado = 3;
    private const int EstatusRiesgoCerrado = 4;

    public async Task<ActividadUsuarioResponse> ObtenerActividadUsuarioAsync(
        int idUsuario, DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var nombreUsuario = await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => u.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("usuario", idUsuario);

        var registros = await (
            from t in contexto.TblRegistroTiempo.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on t.IdWorkItem equals w.IdWorkItem
            where t.IdUsuario == idUsuario && t.Activo
                && t.Fecha >= fechaInicio && t.Fecha <= fechaFin
            orderby t.Fecha descending, t.IdRegistroTiempo descending
            select new
            {
                t.Fecha,
                Detalle = new ActividadDetalleResponse
                {
                    IdRegistroTiempo = t.IdRegistroTiempo,
                    IdWorkItem = t.IdWorkItem,
                    Folio = w.Folio,
                    Titulo = w.Titulo,
                    Minutos = t.Minutos,
                    Descripcion = t.Descripcion
                }
            })
            .ToListAsync(cancellationToken);

        var dias = registros
            .GroupBy(r => r.Fecha)
            .OrderByDescending(g => g.Key)
            .Select(g => new ActividadDiaResponse
            {
                Fecha = g.Key,
                MinutosDia = g.Sum(r => r.Detalle.Minutos),
                Registros = g.Select(r => r.Detalle).ToList()
            })
            .ToList();

        return new ActividadUsuarioResponse
        {
            IdUsuario = idUsuario,
            Usuario = nombreUsuario,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            MinutosTotales = dias.Sum(d => d.MinutosDia),
            Dias = dias
        };
    }

    /// <summary>R01: items terminados, puntos, % a tiempo y eficiencia por persona.</summary>
    public async Task<ProductividadReporteResponse> ObtenerProductividadAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, int? idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var items = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join v in contexto.VwBandejaTrabajo.AsNoTracking() on w.IdWorkItem equals v.IdWorkItem
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItem.Terminado
                && w.FechaFin != null && w.FechaFin >= inicio && w.FechaFin <= fin
                && w.IdAsignado != null
                && (idProyecto == null || w.IdProyecto == idProyecto)
                && (idEquipo == null || w.IdEquipo == idEquipo)
            select new
            {
                IdUsuario = w.IdAsignado!.Value, Usuario = w.IdAsignadoNavigation!.Nombre,
                w.FechaCompromiso, w.FechaFin, Puntos = w.PuntosHistoria ?? 0,
                Presupuesto = w.MinutosPresupuesto ?? 0, Invertido = v.MinutosInvertidos ?? 0,
            }).ToListAsync(cancellationToken);

        var personas = items
            .GroupBy(i => new { i.IdUsuario, i.Usuario })
            .Select(g =>
            {
                var conCompromiso = g.Where(i => i.FechaCompromiso.HasValue).ToList();
                var aTiempo = conCompromiso.Count(i => i.FechaFin <= i.FechaCompromiso);
                var totalInvertido = g.Sum(i => i.Invertido);
                var totalPresupuesto = g.Sum(i => i.Presupuesto);
                return new ProductividadPersonaResponse
                {
                    IdUsuario = g.Key.IdUsuario,
                    Usuario = g.Key.Usuario,
                    ItemsTerminados = g.Count(),
                    PuntosTotales = g.Sum(i => i.Puntos),
                    PorcentajeATiempo = conCompromiso.Count == 0 ? 100m : Math.Round(aTiempo * 100m / conCompromiso.Count, 1),
                    EficienciaPorcentaje = totalInvertido == 0 ? null : Math.Round(totalPresupuesto * 100m / totalInvertido, 1),
                };
            })
            .OrderByDescending(p => p.ItemsTerminados)
            .ToList();

        return new ProductividadReporteResponse { Desde = desde, Hasta = hasta, Personas = personas };
    }

    /// <summary>R02: pivote de horas registradas persona x dia, marcando ausencias aprobadas.</summary>
    public async Task<HorasRegistradasReporteResponse> ObtenerHorasRegistradasAsync(
        DateOnly desde, DateOnly hasta, int? idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var idsUsuariosEquipo = idEquipo == null
            ? null
            : await contexto.TblEquipoMiembro.AsNoTracking()
                .Where(m => m.IdEquipo == idEquipo && m.Activo)
                .Select(m => m.IdUsuario)
                .ToListAsync(cancellationToken);

        var registros = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(t => t.Activo && t.Fecha >= desde && t.Fecha <= hasta
                && (idsUsuariosEquipo == null || idsUsuariosEquipo.Contains(t.IdUsuario)))
            .Select(t => new { t.IdUsuario, Usuario = t.IdUsuarioNavigation.Nombre, t.Fecha, t.Minutos })
            .ToListAsync(cancellationToken);

        var ausencias = await contexto.TblAusencia.AsNoTracking()
            .Where(a => a.Activo && a.IdEstatusAusencia == EstatusAusenciaAprobada
                && a.FechaInicio <= hasta && a.FechaFin >= desde
                && (idsUsuariosEquipo == null || idsUsuariosEquipo.Contains(a.IdUsuario)))
            .Select(a => new { a.IdUsuario, a.FechaInicio, a.FechaFin })
            .ToListAsync(cancellationToken);

        bool EsAusencia(int idUsuario, DateOnly fecha) => ausencias
            .Any(a => a.IdUsuario == idUsuario && fecha >= a.FechaInicio && fecha <= a.FechaFin);

        var usuarios = registros
            .GroupBy(r => new { r.IdUsuario, r.Usuario })
            .Select(g => new HorasUsuarioResponse
            {
                IdUsuario = g.Key.IdUsuario,
                Usuario = g.Key.Usuario,
                MinutosTotales = g.Sum(r => r.Minutos),
                Dias = g.GroupBy(r => r.Fecha)
                    .OrderBy(d => d.Key)
                    .Select(d => new HorasDiaResponse
                    {
                        Fecha = d.Key,
                        Minutos = d.Sum(r => r.Minutos),
                        EsAusencia = EsAusencia(g.Key.IdUsuario, d.Key),
                    })
                    .ToList(),
            })
            .OrderByDescending(u => u.MinutosTotales)
            .ToList();

        return new HorasRegistradasReporteResponse { Desde = desde, Hasta = hasta, Usuarios = usuarios };
    }

    /// <summary>R03: % de tiempo invertido en items tipo Correccion, por persona/proyecto.</summary>
    public async Task<RetrabajoReporteResponse> ObtenerRetrabajoAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var items = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join v in contexto.VwBandejaTrabajo.AsNoTracking() on w.IdWorkItem equals v.IdWorkItem
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItem.Terminado
                && w.FechaFin != null && w.FechaFin >= inicio && w.FechaFin <= fin
                && w.IdAsignado != null
                && (idProyecto == null || w.IdProyecto == idProyecto)
            select new
            {
                IdUsuario = w.IdAsignado!.Value, Usuario = w.IdAsignadoNavigation!.Nombre,
                w.IdProyecto, Proyecto = w.IdProyectoNavigation.Nombre,
                w.IdTipoWorkItem, Invertido = v.MinutosInvertidos ?? 0,
            }).ToListAsync(cancellationToken);

        var detalle = items
            .GroupBy(i => new { i.IdUsuario, i.Usuario, i.IdProyecto, i.Proyecto })
            .Select(g =>
            {
                var totalMinutos = g.Sum(i => i.Invertido);
                var minutosCorreccion = g.Where(i => i.IdTipoWorkItem == TiposWorkItem.Correccion).Sum(i => i.Invertido);
                return new RetrabajoDetalleResponse
                {
                    IdUsuario = g.Key.IdUsuario,
                    Usuario = g.Key.Usuario,
                    IdProyecto = g.Key.IdProyecto,
                    Proyecto = g.Key.Proyecto,
                    MinutosCorreccion = minutosCorreccion,
                    MinutosTotales = totalMinutos,
                    Porcentaje = totalMinutos == 0 ? 0 : Math.Round(minutosCorreccion * 100m / totalMinutos, 1),
                };
            })
            .OrderByDescending(d => d.Porcentaje)
            .ToList();

        return new RetrabajoReporteResponse { Desde = desde, Hasta = hasta, Detalle = detalle };
    }

    /// <summary>R04: densidad, aging y tasa de escape a produccion de bugs por proyecto.</summary>
    public async Task<BugsDefectosReporteResponse> ObtenerBugsDefectosAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var proyectos = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.Activo && (idProyecto == null || p.IdProyecto == idProyecto))
            .Select(p => new { p.IdProyecto, p.Nombre })
            .ToListAsync(cancellationToken);

        var idsEscapados = await contexto.TblIncidente.AsNoTracking()
            .Where(i => i.IdWorkItemCorrectivo != null)
            .Select(i => i.IdWorkItemCorrectivo!.Value)
            .ToListAsync(cancellationToken);

        var resultado = new List<BugsProyectoResponse>();
        foreach (var proyecto in proyectos)
        {
            var totalItems = await contexto.TblWorkItem.AsNoTracking()
                .CountAsync(w => w.Activo && w.IdProyecto == proyecto.IdProyecto
                    && w.FechaRegistro >= inicio && w.FechaRegistro <= fin, cancellationToken);

            var bugs = await contexto.TblWorkItem.AsNoTracking()
                .Where(w => w.Activo && w.IdProyecto == proyecto.IdProyecto
                    && (w.IdTipoWorkItem == TiposWorkItem.Bug || w.IdTipoWorkItem == TiposWorkItem.Correccion)
                    && w.FechaRegistro >= inicio && w.FechaRegistro <= fin)
                .Select(w => new { w.IdWorkItem, w.FechaRegistro, w.FechaFin })
                .ToListAsync(cancellationToken);

            if (bugs.Count == 0 && totalItems == 0) continue;

            var escapados = bugs.Count(b => idsEscapados.Contains(b.IdWorkItem));
            var hoy = DateTime.Now;
            var agingPromedio = bugs.Count == 0 ? 0
                : bugs.Average(b => ((b.FechaFin ?? hoy) - b.FechaRegistro).TotalDays);

            resultado.Add(new BugsProyectoResponse
            {
                IdProyecto = proyecto.IdProyecto,
                Proyecto = proyecto.Nombre,
                TotalBugs = bugs.Count,
                TotalItems = totalItems,
                DensidadPorcentaje = totalItems == 0 ? 0 : Math.Round(bugs.Count * 100m / totalItems, 1),
                AgingPromedioDias = Math.Round((decimal)agingPromedio, 1),
                Escapados = escapados,
                TasaEscapePorcentaje = bugs.Count == 0 ? 0 : Math.Round(escapados * 100m / bugs.Count, 1),
            });
        }

        return new BugsDefectosReporteResponse { Desde = desde, Hasta = hasta, Proyectos = resultado };
    }

    /// <summary>R05: historial de releases, tiempos de aprobacion y frecuencia.</summary>
    public async Task<ReleasesReporteResponse> ObtenerReleasesAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var releases = await contexto.TblRelease.AsNoTracking()
            .Where(r => r.Activo && r.FechaRegistro >= inicio && r.FechaRegistro <= fin
                && (idProyecto == null || r.IdProyecto == idProyecto))
            .Select(r => new
            {
                r.IdRelease, r.Folio, r.Version, r.IdProyecto, Proyecto = r.IdProyectoNavigation.Nombre,
                r.FechaLiberacion, r.FechaRegistro,
                ItemsIncluidos = contexto.TblWorkItem.Count(w => w.IdRelease == r.IdRelease),
            })
            .ToListAsync(cancellationToken);

        var ids = releases.Select(r => r.IdRelease).ToList();
        var aprobaciones = ids.Count == 0
            ? []
            : await contexto.TblHistorialEstatus.AsNoTracking()
                .Where(h => h.Proceso == "Release" && ids.Contains(h.IdRegistro) && h.IdEstatus == EstatusRelease.Aprobado)
                .GroupBy(h => h.IdRegistro)
                .Select(g => new { IdRelease = g.Key, Fecha = g.Min(h => h.FechaInicio) })
                .ToListAsync(cancellationToken);

        var items = releases.Select(r =>
        {
            var aprobacion = aprobaciones.FirstOrDefault(a => a.IdRelease == r.IdRelease);
            return new ReleaseReporteItemResponse
            {
                IdRelease = r.IdRelease,
                Folio = r.Folio,
                Version = r.Version,
                IdProyecto = r.IdProyecto,
                Proyecto = r.Proyecto,
                FechaLiberacion = r.FechaLiberacion,
                DiasAprobacion = aprobacion == null ? null : Math.Round((decimal)(aprobacion.Fecha - r.FechaRegistro).TotalDays, 1),
                ItemsIncluidos = r.ItemsIncluidos,
            };
        }).OrderByDescending(r => r.FechaLiberacion).ToList();

        var diasPeriodo = Math.Max(1, (fin.Date - inicio.Date).Days + 1);
        return new ReleasesReporteResponse
        {
            Desde = desde, Hasta = hasta, TotalReleases = items.Count,
            FrecuenciaPorSemana = Math.Round(items.Count / (diasPeriodo / 7m), 2),
            Releases = items,
        };
    }

    /// <summary>R06: matriz completa de riesgos (tblRiesgo) por estatus.</summary>
    public async Task<RiesgosReporteResponse> ObtenerRiesgosAsync(int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var riesgos = await contexto.TblRiesgo.AsNoTracking()
            .Where(r => r.Activo && (idProyecto == null || r.IdProyecto == idProyecto))
            .Select(r => new RiesgoReporteItemResponse
            {
                IdRiesgo = r.IdRiesgo,
                IdProyecto = r.IdProyecto,
                Proyecto = r.IdProyectoNavigation.Nombre,
                Descripcion = r.Descripcion,
                Probabilidad = r.Probabilidad,
                Impacto = r.Impacto,
                Exposicion = r.Exposicion ?? (byte)(r.Probabilidad * r.Impacto),
                Estatus = r.IdEstatusRiesgoNavigation.Descripcion,
            })
            .ToListAsync(cancellationToken);

        var porEstatus = await contexto.TblRiesgo.AsNoTracking()
            .Where(r => r.Activo && (idProyecto == null || r.IdProyecto == idProyecto))
            .GroupBy(r => r.IdEstatusRiesgo)
            .Select(g => new { IdEstatusRiesgo = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        int Contar(int estatus) => porEstatus.FirstOrDefault(p => p.IdEstatusRiesgo == estatus)?.Total ?? 0;

        return new RiesgosReporteResponse
        {
            TotalExpuestos = Contar(EstatusRiesgoIdentificado),
            TotalMitigados = Contar(EstatusRiesgoEnMitigacion),
            TotalMaterializados = Contar(EstatusRiesgoMaterializado),
            TotalCerrados = Contar(EstatusRiesgoCerrado),
            Riesgos = riesgos.OrderByDescending(r => r.Exposicion).ToList(),
        };
    }

    /// <summary>R07: solicitudes por area con tiempos de triage y entrega. Satisfaccion sin datos (sin encuesta ligada a Solicitud).</summary>
    public async Task<SolicitantesReporteResponse> ObtenerSolicitantesAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var solicitudes = await (
            from s in contexto.TblSolicitud.AsNoTracking()
            where s.Activo && s.FechaRegistro >= inicio && s.FechaRegistro <= fin
            select new
            {
                s.IdSolicitud, s.FechaRegistro,
                Area = s.IdSolicitanteNavigation.IdPuestoNavigation != null
                    ? s.IdSolicitanteNavigation.IdPuestoNavigation.IdAreaNavigation!.Nombre
                    : "Sin area",
            }).ToListAsync(cancellationToken);

        var ids = solicitudes.Select(s => s.IdSolicitud).ToList();
        var historial = ids.Count == 0
            ? []
            : await contexto.TblHistorialEstatus.AsNoTracking()
                .Where(h => h.Proceso == "Solicitud" && ids.Contains(h.IdRegistro)
                    && (h.IdEstatus == EstatusSolicitud.EnAnalisis || h.IdEstatus == EstatusSolicitud.Aprobada
                        || h.IdEstatus == EstatusSolicitud.Convertida))
                .Select(h => new { h.IdRegistro, h.IdEstatus, h.FechaInicio })
                .ToListAsync(cancellationToken);

        var porArea = solicitudes
            .GroupBy(s => s.Area)
            .Select(g =>
            {
                var triage = g.Select(s =>
                    {
                        var primeraTransicion = historial
                            .Where(h => h.IdRegistro == s.IdSolicitud
                                && (h.IdEstatus == EstatusSolicitud.EnAnalisis || h.IdEstatus == EstatusSolicitud.Aprobada))
                            .OrderBy(h => h.FechaInicio).FirstOrDefault();
                        return primeraTransicion == null ? (double?)null : (primeraTransicion.FechaInicio - s.FechaRegistro).TotalDays;
                    })
                    .Where(d => d.HasValue).Select(d => d!.Value).ToList();

                var entrega = g.Select(s =>
                    {
                        var conversion = historial.FirstOrDefault(h => h.IdRegistro == s.IdSolicitud && h.IdEstatus == EstatusSolicitud.Convertida);
                        return conversion == null ? (double?)null : (conversion.FechaInicio - s.FechaRegistro).TotalDays;
                    })
                    .Where(d => d.HasValue).Select(d => d!.Value).ToList();

                return new SolicitudesAreaResponse
                {
                    Area = g.Key,
                    TotalSolicitudes = g.Count(),
                    TiempoTriagePromedioDias = triage.Count == 0 ? null : Math.Round((decimal)triage.Average(), 1),
                    TiempoEntregaPromedioDias = entrega.Count == 0 ? null : Math.Round((decimal)entrega.Average(), 1),
                };
            })
            .OrderByDescending(a => a.TotalSolicitudes)
            .ToList();

        return new SolicitantesReporteResponse { Desde = desde, Hasta = hasta, PorArea = porArea };
    }

    /// <summary>R08: costo real (horas x tarifa, vwCostoRegistroTiempo) por proyecto/mes y por desarrollador.</summary>
    public async Task<CostosReporteResponse> ObtenerCostosAsync(
        int anio, int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var desde = new DateOnly(anio, 1, 1);
        var hasta = new DateOnly(anio, 12, 31);

        var registros = await (
            from c in contexto.VwCostoRegistroTiempo.AsNoTracking()
            join p in contexto.TblProyecto.AsNoTracking() on c.IdProyecto equals p.IdProyecto
            join u in contexto.TblUsuario.AsNoTracking() on c.IdUsuario equals u.IdUsuario
            where c.Fecha >= desde && c.Fecha <= hasta && (idProyecto == null || c.IdProyecto == idProyecto)
            select new { c.IdProyecto, Proyecto = p.Nombre, c.IdUsuario, Usuario = u.Nombre, c.Fecha, c.Minutos, c.Costo }
            ).ToListAsync(cancellationToken);

        var porProyectoMes = registros
            .GroupBy(r => new { r.IdProyecto, r.Proyecto, Mes = r.Fecha.Month })
            .Select(g => new CostoProyectoMesResponse
            {
                IdProyecto = g.Key.IdProyecto, Proyecto = g.Key.Proyecto, Mes = g.Key.Mes,
                HorasReales = Math.Round(g.Sum(r => r.Minutos) / 60m, 1),
                CostoReal = Math.Round(g.Sum(r => r.Costo), 2),
            })
            .OrderBy(c => c.Proyecto).ThenBy(c => c.Mes)
            .ToList();

        var porDesarrollador = registros
            .GroupBy(r => new { r.IdUsuario, r.Usuario })
            .Select(g => new CostoDesarrolladorResponse
            {
                IdUsuario = g.Key.IdUsuario, Usuario = g.Key.Usuario,
                HorasReales = Math.Round(g.Sum(r => r.Minutos) / 60m, 1),
                CostoReal = Math.Round(g.Sum(r => r.Costo), 2),
            })
            .OrderByDescending(c => c.CostoReal)
            .ToList();

        return new CostosReporteResponse { Anio = anio, PorProyectoMes = porProyectoMes, PorDesarrollador = porDesarrollador };
    }

    /// <summary>R09: presupuesto autorizado vs costo real acumulado por proyecto.</summary>
    public async Task<RentabilidadReporteResponse> ObtenerRentabilidadAsync(int anio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var desde = new DateOnly(anio, 1, 1);
        var hasta = new DateOnly(anio, 12, 31);

        var presupuestos = await contexto.TblPresupuestoProyecto.AsNoTracking()
            .Where(p => p.Activo && p.Anio == anio)
            .Select(p => new { p.IdProyecto, Proyecto = p.IdProyectoNavigation.Nombre, p.MontoAutorizado })
            .ToListAsync(cancellationToken);

        var costos = await contexto.VwCostoRegistroTiempo.AsNoTracking()
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta)
            .GroupBy(c => c.IdProyecto)
            .Select(g => new { IdProyecto = g.Key, Costo = g.Sum(c => c.Costo) })
            .ToListAsync(cancellationToken);

        var idsProyectos = presupuestos.Select(p => p.IdProyecto)
            .Union(costos.Select(c => c.IdProyecto))
            .Distinct()
            .ToList();

        var nombresFaltantes = idsProyectos.Except(presupuestos.Select(p => p.IdProyecto)).ToList();
        var nombres = nombresFaltantes.Count == 0
            ? []
            : await contexto.TblProyecto.AsNoTracking()
                .Where(p => nombresFaltantes.Contains(p.IdProyecto))
                .Select(p => new { p.IdProyecto, p.Nombre })
                .ToListAsync(cancellationToken);

        var proyectos = idsProyectos.Select(id =>
        {
            var presupuesto = presupuestos.FirstOrDefault(p => p.IdProyecto == id);
            var costo = costos.FirstOrDefault(c => c.IdProyecto == id)?.Costo ?? 0;
            var nombre = presupuesto?.Proyecto ?? nombres.First(n => n.IdProyecto == id).Nombre;
            decimal? porcentaje = presupuesto == null || presupuesto.MontoAutorizado == 0
                ? null
                : Math.Round(costo * 100m / presupuesto.MontoAutorizado, 1);

            return new RentabilidadProyectoResponse
            {
                IdProyecto = id,
                Proyecto = nombre,
                MontoAutorizado = presupuesto?.MontoAutorizado,
                CostoReal = Math.Round(costo, 2),
                PorcentajeConsumido = porcentaje,
                Semaforo = SemaforoConsumo(porcentaje),
            };
        }).OrderByDescending(p => p.CostoReal).ToList();

        return new RentabilidadReporteResponse { Anio = anio, Proyectos = proyectos };
    }

    /// <summary>Semaforo de consumo de presupuesto (a menor consumo, mejor -- inverso al semaforo de entrega a tiempo del Dashboard P18).</summary>
    private static string SemaforoConsumo(decimal? porcentajeConsumido) => porcentajeConsumido switch
    {
        null => "s/d",
        <= 90 => "Verde",
        <= 100 => "Naranja",
        _ => "Rojo",
    };

    /// <summary>R10: cumplimiento de SLA por prioridad y por agente, con CSAT (transversal, sin filtro de proyecto -- tblTicket no tiene esa FK).</summary>
    public async Task<SlaReporteResponse> ObtenerSlaAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        var resueltos = await contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.FechaResolucion != null && t.FechaResolucion >= inicio && t.FechaResolucion <= fin)
            .Select(t => new
            {
                t.IdTicket, t.FechaLimiteResolucion, t.FechaResolucion,
                Prioridad = t.IdPrioridadNavigation.Nombre, t.IdAsignado,
                Agente = t.IdAsignadoNavigation != null ? t.IdAsignadoNavigation.Nombre : null,
            })
            .ToListAsync(cancellationToken);

        var conLimite = resueltos.Where(t => t.FechaLimiteResolucion.HasValue).ToList();

        var porPrioridad = conLimite
            .GroupBy(t => t.Prioridad)
            .Select(g => new SlaPrioridadResponse
            {
                Prioridad = g.Key,
                TotalTickets = g.Count(),
                DentroSla = g.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion),
                CumplimientoPorcentaje = Math.Round(g.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion) * 100m / g.Count(), 1),
            })
            .OrderBy(p => p.Prioridad)
            .ToList();

        var porAgente = conLimite
            .Where(t => t.IdAsignado.HasValue)
            .GroupBy(t => new { IdUsuario = t.IdAsignado!.Value, Agente = t.Agente! })
            .Select(g => new SlaAgenteResponse
            {
                IdUsuario = g.Key.IdUsuario,
                Usuario = g.Key.Agente,
                TotalTickets = g.Count(),
                DentroSla = g.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion),
                CumplimientoPorcentaje = Math.Round(g.Count(t => t.FechaResolucion <= t.FechaLimiteResolucion) * 100m / g.Count(), 1),
                Incumplimientos = g.Count(t => t.FechaResolucion > t.FechaLimiteResolucion),
            })
            .OrderByDescending(a => a.TotalTickets)
            .ToList();

        var idsResueltos = resueltos.Select(t => t.IdTicket).ToList();
        var calificaciones = idsResueltos.Count == 0
            ? []
            : await contexto.TblEncuestaSatisfaccion.AsNoTracking()
                .Where(e => idsResueltos.Contains(e.IdTicket))
                .Select(e => (int)e.Calificacion)
                .ToListAsync(cancellationToken);

        return new SlaReporteResponse
        {
            Desde = desde, Hasta = hasta,
            Csat = calificaciones.Count == 0 ? null : Math.Round((decimal)calificaciones.Average(), 1),
            PorPrioridad = porPrioridad,
            PorAgente = porAgente,
        };
    }

    /// <summary>R11: series historicas de tblKpiValor con comparativo entre dos anios.</summary>
    public async Task<KpisHistoricosReporteResponse> ObtenerKpisHistoricosAsync(
        int anio, int? anioComparativo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var definiciones = await contexto.TblKpiDefinicion.AsNoTracking()
            .Where(d => d.Activo)
            .Select(d => new { d.IdKpiDefinicion, d.Clave, d.Nombre })
            .ToListAsync(cancellationToken);

        if (definiciones.Count == 0)
        {
            return new KpisHistoricosReporteResponse { Anio = anio, AnioComparativo = anioComparativo, Kpis = [] };
        }

        var idsDefinicion = definiciones.Select(d => d.IdKpiDefinicion).ToList();
        var aniosConsultados = anioComparativo.HasValue ? new[] { anio, anioComparativo.Value } : [anio];
        var valores = await contexto.TblKpiValor.AsNoTracking()
            .Where(v => idsDefinicion.Contains(v.IdKpiDefinicion) && v.Alcance == "global" && aniosConsultados.Contains(v.Fecha.Year))
            .OrderBy(v => v.Fecha)
            .Select(v => new { v.IdKpiDefinicion, v.Fecha, v.Valor })
            .ToListAsync(cancellationToken);

        var kpis = definiciones.Select(d => new KpiHistoricoResponse
        {
            Clave = d.Clave,
            Nombre = d.Nombre,
            SerieAnioActual = valores.Where(v => v.IdKpiDefinicion == d.IdKpiDefinicion && v.Fecha.Year == anio)
                .Select(v => new PuntoKpiResponse { Fecha = v.Fecha, Valor = v.Valor }).ToList(),
            SerieAnioComparativo = !anioComparativo.HasValue ? []
                : valores.Where(v => v.IdKpiDefinicion == d.IdKpiDefinicion && v.Fecha.Year == anioComparativo)
                    .Select(v => new PuntoKpiResponse { Fecha = v.Fecha, Valor = v.Valor }).ToList(),
        }).ToList();

        return new KpisHistoricosReporteResponse { Anio = anio, AnioComparativo = anioComparativo, Kpis = kpis };
    }

    /// <summary>R12: WIP por persona y % de ocupacion vs capacidad de sprint activo.</summary>
    public async Task<CargaTrabajoReporteResponse> ObtenerCargaTrabajoAsync(int? idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var idsUsuariosEquipo = idEquipo == null
            ? null
            : await contexto.TblEquipoMiembro.AsNoTracking()
                .Where(m => m.IdEquipo == idEquipo && m.Activo)
                .Select(m => m.IdUsuario)
                .ToListAsync(cancellationToken);

        var wip = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null
                && (w.IdEstatusWorkItem == EstatusWorkItem.EnProceso || w.IdEstatusWorkItem == EstatusWorkItem.EnPruebas
                    || w.IdEstatusWorkItem == EstatusWorkItem.Correccion)
                && (idsUsuariosEquipo == null || idsUsuariosEquipo.Contains(w.IdAsignado!.Value)))
            .GroupBy(w => new { IdUsuario = w.IdAsignado!.Value, Usuario = w.IdAsignadoNavigation!.Nombre })
            .Select(g => new { g.Key.IdUsuario, g.Key.Usuario, Wip = g.Count() })
            .ToListAsync(cancellationToken);

        var capacidades = await (
            from cs in contexto.TblCapacidadSprint.AsNoTracking()
            join s in contexto.TblSprint.AsNoTracking() on cs.IdSprint equals s.IdSprint
            where s.Activo && s.IdEstatusSprint == EstatusSprint.Activo
            select new { cs.IdUsuario, cs.HorasPorDia, cs.PorcentajeDedicacion, s.FechaInicio, s.FechaFin }
            ).ToListAsync(cancellationToken);

        var personas = wip.Select(w =>
        {
            var capacidad = capacidades.FirstOrDefault(c => c.IdUsuario == w.IdUsuario);
            decimal? ocupacion = null;
            if (capacidad != null)
            {
                var diasSprint = Math.Max(1, (capacidad.FechaFin.ToDateTime(TimeOnly.MinValue) - capacidad.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days);
                var capacidadHoras = capacidad.HorasPorDia * (capacidad.PorcentajeDedicacion / 100m) * diasSprint;
                ocupacion = capacidadHoras == 0 ? null : Math.Round(w.Wip * 100m / capacidadHoras, 1);
            }
            return new CargaTrabajoPersonaResponse
            {
                IdUsuario = w.IdUsuario, Usuario = w.Usuario, Wip = w.Wip, PorcentajeOcupacion = ocupacion,
            };
        }).OrderByDescending(p => p.Wip).ToList();

        return new CargaTrabajoReporteResponse { Personas = personas };
    }

    /// <summary>R13: diagrama de flujo acumulado (CFD) de un proyecto, dia por dia.</summary>
    public async Task<FlujoReporteResponse> ObtenerFlujoAsync(
        DateOnly desde, DateOnly hasta, int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var nombreProyecto = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("proyecto", idProyecto);

        var estatusCatalogo = await contexto.TblEstatusWorkItem.AsNoTracking()
            .OrderBy(e => e.Orden)
            .Select(e => new { e.Id, e.Descripcion })
            .ToListAsync(cancellationToken);

        var items = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdProyecto == idProyecto && w.FechaRegistro <= hasta.ToDateTime(TimeOnly.MaxValue))
            .Select(w => new { w.IdWorkItem, w.FechaRegistro })
            .ToListAsync(cancellationToken);

        var idsItems = items.Select(i => i.IdWorkItem).ToList();
        var transiciones = idsItems.Count == 0
            ? []
            : await contexto.TblHistorialEstatus.AsNoTracking()
                .Where(h => h.Proceso == "WorkItem" && idsItems.Contains(h.IdRegistro) && h.FechaInicio <= hasta.ToDateTime(TimeOnly.MaxValue))
                .OrderBy(h => h.FechaInicio)
                .Select(h => new { h.IdRegistro, h.IdEstatus, h.FechaInicio })
                .ToListAsync(cancellationToken);

        var lineaTiempo = items.ToDictionary(
            i => i.IdWorkItem,
            i => new List<(DateTime Fecha, int Estatus)> { (i.FechaRegistro, EstatusWorkItem.Pendiente) });
        foreach (var t in transiciones)
        {
            lineaTiempo[t.IdRegistro].Add((t.FechaInicio, t.IdEstatus));
        }

        var puntos = new List<PuntoFlujoResponse>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            var finDia = fecha.ToDateTime(TimeOnly.MaxValue);
            var conteo = new Dictionary<string, int>();
            foreach (var (idItem, eventos) in lineaTiempo)
            {
                var vigente = eventos.Where(e => e.Fecha <= finDia).OrderBy(e => e.Fecha).LastOrDefault();
                if (vigente == default) continue;

                var nombreEstatus = estatusCatalogo.FirstOrDefault(e => e.Id == vigente.Estatus)?.Descripcion ?? "Desconocido";
                conteo[nombreEstatus] = conteo.GetValueOrDefault(nombreEstatus, 0) + 1;
            }
            puntos.Add(new PuntoFlujoResponse { Fecha = fecha, ConteoPorEstatus = conteo });
        }

        return new FlujoReporteResponse
        {
            IdProyecto = idProyecto, Proyecto = nombreProyecto, Desde = desde, Hasta = hasta,
            Estatus = estatusCatalogo.Select(e => e.Descripcion).ToList(),
            Puntos = puntos,
        };
    }

    /// <summary>R14: movimientos de bitacora (tblBitacora) por usuario/entidad/rango, paginado.</summary>
    public async Task<PagedResult<AuditoriaItemResponse>> ObtenerAuditoriaAsync(
        DateOnly? desde, DateOnly? hasta, string? usuario, string? entidad,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = contexto.TblBitacora.AsNoTracking().AsQueryable();
        if (desde.HasValue) consulta = consulta.Where(b => b.Fecha >= desde.Value.ToDateTime(TimeOnly.MinValue));
        if (hasta.HasValue) consulta = consulta.Where(b => b.Fecha <= hasta.Value.ToDateTime(TimeOnly.MaxValue));
        if (!string.IsNullOrWhiteSpace(usuario)) consulta = consulta.Where(b => b.Usuario.Contains(usuario));
        if (!string.IsNullOrWhiteSpace(entidad)) consulta = consulta.Where(b => b.Entidad == entidad);

        var total = await consulta.CountAsync(cancellationToken);
        var items = await consulta
            .OrderByDescending(b => b.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AuditoriaItemResponse
            {
                IdBitacora = b.IdBitacora, Usuario = b.Usuario, Entidad = b.Entidad, IdEntidad = b.IdEntidad,
                Accion = b.Accion, Detalle = b.Detalle, Fecha = b.Fecha,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditoriaItemResponse> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    private static (DateTime Inicio, DateTime Fin) RangoFechas(DateOnly desde, DateOnly hasta)
        => (desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue));

    /// <summary>
    /// R15: detalle de work items Terminados en el rango (por FechaFin). A diferencia de R01-R03,
    /// que agregan por persona o proyecto, este devuelve el renglon por actividad, que es lo que
    /// se entrega como evidencia de trabajo del periodo.
    /// </summary>
    public async Task<ActividadesTerminadasReporteResponse> ObtenerActividadesTerminadasAsync(
        DateOnly desde, DateOnly hasta, int? idEquipo, int? idAsignado, int? idProyecto,
        int? idTipoWorkItem, string? folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);
        var folioFiltro = string.IsNullOrWhiteSpace(folio) ? null : folio.Trim();

        // TRAMPA EF: se unen entidades sin proyectar, se filtra y ordena por columnas reales, y
        // se proyecta al final (mismo patron que PlaneacionQueryService.ConsultaBase).
        // Tiempo capturado a mano, agregado por item. Va como LEFT JOIN contra el agregado (no
        // como subconsulta por renglon) para que sean dos escaneos y no uno por fila.
        var registrado = contexto.TblRegistroTiempo.AsNoTracking()
            .Where(t => t.Activo)
            .GroupBy(t => t.IdWorkItem)
            // TRAMPA EF: Minutos va como int? a proposito. En el LEFT JOIN, SUM() devuelve NULL
            // para los items sin registros, y leer ese NULL en un int no nullable revienta al
            // MATERIALIZAR con "Nullable object must have a value" (la consulta traduce bien; el
            // error aparece hasta que se leen las filas).
            .Select(g => new { IdWorkItem = g.Key, Minutos = (int?)g.Sum(t => t.Minutos) });

        var consulta =
            from w in contexto.TblWorkItem.AsNoTracking()
            join v in contexto.VwBandejaTrabajo.AsNoTracking() on w.IdWorkItem equals v.IdWorkItem
            join r in registrado on w.IdWorkItem equals r.IdWorkItem into registrosItem
            from r in registrosItem.DefaultIfEmpty()
            where w.Activo && w.IdEstatusWorkItem == EstatusWorkItem.Terminado
                && w.FechaFin != null && w.FechaFin >= inicio && w.FechaFin <= fin
                && (idEquipo == null || w.IdEquipo == idEquipo)
                && (idAsignado == null || w.IdAsignado == idAsignado)
                && (idProyecto == null || w.IdProyecto == idProyecto)
                && (idTipoWorkItem == null || w.IdTipoWorkItem == idTipoWorkItem)
                && (folioFiltro == null || w.Folio.Contains(folioFiltro))
            orderby w.FechaFin descending, w.IdWorkItem descending
            select new
            {
                w.IdWorkItem, w.Folio, w.Titulo, w.Descripcion,
                Tipo = w.IdTipoWorkItemNavigation.Nombre,
                Proyecto = w.IdProyectoNavigation.Nombre,
                Equipo = w.IdEquipoNavigation != null ? w.IdEquipoNavigation.Nombre : null,
                Asignado = w.IdAsignadoNavigation != null ? w.IdAsignadoNavigation.Nombre : null,
                IdHorarioAsignado = w.IdAsignadoNavigation != null ? w.IdAsignadoNavigation.IdHorario : null,
                Prioridad = w.IdPrioridadNavigation.Nombre,
                Sprint = w.IdSprintNavigation != null ? w.IdSprintNavigation.Nombre : null,
                Release = w.IdReleaseNavigation != null ? w.IdReleaseNavigation.Version : null,
                MinutosInvertidos = v.MinutosInvertidos ?? 0,
                // Se prueba r.Minutos y no "r == null": EF aplana el LEFT JOIN en columnas, asi
                // que la ausencia de registros llega como NULL en esa columna, no como un objeto
                // nulo. Es tambien el unico dato de r que se lee, y ya es nullable.
                MinutosRegistrados = r.Minutos ?? 0,
                TieneRegistro = r.Minutos != null,
                w.FechaRegistro, w.FechaInicio, w.FechaFin, w.FechaCompromiso,
            };

        var crudos = await consulta.Take(TopeRenglonesDetalle + 1).ToListAsync(cancellationToken);
        var truncado = crudos.Count > TopeRenglonesDetalle;
        if (truncado)
        {
            crudos = crudos.Take(TopeRenglonesDetalle).ToList();
        }

        // Tiempo habil: se pide en un solo lote (una carga de tramos/festivos por horario) en vez
        // de una llamada al calendario por renglon.
        var solicitudesResolucion = crudos
            .Where(c => c.IdHorarioAsignado != null)
            .Select(c => new TramoLaborableSolicitado(
                c.IdWorkItem, c.FechaRegistro, c.FechaFin!.Value, c.IdHorarioAsignado!.Value))
            .ToList();

        var solicitudesEspera = crudos
            .Where(c => c.IdHorarioAsignado != null && c.FechaInicio != null)
            .Select(c => new TramoLaborableSolicitado(
                c.IdWorkItem, c.FechaRegistro, c.FechaInicio!.Value, c.IdHorarioAsignado!.Value))
            .ToList();

        var laboralResolucion = await calendario.CalcularMinutosLaboralesLoteAsync(solicitudesResolucion, cancellationToken);
        var laboralEspera = await calendario.CalcularMinutosLaboralesLoteAsync(solicitudesEspera, cancellationToken);

        var items = crudos.Select(c => new ActividadTerminadaResponse
        {
            IdWorkItem = c.IdWorkItem,
            Folio = c.Folio,
            Tipo = c.Tipo,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            Proyecto = c.Proyecto,
            Equipo = c.Equipo,
            Asignado = c.Asignado,
            Prioridad = c.Prioridad,
            Sprint = c.Sprint,
            Release = c.Release,
            MinutosInvertidos = c.MinutosInvertidos,
            MinutosRegistrados = c.MinutosRegistrados,
            DiferenciaMinutos = c.MinutosRegistrados - c.MinutosInvertidos,
            FechaCreacion = c.FechaRegistro,
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            FechaCompromiso = c.FechaCompromiso,
            DiasNaturalesResolucion = DiasNaturales(c.FechaRegistro, c.FechaFin),
            MinutosLaboralesResolucion = laboralResolucion.TryGetValue(c.IdWorkItem, out var mr) ? mr : null,
            DiasNaturalesEspera = DiasNaturales(c.FechaRegistro, c.FechaInicio),
            MinutosLaboralesEspera = laboralEspera.TryGetValue(c.IdWorkItem, out var me) ? me : null,
            // Se comparan solo las partes de FECHA. FechaCompromiso se captura a medianoche y
            // FechaFin trae la hora real, asi que comparar los datetime completos marcaba "No"
            // a todo lo terminado EL DIA del compromiso (a cualquier hora despues de las 00:00).
            // Mismo bug que ya se corrigio en vwBandejaTrabajo.EsVencida (script 27 de 02_Libera).
            EntregadoATiempo = c.FechaCompromiso == null || c.FechaFin == null
                ? null
                : c.FechaFin.Value.Date <= c.FechaCompromiso.Value.Date,
        }).ToList();

        var conCompromiso = items.Where(i => i.EntregadoATiempo != null).ToList();

        var totales = new ActividadesTerminadasTotalesResponse
        {
            Items = items.Count,
            MinutosInvertidos = items.Sum(i => i.MinutosInvertidos),
            MinutosRegistrados = items.Sum(i => i.MinutosRegistrados),
            DiferenciaMinutos = items.Sum(i => i.DiferenciaMinutos),
            ItemsConRegistro = crudos.Count(c => c.TieneRegistro),
            PromedioDiasNaturalesResolucion = PromedioDecimal(items.Select(i => i.DiasNaturalesResolucion)),
            PromedioMinutosLaboralesResolucion = PromedioEntero(items.Select(i => i.MinutosLaboralesResolucion)),
            PromedioDiasNaturalesEspera = PromedioDecimal(items.Select(i => i.DiasNaturalesEspera)),
            PromedioMinutosLaboralesEspera = PromedioEntero(items.Select(i => i.MinutosLaboralesEspera)),
            PorcentajeATiempo = conCompromiso.Count == 0
                ? null
                : Math.Round(conCompromiso.Count(i => i.EntregadoATiempo == true) * 100m / conCompromiso.Count, 1),
        };

        var (tickets, totalesTickets, avisosTickets) = await ObtenerTicketsTerminadosAsync(
            contexto, inicio, fin, idEquipo, idAsignado, idProyecto, folioFiltro, cancellationToken);

        var (incidentes, totalesIncidentes, avisosIncidentes) = await ObtenerIncidentesTerminadosAsync(
            contexto, inicio, fin, idEquipo, idAsignado, idProyecto, folioFiltro, cancellationToken);

        return new ActividadesTerminadasReporteResponse
        {
            Desde = desde, Hasta = hasta, Items = items, Totales = totales, Truncado = truncado,
            Tickets = tickets, TotalesTickets = totalesTickets, AvisosTickets = avisosTickets,
            Incidentes = incidentes, TotalesIncidentes = totalesIncidentes, AvisosIncidentes = avisosIncidentes,
        };
    }

    /// <summary>
    /// Proyeccion intermedia del reloj de estatus.
    ///
    /// TRAMPA EF: tiene que ser un tipo de REFERENCIA con propiedades settables, inicializado con
    /// object initializer. No sirve un ValueTuple (en el LEFT JOIN se compara contra null y un
    /// struct no puede serlo) ni un record posicional: EF Core NO traduce el constructor dentro
    /// del Select de un GroupBy que luego entra a un join -- pierde el tipo, castea las llaves a
    /// object y truena con "The LINQ expression could not be translated" en tiempo de EJECUCION,
    /// no de compilacion. Cubierto por DiagnosticoTraduccionR15 en GTE.Api.Tests.
    /// </summary>
    private sealed class RelojEstatusDTO
    {
        public int IdRegistro { get; set; }

        /// <summary>
        /// Nullable a proposito: en el LEFT JOIN, SUM() devuelve NULL para los registros sin
        /// historial en ese estatus, y leerlo en un int no nullable revienta al materializar.
        /// </summary>
        public int? Minutos { get; set; }
    }

    /// <summary>Minutos laborales acumulados en un estatus dado, desde el historial materializado.</summary>
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

    /// <summary>
    /// R15, seccion Tickets: resueltos o cerrados en el rango (por FechaResolucion). Un ticket no
    /// tiene equipo ni proyecto, asi que filtrar por cualquiera de esos vacia la seccion en vez
    /// de devolver algo que no honra el filtro.
    /// </summary>
    private static async Task<(IReadOnlyList<TicketTerminadoResponse>, TicketsTerminadosTotalesResponse, IReadOnlyList<string>)>
        ObtenerTicketsTerminadosAsync(
            DbContextGTE contexto, DateTime inicio, DateTime fin,
            int? idEquipo, int? idAsignado, int? idProyecto, string? folio,
            CancellationToken cancellationToken)
    {
        var avisos = new List<string>();
        if (idEquipo != null) avisos.Add("Los tickets no se asignan a un equipo, por eso esta seccion queda vacia al filtrar por equipo.");
        if (idProyecto != null) avisos.Add("Los tickets no pertenecen a un proyecto, por eso esta seccion queda vacia al filtrar por proyecto.");

        if (avisos.Count > 0)
        {
            return ([], new TicketsTerminadosTotalesResponse(), avisos);
        }

        var reloj = RelojEstatus(contexto, "Ticket", EstatusTicket.EnAtencion);

        var crudos = await (
            from t in contexto.TblTicket.AsNoTracking()
            join r in reloj on t.IdTicket equals r.IdRegistro into relojes
            from r in relojes.DefaultIfEmpty()
            where t.Activo
                && (t.IdEstatusTicket == EstatusTicket.Resuelto || t.IdEstatusTicket == EstatusTicket.Cerrado)
                && t.FechaResolucion != null && t.FechaResolucion >= inicio && t.FechaResolucion <= fin
                && (idAsignado == null || t.IdAsignado == idAsignado)
                && (folio == null || (t.Folio != null && t.Folio.Contains(folio)))
            orderby t.FechaResolucion descending, t.IdTicket descending
            select new
            {
                t.IdTicket, t.Folio, t.Titulo, t.Descripcion,
                Categoria = t.IdCategoriaTicketNavigation != null ? t.IdCategoriaTicketNavigation.Nombre : null,
                Prioridad = t.IdPrioridadNavigation.Nombre,
                Estatus = t.IdEstatusTicketNavigation.Descripcion,
                Solicitante = t.IdSolicitanteNavigation.Nombre,
                Asignado = t.IdAsignadoNavigation != null ? t.IdAsignadoNavigation.Nombre : null,
                IdHorarioAsignado = t.IdAsignadoNavigation != null ? t.IdAsignadoNavigation.IdHorario : null,
                MinutosEnAtencion = r.Minutos ?? 0,
                t.FechaRegistro, t.FechaPrimeraRespuesta, t.FechaResolucion, t.FechaLimiteResolucion,
            })
            .Take(TopeRenglonesDetalle)
            .ToListAsync(cancellationToken);

        var items = crudos.Select(c => new TicketTerminadoResponse
        {
            IdTicket = c.IdTicket,
            Folio = c.Folio,
            Categoria = c.Categoria,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            Prioridad = c.Prioridad,
            Estatus = c.Estatus,
            Solicitante = c.Solicitante,
            Asignado = c.Asignado,
            MinutosEnAtencion = c.MinutosEnAtencion,
            FechaCreacion = c.FechaRegistro,
            FechaPrimeraRespuesta = c.FechaPrimeraRespuesta,
            FechaResolucion = c.FechaResolucion,
            DiasNaturalesEspera = DiasNaturales(c.FechaRegistro, c.FechaPrimeraRespuesta),
            DiasNaturalesResolucion = DiasNaturales(c.FechaRegistro, c.FechaResolucion),
            // El reloj de estatus ya viene en minutos laborales: no hay que recalcularlo.
            MinutosLaboralesResolucion = c.MinutosEnAtencion == 0 ? null : c.MinutosEnAtencion,
            DentroDeSla = c.FechaLimiteResolucion == null ? null : c.FechaResolucion <= c.FechaLimiteResolucion,
        }).ToList();

        var conSla = items.Where(i => i.DentroDeSla != null).ToList();

        var totales = new TicketsTerminadosTotalesResponse
        {
            Items = items.Count,
            MinutosEnAtencion = items.Sum(i => i.MinutosEnAtencion),
            PromedioDiasNaturalesResolucion = PromedioDecimal(items.Select(i => i.DiasNaturalesResolucion)),
            PorcentajeDentroDeSla = conSla.Count == 0
                ? null
                : Math.Round(conSla.Count(i => i.DentroDeSla == true) * 100m / conSla.Count, 1),
        };

        return (items, totales, avisos);
    }

    /// <summary>
    /// R15, seccion Incidentes: resueltos o cerrados en el rango. Un incidente no tiene asignado
    /// ni equipo (no se atribuye a una persona), asi que esos filtros vacian la seccion.
    /// </summary>
    private static async Task<(IReadOnlyList<IncidenteTerminadoResponse>, IncidentesTerminadosTotalesResponse, IReadOnlyList<string>)>
        ObtenerIncidentesTerminadosAsync(
            DbContextGTE contexto, DateTime inicio, DateTime fin,
            int? idEquipo, int? idAsignado, int? idProyecto, string? folio,
            CancellationToken cancellationToken)
    {
        var avisos = new List<string>();
        if (idEquipo != null) avisos.Add("Los incidentes no se asignan a un equipo, por eso esta seccion queda vacia al filtrar por equipo.");
        if (idAsignado != null) avisos.Add("Los incidentes no tienen persona asignada, por eso esta seccion queda vacia al filtrar por asignado.");

        if (avisos.Count > 0)
        {
            return ([], new IncidentesTerminadosTotalesResponse(), avisos);
        }

        var reloj = RelojEstatus(contexto, "Incidente", EstatusIncidente.EnAtencion);

        var crudos = await (
            from i in contexto.TblIncidente.AsNoTracking()
            join r in reloj on i.IdIncidente equals r.IdRegistro into relojes
            from r in relojes.DefaultIfEmpty()
            where i.Activo
                && (i.IdEstatusIncidente == EstatusIncidente.Resuelto || i.IdEstatusIncidente == EstatusIncidente.Cerrado)
                && i.FechaResolucion != null && i.FechaResolucion >= inicio && i.FechaResolucion <= fin
                && (idProyecto == null || i.IdProyecto == idProyecto)
                && (folio == null || (i.Folio != null && i.Folio.Contains(folio)))
            orderby i.FechaResolucion descending, i.IdIncidente descending
            select new
            {
                i.IdIncidente, i.Folio, i.Titulo, i.Descripcion, i.CausaRaiz,
                Severidad = i.IdSeveridadNavigation.Nombre,
                Proyecto = i.IdProyectoNavigation.Nombre,
                Estatus = i.IdEstatusIncidenteNavigation.Descripcion,
                MinutosEnAtencion = r.Minutos ?? 0,
                i.MinutosIndisponibilidad,
                i.FechaOcurrencia, i.FechaDeteccion, i.FechaResolucion,
            })
            .Take(TopeRenglonesDetalle)
            .ToListAsync(cancellationToken);

        var items = crudos.Select(c => new IncidenteTerminadoResponse
        {
            IdIncidente = c.IdIncidente,
            Folio = c.Folio,
            Severidad = c.Severidad,
            Titulo = c.Titulo,
            Descripcion = c.Descripcion,
            Proyecto = c.Proyecto,
            Estatus = c.Estatus,
            CausaRaiz = c.CausaRaiz,
            MinutosEnAtencion = c.MinutosEnAtencion,
            MinutosIndisponibilidad = c.MinutosIndisponibilidad,
            FechaOcurrencia = c.FechaOcurrencia,
            FechaDeteccion = c.FechaDeteccion,
            FechaResolucion = c.FechaResolucion,
            DiasNaturalesDeteccion = DiasNaturales(c.FechaOcurrencia, c.FechaDeteccion),
            DiasNaturalesResolucion = DiasNaturales(c.FechaOcurrencia, c.FechaResolucion),
        }).ToList();

        var totales = new IncidentesTerminadosTotalesResponse
        {
            Items = items.Count,
            MinutosEnAtencion = items.Sum(i => i.MinutosEnAtencion),
            MinutosIndisponibilidad = items.Sum(i => i.MinutosIndisponibilidad ?? 0),
            PromedioDiasNaturalesResolucion = PromedioDecimal(items.Select(i => i.DiasNaturalesResolucion)),
        };

        return (items, totales, avisos);
    }

    /// <summary>
    /// R16: barras de Gantt de las actividades ya iniciadas que se TRASLAPAN con el periodo.
    /// A diferencia de R15 (que corta por FechaFin y solo ve lo Terminado), aqui entra tambien
    /// lo que sigue abierto: una actividad que empezo antes del rango y no ha cerrado es parte
    /// de lo que se trabajo en el periodo y tiene que aparecer en la grafica.
    /// </summary>
    public async Task<GanttActividadesReporteResponse> ObtenerGanttActividadesAsync(
        DateOnly desde, DateOnly hasta, int? idProyecto, int? idAsignado,
        AgrupacionGantt agruparPor, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (inicio, fin) = RangoFechas(desde, hasta);

        pageSize = Math.Clamp(pageSize, 1, TopeRenglonesDetalle);
        // Un page absurdo escrito a mano en la URL desbordaria el int de (page - 1) * pageSize
        // y el Skip negativo tiraria un 500 en vez de la pagina vacia que corresponde.
        page = Math.Clamp(page, 1, int.MaxValue / pageSize);

        // TRAMPA EF: se filtra y ordena sobre las entidades (columnas reales) y se proyecta hasta
        // el final, ya paginado (mismo patron que PlaneacionQueryService.ConsultaBase).
        // FechaInicio != null es la definicion de "realizada": lo que nunca arranco no tiene barra.
        // El traslape es el clasico (inicioA <= finB && finA >= inicioB) con el extremo derecho
        // abierto cuando no hay FechaFin.
        var consulta = contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo
                && w.FechaInicio != null && w.FechaInicio <= fin
                && (w.FechaFin == null || w.FechaFin >= inicio)
                && (idProyecto == null || w.IdProyecto == idProyecto)
                && (idAsignado == null || w.IdAsignado == idAsignado));

        var total = await consulta.CountAsync(cancellationToken);
        var enProgreso = await consulta.CountAsync(w => w.FechaFin == null, cancellationToken);

        // El orden primario ES la llave de agrupacion: asi una pagina nunca parte un grupo a la
        // mitad y las bandas del Gantt salen contiguas. Sin asignado se manda al final (el bool
        // ordena false antes que true) en vez de encabezar la lista con los huerfanos.
        var ordenada = agruparPor switch
        {
            AgrupacionGantt.Proyecto => consulta
                .OrderBy(w => w.IdProyectoNavigation.Nombre)
                .ThenBy(w => w.FechaInicio)
                .ThenBy(w => w.IdWorkItem),
            AgrupacionGantt.Usuario => consulta
                .OrderBy(w => w.IdAsignado == null)
                .ThenBy(w => w.IdAsignadoNavigation!.Nombre)
                .ThenBy(w => w.FechaInicio)
                .ThenBy(w => w.IdWorkItem),
            _ => consulta
                .OrderBy(w => w.FechaInicio)
                .ThenBy(w => w.IdWorkItem),
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new GanttActividadResponse
            {
                IdWorkItem = w.IdWorkItem,
                Folio = w.Folio,
                Tipo = w.IdTipoWorkItemNavigation.Nombre,
                Titulo = w.Titulo,
                Descripcion = w.Descripcion,
                IdProyecto = w.IdProyecto,
                Proyecto = w.IdProyectoNavigation.Nombre,
                IdAsignado = w.IdAsignado,
                Asignado = w.IdAsignadoNavigation != null ? w.IdAsignadoNavigation.Nombre : null,
                IdEstatusWorkItem = w.IdEstatusWorkItem,
                Estatus = w.IdEstatusWorkItemNavigation.Descripcion,
                // El filtro ya garantiza FechaInicio != null; el .Value es seguro y evita que el
                // DTO cargue con una fecha nullable que el front tendria que volver a validar.
                FechaInicio = w.FechaInicio!.Value,
                FechaFin = w.FechaFin,
                FechaCompromiso = w.FechaCompromiso,
            })
            .ToListAsync(cancellationToken);

        return new GanttActividadesReporteResponse
        {
            Desde = desde,
            Hasta = hasta,
            Pagina = new PagedResult<GanttActividadResponse>
            {
                Items = items, Page = page, PageSize = pageSize, TotalItems = total,
            },
            TotalEnProgreso = enProgreso,
        };
    }

    /// <summary>Tope de renglones del detalle: evita que un rango abierto tumbe la pagina y el Excel.</summary>
    private const int TopeRenglonesDetalle = 5000;

    private static decimal? DiasNaturales(DateTime inicio, DateTime? fin)
        => fin == null ? null : Math.Round((decimal)(fin.Value - inicio).TotalDays, 2);

    private static decimal? PromedioDecimal(IEnumerable<decimal?> valores)
    {
        var lista = valores.Where(v => v != null).Select(v => v!.Value).ToList();
        return lista.Count == 0 ? null : Math.Round(lista.Average(), 2);
    }

    private static int? PromedioEntero(IEnumerable<int?> valores)
    {
        var lista = valores.Where(v => v != null).Select(v => v!.Value).ToList();
        return lista.Count == 0 ? null : (int)Math.Round(lista.Average());
    }
}
