using GTE.Application.CentroMando.Commands;
using GTE.Application.CentroMando.Queries;
using GTE.Application.DTOs.Request.CentroMando;
using GTE.Application.DTOs.Responses.CentroMando;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Centro de Mando TI: evaluacion mensual de los responsables de area, diagnostico de causa
/// raiz y alertas gerenciales. Los permisos (GES.Ver / GES.Administrar) se exigen en cada
/// handler, no aqui.
/// </summary>
[ApiController]
[Route("api/v1/centro-mando")]
public class CentroMandoController(IMediator mediator) : ControllerBase
{
    /// <summary>Dashboard ejecutivo del periodo.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CentroMandoResponse>>> Obtener(
        [FromQuery] int? anio, [FromQuery] int? mes, CancellationToken cancellationToken)
    {
        var (a, m) = ResolverPeriodo(anio, mes);
        var resultado = await mediator.Send(new ObtenerCentroMandoQuery(a, m), cancellationToken);
        return Ok(ApiResponse<CentroMandoResponse>.Exito(resultado));
    }

    /// <summary>Dashboard individual de un responsable.</summary>
    [HttpGet("equipos/{idEquipo:int}")]
    public async Task<ActionResult<ApiResponse<EvaluacionResponsableResponse>>> ObtenerEvaluacion(
        int idEquipo, [FromQuery] int? anio, [FromQuery] int? mes, CancellationToken cancellationToken)
    {
        var (a, m) = ResolverPeriodo(anio, mes);
        var resultado = await mediator.Send(
            new ObtenerEvaluacionResponsableQuery(idEquipo, a, m), cancellationToken);
        return Ok(ApiResponse<EvaluacionResponsableResponse>.Exito(resultado));
    }

    [HttpGet("catalogo")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndicadorGestionResponse>>>> ObtenerCatalogo(
        [FromQuery] string? ambito, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCatalogoIndicadoresQuery(ambito), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IndicadorGestionResponse>>.Exito(resultado));
    }

    [HttpPut("catalogo/{idIndicadorGestion:int}")]
    public async Task<ActionResult<ApiResponse<object>>> ActualizarIndicador(
        int idIndicadorGestion, [FromBody] ActualizarIndicadorGestionRequest datos,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ActualizarIndicadorGestionCommand(idIndicadorGestion, datos), cancellationToken);
        return Ok(ApiResponse<object>.Exito(null!, "Indicador actualizado."));
    }

    [HttpGet("alertas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AlertaGestionResponse>>>> ObtenerAlertas(
        [FromQuery] int? anio, [FromQuery] int? mes, [FromQuery] bool soloVigentes = true,
        CancellationToken cancellationToken = default)
    {
        var (a, m) = ResolverPeriodo(anio, mes);
        var resultado = await mediator.Send(
            new ObtenerAlertasGestionQuery(a, m, soloVigentes), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AlertaGestionResponse>>.Exito(resultado));
    }

    [HttpPost("alertas/{idAlertaGestion:long}/atender")]
    public async Task<ActionResult<ApiResponse<object>>> AtenderAlerta(
        long idAlertaGestion, CancellationToken cancellationToken)
    {
        await mediator.Send(new AtenderAlertaGestionCommand(idAlertaGestion), cancellationToken);
        return Ok(ApiResponse<object>.Exito(null!, "Alerta marcada como atendida."));
    }

    /// <summary>Recalculo manual del periodo; el mensual lo dispara Hangfire.</summary>
    [HttpPost("recalcular")]
    public async Task<ActionResult<ApiResponse<int>>> Recalcular(
        [FromBody] RecalcularPeriodoRequest datos, CancellationToken cancellationToken)
    {
        var evaluados = await mediator.Send(
            new RecalcularPeriodoCommand(datos.Anio, datos.Mes), cancellationToken);
        return Ok(ApiResponse<int>.Exito(evaluados, $"{evaluados} responsable(s) evaluado(s)."));
    }

    /// <summary>Sin periodo explicito se usa el mes en curso.</summary>
    private static (int Anio, int Mes) ResolverPeriodo(int? anio, int? mes)
        => (anio ?? DateTime.Today.Year, mes ?? DateTime.Today.Month);
}
