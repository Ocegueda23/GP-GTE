using GTE.Application.DTOs.Request.Revisiones;
using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Revisiones.Commands;
using GTE.Application.Revisiones.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Revisiones (QA y code review): hallazgos que bloquean el cierre del elemento.</summary>
[ApiController]
[Route("api/v1")]
public class RevisionesController(IMediator mediator) : ControllerBase
{
    [HttpGet("workitems/{idWorkItem:int}/revisiones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionResponse>>>> ObtenerPorWorkItem(
        int idWorkItem, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRevisionesQuery(idWorkItem), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RevisionResponse>>.Exito(resultado));
    }

    /// <summary>Reporta un hallazgo; si el elemento estaba Terminado, lo reabre a Correccion.</summary>
    [HttpPost("workitems/{idWorkItem:int}/revisiones")]
    public async Task<ActionResult<ApiResponse<RevisionResponse>>> Crear(
        int idWorkItem, [FromBody] RevisionCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearRevisionCommand(idWorkItem, request), cancellationToken);
        return Ok(ApiResponse<RevisionResponse>.Exito(resultado, "Hallazgo registrado."));
    }

    /// <summary>Marca corregido o reabre (reabrir exige permiso REV.Reabrir y motivo).</summary>
    [HttpPut("revisiones/{idRevision:int}/correccion")]
    public async Task<ActionResult<ApiResponse<RevisionResponse>>> Corregir(
        int idRevision, [FromBody] RevisionCorregirRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CorregirRevisionCommand(idRevision, request), cancellationToken);
        return Ok(ApiResponse<RevisionResponse>.Exito(resultado,
            request.Corregido ? "Hallazgo marcado como corregido." : "Hallazgo reabierto."));
    }
}
