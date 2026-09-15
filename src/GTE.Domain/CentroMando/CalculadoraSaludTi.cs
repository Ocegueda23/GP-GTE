namespace GTE.Domain.CentroMando;

/// <summary>Score de un responsable, tal como entra al IT Health Score.</summary>
public record ScoreResponsable(int IdEquipo, string Equipo, string? Ambito, decimal? Score, string? Semaforo);

/// <summary>Una de las dimensiones transversales del IT Health Score.</summary>
public record DimensionSalud(string Dimension, decimal Peso, decimal? Valor, string? Semaforo);

/// <summary>Resultado del IT Health Score, con la razon si quedo topado.</summary>
public record SaludTiCalculada(
    decimal? Score,
    string? Semaforo,
    string? Nivel,
    bool Topado,
    string? RazonTope,
    IReadOnlyList<DimensionSalud> Dimensiones,
    IReadOnlyList<ScoreResponsable> Responsables);

/// <summary>
/// IT Health Score del departamento.
///
/// Se arma con dos mitades: las tres areas (45%, 15 cada una) y cinco dimensiones
/// transversales que se calculan agrupando los indicadores de TODOS los equipos por su
/// Categoria -- Operacion 20%, Calidad 15%, Seguridad y continuidad 10%, Satisfaccion 5%,
/// Mejora continua 5%.
///
/// REGLA DE PISO -- es el punto del indicador, no un detalle: un promedio ponderado puro
/// deja que un trimestre excelente de Desarrollo disfrace una infraestructura inestable, y
/// entonces el numero de portada miente justo cuando mas importa. Por eso, si alguna
/// dimension critica (Seguridad y continuidad) o el score de CUALQUIER responsable esta en
/// rojo, el score global se topa en <see cref="TopeConRojo"/> -- se queda en la banda
/// "Requiere atencion" sin importar cuanto suba el promedio, y el dashboard dice por que.
/// </summary>
public static class CalculadoraSaludTi
{
    /// <summary>Techo del score global cuando algo critico esta en rojo.</summary>
    public const decimal TopeConRojo = 79m;

    public const string DimensionOperacion = "Operacion";
    public const string DimensionCalidad = "Calidad";
    public const string DimensionSeguridad = "Seguridad y continuidad";
    public const string DimensionSatisfaccion = "Satisfaccion";
    public const string DimensionMejora = "Mejora continua";

    private const decimal PesoAreas = 0.45m;
    private const decimal PesoOperacion = 0.20m;
    private const decimal PesoCalidad = 0.15m;
    private const decimal PesoSeguridad = 0.10m;
    private const decimal PesoSatisfaccion = 0.05m;
    private const decimal PesoMejora = 0.05m;

    /// <summary>
    /// Categorias de dbo.tblIndicadorGestion que alimentan cada dimension transversal.
    /// "Diagnostico" queda deliberadamente fuera: son senales, no metas.
    /// </summary>
    private static readonly Dictionary<string, string[]> CategoriasPorDimension = new()
    {
        [DimensionOperacion] = ["Cumplimiento", "Puntualidad", "Productividad", "Atencion", "TrabajoPendiente", "Eficiencia", "Tiempos", "Prioridades", "SLA"],
        [DimensionCalidad] = ["Calidad", "Reincidencia", "Incidencias"],
        [DimensionSeguridad] = ["Seguridad", "Continuidad", "Estabilidad", "Prevencion", "Capacidad"],
        [DimensionSatisfaccion] = ["Satisfaccion"],
        [DimensionMejora] = ["MejoraContinua", "Automatizacion", "Documentacion", "Colaboracion"],
    };

    private static readonly Dictionary<string, decimal> PesoPorDimension = new()
    {
        [DimensionOperacion] = PesoOperacion,
        [DimensionCalidad] = PesoCalidad,
        [DimensionSeguridad] = PesoSeguridad,
        [DimensionSatisfaccion] = PesoSatisfaccion,
        [DimensionMejora] = PesoMejora,
    };

    public static SaludTiCalculada Calcular(
        IReadOnlyList<ScoreResponsable> responsables,
        IReadOnlyList<IndicadorEvaluado> todosLosIndicadores)
    {
        var dimensiones = CategoriasPorDimension
            .Select(par =>
            {
                var valor = PromedioPonderadoPorCategorias(todosLosIndicadores, par.Value);
                return new DimensionSalud(
                    par.Key,
                    PesoPorDimension[par.Key],
                    valor,
                    valor.HasValue ? CalculadoraCentroMando.SemaforoDeScore(valor.Value) : null);
            })
            .ToList();

        var scoreAreas = responsables
            .Where(r => r.Score.HasValue)
            .Select(r => r.Score!.Value)
            .ToList();

        // Se acumulan peso y aporte solo de lo que tiene dato, y al final se re-normaliza:
        // una dimension sin captura no debe arrastrar el score global a cero.
        decimal pesoConDato = 0m;
        decimal aporte = 0m;

        if (scoreAreas.Count > 0)
        {
            pesoConDato += PesoAreas;
            aporte += scoreAreas.Average() * PesoAreas;
        }

        foreach (var dimension in dimensiones.Where(d => d.Valor.HasValue))
        {
            pesoConDato += dimension.Peso;
            aporte += dimension.Valor!.Value * dimension.Peso;
        }

        // Mismo piso de cobertura que el score individual: un 100 global armado con una sola
        // dimension medida diria "todo bien" de un departamento que apenas se esta midiendo.
        var pesoTotal = PesoAreas + PesoPorDimension.Values.Sum();
        if (pesoConDato == 0m || pesoConDato / pesoTotal < CalculadoraCentroMando.CoberturaMinima)
        {
            return new SaludTiCalculada(
                null, null, NivelDesempeno.SinDatosSuficientes, false,
                "Cobertura insuficiente: hay muy pocos indicadores con dato para calcular un "
                + "score global confiable. Falta instrumentar o capturar el resto del modelo.",
                dimensiones, responsables);
        }

        var bruto = Math.Round(aporte / pesoConDato, 2);

        var razonTope = ResolverRazonTope(responsables, dimensiones);
        var topado = razonTope is not null && bruto > TopeConRojo;
        var final = topado ? TopeConRojo : bruto;

        return new SaludTiCalculada(
            final,
            CalculadoraCentroMando.SemaforoDeScore(final),
            CalculadoraCentroMando.Nivel(final),
            topado,
            razonTope,
            dimensiones,
            responsables);
    }

    private static string? ResolverRazonTope(
        IReadOnlyList<ScoreResponsable> responsables, IReadOnlyList<DimensionSalud> dimensiones)
    {
        var seguridad = dimensiones.FirstOrDefault(d => d.Dimension == DimensionSeguridad);
        if (seguridad?.Semaforo == SemaforoCentroMando.Rojo)
        {
            return $"{DimensionSeguridad} en rojo ({seguridad.Valor:0.#}/100).";
        }

        var enRojo = responsables
            .Where(r => r.Semaforo == SemaforoCentroMando.Rojo)
            .Select(r => r.Equipo)
            .ToList();

        return enRojo.Count > 0
            ? $"Score en rojo de: {string.Join(", ", enRojo)}."
            : null;
    }

    private static decimal? PromedioPonderadoPorCategorias(
        IReadOnlyList<IndicadorEvaluado> indicadores, string[] categorias)
    {
        var relevantes = indicadores
            .Where(i => i.Definicion.PonderaEnScore
                && !i.SinDatos
                && i.ValorNormalizado.HasValue
                && categorias.Contains(i.Definicion.Categoria))
            .ToList();

        if (relevantes.Count == 0)
        {
            return null;
        }

        var pesoTotal = relevantes.Sum(i => i.Definicion.Peso);
        if (pesoTotal <= 0)
        {
            return Math.Round(relevantes.Average(i => i.ValorNormalizado!.Value), 2);
        }

        return Math.Round(relevantes.Sum(i => i.ValorNormalizado!.Value * i.Definicion.Peso) / pesoTotal, 2);
    }
}
