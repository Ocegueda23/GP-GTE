namespace GTE.Domain.CentroMando;

/// <summary>
/// Los seis indices que el modelo revisa ANTES de concluir que un score bajo es
/// responsabilidad de la persona. Cualquiera puede venir en null: significa que hoy no hay
/// fuente para calcularlo, no que este en cero -- la diferencia importa, porque un indice
/// nulo no puede descartar ni confirmar nada.
/// </summary>
public record IndicesDiagnostico(
    /// <summary>% del tiempo del equipo bloqueado esperando a un tercero.</summary>
    decimal? Espera = null,
    /// <summary>Reasignaciones o urgencias no planeadas por persona en el mes.</summary>
    decimal? CambioPrioridad = null,
    /// <summary>% del retrabajo atribuible a requerimiento mal definido o cambiado.</summary>
    decimal? AlcanceInestable = null,
    /// <summary>Trabajo asignado contra capacidad disponible, en porcentaje.</summary>
    decimal? Carga = null,
    /// <summary>% del tiempo en tareas repetitivas ya identificadas como automatizables.</summary>
    decimal? TrabajoManual = null,
    /// <summary>Actividades criticas que solo una persona sabe operar (bus factor = 1).</summary>
    int? DependenciaUnica = null);

/// <summary>Una causa detectada, con la evidencia que la sostiene.</summary>
public record CausaDetectada(
    string Causa,
    string IndiceClave,
    decimal? Valor,
    decimal? Umbral,
    string Evidencia);

/// <summary>
/// Modelo de diagnostico PERSONA -> PROCESO -> RECURSOS -> DEPENDENCIA -> PRIORIDAD.
///
/// La regla que da sentido a todo el Centro de Mando: un score bajo NO se atribuye a la
/// persona mientras alguno de los seis indices siga fuera de umbral. Persona es el
/// diagnostico por descarte, no el primero -- y solo aplica cuando ademas hay evidencia
/// suficiente (al menos un indice con dato). Con todos los indices en null no se concluye
/// nada: se reporta que falta instrumentacion, que es un problema distinto y de la gerencia,
/// no del responsable evaluado.
///
/// Los indices se evaluan en el orden del documento; se devuelven TODAS las causas fuera de
/// umbral (no solo la primera) porque en la practica se acumulan -- sobrecarga y dependencia
/// suelen venir juntas, y el gerente necesita ver ambas para decidir.
/// </summary>
public static class DiagnosticoCausaRaiz
{
    public const decimal UmbralEspera = 20m;
    public const decimal UmbralCambioPrioridad = 5m;
    public const decimal UmbralAlcanceInestable = 50m;
    public const decimal UmbralCarga = 120m;
    public const decimal UmbralTrabajoManual = 25m;
    public const int UmbralDependenciaUnica = 1;

    /// <summary>Clave del "diagnostico" que se emite cuando no hay ningun indice con dato.</summary>
    public const string IndiceSinInstrumentacion = "sin-instrumentacion";

    public static IReadOnlyList<CausaDetectada> Diagnosticar(IndicesDiagnostico indices)
    {
        var causas = new List<CausaDetectada>();

        if (indices.Espera is { } espera && espera > UmbralEspera)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Dependencia, "espera", espera, UmbralEspera,
                $"El equipo paso {espera:0.#}% del tiempo bloqueado esperando a un tercero (umbral {UmbralEspera:0.#}%). " +
                "El retraso se origina fuera del area."));
        }

        if (indices.CambioPrioridad is { } prioridad && prioridad > UmbralCambioPrioridad)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Prioridad, "cambio-prioridad", prioridad, UmbralCambioPrioridad,
                $"{prioridad:0.#} cambios de prioridad o urgencias no planeadas por persona en el mes " +
                $"(umbral {UmbralCambioPrioridad:0.#}). El plan se reescribe mas rapido de lo que se ejecuta."));
        }

        if (indices.AlcanceInestable is { } alcance && alcance > UmbralAlcanceInestable)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Proceso, "alcance-inestable", alcance, UmbralAlcanceInestable,
                $"{alcance:0.#}% del retrabajo viene de requerimientos mal definidos o cambiados a medio camino " +
                $"(umbral {UmbralAlcanceInestable:0.#}%). El problema esta en como se toma el requerimiento."));
        }

        if (indices.Carga is { } carga && carga > UmbralCarga)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Recursos, "carga", carga, UmbralCarga,
                $"Carga de trabajo al {carga:0.#}% de la capacidad disponible (umbral {UmbralCarga:0.#}%). " +
                "No es un problema de ritmo, es de dimensionamiento."));
        }

        if (indices.TrabajoManual is { } manual && manual > UmbralTrabajoManual)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Proceso, "trabajo-manual", manual, UmbralTrabajoManual,
                $"{manual:0.#}% del tiempo se va en tareas repetitivas ya identificadas como automatizables " +
                $"(umbral {UmbralTrabajoManual:0.#}%). Es capacidad recuperable sin contratar."));
        }

        if (indices.DependenciaUnica is { } busFactor && busFactor >= UmbralDependenciaUnica)
        {
            causas.Add(new CausaDetectada(CausaRaiz.Dependencia, "dependencia-unica", busFactor, UmbralDependenciaUnica,
                $"{busFactor} actividad(es) critica(s) que solo una persona sabe operar. " +
                "Es un riesgo de continuidad, no un merito individual."));
        }

        if (causas.Count > 0)
        {
            return causas;
        }

        // Sin causas externas: solo aqui tiene sentido hablar de la persona, y unicamente si
        // de verdad se midio algo. Sin ningun indice con dato, el hallazgo es la falta de
        // instrumentacion.
        var hayEvidencia = indices.Espera.HasValue || indices.CambioPrioridad.HasValue
            || indices.AlcanceInestable.HasValue || indices.Carga.HasValue
            || indices.TrabajoManual.HasValue || indices.DependenciaUnica.HasValue;

        if (!hayEvidencia)
        {
            return
            [
                new CausaDetectada(CausaRaiz.Proceso, IndiceSinInstrumentacion, null, null,
                    "No hay datos suficientes para descartar proceso, recursos, dependencia ni prioridad. " +
                    "Antes de evaluar a la persona hace falta instrumentar estos indices."),
            ];
        }

        return
        [
            new CausaDetectada(CausaRaiz.Persona, "descarte", null, null,
                "Ningun indice de proceso, recursos, dependencia o prioridad esta fuera de umbral. " +
                "Es el unico caso en que corresponde una conversacion de desarrollo individual."),
        ];
    }

    /// <summary>
    /// Causa dominante para mostrar en el encabezado del dashboard individual: la primera en
    /// el orden del modelo, que es el orden en que <see cref="Diagnosticar"/> las devuelve.
    /// </summary>
    public static CausaDetectada? CausaPrincipal(IReadOnlyList<CausaDetectada> causas)
        => causas.Count > 0 ? causas[0] : null;
}
