using GTE.Application.DTOs.Responses.MiDia;
using GTE.Application.MiDia.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>P02 - Vista personal del dia del usuario del token.</summary>
[ApiController]
[Route("api/v1/mi-dia")]
public class MiDiaController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MiDiaResponse>>> Obtener(CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMiDiaQuery(), cancellationToken);
        return Ok(ApiResponse<MiDiaResponse>.Exito(resultado));
    }
}
