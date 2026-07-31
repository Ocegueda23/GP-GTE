using GTE.Application.DTOs.Request.Planeacion;
using GTE.Application.DTOs.Responses.Planeacion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Planeacion.Commands;
using GTE.Application.Planeacion.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Planeacion: sprints, backlog y tablero kanban.</summary>
[ApiController]
[Route("api/v1")]
public class PlaneacionController(IMediator mediator) : ControllerBase
{
    /* ---------- Sprints ---------- */

    [HttpGet("sprints")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SprintResponse>>>> ObtenerSprints(
        [FromQuery] int? idEquipo = null,
        [FromQuery] bool soloAbiertos = true,
        CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerSprintsQuery(idEquipo, soloAbiertos), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SprintResponse>>.Exito(resultado));
    }

    [HttpPost("sprints")]
    public async Task<ActionResult<ApiResponse<SprintResponse>>> CrearSprint(
        [FromBody] SprintCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearSprintCommand(request), cancellationToken);
        return Ok(ApiResponse<SprintResponse>.Exito(resultado, $"Sprint {resultado.Nombre} creado."));
    }

    /// <summary>ACTIVAR o CERRAR. Al cerrar, destinoItemsAbiertos = Backlog o SiguienteSprint.</summary>
    [HttpPut("sprints/{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<SprintResponse>>> CambiarEstatusSprint(
        int id, [FromBody] CambiarEstatusSprintRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CambiarEstatusSprintCommand(id, request.Accion, request.DestinoItemsAbiertos), cancellationToken);
        return Ok(ApiResponse<SprintResponse>.Exito(resultado, $"El sprint paso a {resultado.Estatus}."));
    }

    [HttpGet("sprints/{id:int}/items")]
    public async Task<ActionResult<ApiResponse<BacklogResponse>>> ObtenerItemsSprint(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerItemsSprintQuery(id), cancellationToken);
        return Ok(ApiResponse<BacklogResponse>.Exito(resultado));
    }

    [HttpGet("sprints/{id:int}/capacidad")]
    public async Task<ActionResult<ApiResponse<CapacidadSprintResponse>>> ObtenerCapacidad(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCapacidadSprintQuery(id), cancellationToken);
        return Ok(ApiResponse<CapacidadSprintResponse>.Exito(resultado));
    }

    [HttpGet("sprints/{id:int}/burndown")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoBurndownResponse>>>> ObtenerBurndown(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerBurndownQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PuntoBurndownResponse>>.Exito(resultado));
    }

    /* ---------- Backlog ---------- */

    [HttpGet("proyectos/{idProyecto:int}/backlog")]
    public async Task<ActionResult<ApiResponse<BacklogResponse>>> ObtenerBacklog(
        int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerBacklogQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<BacklogResponse>.Exito(resultado));
    }

    [HttpPut("backlog/orden")]
    public async Task<ActionResult<ApiResponse<object>>> ReordenarBacklog(
        [FromBody] ReordenarBacklogRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReordenarBacklogCommand(request), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Orden del backlog actualizado."));
    }

    /// <summary>Mueve el elemento a un sprint; idSprint null lo regresa al backlog.</summary>
    [HttpPut("workitems/{id:int}/sprint")]
    public async Task<ActionResult<ApiResponse<object>>> AsignarSprint(
        int id, [FromBody] AsignarSprintRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new AsignarSprintCommand(id, request.IdSprint), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { },
            request.IdSprint.HasValue ? "Elemento movido al sprint." : "Elemento regresado al backlog."));
    }

    /* ---------- Tablero kanban ---------- */

    [HttpGet("equipos/{idEquipo:int}/tablero")]
    public async Task<ActionResult<ApiResponse<TableroResponse>>> ObtenerTablero(
        int idEquipo, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTableroQuery(idEquipo), cancellationToken);
        return Ok(ApiResponse<TableroResponse>.Exito(resultado));
    }

    /// <summary>Soltar una tarjeta en otra columna: se traduce a la accion del grafo.</summary>
    [HttpPut("workitems/{id:int}/columna")]
    public async Task<ActionResult<ApiResponse<EstatusCambiadoResponse>>> MoverTarjeta(
        int id, [FromBody] MoverTarjetaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new MoverTarjetaCommand(id, request.IdEstatusDestino), cancellationToken);
        return Ok(ApiResponse<EstatusCambiadoResponse>.Exito(resultado, $"Movido a {resultado.Estatus}."));
    }
}
