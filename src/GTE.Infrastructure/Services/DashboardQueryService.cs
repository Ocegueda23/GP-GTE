using GTE.Application.DTOs.Responses.Catalogos;
using GTE.Application.DTOs.Responses.Dashboard;
using GTE.Application.Interfaces;
using GTE.Domain.Dashboard;
using GTE.Domain.Entregas;
using GTE.Domain.Operacion;
using GTE.Domain.Solicitudes;
using GTE.Domain.Soporte;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Dashboard Ejecutivo de Metricas (Doctos/GTE-DocumentoMaestro.md 3.10). Todo se calcula en
/// tiempo real contra DbContextGTE -- no hay job nocturno ni snapshot persistido (a
/// diferencia de lo que el Documento Maestro dejaba previsto via tblKpiDefinicion/
/// tblKpiValor para KPIs personalizados de equipo/proyecto): con el volumen de datos de un
/// equipo interno esto es rapido y siempre exacto, y evita construir infraestructura de
/// jobs que hoy no existe en el proyecto. Si el equipo crece mucho, ese job queda como
/// mejora futura natural sin cambiar el contrato de la API.
///
/// Formulas de puntaje: ver <see cref="GTE.Domain.Dashboard.CalculadoraPuntaje"/>.
/// </summary>
public class DashboardQueryService(FabricaContexto fabrica, ICalendarioLaboral calendario) : IDashboardQueryService
{
    private const int UmbralDiasIncidenteVencido = 3;
    private const decimal TopeIndiceEficiencia = 200m;

    private sealed record DatosUsuarioDashboard(string Nombre, string? Area, string? Puesto, int? IdHorario);

    public async Task<DashboardResponse> ObtenerDashboardAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal, bool tieneAlcanceDepartamento,
        int anio, int mes, int? idProyecto, int? idArea, int? idUsuarioFoco,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var excluidos = await ObtenerIdsExcluidosAsync(contexto, cancellationToken);

        var (usuariosAlcance, alcance) = await ResolverUsuariosVisiblesAsync(contexto, idUsuarioActual, tieneAlcanceGlobal, tieneAlcanceDepartamento, cancellationToken);
        var usuarios = usuariosAlcance.Where(id => !excluidos.Contains(id)).ToList();
        if (idArea.HasValue)
        {
            usuarios = await FiltrarPorAreaAsync(contexto, usuarios, idArea.Value, cancellationToken);
        }
        if (idUsuarioFoco.HasValue)
        {
            usuarios = usuarios.Contains(idUsuarioFoco.Value) ? [idUsuarioFoco.Value] : [];
        }

        List<int>? proyectosVisibles = alcance == "Global" && !idArea.HasValue && !idUsuarioFoco.HasValue
            ? null
            : await ResolverProyectosVisiblesAsync(contexto, usuarios, cancellationToken);

        var (desde, hasta) = RangoMes(anio, mes);

        var datosUsuarios = await ObtenerDatosUsuariosAsync(contexto, usuarios, cancellationToken);
        var puntajes = await ObtenerPuntajesBatchAsync(contexto, usuarios, desde, hasta, cancellationToken);
        var eficiencia = await ObtenerIndiceEficienciaBatchAsync(contexto, usuarios, desde, hasta, cancellationToken);

        // Empleado del mes/historico: SIEMPRE de todos los empleados elegibles del sistema,
        // sin importar el alcance del usuario que consulta ni los filtros de Area/Proyecto/
        // Empleado -- es un reconocimiento unico, no una vista filtrable (pedido explicito).
        // El PERIODO tampoco sigue el filtro Anio/Mes del dashboard (que por default es el
        // mes en curso): siempre es el mes calendario ya cerrado (el anterior a hoy), porque
        // el mes en curso todavia no tiene datos completos para evaluarse.
        var todosElegibles = await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.Activo)
            .Select(u => u.IdUsuario)
            .ToListAsync(cancellationToken);
        todosElegibles = todosElegibles.Where(id => !excluidos.Contains(id)).ToList();
        var datosTodosElegibles = await ObtenerDatosUsuariosAsync(contexto, todosElegibles, cancellationToken);
        var (anioMesCerrado, mesCerrado) = MesAnterior(DateTime.Today.Year, DateTime.Today.Month);
        var (desdeCerrado, hastaCerrado) = RangoMes(anioMesCerrado, mesCerrado);
        var puntajesMesCerrado = await ObtenerPuntajesBatchAsync(contexto, todosElegibles, desdeCerrado, hastaCerrado, cancellationToken);
        var (empleadoDelMes, historicoEmpleadoDelMes) = await ObtenerEmpleadoDelMesAsync(
            contexto, todosElegibles, datosTodosElegibles, anioMesCerrado, mesCerrado, puntajesMesCerrado, cancellationToken);

        var resumen = await ArmarResumenAsync(contexto, usuarios, proyectosVisibles, idProyecto, anio, mes, cancellationToken);
        var carga = await ArmarCargaTrabajoAsync(contexto, usuarios, datosUsuarios, idProyecto, desde, hasta, cancellationToken);
        var desgloseCarga = await ArmarDesgloseCargaTrabajoAsync(contexto, usuarios, datosUsuarios, idProyecto, desde, hasta, cancellationToken);
        var rankings = ArmarRankings(datosUsuarios, puntajes, eficiencia);
        var comparativos = await ArmarComparativosAsync(
            contexto, usuarios, datosUsuarios, puntajes, proyectosVisibles, idProyecto, idUsuarioFoco, anio, mes, desde, hasta, cancellationToken);

        return new DashboardResponse
        {
            Anio = anio,
            Mes = mes,
            AlcanceVisibilidad = alcance,
            EmpleadoDelMes = empleadoDelMes,
            HistoricoEmpleadoDelMes = historicoEmpleadoDelMes,
            ResumenEjecutivo = resumen,
            CargaTrabajo = carga,
            DesgloseCargaTrabajo = desgloseCarga,
            Rankings = rankings,
            Comparativos = comparativos,
        };
    }

    public async Task<IndicadoresEmpleadoResponse?> ObtenerIndicadoresEmpleadoAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal, bool tieneAlcanceDepartamento,
        int idUsuarioConsultado, int anio, int mes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var (visibles, _) = await ResolverUsuariosVisiblesAsync(contexto, idUsuarioActual, tieneAlcanceGlobal, tieneAlcanceDepartamento, cancellationToken);
        if (!visibles.Contains(idUsuarioConsultado))
        {
            return null;
        }

        var datosUsuarios = await ObtenerDatosUsuariosAsync(contexto, [idUsuarioConsultado], cancellationToken);
        if (!datosUsuarios.TryGetValue(idUsuarioConsultado, out var datos))
        {
            return null;
        }

        var colegas = await ObtenerColegasAsync(contexto, idUsuarioConsultado, cancellationToken);
        if (!colegas.Contains(idUsuarioConsultado))
        {
            colegas.Add(idUsuarioConsultado);
        }

        var (desde, hasta) = RangoMes(anio, mes);

        var horasEstimadasMin = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado == idUsuarioConsultado
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7))
            .SumAsync(w => (int?)w.MinutosPresupuesto, cancellationToken) ?? 0;

        var horasRealesMin = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(r => r.Activo && r.IdUsuario == idUsuarioConsultado
                && r.Fecha >= DateOnly.FromDateTime(desde) && r.Fecha <= DateOnly.FromDateTime(hasta))
            .SumAsync(r => (int?)r.Minutos, cancellationToken) ?? 0;

        var indiceEficiencia = horasRealesMin > 0
            ? Math.Min(TopeIndiceEficiencia, Math.Round(100m * horasEstimadasMin / horasRealesMin, 1))
            : (decimal?)null;

        var puntajesEquipo = await ObtenerPuntajesBatchAsync(contexto, colegas, desde, hasta, cancellationToken);
        var (insumosPropios, puntajePropio) = puntajesEquipo.TryGetValue(idUsuarioConsultado, out var propio)
            ? propio
            : (null, new PuntajeEmpleadoCalculado([], null));

        var evolucion = new List<PuntoPuntajeMensualResponse>();
        for (var m = 1; m <= 12; m++)
        {
            var (d, h) = RangoMes(anio, m);
            if (d > DateTime.Now)
            {
                evolucion.Add(new PuntoPuntajeMensualResponse { Mes = m, Puntaje = null });
                continue;
            }

            var puntajesMes = m == mes ? puntajesEquipo : await ObtenerPuntajesBatchAsync(contexto, colegas, d, h, cancellationToken);
            var valor = puntajesMes.TryGetValue(idUsuarioConsultado, out var p) ? p.Puntaje.PuntajeGeneral : null;
            evolucion.Add(new PuntoPuntajeMensualResponse { Mes = m, Puntaje = valor });
        }

        return new IndicadoresEmpleadoResponse
        {
            Empleado = new EmpleadoResumenResponse
            {
                IdUsuario = idUsuarioConsultado,
                Nombre = datos.Nombre,
                Area = datos.Area,
                Puesto = datos.Puesto,
                UrlFoto = await ObtenerUrlFotoAsync(contexto, idUsuarioConsultado, cancellationToken),
            },
            Anio = anio,
            Mes = mes,
            HorasEstimadas = Math.Round(horasEstimadasMin / 60m, 1),
            HorasReales = Math.Round(horasRealesMin / 60m, 1),
            IndiceEficiencia = indiceEficiencia,
            ItemsTerminados = insumosPropios?.TerminadosPeriodo ?? 0,
            PromedioTerminadosEquipo = insumosPropios?.PromedioTerminadosEquipo ?? 0,
            EntregasATiempo = insumosPropios?.ATiempoPeriodo ?? 0,
            EntregasRetrasadas = (insumosPropios?.ConCompromisoPeriodo ?? 0) - (insumosPropios?.ATiempoPeriodo ?? 0),
            PorcentajeCumplimientoEntregas = puntajePropio.Dimensiones.FirstOrDefault(d => d.Dimension == DimensionEvaluacion.Puntualidad)?.Valor,
            Evaluacion = puntajePropio.Dimensiones
                .Select(d => new PuntajeDimensionResponse { Dimension = d.Dimension.ToString(), Valor = d.Valor })
                .ToList(),
            PuntajeMensual = puntajePropio.PuntajeGeneral,
            EvolucionPuntajeAnio = evolucion,
        };
    }

    public async Task<IReadOnlyList<TendenciaResponse>> ObtenerTendenciasAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal, bool tieneAlcanceDepartamento,
        int anio, int? idProyecto, int? idArea, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var excluidos = await ObtenerIdsExcluidosAsync(contexto, cancellationToken);
        var (usuariosAlcance, alcance) = await ResolverUsuariosVisiblesAsync(contexto, idUsuarioActual, tieneAlcanceGlobal, tieneAlcanceDepartamento, cancellationToken);
        var usuarios = usuariosAlcance.Where(id => !excluidos.Contains(id)).ToList();
        if (idArea.HasValue)
        {
            usuarios = await FiltrarPorAreaAsync(contexto, usuarios, idArea.Value, cancellationToken);
        }

        List<int>? proyectosVisibles = alcance == "Global" && !idArea.HasValue
            ? null
            : await ResolverProyectosVisiblesAsync(contexto, usuarios, cancellationToken);

        var puntosWi = new List<PuntoTendenciaResponse>();
        var puntosTk = new List<PuntoTendenciaResponse>();
        var puntosInc = new List<PuntoTendenciaResponse>();
        var puntosRel = new List<PuntoTendenciaResponse>();
        var puntosPuntaje = new List<PuntoTendenciaResponse>();
        var puntosProductividad = new List<PuntoTendenciaResponse>();

        for (var mes = 1; mes <= 12; mes++)
        {
            var (desde, hasta) = RangoMes(anio, mes);
            if (desde > DateTime.Now)
            {
                break;
            }

            var wi = await ContarWorkItemsAsync(contexto, desde, hasta, usuarios, idProyecto, cancellationToken);
            var tk = await ContarTicketsAsync(contexto, desde, hasta, usuarios, cancellationToken);
            var inc = await ContarIncidenciasAsync(contexto, desde, hasta, idProyecto.HasValue ? [idProyecto.Value] : proyectosVisibles, cancellationToken);
            var rel = await ContarReleasesAsync(contexto, desde, hasta, idProyecto.HasValue ? [idProyecto.Value] : proyectosVisibles, cancellationToken);
            var puntajes = await ObtenerPuntajesBatchAsync(contexto, usuarios, desde, hasta, cancellationToken);

            var puntajesConDato = puntajes.Values.Where(p => p.Puntaje.PuntajeGeneral.HasValue).Select(p => p.Puntaje.PuntajeGeneral!.Value).ToList();
            var puntajePromedio = puntajesConDato.Count > 0 ? puntajesConDato.Average() : 0m;

            var productividadConDato = puntajes.Values
                .Select(p => p.Puntaje.Dimensiones.FirstOrDefault(d => d.Dimension == DimensionEvaluacion.Productividad)?.Valor)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
            var productividadPromedio = productividadConDato.Count > 0 ? productividadConDato.Average() : 0m;

            puntosWi.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = wi.Terminados });
            puntosTk.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = tk.Cerrados });
            puntosInc.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = inc.Cerrados });
            puntosRel.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = rel.Liberadas });
            puntosPuntaje.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = Math.Round(puntajePromedio, 1) });
            puntosProductividad.Add(new PuntoTendenciaResponse { Anio = anio, Mes = mes, Valor = Math.Round(productividadPromedio, 1) });
        }

        return
        [
            new TendenciaResponse { Metrica = "WorkItems terminados", Puntos = puntosWi },
            new TendenciaResponse { Metrica = "Tickets cerrados", Puntos = puntosTk },
            new TendenciaResponse { Metrica = "Incidencias cerradas", Puntos = puntosInc },
            new TendenciaResponse { Metrica = "Releases liberados", Puntos = puntosRel },
            new TendenciaResponse { Metrica = "Puntaje / evaluacion mensual promedio", Puntos = puntosPuntaje },
            new TendenciaResponse { Metrica = "Productividad promedio", Puntos = puntosProductividad },
        ];
    }

    public async Task<FiltroCatalogosDashboardResponse> ObtenerFiltrosAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal, bool tieneAlcanceDepartamento, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var excluidos = await ObtenerIdsExcluidosAsync(contexto, cancellationToken);
        var (usuariosAlcance, alcance) = await ResolverUsuariosVisiblesAsync(contexto, idUsuarioActual, tieneAlcanceGlobal, tieneAlcanceDepartamento, cancellationToken);
        var usuarios = usuariosAlcance.Where(id => !excluidos.Contains(id)).ToList();
        List<int>? proyectosVisibles = alcance == "Global" ? null : await ResolverProyectosVisiblesAsync(contexto, usuarios, cancellationToken);

        var proyectosQuery = contexto.TblProyecto.AsNoTracking().Where(p => p.Activo);
        if (proyectosVisibles != null)
        {
            proyectosQuery = proyectosQuery.Where(p => proyectosVisibles.Contains(p.IdProyecto));
        }

        var proyectos = await (
            from p in proyectosQuery
            join c in contexto.TblCategoriaProyecto.AsNoTracking() on p.IdCategoriaProyecto equals c.Id
            orderby p.Nombre
            select new ProyectoItemResponse { Id = p.IdProyecto, Clave = p.Clave, Nombre = p.Nombre, CategoriaProyecto = c.Nombre })
            .ToListAsync(cancellationToken);

        var areas = await (
            from u in contexto.TblUsuario.AsNoTracking()
            join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
            join a in contexto.TblArea.AsNoTracking() on p.IdArea equals a.IdArea
            where usuarios.Contains(u.IdUsuario)
            select new CatalogoItemResponse { Id = a.IdArea, Nombre = a.Nombre })
            .Distinct()
            .OrderBy(a => a.Nombre)
            .ToListAsync(cancellationToken);

        var empleados = await contexto.TblUsuario.AsNoTracking()
            .Where(u => usuarios.Contains(u.IdUsuario))
            .OrderBy(u => u.Nombre)
            .Select(u => new CatalogoItemResponse { Id = u.IdUsuario, Nombre = u.Nombre })
            .ToListAsync(cancellationToken);

        return new FiltroCatalogosDashboardResponse
        {
            Proyectos = proyectos,
            Areas = areas,
            Empleados = empleados,
            AlcanceVisibilidad = alcance,
        };
    }

    // ------------------------------------------------------------------
    // Alcance / visibilidad
    // ------------------------------------------------------------------

    private static async Task<(List<int> Usuarios, string Alcance)> ResolverUsuariosVisiblesAsync(
        DbContextGTE contexto, int idUsuarioActual, bool global, bool departamento, CancellationToken ct)
    {
        if (global)
        {
            var todos = await contexto.TblUsuario.AsNoTracking().Where(u => u.Activo).Select(u => u.IdUsuario).ToListAsync(ct);
            return (todos, "Global");
        }

        if (departamento)
        {
            var idArea = await (
                from u in contexto.TblUsuario.AsNoTracking()
                join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
                where u.IdUsuario == idUsuarioActual
                select (int?)p.IdArea)
                .FirstOrDefaultAsync(ct);

            if (idArea.HasValue)
            {
                var delArea = await (
                    from u in contexto.TblUsuario.AsNoTracking()
                    join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
                    where u.Activo && p.IdArea == idArea.Value
                    select u.IdUsuario)
                    .ToListAsync(ct);
                if (!delArea.Contains(idUsuarioActual))
                {
                    delArea.Add(idUsuarioActual);
                }
                return (delArea, "Departamento");
            }
        }

        var liderados = await (
            from u in contexto.TblUsuario.AsNoTracking()
            where u.Activo && (
                u.IdJefe == idUsuarioActual ||
                contexto.TblEquipoMiembro.Any(em => em.Activo && em.IdUsuario == u.IdUsuario &&
                    contexto.TblEquipo.Any(e => e.IdEquipo == em.IdEquipo && e.IdLider == idUsuarioActual)))
            select u.IdUsuario)
            .ToListAsync(ct);

        if (liderados.Count > 0)
        {
            if (!liderados.Contains(idUsuarioActual))
            {
                liderados.Add(idUsuarioActual);
            }
            return (liderados, "Equipo");
        }

        return ([idUsuarioActual], "Personal");
    }

    /// <summary>
    /// Cuentas que no deben aparecer como sujeto de ninguna metrica (aunque puedan usar el
    /// dashboard para ver a otros): rol Administrador (no son colaboradores medidos, son
    /// cuentas de operacion del sistema) y cuentas externas (`tblUsuario.EsExterno`, ej.
    /// "Solicitante Externo (migracion GT)").
    /// </summary>
    private static async Task<HashSet<int>> ObtenerIdsExcluidosAsync(DbContextGTE contexto, CancellationToken ct)
    {
        var excluidos = await (
            from u in contexto.TblUsuario.AsNoTracking()
            where u.EsExterno || contexto.TblUsuarioRol.Any(ur =>
                ur.Activo && ur.IdUsuario == u.IdUsuario &&
                contexto.TblRol.Any(r => r.IdRol == ur.IdRol && r.Nombre == "Administrador"))
            select u.IdUsuario)
            .ToListAsync(ct);
        return excluidos.ToHashSet();
    }

    private static async Task<List<int>> ResolverProyectosVisiblesAsync(DbContextGTE contexto, List<int> usuarios, CancellationToken ct)
    {
        if (usuarios.Count == 0)
        {
            return [];
        }

        return await (
            from pr in contexto.TblProyecto.AsNoTracking()
            where pr.Activo && (
                (pr.IdResponsable != null && usuarios.Contains(pr.IdResponsable.Value)) ||
                (pr.IdEquipo != null && contexto.TblEquipoMiembro.Any(em => em.Activo && em.IdEquipo == pr.IdEquipo && usuarios.Contains(em.IdUsuario))))
            select pr.IdProyecto)
            .Distinct()
            .ToListAsync(ct);
    }

    private static async Task<List<int>> FiltrarPorAreaAsync(DbContextGTE contexto, List<int> usuarios, int idArea, CancellationToken ct)
    {
        return await (
            from u in contexto.TblUsuario.AsNoTracking()
            join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
            where usuarios.Contains(u.IdUsuario) && p.IdArea == idArea
            select u.IdUsuario)
            .ToListAsync(ct);
    }

    private static async Task<List<int>> ObtenerColegasAsync(DbContextGTE contexto, int idUsuario, CancellationToken ct)
    {
        var porEquipo = await (
            from em in contexto.TblEquipoMiembro.AsNoTracking()
            where em.Activo && contexto.TblEquipoMiembro.Any(x => x.Activo && x.IdUsuario == idUsuario && x.IdEquipo == em.IdEquipo)
            select em.IdUsuario)
            .Distinct()
            .ToListAsync(ct);

        if (porEquipo.Count > 1)
        {
            return porEquipo;
        }

        var idArea = await (
            from u in contexto.TblUsuario.AsNoTracking()
            join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
            where u.IdUsuario == idUsuario
            select (int?)p.IdArea)
            .FirstOrDefaultAsync(ct);

        if (idArea.HasValue)
        {
            return await (
                from u in contexto.TblUsuario.AsNoTracking()
                join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto
                where u.Activo && p.IdArea == idArea.Value
                select u.IdUsuario)
                .ToListAsync(ct);
        }

        return [idUsuario];
    }

    private static async Task<Dictionary<int, DatosUsuarioDashboard>> ObtenerDatosUsuariosAsync(
        DbContextGTE contexto, IEnumerable<int> ids, CancellationToken ct)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0)
        {
            return [];
        }

        var filas = await (
            from u in contexto.TblUsuario.AsNoTracking()
            where lista.Contains(u.IdUsuario)
            join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto into puestos
            from p in puestos.DefaultIfEmpty()
            join a in contexto.TblArea.AsNoTracking() on (p == null ? (int?)null : p.IdArea) equals a.IdArea into areas
            from a in areas.DefaultIfEmpty()
            select new { u.IdUsuario, u.Nombre, Area = a != null ? a.Nombre : null, Puesto = p != null ? p.Nombre : null, u.IdHorario })
            .ToListAsync(ct);

        return filas.ToDictionary(x => x.IdUsuario, x => new DatosUsuarioDashboard(x.Nombre, x.Area, x.Puesto, x.IdHorario));
    }

    private static async Task<string?> ObtenerUrlFotoAsync(DbContextGTE contexto, int idUsuario, CancellationToken ct)
    {
        var guid = await (
            from v in contexto.TblArchivoVinculo.AsNoTracking()
            join a in contexto.TblArchivo.AsNoTracking() on v.IdArchivo equals a.IdArchivo
            where v.Activo && a.Activo && v.Entidad == "Usuario" && v.IdEntidad == idUsuario
            orderby a.FechaRegistro descending
            select a.GuidArchivo)
            .FirstOrDefaultAsync(ct);

        return guid == Guid.Empty ? null : $"/api/v1/archivos/{guid}";
    }

    // ------------------------------------------------------------------
    // Resumen ejecutivo
    // ------------------------------------------------------------------

    private static (DateTime Desde, DateTime Hasta) RangoMes(int anio, int mes)
    {
        var desde = new DateTime(anio, mes, 1);
        return (desde, desde.AddMonths(1).AddTicks(-1));
    }

    private static (int Anio, int Mes) MesAnterior(int anio, int mes)
        => mes == 1 ? (anio - 1, 12) : (anio, mes - 1);

    private static KpiEstadoResponse ArmarEstado(string estado, int actual, int anterior)
    {
        decimal? variacion = anterior > 0
            ? Math.Round(100m * (actual - anterior) / anterior, 1)
            : (actual > 0 ? 100m : null);
        return new KpiEstadoResponse { Estado = estado, Total = actual, TotalMesAnterior = anterior, VariacionPorcentaje = variacion };
    }

    private static async Task<(int Pendientes, int EnProceso, int Terminados, int Vencidos)> ContarWorkItemsAsync(
        DbContextGTE contexto, DateTime desde, DateTime hasta, IReadOnlyCollection<int> usuarios, int? idProyecto, CancellationToken ct)
    {
        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;
        var query = contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value) && w.FechaRegistro <= hasta);
        if (idProyecto.HasValue)
        {
            query = query.Where(w => w.IdProyecto == idProyecto.Value);
        }

        var abiertos = query.Where(w => w.FechaFin == null || w.FechaFin > fechaCorte);
        var pendientes = await abiertos.CountAsync(w => w.IdEstatusWorkItem == 1, ct);
        var enProceso = await abiertos.CountAsync(w => w.IdEstatusWorkItem >= 2 && w.IdEstatusWorkItem <= 5, ct);
        var vencidos = await abiertos.CountAsync(w => w.FechaCompromiso != null && w.FechaCompromiso < fechaCorte, ct);
        var terminados = await query.CountAsync(w =>
            w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7), ct);

        return (pendientes, enProceso, terminados, vencidos);
    }

    private static async Task<(int Pendientes, int EnProceso, int Cerrados, int Vencidos)> ContarTicketsAsync(
        DbContextGTE contexto, DateTime desde, DateTime hasta, IReadOnlyCollection<int> usuarios, CancellationToken ct)
    {
        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;
        var query = contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.IdAsignado != null && usuarios.Contains(t.IdAsignado.Value) && t.FechaRegistro <= hasta);

        var abiertos = query.Where(t => t.IdEstatusTicket != 6);
        var pendientes = await abiertos.CountAsync(t => t.IdEstatusTicket == 1 || t.IdEstatusTicket == 2, ct);
        var enProceso = await abiertos.CountAsync(t => t.IdEstatusTicket >= 3 && t.IdEstatusTicket <= 5, ct);
        var vencidos = await query.CountAsync(t =>
            t.IdEstatusTicket != 5 && t.IdEstatusTicket != 6 && t.FechaLimiteResolucion != null && t.FechaLimiteResolucion < fechaCorte, ct);
        var cerrados = await query.CountAsync(t =>
            t.IdEstatusTicket == 6 && t.FechaResolucion != null && t.FechaResolucion >= desde && t.FechaResolucion <= hasta, ct);

        return (pendientes, enProceso, cerrados, vencidos);
    }

    private static async Task<(int Pendientes, int EnProceso, int Cerrados, int Vencidos)> ContarIncidenciasAsync(
        DbContextGTE contexto, DateTime desde, DateTime hasta, IReadOnlyCollection<int>? proyectos, CancellationToken ct)
    {
        if (proyectos != null && proyectos.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;
        var query = contexto.TblIncidente.AsNoTracking().Where(i => i.Activo && i.FechaRegistro <= hasta);
        if (proyectos != null)
        {
            query = query.Where(i => proyectos.Contains(i.IdProyecto));
        }

        var abiertos = query.Where(i => i.FechaResolucion == null || i.FechaResolucion > fechaCorte);
        var pendientes = await abiertos.CountAsync(i => i.IdEstatusIncidente == 1, ct);
        var enProceso = await abiertos.CountAsync(i => i.IdEstatusIncidente >= 2 && i.IdEstatusIncidente <= 4, ct);
        var umbral = fechaCorte.AddDays(-UmbralDiasIncidenteVencido);
        var vencidos = await abiertos.CountAsync(i => i.FechaOcurrencia < umbral, ct);
        var cerrados = await query.CountAsync(i =>
            i.IdEstatusIncidente == 5 && i.FechaResolucion != null && i.FechaResolucion >= desde && i.FechaResolucion <= hasta, ct);

        return (pendientes, enProceso, cerrados, vencidos);
    }

    private static async Task<(int Pendientes, int EnProceso, int Liberadas, int Retrasadas)> ContarReleasesAsync(
        DbContextGTE contexto, DateTime desde, DateTime hasta, IReadOnlyCollection<int>? proyectos, CancellationToken ct)
    {
        if (proyectos != null && proyectos.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;
        var fechaCorteFecha = DateOnly.FromDateTime(fechaCorte);
        var query = contexto.TblRelease.AsNoTracking().Where(r => r.Activo && r.FechaRegistro <= hasta);
        if (proyectos != null)
        {
            query = query.Where(r => proyectos.Contains(r.IdProyecto));
        }

        var abiertos = query.Where(r => r.FechaLiberacion == null || r.FechaLiberacion > fechaCorte);
        var pendientes = await abiertos.CountAsync(r => r.IdEstatusRelease == 1, ct);
        var enProceso = await abiertos.CountAsync(r => r.IdEstatusRelease == 2 || r.IdEstatusRelease == 3, ct);
        var retrasadas = await abiertos.CountAsync(r =>
            r.FechaPlan != null && r.FechaPlan < fechaCorteFecha && r.IdEstatusRelease != 5 && r.IdEstatusRelease != 6, ct);
        var liberadas = await query.CountAsync(r =>
            r.IdEstatusRelease == 4 && r.FechaLiberacion != null && r.FechaLiberacion >= desde && r.FechaLiberacion <= hasta, ct);

        return (pendientes, enProceso, liberadas, retrasadas);
    }

    private static async Task<List<KpiGrupoResponse>> ArmarResumenAsync(
        DbContextGTE contexto, List<int> usuarios, List<int>? proyectosVisibles, int? idProyectoFiltro,
        int anio, int mes, CancellationToken ct)
    {
        var (desde, hasta) = RangoMes(anio, mes);
        var (anioAnt, mesAnt) = MesAnterior(anio, mes);
        var (desdeAnt, hastaAnt) = RangoMes(anioAnt, mesAnt);

        List<int>? proyectosParaIncidenteRelease = idProyectoFiltro.HasValue ? [idProyectoFiltro.Value] : proyectosVisibles;

        var wiActual = await ContarWorkItemsAsync(contexto, desde, hasta, usuarios, idProyectoFiltro, ct);
        var wiAnterior = await ContarWorkItemsAsync(contexto, desdeAnt, hastaAnt, usuarios, idProyectoFiltro, ct);
        var tkActual = await ContarTicketsAsync(contexto, desde, hasta, usuarios, ct);
        var tkAnterior = await ContarTicketsAsync(contexto, desdeAnt, hastaAnt, usuarios, ct);
        var incActual = await ContarIncidenciasAsync(contexto, desde, hasta, proyectosParaIncidenteRelease, ct);
        var incAnterior = await ContarIncidenciasAsync(contexto, desdeAnt, hastaAnt, proyectosParaIncidenteRelease, ct);
        var relActual = await ContarReleasesAsync(contexto, desde, hasta, proyectosParaIncidenteRelease, ct);
        var relAnterior = await ContarReleasesAsync(contexto, desdeAnt, hastaAnt, proyectosParaIncidenteRelease, ct);

        return
        [
            new KpiGrupoResponse
            {
                Grupo = "WorkItems",
                Estados =
                [
                    ArmarEstado("Pendientes", wiActual.Pendientes, wiAnterior.Pendientes),
                    ArmarEstado("En proceso", wiActual.EnProceso, wiAnterior.EnProceso),
                    ArmarEstado("Terminados", wiActual.Terminados, wiAnterior.Terminados),
                    ArmarEstado("Vencidos", wiActual.Vencidos, wiAnterior.Vencidos),
                ],
            },
            new KpiGrupoResponse
            {
                Grupo = "Incidencias",
                Estados =
                [
                    ArmarEstado("Pendientes", incActual.Pendientes, incAnterior.Pendientes),
                    ArmarEstado("En proceso", incActual.EnProceso, incAnterior.EnProceso),
                    ArmarEstado("Terminadas", incActual.Cerrados, incAnterior.Cerrados),
                    ArmarEstado("Vencidas", incActual.Vencidos, incAnterior.Vencidos),
                ],
            },
            new KpiGrupoResponse
            {
                Grupo = "Releases",
                Estados =
                [
                    ArmarEstado("Pendientes", relActual.Pendientes, relAnterior.Pendientes),
                    ArmarEstado("En proceso", relActual.EnProceso, relAnterior.EnProceso),
                    ArmarEstado("Liberadas", relActual.Liberadas, relAnterior.Liberadas),
                    ArmarEstado("Retrasadas", relActual.Retrasadas, relAnterior.Retrasadas),
                ],
            },
            new KpiGrupoResponse
            {
                Grupo = "Tickets",
                Estados =
                [
                    ArmarEstado("Pendientes", tkActual.Pendientes, tkAnterior.Pendientes),
                    ArmarEstado("En proceso", tkActual.EnProceso, tkAnterior.EnProceso),
                    ArmarEstado("Cerrados", tkActual.Cerrados, tkAnterior.Cerrados),
                    ArmarEstado("Vencidos", tkActual.Vencidos, tkAnterior.Vencidos),
                ],
            },
        ];
    }

    // ------------------------------------------------------------------
    // Carga de trabajo
    // ------------------------------------------------------------------

    private async Task<List<CargaTrabajoEmpleadoResponse>> ArmarCargaTrabajoAsync(
        DbContextGTE contexto, List<int> usuarios, Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        if (usuarios.Count == 0)
        {
            return [];
        }

        var asignadosQuery = contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaRegistro <= hasta && (w.FechaFin == null || w.FechaFin >= desde));
        if (idProyecto.HasValue)
        {
            asignadosQuery = asignadosQuery.Where(w => w.IdProyecto == idProyecto.Value);
        }

        var asignados = await asignadosQuery
            .GroupBy(w => w.IdAsignado!.Value)
            .Select(g => new { IdUsuario = g.Key, Minutos = g.Sum(w => (int?)w.MinutosPresupuesto) ?? 0 })
            .ToDictionaryAsync(x => x.IdUsuario, x => x.Minutos, ct);

        var proyectoPrincipalCrudo = await asignadosQuery
            .GroupBy(w => new { IdUsuario = w.IdAsignado!.Value, w.IdProyecto })
            .Select(g => new { g.Key.IdUsuario, g.Key.IdProyecto, Minutos = g.Sum(w => (int?)w.MinutosPresupuesto) ?? 0 })
            .ToListAsync(ct);
        var proyectoPorUsuario = proyectoPrincipalCrudo
            .GroupBy(x => x.IdUsuario)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Minutos).First().IdProyecto);
        var nombresProyecto = await contexto.TblProyecto.AsNoTracking()
            .Where(p => proyectoPorUsuario.Values.Contains(p.IdProyecto))
            .ToDictionaryAsync(p => p.IdProyecto, p => p.Nombre, ct);

        var consumidos = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(r => r.Activo && usuarios.Contains(r.IdUsuario)
                && r.Fecha >= DateOnly.FromDateTime(desde) && r.Fecha <= DateOnly.FromDateTime(hasta))
            .GroupBy(r => r.IdUsuario)
            .Select(g => new { IdUsuario = g.Key, Minutos = g.Sum(r => r.Minutos) })
            .ToDictionaryAsync(x => x.IdUsuario, x => x.Minutos, ct);

        var cacheMinutosLaborables = new Dictionary<int, int>();
        var resultado = new List<CargaTrabajoEmpleadoResponse>();

        foreach (var idUsuario in usuarios)
        {
            if (!datosUsuarios.TryGetValue(idUsuario, out var datos))
            {
                continue;
            }

            var minutosAsignados = asignados.GetValueOrDefault(idUsuario, 0);
            var minutosConsumidos = consumidos.GetValueOrDefault(idUsuario, 0);

            decimal? horasDisponibles = null;
            if (datos.IdHorario.HasValue)
            {
                if (!cacheMinutosLaborables.TryGetValue(datos.IdHorario.Value, out var minutosLaborables))
                {
                    minutosLaborables = await calendario.CalcularMinutosLaboralesAsync(desde, hasta, datos.IdHorario.Value, ct);
                    cacheMinutosLaborables[datos.IdHorario.Value] = minutosLaborables;
                }
                horasDisponibles = Math.Round(minutosLaborables / 60m, 1);
            }

            var horasConsumidas = Math.Round(minutosConsumidos / 60m, 1);
            resultado.Add(new CargaTrabajoEmpleadoResponse
            {
                IdUsuario = idUsuario,
                Nombre = datos.Nombre,
                Area = datos.Area,
                ProyectoPrincipal = proyectoPorUsuario.TryGetValue(idUsuario, out var idProy) && nombresProyecto.TryGetValue(idProy, out var nombreProy)
                    ? nombreProy
                    : null,
                HorasAsignadas = Math.Round(minutosAsignados / 60m, 1),
                HorasConsumidas = horasConsumidas,
                HorasDisponibles = horasDisponibles,
                PorcentajeUtilizacion = horasDisponibles is > 0 ? Math.Round(100m * horasConsumidas / horasDisponibles.Value, 1) : null,
            });
        }

        return resultado;
    }

    /// <summary>
    /// Fila unificada de un elemento (WorkItem/Ticket/Solicitud/Incidente/Release) atribuido a
    /// un colaborador, para el desglose de carga de trabajo. FechaCierre null = sigue abierto
    /// (EsPendiente distingue Pendiente/EnProceso dentro de los abiertos); FechaCierre con
    /// valor = ya llego a un estatus terminal en esa fecha (Terminado, si cae en el periodo).
    /// FechaLimite es el compromiso/SLA/plan de cada entidad (para Retrasos); FechaInicio es el
    /// arranque para PromedioDuracionDias.
    /// </summary>
    private sealed record ItemCargaDetalle(
        int IdUsuario, bool EsPendiente, DateTime? FechaCierre, DateTime? FechaLimite, DateTime FechaInicio);

    /// <summary>
    /// Desglose por conteo de elementos (no horas), estilo reporte "Carga de trabajo" del GT,
    /// sumando TODOS los tipos de elemento (WorkItem, Ticket, Solicitud, Incidente, Release):
    /// Pendiente/EnProceso = abiertos al cierre del periodo; Terminado = cerrados durante el
    /// periodo; Retrasos/EficienciaEntrega = de los terminados con compromiso/SLA/plan, cuantos
    /// se entregaron tarde; PromedioDuracionDias = dias promedio arranque -> cierre de los
    /// terminados en el periodo. WorkItem/Ticket se atribuyen por asignado; Solicitud por
    /// solicitante (es lo que esa persona tiene pendiente de que le resuelvan); Incidente/
    /// Release no tienen asignacion directa a un usuario en el modelo, se atribuyen al
    /// responsable del proyecto (mismo criterio ya usado para acotarlos por alcance en el
    /// resto del dashboard). idProyecto solo filtra las entidades que de verdad tienen
    /// proyecto (WorkItem/Solicitud/Incidente/Release); Ticket no tiene ese concepto y se
    /// excluye del desglose cuando el filtro de proyecto esta activo.
    /// </summary>
    private static async Task<List<CargaTrabajoDetalleResponse>> ArmarDesgloseCargaTrabajoAsync(
        DbContextGTE contexto, List<int> usuarios, Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        if (usuarios.Count == 0)
        {
            return [];
        }

        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;

        var itemsWorkItem = await ObtenerItemsCargaWorkItemAsync(contexto, usuarios, idProyecto, desde, hasta, ct);
        List<ItemCargaDetalle> itemsTicket = idProyecto.HasValue
            ? []
            : await ObtenerItemsCargaTicketAsync(contexto, usuarios, desde, hasta, ct);
        var itemsSolicitud = await ObtenerItemsCargaSolicitudAsync(contexto, usuarios, idProyecto, desde, hasta, ct);
        var itemsIncidente = await ObtenerItemsCargaIncidenteAsync(contexto, usuarios, idProyecto, desde, hasta, ct);
        var itemsRelease = await ObtenerItemsCargaReleaseAsync(contexto, usuarios, idProyecto, desde, hasta, ct);

        var todos = itemsWorkItem.Concat(itemsTicket).Concat(itemsSolicitud)
            .Concat(itemsIncidente).Concat(itemsRelease).ToList();

        var resultado = new List<CargaTrabajoDetalleResponse>();
        foreach (var idUsuario in usuarios)
        {
            if (!datosUsuarios.TryGetValue(idUsuario, out var datos))
            {
                continue;
            }

            var propios = todos.Where(f => f.IdUsuario == idUsuario).ToList();
            var abiertos = propios.Where(f => f.FechaCierre == null || f.FechaCierre > fechaCorte).ToList();

            var pendiente = abiertos.Count(f => f.EsPendiente);
            var enProceso = abiertos.Count(f => !f.EsPendiente);
            var terminadosEnPeriodo = propios.Where(f =>
                f.FechaCierre != null && f.FechaCierre >= desde && f.FechaCierre <= hasta).ToList();

            var conLimite = terminadosEnPeriodo.Where(f => f.FechaLimite.HasValue).ToList();
            var retrasos = conLimite.Count(f => f.FechaCierre > f.FechaLimite);
            var eficienciaEntrega = conLimite.Count > 0
                ? Math.Round(100m * (conLimite.Count - retrasos) / conLimite.Count, 1)
                : (decimal?)null;

            var promedioDuracionDias = terminadosEnPeriodo.Count > 0
                ? Math.Round((decimal)terminadosEnPeriodo.Average(f => (f.FechaCierre!.Value - f.FechaInicio).TotalDays), 1)
                : (decimal?)null;

            resultado.Add(new CargaTrabajoDetalleResponse
            {
                IdUsuario = idUsuario,
                Nombre = datos.Nombre,
                Pendiente = pendiente,
                EnProceso = enProceso,
                Terminado = terminadosEnPeriodo.Count,
                Retrasos = retrasos,
                PromedioDuracionDias = promedioDuracionDias,
                Total = pendiente + enProceso + terminadosEnPeriodo.Count,
                EficienciaEntrega = eficienciaEntrega,
            });
        }

        return resultado;
    }

    private static async Task<List<ItemCargaDetalle>> ObtenerItemsCargaWorkItemAsync(
        DbContextGTE contexto, List<int> usuarios, int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var query = contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaRegistro <= hasta && (w.FechaFin == null || w.FechaFin >= desde));
        if (idProyecto.HasValue)
        {
            query = query.Where(w => w.IdProyecto == idProyecto.Value);
        }

        return await query
            .Select(w => new ItemCargaDetalle(
                w.IdAsignado!.Value,
                w.IdEstatusWorkItem == 1,
                w.FechaFin,
                w.FechaCompromiso,
                w.FechaInicio ?? w.FechaRegistro))
            .ToListAsync(ct);
    }

    /// <summary>Solo tickets ya asignados: uno sin asignar todavia no es carga de nadie. Sin concepto de proyecto.</summary>
    private static async Task<List<ItemCargaDetalle>> ObtenerItemsCargaTicketAsync(
        DbContextGTE contexto, List<int> usuarios, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        return await contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.IdAsignado != null && usuarios.Contains(t.IdAsignado.Value)
                && t.FechaRegistro <= hasta && (t.FechaResolucion == null || t.FechaResolucion >= desde))
            .Select(t => new ItemCargaDetalle(
                t.IdAsignado!.Value,
                t.IdEstatusTicket == EstatusTicket.Nuevo || t.IdEstatusTicket == EstatusTicket.Asignado,
                (t.IdEstatusTicket == EstatusTicket.Resuelto || t.IdEstatusTicket == EstatusTicket.Cerrado)
                    ? t.FechaResolucion : null,
                t.FechaLimiteResolucion,
                t.FechaRegistro))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Atribuida al solicitante: es lo que esa persona tiene pendiente de que le resuelvan,
    /// no una tarea que ejecuta. FechaLimite = FechaDeseada (lo que el solicitante pidio).
    /// </summary>
    private static async Task<List<ItemCargaDetalle>> ObtenerItemsCargaSolicitudAsync(
        DbContextGTE contexto, List<int> usuarios, int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        // FechaMovto es un campo generico de auditoria (se actualiza en CUALQUIER cambio de
        // estatus, no solo al cerrar), asi que el corte por fecha solo puede aplicarse a las
        // filas ya terminales -- una solicitud todavia abierta se conserva sin importar cuando
        // fue su ultimo movimiento.
        var esTerminal = new[] { EstatusSolicitud.Rechazada, EstatusSolicitud.Convertida, EstatusSolicitud.Cancelada };
        var query = contexto.TblSolicitud.AsNoTracking()
            .Where(s => s.Activo && s.IdEstatusSolicitud != EstatusSolicitud.Borrador && usuarios.Contains(s.IdSolicitante)
                && s.FechaRegistro <= hasta
                && (!esTerminal.Contains(s.IdEstatusSolicitud) || s.FechaMovto == null || s.FechaMovto >= desde));
        if (idProyecto.HasValue)
        {
            query = query.Where(s => s.IdProyecto == idProyecto.Value);
        }

        return await query
            .Select(s => new ItemCargaDetalle(
                s.IdSolicitante,
                s.IdEstatusSolicitud == EstatusSolicitud.Enviada,
                esTerminal.Contains(s.IdEstatusSolicitud) ? s.FechaMovto : null,
                s.FechaDeseada == null ? null : s.FechaDeseada.Value.ToDateTime(TimeOnly.MinValue),
                s.FechaRegistro))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Sin asignacion directa a un usuario en el modelo: se atribuye al responsable del
    /// proyecto (mismo criterio de IncidenteQueryService.ObtenerRelevantesAsync). FechaLimite
    /// usa el mismo umbral de "vencido" que el resto del dashboard (UmbralDiasIncidenteVencido).
    /// </summary>
    private static async Task<List<ItemCargaDetalle>> ObtenerItemsCargaIncidenteAsync(
        DbContextGTE contexto, List<int> usuarios, int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var query =
            from i in contexto.TblIncidente.AsNoTracking()
            join p in contexto.TblProyecto.AsNoTracking() on i.IdProyecto equals p.IdProyecto
            where i.Activo && p.IdResponsable != null && usuarios.Contains(p.IdResponsable.Value)
                  && i.FechaRegistro <= hasta && (i.FechaResolucion == null || i.FechaResolucion >= desde)
            select new { i, p.IdResponsable };
        if (idProyecto.HasValue)
        {
            query = query.Where(x => x.i.IdProyecto == idProyecto.Value);
        }

        var esTerminal = new[] { EstatusIncidente.Resuelto, EstatusIncidente.Cerrado };
        return await query
            .Select(x => new ItemCargaDetalle(
                x.IdResponsable!.Value,
                x.i.IdEstatusIncidente == EstatusIncidente.Detectado,
                esTerminal.Contains(x.i.IdEstatusIncidente) ? x.i.FechaResolucion : null,
                x.i.FechaOcurrencia.AddDays(UmbralDiasIncidenteVencido),
                x.i.FechaOcurrencia))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Sin asignacion directa a un usuario: se atribuye al responsable del proyecto. Revertido/
    /// Cancelado cuentan como Terminado (ya no es trabajo abierto), igual que WorkItem cuenta
    /// Cancelado dentro de Terminado.
    /// </summary>
    private static async Task<List<ItemCargaDetalle>> ObtenerItemsCargaReleaseAsync(
        DbContextGTE contexto, List<int> usuarios, int? idProyecto, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        // Igual que en Solicitud: FechaMovto es generico (cambia con cualquier transicion), asi
        // que el corte por fecha de cierre solo aplica a las filas ya terminales.
        var esTerminal = new[] { EstatusRelease.Liberado, EstatusRelease.Revertido, EstatusRelease.Cancelado };
        var query =
            from r in contexto.TblRelease.AsNoTracking()
            join p in contexto.TblProyecto.AsNoTracking() on r.IdProyecto equals p.IdProyecto
            where r.Activo && p.IdResponsable != null && usuarios.Contains(p.IdResponsable.Value)
                  && r.FechaRegistro <= hasta
                  && (!esTerminal.Contains(r.IdEstatusRelease)
                      || (r.FechaLiberacion ?? r.FechaMovto) == null || (r.FechaLiberacion ?? r.FechaMovto) >= desde)
            select new { r, p.IdResponsable };
        if (idProyecto.HasValue)
        {
            query = query.Where(x => x.r.IdProyecto == idProyecto.Value);
        }

        return await query
            .Select(x => new ItemCargaDetalle(
                x.IdResponsable!.Value,
                x.r.IdEstatusRelease == EstatusRelease.EnPreparacion,
                esTerminal.Contains(x.r.IdEstatusRelease) ? (x.r.FechaLiberacion ?? x.r.FechaMovto) : null,
                x.r.FechaPlan == null ? null : x.r.FechaPlan.Value.ToDateTime(TimeOnly.MinValue),
                x.r.FechaRegistro))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------------
    // Puntaje / evaluacion mensual (base de Empleado del mes, Rankings, Comparativos)
    // ------------------------------------------------------------------

    private static async Task<Dictionary<int, (InsumosPuntajeEmpleado Insumos, PuntajeEmpleadoCalculado Puntaje)>> ObtenerPuntajesBatchAsync(
        DbContextGTE contexto, List<int> usuarios, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        if (usuarios.Count == 0)
        {
            return [];
        }

        var fechaCorte = hasta > DateTime.Now ? DateTime.Now : hasta;

        var wiTerminados = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7))
            .Select(w => new { IdUsuario = w.IdAsignado!.Value, w.FechaCompromiso, w.FechaFin })
            .ToListAsync(ct);

        var tkCerrados = await contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.IdAsignado != null && usuarios.Contains(t.IdAsignado.Value)
                && t.IdEstatusTicket == 6 && t.FechaResolucion != null && t.FechaResolucion >= desde && t.FechaResolucion <= hasta)
            .Select(t => new { IdUsuario = t.IdAsignado!.Value, FechaCompromiso = t.FechaLimiteResolucion, FechaFin = t.FechaResolucion })
            .ToListAsync(ct);

        var reaperturas = await (
            from inc in contexto.TblIncidente.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on inc.IdWorkItemCorrectivo equals w.IdWorkItem
            where inc.Activo && w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta && inc.FechaOcurrencia > w.FechaFin
            select new { IdUsuario = w.IdAsignado!.Value, w.IdWorkItem })
            .Distinct()
            .ToListAsync(ct);

        var wiVigentes = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaRegistro <= hasta && (w.FechaFin == null || w.FechaFin > fechaCorte))
            .Select(w => new { IdUsuario = w.IdAsignado!.Value, w.FechaCompromiso })
            .ToListAsync(ct);

        var tkVigentes = await contexto.TblTicket.AsNoTracking()
            .Where(t => t.Activo && t.IdAsignado != null && usuarios.Contains(t.IdAsignado.Value)
                && t.FechaRegistro <= hasta && t.IdEstatusTicket != 6)
            .Select(t => new { IdUsuario = t.IdAsignado!.Value, FechaCompromiso = t.FechaLimiteResolucion })
            .ToListAsync(ct);

        var comentarios = await (
            from c in contexto.TblComentario.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on c.IdEntidad equals w.IdWorkItem
            join autor in contexto.TblUsuario.AsNoTracking() on c.UsuarioRegistro equals autor.Dominio
            where c.Activo && c.Entidad == "WorkItem" && c.FechaRegistro >= desde && c.FechaRegistro <= hasta
                && usuarios.Contains(autor.IdUsuario)
            select new { IdAutor = autor.IdUsuario, IdAsignadoItem = w.IdAsignado })
            .ToListAsync(ct);

        var promedioTerminados = (decimal)(wiTerminados.Count + tkCerrados.Count) / usuarios.Count;
        var comentariosPropiosTotal = comentarios.Count(c => c.IdAsignadoItem == c.IdAutor);
        var comentariosAjenosTotal = comentarios.Count(c => c.IdAsignadoItem != null && c.IdAsignadoItem != c.IdAutor);
        var promedioComentariosPropios = (decimal)comentariosPropiosTotal / usuarios.Count;
        var promedioComentariosAjenos = (decimal)comentariosAjenosTotal / usuarios.Count;

        var resultado = new Dictionary<int, (InsumosPuntajeEmpleado, PuntajeEmpleadoCalculado)>();
        foreach (var idUsuario in usuarios)
        {
            var terminadosWi = wiTerminados.Where(x => x.IdUsuario == idUsuario).ToList();
            var terminadosTk = tkCerrados.Where(x => x.IdUsuario == idUsuario).ToList();
            var conCompromiso = terminadosWi.Count(x => x.FechaCompromiso.HasValue) + terminadosTk.Count(x => x.FechaCompromiso.HasValue);
            var aTiempo = terminadosWi.Count(x => x.FechaCompromiso.HasValue && x.FechaFin <= x.FechaCompromiso)
                + terminadosTk.Count(x => x.FechaCompromiso.HasValue && x.FechaFin <= x.FechaCompromiso);
            var reaperturasUsuario = reaperturas.Where(x => x.IdUsuario == idUsuario).Select(x => x.IdWorkItem).Distinct().Count();

            var vigentesWi = wiVigentes.Where(x => x.IdUsuario == idUsuario).ToList();
            var vigentesTk = tkVigentes.Where(x => x.IdUsuario == idUsuario).ToList();
            var vencidosTotal = vigentesWi.Count(x => x.FechaCompromiso.HasValue && x.FechaCompromiso < fechaCorte)
                + vigentesTk.Count(x => x.FechaCompromiso.HasValue && x.FechaCompromiso < fechaCorte);

            var comentariosPropios = comentarios.Count(c => c.IdAutor == idUsuario && c.IdAsignadoItem == idUsuario);
            var comentariosAjenos = comentarios.Count(c => c.IdAutor == idUsuario && c.IdAsignadoItem != null && c.IdAsignadoItem != idUsuario);

            var insumos = new InsumosPuntajeEmpleado(
                TerminadosPeriodo: terminadosWi.Count + terminadosTk.Count,
                ReaperturasPeriodo: reaperturasUsuario,
                ConCompromisoPeriodo: conCompromiso,
                ATiempoPeriodo: aTiempo,
                AsignadosVigentesPeriodo: vigentesWi.Count + vigentesTk.Count,
                VencidosVigentesPeriodo: vencidosTotal,
                ComentariosPropiosPeriodo: comentariosPropios,
                ComentariosEnItemsAjenosPeriodo: comentariosAjenos,
                PromedioTerminadosEquipo: promedioTerminados,
                PromedioComentariosPropiosEquipo: promedioComentariosPropios,
                PromedioComentariosAjenosEquipo: promedioComentariosAjenos);

            resultado[idUsuario] = (insumos, CalculadoraPuntaje.Calcular(insumos));
        }

        return resultado;
    }

    private static async Task<Dictionary<int, decimal?>> ObtenerIndiceEficienciaBatchAsync(
        DbContextGTE contexto, List<int> usuarios, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        if (usuarios.Count == 0)
        {
            return [];
        }

        var horasEstimadas = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
                && w.FechaFin != null && w.FechaFin >= desde && w.FechaFin <= hasta
                && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7))
            .GroupBy(w => w.IdAsignado!.Value)
            .Select(g => new { IdUsuario = g.Key, Minutos = g.Sum(w => (int?)w.MinutosPresupuesto) ?? 0 })
            .ToDictionaryAsync(x => x.IdUsuario, x => x.Minutos, ct);

        var horasReales = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(r => r.Activo && usuarios.Contains(r.IdUsuario) && r.Fecha >= DateOnly.FromDateTime(desde) && r.Fecha <= DateOnly.FromDateTime(hasta))
            .GroupBy(r => r.IdUsuario)
            .Select(g => new { IdUsuario = g.Key, Minutos = g.Sum(r => r.Minutos) })
            .ToDictionaryAsync(x => x.IdUsuario, x => x.Minutos, ct);

        var resultado = new Dictionary<int, decimal?>();
        foreach (var idUsuario in usuarios)
        {
            var estimados = horasEstimadas.GetValueOrDefault(idUsuario, 0);
            var reales = horasReales.GetValueOrDefault(idUsuario, 0);
            resultado[idUsuario] = reales > 0 ? Math.Min(TopeIndiceEficiencia, Math.Round(100m * estimados / reales, 1)) : null;
        }

        return resultado;
    }

    // ------------------------------------------------------------------
    // Empleado del mes
    // ------------------------------------------------------------------

    private async Task<(EmpleadoDelMesResponse? Actual, List<EmpleadoDelMesResponse> Historico)> ObtenerEmpleadoDelMesAsync(
        DbContextGTE contexto, List<int> usuarios, Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        int anio, int mes, Dictionary<int, (InsumosPuntajeEmpleado Insumos, PuntajeEmpleadoCalculado Puntaje)> puntajesMesActual,
        CancellationToken ct)
    {
        const int MesesHistorico = 6;
        var historico = new List<EmpleadoDelMesResponse>();
        EmpleadoDelMesResponse? actual = null;

        var (anioIter, mesIter) = (anio, mes);
        for (var i = 0; i < MesesHistorico; i++)
        {
            var puntajes = i == 0 ? puntajesMesActual : await ObtenerPuntajesBatchAsync(contexto, usuarios, RangoMes(anioIter, mesIter).Desde, RangoMes(anioIter, mesIter).Hasta, ct);
            var item = await ArmarGanadorAsync(contexto, datosUsuarios, puntajes, anioIter, mesIter, ct);

            if (i == 0)
            {
                actual = item;
            }
            else if (item != null)
            {
                historico.Add(item);
            }

            (anioIter, mesIter) = MesAnterior(anioIter, mesIter);
        }

        return (actual, historico);
    }

    private async Task<EmpleadoDelMesResponse?> ArmarGanadorAsync(
        DbContextGTE contexto, Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        Dictionary<int, (InsumosPuntajeEmpleado Insumos, PuntajeEmpleadoCalculado Puntaje)> puntajes,
        int anio, int mes, CancellationToken ct)
    {
        var conDato = puntajes.Where(p => p.Value.Puntaje.PuntajeGeneral.HasValue).ToList();
        if (conDato.Count == 0)
        {
            return null;
        }

        var ganador = conDato.OrderByDescending(p => p.Value.Puntaje.PuntajeGeneral!.Value).First();
        if (!datosUsuarios.TryGetValue(ganador.Key, out var datos))
        {
            return null;
        }

        var urlFoto = await ObtenerUrlFotoAsync(contexto, ganador.Key, ct);
        var puntaje = ganador.Value.Puntaje.PuntajeGeneral!.Value;

        return new EmpleadoDelMesResponse
        {
            Empleado = new EmpleadoResumenResponse { IdUsuario = ganador.Key, Nombre = datos.Nombre, Area = datos.Area, Puesto = datos.Puesto, UrlFoto = urlFoto },
            Anio = anio,
            Mes = mes,
            Puntaje = puntaje,
            Motivo = $"Mayor puntaje del mes ({puntaje:0.0} pts) entre {conDato.Count} colaboradores evaluados.",
        };
    }

    // ------------------------------------------------------------------
    // Rankings
    // ------------------------------------------------------------------

    private static List<RankingResponse> ArmarRankings(
        Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        Dictionary<int, (InsumosPuntajeEmpleado Insumos, PuntajeEmpleadoCalculado Puntaje)> puntajes,
        Dictionary<int, decimal?> eficiencia)
    {
        decimal? Dimension(int id, DimensionEvaluacion dim) =>
            puntajes.TryGetValue(id, out var p) ? p.Puntaje.Dimensiones.FirstOrDefault(d => d.Dimension == dim)?.Valor : null;

        RankingResponse Armar(string metrica, Func<int, decimal?> selector)
        {
            var valores = datosUsuarios.Keys
                .Select(id => new { IdUsuario = id, Valor = selector(id) })
                .Where(x => x.Valor.HasValue)
                .Select(x => new RankingItemResponse
                {
                    IdUsuario = x.IdUsuario,
                    Nombre = datosUsuarios[x.IdUsuario].Nombre,
                    Area = datosUsuarios[x.IdUsuario].Area,
                    Valor = x.Valor!.Value,
                })
                .ToList();

            return new RankingResponse
            {
                Metrica = metrica,
                Top10 = valores.OrderByDescending(v => v.Valor).Take(10).ToList(),
                Bottom10 = valores.OrderBy(v => v.Valor).Take(10).ToList(),
            };
        }

        return
        [
            Armar("Puntaje", id => puntajes.TryGetValue(id, out var p) ? p.Puntaje.PuntajeGeneral : null),
            Armar("Productividad", id => Dimension(id, DimensionEvaluacion.Productividad)),
            Armar("Eficiencia", id => eficiencia.GetValueOrDefault(id)),
            Armar("Entregas a tiempo", id => Dimension(id, DimensionEvaluacion.Puntualidad)),
        ];
    }

    // ------------------------------------------------------------------
    // Comparativos
    // ------------------------------------------------------------------

    private async Task<ComparativosResponse> ArmarComparativosAsync(
        DbContextGTE contexto, List<int> usuarios, Dictionary<int, DatosUsuarioDashboard> datosUsuarios,
        Dictionary<int, (InsumosPuntajeEmpleado Insumos, PuntajeEmpleadoCalculado Puntaje)> puntajes,
        List<int>? proyectosVisibles, int? idProyectoFiltro, int? idUsuarioFoco,
        int anio, int mes, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var resultado = new ComparativosResponse();

        if (idUsuarioFoco.HasValue && puntajes.TryGetValue(idUsuarioFoco.Value, out var puntajeFoco) && puntajeFoco.Puntaje.PuntajeGeneral.HasValue)
        {
            var restoConDato = puntajes.Where(p => p.Key != idUsuarioFoco.Value && p.Value.Puntaje.PuntajeGeneral.HasValue).ToList();
            var promedioResto = restoConDato.Count > 0 ? restoConDato.Average(p => p.Value.Puntaje.PuntajeGeneral!.Value) : 0m;
            resultado.EmpleadoVsPromedioEquipo =
            [
                new ComparativoBarraResponse
                {
                    Etiqueta = datosUsuarios.TryGetValue(idUsuarioFoco.Value, out var d) ? d.Nombre : "Empleado",
                    Valor = puntajeFoco.Puntaje.PuntajeGeneral!.Value,
                },
                new ComparativoBarraResponse { Etiqueta = "Promedio del equipo", Valor = Math.Round(promedioResto, 1) },
            ];
        }

        resultado.PorArea = puntajes
            .Where(p => p.Value.Puntaje.PuntajeGeneral.HasValue)
            .GroupBy(p => datosUsuarios.TryGetValue(p.Key, out var d) ? d.Area ?? "Sin area" : "Sin area")
            .Select(g => new ComparativoBarraResponse { Etiqueta = g.Key, Valor = Math.Round(g.Average(x => x.Value.Puntaje.PuntajeGeneral!.Value), 1) })
            .OrderByDescending(x => x.Valor)
            .ToList();

        var proyectosParaComparar = idProyectoFiltro.HasValue ? [idProyectoFiltro.Value] : proyectosVisibles;
        if (usuarios.Count > 0 && (proyectosParaComparar == null || proyectosParaComparar.Count > 0))
        {
            var query =
                from r in contexto.TblRegistroTiempo.AsNoTracking()
                join w in contexto.TblWorkItem.AsNoTracking() on r.IdWorkItem equals w.IdWorkItem
                join pr in contexto.TblProyecto.AsNoTracking() on w.IdProyecto equals pr.IdProyecto
                where r.Activo && usuarios.Contains(r.IdUsuario)
                    && r.Fecha >= DateOnly.FromDateTime(desde) && r.Fecha <= DateOnly.FromDateTime(hasta)
                select new { pr.IdProyecto, pr.Nombre, r.Minutos };

            if (proyectosParaComparar != null)
            {
                query = query.Where(x => proyectosParaComparar.Contains(x.IdProyecto));
            }

            resultado.PorProyecto = await query
                .GroupBy(x => x.Nombre)
                .Select(g => new ComparativoBarraResponse { Etiqueta = g.Key, Valor = Math.Round(g.Sum(x => x.Minutos) / 60m, 1) })
                .OrderByDescending(x => x.Valor)
                .Take(10)
                .ToListAsync(ct);
        }

        var desdeAnioActual = new DateTime(anio, 1, 1);
        var desdeAnioAnterior = new DateTime(anio - 1, 1, 1);
        var hastaAnioAnterior = new DateTime(anio - 1, mes, 1).AddMonths(1).AddTicks(-1);

        var terminadosAnioActual = usuarios.Count == 0 ? 0 : await contexto.TblWorkItem.AsNoTracking().CountAsync(w =>
            w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
            && w.FechaFin != null && w.FechaFin >= desdeAnioActual && w.FechaFin <= hasta
            && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7), ct);
        var terminadosAnioAnterior = usuarios.Count == 0 ? 0 : await contexto.TblWorkItem.AsNoTracking().CountAsync(w =>
            w.Activo && w.IdAsignado != null && usuarios.Contains(w.IdAsignado.Value)
            && w.FechaFin != null && w.FechaFin >= desdeAnioAnterior && w.FechaFin <= hastaAnioAnterior
            && (w.IdEstatusWorkItem == 6 || w.IdEstatusWorkItem == 7), ct);

        resultado.AnioActualVsAnterior =
        [
            new ComparativoBarraResponse { Etiqueta = anio.ToString(), Valor = terminadosAnioActual },
            new ComparativoBarraResponse { Etiqueta = (anio - 1).ToString(), Valor = terminadosAnioAnterior },
        ];

        return resultado;
    }
}
