using GTE.Application.Comentarios.Commands;
using GTE.Application.Comentarios.Queries;
using GTE.Application.DTOs.Request.Comentarios;
using GTE.Application.DTOs.Responses.Comentarios;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Comentarios sobre WorkItem: hilos con formato basico y menciones.</summary>
[ApiController]
[Route("api/v1")]
public class ComentariosController(IMediator mediator) : ControllerBase
{
    [HttpGet("workitems/{idWorkItem:int}/comentarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComentarioResponse>>>> ObtenerPorWorkItem(
        int idWorkItem, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerComentariosQuery(idWorkItem), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ComentarioResponse>>.Exito(resultado));
    }

    [HttpPost("workitems/{idWorkItem:int}/comentarios")]
    public async Task<ActionResult<ApiResponse<ComentarioResponse>>> Crear(
        int idWorkItem, [FromBody] ComentarioCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearComentarioCommand(idWorkItem, request), cancellationToken);
        return Ok(ApiResponse<ComentarioResponse>.Exito(resultado, "Comentario publicado."));
    }

    /// <summary>Baja logica. Solo quien escribio el comentario puede eliminarlo.</summary>
    [HttpDelete("comentarios/{idComentario:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Eliminar(int idComentario, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarComentarioCommand(idComentario), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Comentario eliminado."));
    }
}
