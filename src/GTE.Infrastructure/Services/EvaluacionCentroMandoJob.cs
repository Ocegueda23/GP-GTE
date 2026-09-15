using GTE.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Job mensual de Hangfire del Centro de Mando TI: recalcula el periodo y genera las
/// alertas. Clase plana sin dependencia de Hangfire (la programacion recurrente vive en
/// Program.cs via RecurringJob.AddOrUpdate), mismo patron que
/// <see cref="SnapshotKpiJob"/>.
///
/// EVALUA EL MES YA CERRADO, no el mes en curso: un mes a medias produce indicadores
/// enganosos (medio backlog, media capacidad) y generaria alertas por un periodo que
/// todavia puede corregirse solo. Mismo criterio que ya usa el "Empleado del mes" del
/// dashboard de colaborador.
/// </summary>
public class EvaluacionCentroMandoJob(
    IMotorEvaluacionCentroMando motor,
    GeneradorAlertasCentroMando alertas,
    ILogger<EvaluacionCentroMandoJob> logger)
{
    public async Task EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var hoy = DateTime.Today;
        var (anio, mes) = hoy.Month == 1 ? (hoy.Year - 1, 12) : (hoy.Year, hoy.Month - 1);

        await EjecutarPeriodoAsync(anio, mes, cancellationToken);
    }

    public async Task EjecutarPeriodoAsync(int anio, int mes, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Centro de Mando: iniciando evaluacion de {Anio}-{Mes:00}.", anio, mes);

        var evaluados = await motor.RecalcularPeriodoAsync(anio, mes, cancellationToken);
        var generadas = await alertas.GenerarAsync(anio, mes, cancellationToken);

        logger.LogInformation(
            "Centro de Mando: {Anio}-{Mes:00} listo. {Evaluados} responsable(s), {Generadas} alerta(s).",
            anio, mes, evaluados, generadas);
    }
}
