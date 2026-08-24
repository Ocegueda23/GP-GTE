using GTE.Application.CatalogoGenerico.Commands;
using GTE.Application.CatalogoGenerico.Queries;
using GTE.Application.Common;
using GTE.Application.DTOs.Request.CatalogoGenerico;
using GTE.Application.DTOs.Responses.CatalogoGenerico;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Motor de catalogos genericos: consulta y CRUD de datos de un catalogo ya configurado.
/// No confundir con CatalogosController (dropdowns simples de bandeja/admin).
/// </summary>
[ApiController]
[Route("api/v1/catalogo-generico")]
public class CatalogoGenericoController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogoResumenResponse>>>> ObtenerCatalogos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCatalogosQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CatalogoResumenResponse>>.Exito(resultado));
    }

    [HttpGet("{clave}/config")]
    public async Task<ActionResult<ApiResponse<ConfiguracionCatalogoResponse>>> ObtenerConfig(
        string clave, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerConfigCatalogoQuery(clave), cancellationToken);
        return Ok(ApiResponse<ConfiguracionCatalogoResponse>.Exito(resultado));
    }

    [HttpPost("{clave}/registros/consulta")]
    public async Task<ActionResult<ApiResponse<PagedResult<Dictionary<string, object?>>>>> ListarRegistros(
        string clave, [FromBody] ListarRegistrosRequest filtros, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ListarRegistrosCatalogoQuery(clave, filtros), cancellationToken);
        return Ok(ApiResponse<PagedResult<Dictionary<string, object?>>>.Exito(resultado));
    }

    [HttpGet("{clave}/registros/valores-distintos/{nombreColumna}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> ObtenerValoresDistintos(
        string clave, string nombreColumna, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerValoresDistintosQuery(clave, nombreColumna), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<string>>.Exito(resultado));
    }

    [HttpPost("{clave}/registros/descifrar")]
    public async Task<ActionResult<ApiResponse<string?>>> DescifrarValor(
        string clave, [FromBody] DescifrarValorRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new DescifrarValorQuery(clave, request), cancellationToken);
        return Ok(ApiResponse<string?>.Exito(resultado));
    }

    [HttpGet("{clave}/registros/opciones-fk/{nombreColumna}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpcionFkResponse>>>> ObtenerOpcionesFk(
        string clave, string nombreColumna, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerOpcionesFkQuery(clave, nombreColumna), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OpcionFkResponse>>.Exito(resultado));
    }

    [HttpPost("{clave}/registros")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> CrearRegistro(
        string clave, [FromBody] CrearRegistroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearRegistroCatalogoCommand(clave, request.Valores), cancellationToken);
        return Ok(ApiResponse<Dictionary<string, object?>>.Exito(resultado, "Registro creado."));
    }

    [HttpPut("{clave}/registros")]
    public async Task<ActionResult<ApiResponse<object>>> ActualizarRegistro(
        string clave, [FromBody] ActualizarRegistroRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ActualizarRegistroCatalogoCommand(clave, request.ClavesPk, request.Valores), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Registro actualizado."));
    }

    [HttpDelete("{clave}/registros")]
    public async Task<ActionResult<ApiResponse<object>>> EliminarRegistro(
        string clave, [FromBody] EliminarRegistroRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarRegistroCatalogoCommand(clave, request.ClavesPk), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Registro eliminado."));
    }
}
