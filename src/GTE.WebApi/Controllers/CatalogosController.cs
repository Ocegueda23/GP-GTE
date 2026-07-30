using GTE.Application.Catalogos.Queries;
using GTE.Application.DTOs.Responses.Catalogos;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

[ApiController]
[Route("api/v1/catalogos")]
public class CatalogosController(IMediator mediator) : ControllerBase
{
    /// <summary>Catalogos para la barra de filtros de la bandeja de trabajo.</summary>
    [HttpGet("bandeja")]
    public async Task<ActionResult<ApiResponse<CatalogosBandejaResponse>>> ObtenerBandeja(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCatalogosBandejaQuery(), cancellationToken);
        return Ok(ApiResponse<CatalogosBandejaResponse>.Exito(resultado));
    }
}
