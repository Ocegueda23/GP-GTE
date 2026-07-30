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
}
