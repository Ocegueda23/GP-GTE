using GTE.Application.Calidad.Commands;
using GTE.Application.Calidad.Queries;
using GTE.Application.DTOs.Request.Calidad;
using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>QA: planes, casos con pasos, ciclos, ejecuciones y trazabilidad.</summary>
[ApiController]
[Route("api/v1")]
public class CalidadController(IMediator mediator) : ControllerBase
{
    [HttpGet("planesprueba")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanPruebaResponse>>>> ObtenerPlanes(
        [FromQuery] int? idProyecto = null, CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerPlanesQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlanPruebaResponse>>.Exito(resultado));
    }

    [HttpPost("planesprueba")]
    public async Task<ActionResult<ApiResponse<PlanPruebaResponse>>> CrearPlan(
        [FromBody] PlanPruebaCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearPlanPruebaCommand(request), cancellationToken);
        return Ok(ApiResponse<PlanPruebaResponse>.Exito(resultado, $"Plan {resultado.Nombre} creado."));
    }

    [HttpGet("planesprueba/{id:int}")]
    public async Task<ActionResult<ApiResponse<PlanPruebaResponse>>> ObtenerPlan(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerPlanQuery(id), cancellationToken);
        return Ok(ApiResponse<PlanPruebaResponse>.Exito(resultado));
    }

    /// <summary>Casos del plan; si se indica ciclo, trae el resultado de la ultima ejecucion en el.</summary>
    [HttpGet("planesprueba/{id:int}/casos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CasoPruebaResponse>>>> ObtenerCasos(
        int id, [FromQuery] int? idCiclo = null, CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerCasosQuery(id, idCiclo), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CasoPruebaResponse>>.Exito(resultado));
    }

    [HttpPost("planesprueba/{id:int}/casos")]
    public async Task<ActionResult<ApiResponse<int>>> CrearCaso(
        int id, [FromBody] CasoPruebaCrearRequest request, CancellationToken cancellationToken)
    {
        var idCaso = await mediator.Send(new CrearCasoPruebaCommand(id, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(idCaso, "Caso de prueba creado."));
    }

    [HttpGet("planesprueba/{id:int}/ciclos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CicloPruebaResponse>>>> ObtenerCiclos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCiclosQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CicloPruebaResponse>>.Exito(resultado));
    }

    [HttpPost("planesprueba/{id:int}/ciclos")]
    public async Task<ActionResult<ApiResponse<int>>> CrearCiclo(
        int id, [FromBody] CicloPruebaCrearRequest request, CancellationToken cancellationToken)
    {
        var idCiclo = await mediator.Send(new CrearCicloPruebaCommand(id, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(idCiclo, "Ciclo de pruebas creado."));
    }

    [HttpGet("planesprueba/{id:int}/matriz")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrazabilidadResponse>>>> ObtenerMatriz(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTrazabilidadQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TrazabilidadResponse>>.Exito(resultado));
    }

    /// <summary>Registra el resultado de un caso en el ciclo (Pasa, Falla, Bloqueado, No aplica).</summary>
    [HttpPost("ciclos/{idCiclo:int}/ejecuciones")]
    public async Task<ActionResult<ApiResponse<int>>> RegistrarEjecucion(
        int idCiclo, [FromBody] EjecucionRegistrarRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new RegistrarEjecucionCommand(idCiclo, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(id, "Resultado registrado."));
    }

    /// <summary>Crea el bug de una ejecucion fallida, precargado con el caso y sus observaciones.</summary>
    [HttpPost("ejecuciones/{idEjecucion:int}/bug")]
    public async Task<ActionResult<ApiResponse<WorkItemResponse>>> CrearBug(
        int idEjecucion, [FromBody] BugDesdeEjecucionRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CrearBugDesdeEjecucionCommand(idEjecucion, request), cancellationToken);
        return Ok(ApiResponse<WorkItemResponse>.Exito(resultado, $"Bug {resultado.Folio} creado."));
    }
}
