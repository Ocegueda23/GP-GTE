using GTE.Application.Calidad.Commands;
using GTE.Application.Calidad.Queries;
using GTE.Application.DTOs.Request.Calidad;
using GTE.Application.DTOs.Responses.Calidad;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Calidad (QA): catalogo de casos de prueba por proyecto, su asignacion a WorkItems y el
/// registro de ejecuciones. Una falla no crea un WorkItem nuevo: crea un hallazgo (ver
/// RevisionesController) sobre el mismo item, para no perder la trazabilidad de calidad.
/// </summary>
[ApiController]
[Route("api/v1")]
public class CalidadController(IMediator mediator) : ControllerBase
{
    [HttpGet("proyectos/{idProyecto:int}/casosprueba")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CasoPruebaResponse>>>> ObtenerCasosDisponibles(
        int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCasosDisponiblesQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CasoPruebaResponse>>.Exito(resultado));
    }

    [HttpGet("workitems/{idWorkItem:int}/casosprueba")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CasoAsignadoResponse>>>> ObtenerCasosAsignados(
        int idWorkItem, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCasosAsignadosQuery(idWorkItem), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CasoAsignadoResponse>>.Exito(resultado));
    }

    /// <summary>Crea un caso nuevo (libre o reutilizable) y lo asigna al WorkItem en el mismo paso.</summary>
    [HttpPost("workitems/{idWorkItem:int}/casosprueba")]
    public async Task<ActionResult<ApiResponse<int>>> CrearCasoYAsignar(
        int idWorkItem, [FromBody] CasoPruebaCrearRequest request, CancellationToken cancellationToken)
    {
        var idCaso = await mediator.Send(new CrearCasoYAsignarCommand(idWorkItem, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(idCaso, "Caso de prueba creado y asignado."));
    }

    /// <summary>Asigna un caso reutilizable ya existente del catalogo del proyecto.</summary>
    [HttpPost("workitems/{idWorkItem:int}/casosprueba/asignar")]
    public async Task<ActionResult<ApiResponse<object>>> AsignarCasoExistente(
        int idWorkItem, [FromBody] AsignarCasoRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new AsignarCasoExistenteCommand(idWorkItem, request), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Caso asignado."));
    }

    [HttpPut("workitemcasoprueba/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarAsignacion(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarAsignacionCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Caso desasignado."));
    }

    [HttpPut("casosprueba/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> ActualizarCaso(
        int id, [FromBody] CasoPruebaEditarRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ActualizarCasoPruebaCommand(id, request), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Caso de prueba actualizado."));
    }

    [HttpPut("casosprueba/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarCaso(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarCasoPruebaCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Caso de prueba retirado."));
    }

    /// <summary>Registra el resultado de un caso contra el WorkItem (Pasa, Falla, Bloqueado, No aplica).
    /// Si Falla, crea el hallazgo en automatico y su id viene en la respuesta.</summary>
    [HttpPost("workitems/{idWorkItem:int}/ejecuciones")]
    public async Task<ActionResult<ApiResponse<EjecucionRegistradaResponse>>> RegistrarEjecucion(
        int idWorkItem, [FromBody] EjecucionRegistrarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RegistrarEjecucionCommand(idWorkItem, request), cancellationToken);
        return Ok(ApiResponse<EjecucionRegistradaResponse>.Exito(resultado, "Resultado registrado."));
    }
}
