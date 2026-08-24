using GTE.Application.CatalogoGenerico.Commands;
using GTE.Application.CatalogoGenerico.Queries;
using GTE.Application.DTOs.Request.CatalogoGenerico;
using GTE.Application.DTOs.Responses.CatalogoGenerico;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Administracion del motor de catalogos genericos: alta de catalogos nuevos y edicion
/// de la configuracion de presentacion por columna. Requiere ADM.CatalogoGenerico.
/// </summary>
[ApiController]
[Route("api/v1/catalogo-generico/admin")]
public class CatalogoGenericoAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("tablas-disponibles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> ObtenerTablasDisponibles(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTablasDisponiblesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<string>>.Exito(resultado));
    }

    [HttpGet("tablas/{nombreTabla}/columnas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ColumnaEsquemaResponse>>>> ObtenerColumnasEsquema(
        string nombreTabla, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerColumnasEsquemaQuery(nombreTabla), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ColumnaEsquemaResponse>>.Exito(resultado));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConfiguracionCatalogoResponse>>> CrearCatalogo(
        [FromBody] CatalogoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearCatalogoCommand(request), cancellationToken);
        return Ok(ApiResponse<ConfiguracionCatalogoResponse>.Exito(resultado, $"Catalogo {resultado.Titulo} creado."));
    }

    [HttpPut("{clave}/columnas")]
    public async Task<ActionResult<ApiResponse<ConfiguracionCatalogoResponse>>> ActualizarConfigColumnas(
        string clave, [FromBody] ActualizarConfigColumnasRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ActualizarConfigColumnasCommand(clave, request.Columnas), cancellationToken);
        return Ok(ApiResponse<ConfiguracionCatalogoResponse>.Exito(resultado, "Configuracion de columnas actualizada."));
    }
}
