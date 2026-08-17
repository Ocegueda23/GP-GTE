namespace GTE.Domain.Dashboard;

/// <summary>
/// Las 6 dimensiones de la Evaluacion Mensual del Dashboard Ejecutivo. Todas se calculan
/// de forma automatica a partir de datos ya existentes (WorkItems, Tickets, tiempo
/// registrado, comentarios) -- no hay captura manual. Formulas ver <see cref="CalculadoraPuntaje"/>.
/// </summary>
public enum DimensionEvaluacion
{
    Calidad,
    Productividad,
    Puntualidad,
    TrabajoEnEquipo,
    Cumplimiento,
    Comunicacion,
}

/// <summary>
/// Insumos ya agregados (conteos/sumas) que necesita <see cref="CalculadoraPuntaje"/> para
/// calcular el puntaje de un empleado en un periodo. Se arma en el QueryService a partir de
/// varias consultas contra WorkItem/Ticket/RegistroTiempo/Comentario -- esta clase no toca EF.
/// </summary>
public record InsumosPuntajeEmpleado(
    int TerminadosPeriodo,
    int ReaperturasPeriodo,
    int ConCompromisoPeriodo,
    int ATiempoPeriodo,
    int AsignadosVigentesPeriodo,
    int VencidosVigentesPeriodo,
    int ComentariosPropiosPeriodo,
    int ComentariosEnItemsAjenosPeriodo,
    decimal PromedioTerminadosEquipo,
    decimal PromedioComentariosPropiosEquipo,
    decimal PromedioComentariosAjenosEquipo);

public record PuntajeDimension(DimensionEvaluacion Dimension, decimal? Valor);

public record PuntajeEmpleadoCalculado(IReadOnlyList<PuntajeDimension> Dimensiones, decimal? PuntajeGeneral);

/// <summary>
/// Logica de negocio pura (sin EF) para convertir insumos agregados en el puntaje 0-100 de
/// cada dimension y el puntaje general (promedio de las dimensiones con dato). Formulas v1,
/// heuristicas y documentadas a proposito -- no hay captura manual que las corrija, se
/// esperan ajustes cuando el negocio las revise con datos reales:
///
///   Puntualidad     = % de items con fecha compromiso, cerrados en el periodo, que se
///                     cerraron a tiempo.
///   Cumplimiento    = % de items asignados vigentes en el periodo que NO estan vencidos
///                     (mide "al corriente", no solo lo ya cerrado).
///   Productividad   = items terminados por el empleado en el periodo, relativo al
///                     promedio del equipo (100 = igual al promedio, tope 130 por
///                     sobre-desempeno).
///   Calidad         = 100 menos el % de reaperturas sobre lo terminado en el periodo
///                     (proxy de retrabajo/defectos).
///   TrabajoEnEquipo = comentarios registrados en items de OTROS miembros del equipo,
///                     relativo al promedio del equipo (proxy de colaboracion cruzada).
///   Comunicacion    = total de comentarios (propios + en items ajenos), relativo al
///                     promedio del equipo (proxy de participacion/documentacion).
///
/// Una dimension sin datos suficientes (denominador 0) se reporta como null ("sin datos")
/// y no participa en el promedio del puntaje general.
/// </summary>
public static class CalculadoraPuntaje
{
    private const decimal TopeSobreDesempeno = 130m;

    public static PuntajeEmpleadoCalculado Calcular(InsumosPuntajeEmpleado insumos)
    {
        var dimensiones = new List<PuntajeDimension>
        {
            new(DimensionEvaluacion.Puntualidad,
                Porcentaje(insumos.ATiempoPeriodo, insumos.ConCompromisoPeriodo)),
            new(DimensionEvaluacion.Cumplimiento,
                Porcentaje(insumos.AsignadosVigentesPeriodo - insumos.VencidosVigentesPeriodo, insumos.AsignadosVigentesPeriodo)),
            new(DimensionEvaluacion.Productividad,
                RelativoAEquipo(insumos.TerminadosPeriodo, insumos.PromedioTerminadosEquipo)),
            new(DimensionEvaluacion.Calidad,
                insumos.TerminadosPeriodo > 0
                    ? Math.Max(0m, 100m - (Porcentaje(insumos.ReaperturasPeriodo, insumos.TerminadosPeriodo) ?? 0m))
                    : null),
            new(DimensionEvaluacion.TrabajoEnEquipo,
                RelativoAEquipo(insumos.ComentariosEnItemsAjenosPeriodo, insumos.PromedioComentariosAjenosEquipo)),
            new(DimensionEvaluacion.Comunicacion,
                RelativoAEquipo(
                    insumos.ComentariosPropiosPeriodo + insumos.ComentariosEnItemsAjenosPeriodo,
                    insumos.PromedioComentariosPropiosEquipo + insumos.PromedioComentariosAjenosEquipo)),
        };

        var conDato = dimensiones.Where(d => d.Valor.HasValue).ToList();
        var general = conDato.Count > 0 ? Math.Round(conDato.Average(d => d.Valor!.Value), 1) : (decimal?)null;
        return new PuntajeEmpleadoCalculado(dimensiones, general);
    }

    private static decimal? Porcentaje(int numerador, int denominador)
        => denominador > 0 ? Math.Round(100m * numerador / denominador, 1) : null;

    private static decimal? RelativoAEquipo(int propio, decimal promedioEquipo)
    {
        if (propio == 0 && promedioEquipo == 0) return null;
        if (promedioEquipo <= 0) return propio > 0 ? 100m : 0m;
        return Math.Min(TopeSobreDesempeno, Math.Round(100m * propio / promedioEquipo, 1));
    }
}

public static class PermisosDashboard
{
    /// <summary>Alcance global (todos los usuarios del sistema). Ya sembrado en el script 02.</summary>
    public const string VerEjecutivo = "DASH.Ejecutivo";

    /// <summary>Alcance del area/departamento propio del usuario. Script 26.</summary>
    public const string VerDepartamento = "DASH.VerDepartamento";
}
