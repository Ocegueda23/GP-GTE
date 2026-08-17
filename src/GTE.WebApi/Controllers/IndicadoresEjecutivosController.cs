using GTE.Application.DTOs.Request.IndicadoresEjecutivos;
using GTE.Application.DTOs.Responses.IndicadoresEjecutivos;
using GTE.Application.IndicadoresEjecutivos.Commands;
using GTE.Application.IndicadoresEjecutivos.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Dashboard Ejecutivo P18 (Doctos/GTE-DocumentoMaestro.md 3.10/5.10): vista de
/// equipo/proyecto (DORA, costo, rentabilidad, OKR). Requiere DASH.Ejecutivo o
/// DASH.VerDepartamento (403 sin ninguno de los dos) -- a diferencia de
/// GET /api/v1/dashboard (colaborador individual), aqui no hay un alcance "personal".
/// </summary>
[ApiController]
[Route("api/v1/indicadores-ejecutivos")]
public class IndicadoresEjecutivosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IndicadoresEjecutivosResponse>>> Obtener(
        [FromQuery] int anio, [FromQuery] int mes,
        [FromQuery] int? idEquipo, [FromQuery] int? idProyecto,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ObtenerIndicadoresEjecutivosQuery(anio, mes, idEquipo, idProyecto), cancellationToken);
        return Ok(ApiResponse<IndicadoresEjecutivosResponse>.Exito(resultado));
    }

    [HttpGet("layout")]
    public async Task<ActionResult<ApiResponse<LayoutDashboardEjecutivoResponse>>> ObtenerLayout(CancellationToken cancellationToken)
    {
        var layoutJson = await mediator.Send(new ObtenerLayoutDashboardEjecutivoQuery(), cancellationToken);
        return Ok(ApiResponse<LayoutDashboardEjecutivoResponse>.Exito(new LayoutDashboardEjecutivoResponse { LayoutJson = layoutJson }));
    }

    [HttpPut("layout")]
    public async Task<ActionResult<ApiResponse<object>>> GuardarLayout(
        [FromBody] GuardarLayoutDashboardEjecutivoRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new GuardarLayoutDashboardEjecutivoCommand(request.LayoutJson), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Layout guardado."));
    }
}
