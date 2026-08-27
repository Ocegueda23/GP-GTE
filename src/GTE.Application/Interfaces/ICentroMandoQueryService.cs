using GTE.Application.DTOs.Responses.CentroMando;

namespace GTE.Application.Interfaces;

/// <summary>Lectura del Centro de Mando TI (dashboards ejecutivo e individual, catalogo y alertas).</summary>
public interface ICentroMandoQueryService
{
    /// <summary>Dashboard ejecutivo del periodo: salud, responsables, alertas y tendencia.</summary>
    Task<CentroMandoResponse> ObtenerCentroMandoAsync(int anio, int mes, CancellationToken cancellationToken = default);

    /// <summary>Dashboard individual de un responsable, con diagnostico y conclusion.</summary>
    Task<EvaluacionResponsableResponse?> ObtenerEvaluacionAsync(
        int idEquipo, int anio, int mes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IndicadorGestionResponse>> ObtenerCatalogoAsync(
        string? ambito, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertaGestionResponse>> ObtenerAlertasAsync(
        int anio, int mes, bool soloVigentes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Motor de evaluacion: calcula los indicadores de un periodo y persiste la evaluacion, el
/// diagnostico de causa y las alertas. Lo consume tanto el job mensual de Hangfire como el
/// comando de recalculo manual.
/// </summary>
public interface IMotorEvaluacionCentroMando
{
    /// <summary>
    /// Recalcula el periodo completo (todos los equipos con lider) y devuelve cuantas
    /// evaluaciones se escribieron. Es idempotente: re-ejecutarlo sobre el mismo periodo
    /// reemplaza los valores en vez de duplicarlos.
    /// </summary>
    Task<int> RecalcularPeriodoAsync(int anio, int mes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generacion de alertas gerenciales a partir de las evaluaciones ya calculadas. Es un
/// puerto aparte del motor porque son dos pasos distintos, pero SIEMPRE van juntos: si se
/// recalcula un periodo sin regenerar sus alertas, el tablero muestra alertas de una
/// evaluacion que ya no existe.
/// </summary>
public interface IGeneradorAlertasCentroMando
{
    Task<int> GenerarAsync(int anio, int mes, CancellationToken cancellationToken = default);
}

/// <summary>Escritura del catalogo de indicadores y del ciclo de vida de las alertas.</summary>
public interface ICentroMandoRepository
{
    Task ActualizarIndicadorAsync(
        int idIndicadorGestion, decimal? meta, decimal? umbralAlerta, decimal peso,
        bool ponderaEnScore, string? accionSugerida, bool activo, CancellationToken cancellationToken = default);

    Task<bool> ExisteIndicadorAsync(int idIndicadorGestion, CancellationToken cancellationToken = default);

    Task MarcarAlertaAtendidaAsync(long idAlertaGestion, CancellationToken cancellationToken = default);

    Task<bool> ExisteAlertaAsync(long idAlertaGestion, CancellationToken cancellationToken = default);
}
