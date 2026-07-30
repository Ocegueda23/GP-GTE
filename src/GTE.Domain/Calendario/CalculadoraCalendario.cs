using GTE.Domain.Exceptions;

namespace GTE.Domain.Calendario;

/// <summary>
/// Tramo laborable de un horario. DiaSemana: 1 = lunes ... 7 = domingo
/// (mismo contrato que dbo.tblHorarioTramo).
/// </summary>
public record TramoHorario(byte DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

/// <summary>
/// Logica pura de calendario laboral (sin EF). Implementa el MISMO contrato que
/// dbo.fnMinutosLaborales; el paquete de pruebas comparte los vectores validados
/// contra SQL para garantizar paridad entre ambos motores.
/// </summary>
public static class CalculadoraCalendario
{
    /// <summary>Minutos laborables entre dos fechas segun tramos y festivos.</summary>
    public static int CalcularMinutosLaborales(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<TramoHorario> tramos,
        IReadOnlySet<DateOnly> festivos)
    {
        if (fin <= inicio || tramos.Count == 0)
        {
            return 0;
        }

        var total = 0;
        var ultimoDia = DateOnly.FromDateTime(fin);

        for (var dia = DateOnly.FromDateTime(inicio); dia <= ultimoDia; dia = dia.AddDays(1))
        {
            if (festivos.Contains(dia))
            {
                continue;
            }

            var diaSemana = ObtenerDiaSemana(dia);
            foreach (var tramo in tramos)
            {
                if (tramo.DiaSemana != diaSemana)
                {
                    continue;
                }

                var tramoInicio = dia.ToDateTime(tramo.HoraInicio);
                var tramoFin = dia.ToDateTime(tramo.HoraFin);
                var efectivoInicio = inicio > tramoInicio ? inicio : tramoInicio;
                var efectivoFin = fin < tramoFin ? fin : tramoFin;

                if (efectivoFin > efectivoInicio)
                {
                    total += (int)(efectivoFin - efectivoInicio).TotalMinutes;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Fecha resultante de sumar minutos laborables a partir de un instante
    /// (calculo inverso, usado para fechas limite de SLA).
    /// </summary>
    public static DateTime SumarMinutosLaborales(
        DateTime inicio,
        int minutos,
        IReadOnlyList<TramoHorario> tramos,
        IReadOnlySet<DateOnly> festivos,
        int maximoDias = 366)
    {
        if (minutos <= 0)
        {
            return inicio;
        }

        if (tramos.Count == 0)
        {
            throw new BusinessException("El horario no tiene tramos laborables configurados.");
        }

        var restantes = minutos;
        var dia = DateOnly.FromDateTime(inicio);

        for (var i = 0; i < maximoDias; i++, dia = dia.AddDays(1))
        {
            if (festivos.Contains(dia))
            {
                continue;
            }

            var diaSemana = ObtenerDiaSemana(dia);
            foreach (var tramo in tramos.Where(t => t.DiaSemana == diaSemana)
                                        .OrderBy(t => t.HoraInicio))
            {
                var tramoInicio = dia.ToDateTime(tramo.HoraInicio);
                var tramoFin = dia.ToDateTime(tramo.HoraFin);
                var efectivoInicio = inicio > tramoInicio ? inicio : tramoInicio;

                if (efectivoInicio >= tramoFin)
                {
                    continue;
                }

                var disponibles = (int)(tramoFin - efectivoInicio).TotalMinutes;
                if (disponibles >= restantes)
                {
                    return efectivoInicio.AddMinutes(restantes);
                }

                restantes -= disponibles;
            }
        }

        throw new BusinessException(
            $"No fue posible calcular la fecha limite: se agotaron {maximoDias} dias de busqueda.");
    }

    /// <summary>Indica si el dia tiene tramos laborables y no es festivo.</summary>
    public static bool EsDiaLaborable(
        DateOnly dia,
        IReadOnlyList<TramoHorario> tramos,
        IReadOnlySet<DateOnly> festivos)
    {
        if (festivos.Contains(dia))
        {
            return false;
        }

        var diaSemana = ObtenerDiaSemana(dia);
        return tramos.Any(t => t.DiaSemana == diaSemana);
    }

    /// <summary>1 = lunes ... 7 = domingo, independiente de la cultura.</summary>
    private static int ObtenerDiaSemana(DateOnly dia)
    {
        return dia.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)dia.DayOfWeek;
    }
}
