using GTE.Application.DTOs.Responses.CentroMando;
using GTE.Application.Interfaces;
using GTE.Domain.CentroMando;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Lectura del Centro de Mando TI. Todo sale de lo que ya materializo
/// <see cref="MotorEvaluacionCentroMando"/>: aqui NO se recalcula nada, para que lo que ve
/// el gerente sea exactamente lo que se evaluo y se puede auditar, y no un numero distinto
/// cada vez que se abre la pantalla.
///
/// La unica logica que corre en vivo es el IT Health Score, porque combina las evaluaciones
/// ya guardadas de todos los equipos y depende de cuales existan en el periodo.
/// </summary>
public class CentroMandoQueryService(FabricaContexto fabrica) : ICentroMandoQueryService
{
    private const int MesesTendencia = 6;

    public async Task<CentroMandoResponse> ObtenerCentroMandoAsync(
        int anio, int mes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var equipos = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.Activo && e.Anio == anio && e.Mes == mes)
            .Select(e => e.IdEquipo)
            .ToListAsync(cancellationToken);

        var responsables = new List<EvaluacionResponsableResponse>();
        foreach (var idEquipo in equipos)
        {
            var evaluacion = await ArmarEvaluacionAsync(contexto, idEquipo, anio, mes, cancellationToken);
            if (evaluacion is not null)
            {
                responsables.Add(evaluacion);
            }
        }

        var indicadoresGlobales = responsables
            .SelectMany(r => r.Indicadores)
            .Select(AIndicadorEvaluado)
            .ToList();

        var scores = responsables
            .Select(r => new ScoreResponsable(r.IdEquipo, r.Equipo, r.Ambito, r.ScoreGeneral, r.Semaforo))
            .ToList();

        var salud = CalculadoraSaludTi.Calcular(scores, indicadoresGlobales);
        var alertas = await ObtenerAlertasAsync(anio, mes, soloVigentes: true, cancellationToken);
        var tendencias = await ArmarTendenciasAsync(contexto, anio, mes, cancellationToken);

        var ultimoCalculo = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.Activo && e.Anio == anio && e.Mes == mes)
            .MaxAsync(e => (DateTime?)e.FechaCalculo, cancellationToken);

        return new CentroMandoResponse
        {
            Anio = anio,
            Mes = mes,
            SaludTi = salud.Score,
            SaludSemaforo = salud.Semaforo,
            SaludNivel = salud.Nivel,
            SaludTopada = salud.Topado,
            RazonTope = salud.RazonTope,
            Dimensiones = salud.Dimensiones
                .Select(d => new DimensionSaludResponse
                {
                    Dimension = d.Dimension,
                    Peso = d.Peso,
                    Valor = d.Valor,
                    Semaforo = d.Semaforo,
                })
                .ToList(),
            Responsables = responsables.OrderBy(r => r.ScoreGeneral ?? decimal.MaxValue).ToList(),
            Alertas = alertas,
            Tendencias = tendencias,
            FechaUltimoCalculo = ultimoCalculo,
        };
    }

    public async Task<EvaluacionResponsableResponse?> ObtenerEvaluacionAsync(
        int idEquipo, int anio, int mes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await ArmarEvaluacionAsync(contexto, idEquipo, anio, mes, cancellationToken);
    }

    public async Task<IReadOnlyList<IndicadorGestionResponse>> ObtenerCatalogoAsync(
        string? ambito, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = contexto.TblIndicadorGestion.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ambito))
        {
            consulta = consulta.Where(i => i.Ambito == ambito);
        }

        return await consulta
            .OrderBy(i => i.Ambito).ThenByDescending(i => i.Peso).ThenBy(i => i.Nombre)
            .Select(i => new IndicadorGestionResponse
            {
                IdIndicadorGestion = i.IdIndicadorGestion,
                Clave = i.Clave,
                Nombre = i.Nombre,
                Descripcion = i.Descripcion,
                Categoria = i.Categoria,
                Ambito = i.Ambito,
                Origen = i.Origen,
                Formula = i.Formula,
                Unidad = i.Unidad,
                Meta = i.Meta,
                UmbralAlerta = i.UmbralAlerta,
                Direccion = i.Direccion,
                Peso = i.Peso,
                PonderaEnScore = i.PonderaEnScore,
                Periodicidad = i.Periodicidad,
                InterpretacionBuena = i.InterpretacionBuena,
                InterpretacionMala = i.InterpretacionMala,
                AccionSugerida = i.AccionSugerida,
                Activo = i.Activo,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertaGestionResponse>> ObtenerAlertasAsync(
        int anio, int mes, bool soloVigentes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // Se unen sin proyectar, se filtra y ordena por columnas reales, y se proyecta al
        // final (TRAMPA EF 7.8 -- ver PlaneacionQueryService).
        var consulta = contexto.TblAlertaGestion.AsNoTracking()
            .Where(a => a.Activo && a.Anio == anio && a.Mes == mes);

        if (soloVigentes)
        {
            consulta = consulta.Where(a => !a.Atendida);
        }

        var alertas = await consulta
            .OrderBy(a => a.Severidad == "Critica" ? 0 : a.Severidad == "Atencion" ? 1 : 2)
            .ThenByDescending(a => a.FechaRegistro)
            .Select(a => new AlertaGestionResponse
            {
                IdAlertaGestion = a.IdAlertaGestion,
                Clave = a.Clave,
                Severidad = a.Severidad,
                IdEquipo = a.IdEquipo,
                Equipo = a.IdEquipoNavigation != null ? a.IdEquipoNavigation.Nombre : null,
                Indicador = a.IdIndicadorGestionNavigation != null ? a.IdIndicadorGestionNavigation.Nombre : null,
                Anio = a.Anio,
                Mes = a.Mes,
                Titulo = a.Titulo,
                Mensaje = a.Mensaje,
                RequiereGerencia = a.RequiereGerencia,
                Atendida = a.Atendida,
                FechaRegistro = a.FechaRegistro,
            })
            .ToListAsync(cancellationToken);

        return alertas;
    }

    // ------------------------------------------------------------------

    private static async Task<EvaluacionResponsableResponse?> ArmarEvaluacionAsync(
        DbContextGTE contexto, int idEquipo, int anio, int mes, CancellationToken ct)
    {
        var cabecera = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.Activo && e.IdEquipo == idEquipo && e.Anio == anio && e.Mes == mes)
            .Select(e => new
            {
                e.IdEvaluacionEquipo,
                e.IdEquipo,
                Equipo = e.IdEquipoNavigation.Nombre,
                Ambito = e.IdEquipoNavigation.AmbitoCentroMando,
                e.IdResponsable,
                Responsable = e.IdResponsableNavigation != null ? e.IdResponsableNavigation.Nombre : null,
                e.ScoreGeneral,
                e.Nivel,
                e.Semaforo,
                e.IndiceCarga,
                e.IndicadoresConDato,
                e.IndicadoresTotales,
            })
            .FirstOrDefaultAsync(ct);

        if (cabecera is null)
        {
            return null;
        }

        var detalle = await contexto.TblEvaluacionEquipoDetalle.AsNoTracking()
            .Where(d => d.IdEvaluacionEquipo == cabecera.IdEvaluacionEquipo)
            .Select(d => new IndicadorEvaluadoResponse
            {
                IdIndicadorGestion = d.IdIndicadorGestion,
                Clave = d.IdIndicadorGestionNavigation.Clave,
                Nombre = d.IdIndicadorGestionNavigation.Nombre,
                Categoria = d.IdIndicadorGestionNavigation.Categoria,
                Ambito = d.IdIndicadorGestionNavigation.Ambito,
                Origen = d.IdIndicadorGestionNavigation.Origen,
                Unidad = d.IdIndicadorGestionNavigation.Unidad,
                Valor = d.Valor,
                ValorNormalizado = d.ValorNormalizado,
                Meta = d.IdIndicadorGestionNavigation.Meta,
                UmbralAlerta = d.IdIndicadorGestionNavigation.UmbralAlerta,
                Direccion = d.IdIndicadorGestionNavigation.Direccion,
                Peso = d.IdIndicadorGestionNavigation.Peso,
                PonderaEnScore = d.IdIndicadorGestionNavigation.PonderaEnScore,
                Semaforo = d.Semaforo,
                SinDatos = d.SinDatos,
                ValorPeriodoAnterior = d.ValorPeriodoAnterior,
                AccionSugerida = d.IdIndicadorGestionNavigation.AccionSugerida,
                InterpretacionMala = d.IdIndicadorGestionNavigation.InterpretacionMala,
            })
            .ToListAsync(ct);

        foreach (var indicador in detalle)
        {
            indicador.Tendencia = ResolverTendencia(indicador);
        }

        var causas = await contexto.TblDiagnosticoCausa.AsNoTracking()
            .Where(d => d.IdEvaluacionEquipo == cabecera.IdEvaluacionEquipo)
            .Select(d => new CausaDiagnosticoResponse
            {
                Causa = d.Causa,
                IndiceClave = d.IndiceClave,
                Valor = d.Valor,
                Umbral = d.Umbral,
                Evidencia = d.Evidencia ?? string.Empty,
            })
            .ToListAsync(ct);

        var (anioAnt, mesAnt) = mes == 1 ? (anio - 1, 12) : (anio, mes - 1);
        var scoreAnterior = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.IdEquipo == idEquipo && e.Anio == anioAnt && e.Mes == mesAnt)
            .Select(e => e.ScoreGeneral)
            .FirstOrDefaultAsync(ct);

        var urlFoto = cabecera.IdResponsable.HasValue
            ? await ObtenerUrlFotoAsync(contexto, cabecera.IdResponsable.Value, ct)
            : null;

        return new EvaluacionResponsableResponse
        {
            IdEquipo = cabecera.IdEquipo,
            Equipo = cabecera.Equipo,
            Ambito = cabecera.Ambito,
            IdResponsable = cabecera.IdResponsable,
            Responsable = cabecera.Responsable,
            UrlFoto = urlFoto,
            Anio = anio,
            Mes = mes,
            ScoreGeneral = cabecera.ScoreGeneral,
            ScoreMesAnterior = scoreAnterior,
            Nivel = cabecera.Nivel,
            Semaforo = cabecera.Semaforo,
            IndicadoresConDato = cabecera.IndicadoresConDato,
            IndicadoresTotales = cabecera.IndicadoresTotales,
            Carga = await ArmarCargaAsync(contexto, idEquipo, cabecera.IndiceCarga, anio, mes, ct),
            Indicadores = detalle,
            Diagnostico = causas,
            Conclusion = ArmarConclusion(cabecera.ScoreGeneral, cabecera.Nivel, detalle, causas),
        };
    }

    /// <summary>
    /// Conclusion automatica: primero QUE pasa (score y su banda), luego POR QUE, tomando la
    /// causa principal del diagnostico. Es el texto que hace que el dashboard no solo diga
    /// que un resultado es malo.
    /// </summary>
    private static string ArmarConclusion(
        decimal? score, string? nivel,
        IReadOnlyList<IndicadorEvaluadoResponse> indicadores,
        IReadOnlyList<CausaDiagnosticoResponse> causas)
    {
        if (score is null)
        {
            var conDato = indicadores.Count(i => !i.SinDatos);
            var ponderables = indicadores.Count(i => i.PonderaEnScore);
            var faltantes = indicadores
                .Where(i => i.SinDatos && i.PonderaEnScore)
                .OrderByDescending(i => i.Peso)
                .Take(3)
                .Select(i => i.Nombre)
                .ToList();

            var detalle = faltantes.Count > 0
                ? $" Lo que mas falta capturar: {string.Join(", ", faltantes)}."
                : string.Empty;

            return $"Sin datos suficientes para evaluar el periodo ({conDato} de {ponderables} indicadores "
                + $"con dato). No se publica un score porque la muestra no lo sostiene.{detalle}";
        }

        var enRojo = indicadores
            .Where(i => i.Semaforo == SemaforoCentroMando.Rojo && i.PonderaEnScore)
            .OrderByDescending(i => i.Peso)
            .Take(2)
            .Select(i => i.Nombre)
            .ToList();

        var encabezado = $"Desempeno {score:0.#}/100 ({DescribirNivel(nivel)}).";

        if (enRojo.Count > 0)
        {
            encabezado += $" Lo que mas pesa en contra: {string.Join(" y ", enRojo)}.";
        }

        var principal = causas.FirstOrDefault();
        if (principal is null)
        {
            return encabezado;
        }

        var atribucion = principal.Causa == CausaRaiz.Persona
            ? "El diagnostico apunta a la persona: ningun indice de proceso, recursos, dependencia o prioridad esta fuera de umbral."
            : $"El diagnostico apunta a {principal.Causa.ToUpperInvariant()}, no a la persona. {principal.Evidencia}";

        return $"{encabezado} {atribucion}";
    }

    private static string DescribirNivel(string? nivel) => nivel switch
    {
        NivelDesempeno.Excelente => "Excelente",
        NivelDesempeno.Optimo => "Optimo",
        NivelDesempeno.RequiereAtencion => "Requiere atencion",
        NivelDesempeno.Critico => "Critico",
        NivelDesempeno.CriticoSevero => "Critico severo",
        _ => "sin banda",
    };

    /// <summary>
    /// Tendencia contra el periodo anterior, respetando la direccion del indicador: en uno
    /// que debe BAJAR, un valor menor es "Mejora". Se ignoran variaciones menores al 2% para
    /// no marcar ruido como cambio.
    /// </summary>
    private static string? ResolverTendencia(IndicadorEvaluadoResponse indicador)
    {
        if (indicador.Valor is not { } actual || indicador.ValorPeriodoAnterior is not { } anterior)
        {
            return null;
        }

        if (anterior == 0)
        {
            return actual == 0 ? "Estable" : null;
        }

        var variacion = (actual - anterior) / Math.Abs(anterior) * 100m;
        if (Math.Abs(variacion) < 2m)
        {
            return "Estable";
        }

        var subio = variacion > 0;
        var subirEsBueno = indicador.Direccion == DireccionIndicador.Subir;
        return subio == subirEsBueno ? "Mejora" : "Empeora";
    }

    private static async Task<CargaEquipoResponse?> ArmarCargaAsync(
        DbContextGTE contexto, int idEquipo, decimal? indiceCarga, int anio, int mes, CancellationToken ct)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var miembros = await contexto.TblEquipoMiembro.AsNoTracking()
            .Where(m => m.Activo && m.IdEquipo == idEquipo)
            .Select(m => m.IdUsuario)
            .ToListAsync(ct);

        var minutosAsignados = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.Activo && w.IdEquipo == idEquipo
                && w.FechaRegistro <= hasta && (w.FechaFin == null || w.FechaFin >= desde))
            .SumAsync(w => (int?)w.MinutosPresupuesto, ct) ?? 0;

        var fechaDesde = DateOnly.FromDateTime(desde);
        var fechaHasta = DateOnly.FromDateTime(hasta);
        var minutosEjecutados = miembros.Count == 0
            ? 0
            : await contexto.TblRegistroTiempo.AsNoTracking()
                .Where(r => r.Activo && miembros.Contains(r.IdUsuario)
                    && r.Fecha >= fechaDesde && r.Fecha <= fechaHasta)
                .SumAsync(r => (int?)r.Minutos, ct) ?? 0;

        var asignadas = Math.Round(minutosAsignados / 60m, 1);
        var ejecutadas = Math.Round(minutosEjecutados / 60m, 1);
        var disponibles = indiceCarga is > 0 ? Math.Round(asignadas * 100m / indiceCarga.Value, 1) : 0m;

        return new CargaEquipoResponse
        {
            HorasDisponibles = disponibles,
            HorasAsignadas = asignadas,
            HorasEjecutadas = ejecutadas,
            IndiceCarga = indiceCarga,
            Situacion = ClasificarCarga(indiceCarga),
            Integrantes = miembros.Count,
        };
    }

    private static string? ClasificarCarga(decimal? indice) => indice switch
    {
        null => null,
        < 70m => "Subutilizado",
        <= 110m => "Adecuado",
        <= 130m => "SobrecargaModerada",
        _ => "SobrecargaCritica",
    };

    private static async Task<IReadOnlyList<TendenciaCentroMandoResponse>> ArmarTendenciasAsync(
        DbContextGTE contexto, int anio, int mes, CancellationToken ct)
    {
        var periodos = new List<(int Anio, int Mes)>();
        var cursorAnio = anio;
        var cursorMes = mes;
        for (var i = 0; i < MesesTendencia; i++)
        {
            periodos.Insert(0, (cursorAnio, cursorMes));
            (cursorAnio, cursorMes) = cursorMes == 1 ? (cursorAnio - 1, 12) : (cursorAnio, cursorMes - 1);
        }

        var anioMinimo = periodos.Min(p => p.Anio);

        var filas = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.Activo && e.Anio >= anioMinimo)
            .Select(e => new
            {
                e.IdEquipo,
                Equipo = e.IdEquipoNavigation.Nombre,
                e.Anio,
                e.Mes,
                e.ScoreGeneral,
            })
            .ToListAsync(ct);

        return filas
            .GroupBy(f => new { f.IdEquipo, f.Equipo })
            .Select(g => new TendenciaCentroMandoResponse
            {
                Serie = g.Key.Equipo,
                IdEquipo = g.Key.IdEquipo,
                Puntos = periodos
                    .Select(p => new PuntoTendenciaCentroMandoResponse
                    {
                        Anio = p.Anio,
                        Mes = p.Mes,
                        Valor = g.FirstOrDefault(f => f.Anio == p.Anio && f.Mes == p.Mes)?.ScoreGeneral,
                    })
                    .ToList(),
            })
            .OrderBy(t => t.Serie)
            .ToList();
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

    /// <summary>Reconstruye el tipo de dominio a partir del DTO, para reusar la calculadora pura.</summary>
    private static IndicadorEvaluado AIndicadorEvaluado(IndicadorEvaluadoResponse r) => new(
        new DefinicionIndicador(
            r.IdIndicadorGestion, r.Clave, r.Nombre, r.Categoria, r.Ambito, r.Origen, r.Unidad,
            r.Meta, r.UmbralAlerta, r.Direccion, r.Peso, r.PonderaEnScore),
        r.Valor, r.ValorNormalizado, r.Semaforo, r.SinDatos);
}
