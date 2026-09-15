using GTE.Application.DTOs.Request.NotasVersion;
using GTE.Application.DTOs.Responses.NotasVersion;
using GTE.Application.NotasVersion.Commands;
using GTE.Application.NotasVersion.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Notas de version: que trae cada liberacion, redactado para el usuario final. El historial
/// publicado lo puede leer cualquier usuario autenticado (es lo que abre al hacer click en la
/// version de la barra superior); redactar y publicar exige ADM.NotasVersion.
/// </summary>
[ApiController]
[Route("api/v1/notas-version")]
public class NotasVersionController(IMediator mediator) : ControllerBase
{
    /// <summary>Historial publicado. Sin permiso: todos deben poder ver que version traen.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotaVersionResponse>>>> ObtenerPublicadas(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerNotasVersionPublicadasQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotaVersionResponse>>.Exito(resultado));
    }

    [HttpGet("tipos-cambio")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TipoCambioVersionResponse>>>> ObtenerTiposCambio(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTiposCambioVersionQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TipoCambioVersionResponse>>.Exito(resultado));
    }

    /// <summary>Listado de administracion: incluye borradores.</summary>
    [HttpGet("administracion")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotaVersionListaResponse>>>> ObtenerTodas(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerNotasVersionQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotaVersionListaResponse>>.Exito(resultado));
    }

    [HttpGet("{idNotaVersion:int}")]
    public async Task<ActionResult<ApiResponse<NotaVersionResponse>>> ObtenerPorId(
        int idNotaVersion, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerNotaVersionQuery(idNotaVersion), cancellationToken);
        return Ok(ApiResponse<NotaVersionResponse>.Exito(resultado));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotaVersionResponse>>> Crear(
        [FromBody] NotaVersionUpsertRequest datos, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearNotaVersionCommand(datos), cancellationToken);
        return Ok(ApiResponse<NotaVersionResponse>.Exito(resultado, "Nota de version guardada."));
    }

    [HttpPut("{idNotaVersion:int}")]
    public async Task<ActionResult<ApiResponse<NotaVersionResponse>>> Actualizar(
        int idNotaVersion, [FromBody] NotaVersionUpsertRequest datos, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ActualizarNotaVersionCommand(idNotaVersion, datos), cancellationToken);
        return Ok(ApiResponse<NotaVersionResponse>.Exito(resultado, "Nota de version actualizada."));
    }

    [HttpDelete("{idNotaVersion:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Eliminar(
        int idNotaVersion, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarNotaVersionCommand(idNotaVersion), cancellationToken);
        return Ok(ApiResponse<object>.Exito(null!, "Nota de version eliminada."));
    }
}
