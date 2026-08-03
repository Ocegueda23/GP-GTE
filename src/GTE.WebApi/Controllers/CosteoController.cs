using GTE.Application.Costeo.Commands;
using GTE.Application.Costeo.Queries;
using GTE.Application.DTOs.Request.Costeo;
using GTE.Application.DTOs.Responses.Costeo;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Portafolio: costeo real por proyecto (tarifas por nivel, presupuesto, reporte de costo).</summary>
[ApiController]
[Route("api/v1/costeo")]
public class CosteoController(IMediator mediator) : ControllerBase
{
    /* ---------- Tarifas por nivel ---------- */

    [HttpGet("tarifas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TarifaNivelResponse>>>> ObtenerTarifas(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTarifasNivelQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TarifaNivelResponse>>.Exito(resultado));
    }

    [HttpPost("tarifas")]
    public async Task<ActionResult<ApiResponse<TarifaNivelResponse>>> CrearTarifa(
        [FromBody] TarifaNivelCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearTarifaNivelCommand(request), cancellationToken);
        return Ok(ApiResponse<TarifaNivelResponse>.Exito(resultado, "Tarifa registrada correctamente."));
    }

    [HttpPut("tarifas/{id:int}")]
    public async Task<ActionResult<ApiResponse<TarifaNivelResponse>>> ActualizarTarifa(
        int id, [FromBody] TarifaNivelEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarTarifaNivelCommand(id, request), cancellationToken);
        return Ok(ApiResponse<TarifaNivelResponse>.Exito(resultado, "Tarifa actualizada."));
    }

    [HttpPut("tarifas/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarTarifa(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarTarifaNivelCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Tarifa retirada."));
    }

    /* ---------- Presupuesto por proyecto ---------- */

    [HttpGet("presupuestos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PresupuestoProyectoResponse>>>> ObtenerPresupuestos(
        [FromQuery] int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerPresupuestosProyectoQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PresupuestoProyectoResponse>>.Exito(resultado));
    }

    [HttpPost("presupuestos")]
    public async Task<ActionResult<ApiResponse<PresupuestoProyectoResponse>>> CrearPresupuesto(
        [FromBody] PresupuestoProyectoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearPresupuestoProyectoCommand(request), cancellationToken);
        return Ok(ApiResponse<PresupuestoProyectoResponse>.Exito(resultado, "Presupuesto registrado correctamente."));
    }

    [HttpPut("presupuestos/{id:int}")]
    public async Task<ActionResult<ApiResponse<PresupuestoProyectoResponse>>> ActualizarPresupuesto(
        int id, [FromBody] PresupuestoProyectoEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarPresupuestoProyectoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<PresupuestoProyectoResponse>.Exito(resultado, "Presupuesto actualizado."));
    }

    [HttpPut("presupuestos/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarPresupuesto(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarPresupuestoProyectoCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Presupuesto retirado."));
    }

    /* ---------- Reporte de costo real ---------- */

    [HttpGet("proyectos/{id:int}")]
    public async Task<ActionResult<ApiResponse<CostoProyectoResponse>>> ObtenerCostoProyecto(
        int id, [FromQuery] int anio, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCostoProyectoQuery(id, anio), cancellationToken);
        return Ok(ApiResponse<CostoProyectoResponse>.Exito(resultado));
    }
}
