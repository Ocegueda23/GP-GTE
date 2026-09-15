namespace GTE.Domain.CentroMando;

/// <summary>
/// Definicion de un indicador tal como la necesita el calculo (subconjunto de
/// dbo.tblIndicadorGestion, sin EF).
/// </summary>
public record DefinicionIndicador(
    int IdIndicadorGestion,
    string Clave,
    string Nombre,
    string Categoria,
    string Ambito,
    string Origen,
    string Unidad,
    decimal? Meta,
    decimal? UmbralAlerta,
    string Direccion,
    decimal Peso,
    bool PonderaEnScore);

/// <summary>Resultado de evaluar un indicador en un periodo.</summary>
public record IndicadorEvaluado(
    DefinicionIndicador Definicion,
    decimal? Valor,
    decimal? ValorNormalizado,
    string? Semaforo,
    bool SinDatos);

/// <summary>Score de un responsable en un periodo, con su desglose.</summary>
public record EvaluacionCalculada(
    decimal? ScoreGeneral,
    string? Nivel,
    string? Semaforo,
    IReadOnlyList<IndicadorEvaluado> Indicadores,
    int IndicadoresConDato,
    int IndicadoresTotales);

/// <summary>
/// Logica pura del Centro de Mando TI (sin EF): convierte valores crudos en escala 0-100,
/// asigna semaforo, y combina el bloque comun (60%) con el bloque tecnico del area (40%)
/// para producir el score del responsable.
///
/// NORMALIZACION -- decision central y deliberada: el valor se lleva a 0-100 anclando
/// **100 en la meta y 50 en el umbral de alerta**, con interpolacion lineal y recorte a
/// [0, 100]. Esto funciona igual para indicadores que suben (meta 90%, umbral 75%) y para
/// los que bajan (meta 8%, umbral 15%), y permite promediar en un mismo score cosas de
/// unidades incompatibles (porcentajes, horas, conteos, indices) sin inventar escalas por
/// indicador.
///
/// SEMAFORO SEPARADO DEL SCORE -- a proposito: el semaforo se decide contra el valor CRUDO
/// (cumple meta = verde; entre meta y umbral = amarillo; peor que umbral = rojo), no contra
/// el normalizado. Asi "verde" siempre significa literalmente "cumplio la meta", que es lo
/// que el gerente espera al verlo, en vez de un artefacto de la formula.
///
/// SIN DATOS NO PENALIZA: un indicador sin dato suficiente (denominador 0, u origen Manual
/// sin captura) queda fuera del promedio y los pesos se re-normalizan sobre los que si
/// tienen dato -- mismo criterio que <see cref="GTE.Domain.Dashboard.CalculadoraPuntaje"/>
/// ya usa en el dashboard de colaborador. Castigar la falta de captura convertiria el score
/// en una medida de cuanto se captura, no de como se trabaja.
/// </summary>
public static class CalculadoraCentroMando
{
    /// <summary>Peso del bloque comun (16 indicadores transversales) dentro del score.</summary>
    public const decimal PesoBloqueComun = 0.60m;

    /// <summary>Peso del bloque tecnico del area dentro del score.</summary>
    public const decimal PesoBloqueTecnico = 0.40m;

    /// <summary>
    /// Fraccion minima de indicadores con dato para que el score signifique algo.
    ///
    /// SIN ESTE PISO EL DASHBOARD MIENTE: con 2 indicadores medidos de 27, si esos dos salen
    /// bien, el promedio ponderado da 100 y la pantalla anuncia "Excelente" para un area de
    /// la que en realidad no se sabe casi nada. Un score construido sobre una muestra asi no
    /// es una medicion, es una casualidad -- y es justo el tipo de numero enganoso que este
    /// modelo existe para evitar. Debajo de este umbral no se reporta score: se reporta
    /// SinDatosSuficientes, que es la verdad y ademas es accionable (hay que instrumentar).
    /// </summary>
    public const decimal CoberturaMinima = 0.40m;

    private const decimal ScoreEnMeta = 100m;
    private const decimal ScoreEnUmbral = 50m;

    /// <summary>
    /// Lleva un valor crudo a escala 0-100 anclando 100 en la meta y 50 en el umbral.
    /// Devuelve null si no hay valor o si falta la referencia para interpretarlo.
    /// </summary>
    public static decimal? Normalizar(decimal? valor, decimal? meta, decimal? umbral, string direccion)
    {
        if (valor is null || meta is null)
        {
            return null;
        }

        // Sin umbral no hay pendiente que interpolar: solo se puede decir si cumple o no.
        if (umbral is null || meta == umbral)
        {
            var cumple = direccion == DireccionIndicador.Bajar ? valor <= meta : valor >= meta;
            return cumple ? ScoreEnMeta : 0m;
        }

        // Distancia meta-umbral con el signo ya resuelto por la direccion.
        var recorrido = direccion == DireccionIndicador.Bajar
            ? umbral.Value - meta.Value
            : meta.Value - umbral.Value;

        if (recorrido == 0)
        {
            return null;
        }

        var avance = direccion == DireccionIndicador.Bajar
            ? umbral.Value - valor.Value
            : valor.Value - umbral.Value;

        var score = ScoreEnUmbral + (ScoreEnMeta - ScoreEnUmbral) * (avance / recorrido);
        return Math.Round(Math.Clamp(score, 0m, 100m), 2);
    }

    /// <summary>
    /// Semaforo contra el valor crudo: cumple meta = Verde; entre meta y umbral = Amarillo;
    /// peor que el umbral = Rojo.
    /// </summary>
    public static string? Semaforo(decimal? valor, decimal? meta, decimal? umbral, string direccion)
    {
        if (valor is null || meta is null)
        {
            return null;
        }

        var esBajar = direccion == DireccionIndicador.Bajar;
        var cumpleMeta = esBajar ? valor <= meta : valor >= meta;
        if (cumpleMeta)
        {
            return SemaforoCentroMando.Verde;
        }

        if (umbral is null)
        {
            return SemaforoCentroMando.Rojo;
        }

        var peorQueUmbral = esBajar ? valor > umbral : valor < umbral;
        return peorQueUmbral ? SemaforoCentroMando.Rojo : SemaforoCentroMando.Amarillo;
    }

    /// <summary>Evalua un indicador: normaliza, asigna semaforo y marca si quedo sin datos.</summary>
    public static IndicadorEvaluado Evaluar(DefinicionIndicador definicion, decimal? valor)
    {
        if (valor is null)
        {
            return new IndicadorEvaluado(definicion, null, null, null, SinDatos: true);
        }

        var normalizado = Normalizar(valor, definicion.Meta, definicion.UmbralAlerta, definicion.Direccion);
        var semaforo = Semaforo(valor, definicion.Meta, definicion.UmbralAlerta, definicion.Direccion);

        return new IndicadorEvaluado(definicion, valor, normalizado, semaforo, SinDatos: normalizado is null);
    }

    /// <summary>
    /// Combina los indicadores evaluados en el score del responsable: promedio ponderado
    /// dentro de cada bloque (con los pesos re-normalizados sobre los que tienen dato) y
    /// luego 60% comun + 40% tecnico. Si un bloque quedo completamente sin datos, el otro
    /// se lleva el 100% en vez de arrastrar el score a la mitad.
    /// </summary>
    public static EvaluacionCalculada Combinar(IReadOnlyList<IndicadorEvaluado> indicadores)
    {
        var participantes = indicadores
            .Where(i => i.Definicion.PonderaEnScore && !i.SinDatos && i.ValorNormalizado.HasValue)
            .ToList();

        var scoreComun = PromedioPonderado(participantes.Where(i => i.Definicion.Ambito == AmbitoCentroMando.Comun));
        var scoreTecnico = PromedioPonderado(participantes.Where(i => i.Definicion.Ambito != AmbitoCentroMando.Comun));

        decimal? score = (scoreComun, scoreTecnico) switch
        {
            (null, null) => null,
            (not null, null) => scoreComun,
            (null, not null) => scoreTecnico,
            _ => scoreComun * PesoBloqueComun + scoreTecnico * PesoBloqueTecnico,
        };

        var redondeado = score.HasValue ? Math.Round(score.Value, 2) : (decimal?)null;
        var ponderables = indicadores.Count(i => i.Definicion.PonderaEnScore);

        // Cobertura insuficiente: se conserva el detalle (para ver QUE falta capturar) pero no
        // se publica un score que la muestra no sostiene.
        var cobertura = ponderables == 0 ? 0m : (decimal)participantes.Count / ponderables;
        if (redondeado.HasValue && cobertura < CoberturaMinima)
        {
            return new EvaluacionCalculada(
                null, NivelDesempeno.SinDatosSuficientes, null,
                indicadores, participantes.Count, ponderables);
        }

        return new EvaluacionCalculada(
            redondeado,
            redondeado.HasValue ? Nivel(redondeado.Value) : null,
            redondeado.HasValue ? SemaforoDeScore(redondeado.Value) : null,
            indicadores,
            participantes.Count,
            ponderables);
    }

    private static decimal? PromedioPonderado(IEnumerable<IndicadorEvaluado> indicadores)
    {
        var lista = indicadores.ToList();
        if (lista.Count == 0)
        {
            return null;
        }

        var pesoTotal = lista.Sum(i => i.Definicion.Peso);

        // Todos con peso 0: el bloque existe pero nadie configuro pesos -- promedio simple
        // en vez de division por cero.
        if (pesoTotal <= 0)
        {
            return lista.Average(i => i.ValorNormalizado!.Value);
        }

        return lista.Sum(i => i.ValorNormalizado!.Value * i.Definicion.Peso) / pesoTotal;
    }

    public static string Nivel(decimal score) => score switch
    {
        >= 90m => NivelDesempeno.Excelente,
        >= 80m => NivelDesempeno.Optimo,
        >= 70m => NivelDesempeno.RequiereAtencion,
        >= 50m => NivelDesempeno.Critico,
        _ => NivelDesempeno.CriticoSevero,
    };

    public static string SemaforoDeScore(decimal score) => score switch
    {
        >= 80m => SemaforoCentroMando.Verde,
        >= 70m => SemaforoCentroMando.Amarillo,
        _ => SemaforoCentroMando.Rojo,
    };
}
