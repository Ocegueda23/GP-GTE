namespace GTE.Application.Interfaces;

/// <summary>
/// Motor UNICO de tiempo laborable (sustituye a los 4 motores inconsistentes del GT).
/// Toda metrica de tiempo del sistema pasa por aqui: tiempos de proceso, SLA, capacidad.
/// Considera tramos de horario por dia y dias festivos.
/// </summary>
public interface ICalendarioLaboral
{
    Task<int> CalcularMinutosLaboralesAsync(
        DateTime inicio,
        DateTime fin,
        int idHorario,
        CancellationToken cancellationToken = default);

    Task<DateTime> SumarMinutosLaboralesAsync(
        DateTime inicio,
        int minutos,
        int idHorario,
        CancellationToken cancellationToken = default);

    Task<bool> EsDiaLaborableAsync(
        DateOnly fecha,
        int idHorario,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula minutos laborales de MUCHOS pares inicio/fin en una sola pasada, agrupando por
    /// horario y cargando tramos y festivos una sola vez por horario. Existe para reportes de
    /// detalle (R15), donde llamar a CalcularMinutosLaboralesAsync por renglon abriria una
    /// conexion por item. Las claves cuyo horario no tenga tramos configurados NO aparecen en
    /// el diccionario: el reporte pinta ese renglon sin el dato en vez de fallar completo.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> CalcularMinutosLaboralesLoteAsync(
        IReadOnlyList<TramoLaborableSolicitado> solicitudes,
        CancellationToken cancellationToken = default);
}

/// <summary>Un par inicio/fin a medir, identificado por Clave para reasociar el resultado.</summary>
public record TramoLaborableSolicitado(int Clave, DateTime Inicio, DateTime Fin, int IdHorario);
