using GTE.Application.DTOs.Request.Okr;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Okr.Commands;
using GTE.Application.Okr.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Portafolio: objetivos trimestrales (OKR) con resultados clave.</summary>
[ApiController]
[Route("api/v1/okr")]
public class OkrController(IMediator mediator) : ControllerBase
{
    /* ---------- Objetivos ---------- */

    [HttpGet("objetivos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjetivoOkrResponse>>>> ObtenerObjetivos(
        [FromQuery] int? idProyecto, [FromQuery] int? idEquipo, [FromQuery] int? anio,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerObjetivosOkrQuery(idProyecto, idEquipo, anio), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ObjetivoOkrResponse>>.Exito(resultado));
    }

    [HttpPost("objetivos")]
    public async Task<ActionResult<ApiResponse<ObjetivoOkrResponse>>> CrearObjetivo(
        [FromBody] ObjetivoOkrCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearObjetivoOkrCommand(request), cancellationToken);
        return Ok(ApiResponse<ObjetivoOkrResponse>.Exito(resultado, $"Objetivo {resultado.Nombre} creado."));
    }

    [HttpPut("objetivos/{id:int}")]
    public async Task<ActionResult<ApiResponse<ObjetivoOkrResponse>>> ActualizarObjetivo(
        int id, [FromBody] ObjetivoOkrEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarObjetivoOkrCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ObjetivoOkrResponse>.Exito(resultado, "Objetivo actualizado."));
    }

    [HttpPut("objetivos/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarObjetivo(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarObjetivoOkrCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Objetivo retirado."));
    }

    /* ---------- Resultados clave ---------- */

    [HttpPost("objetivos/{id:int}/resultados")]
    public async Task<ActionResult<ApiResponse<ObjetivoOkrResponse>>> CrearResultadoClave(
        int id, [FromBody] ResultadoClaveCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearResultadoClaveCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ObjetivoOkrResponse>.Exito(resultado, "Resultado clave agregado."));
    }

    [HttpPut("objetivos/{id:int}/resultados/{idResultado:int}")]
    public async Task<ActionResult<ApiResponse<ObjetivoOkrResponse>>> ActualizarResultadoClave(
        int id, int idResultado, [FromBody] ResultadoClaveEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarResultadoClaveCommand(id, idResultado, request), cancellationToken);
        return Ok(ApiResponse<ObjetivoOkrResponse>.Exito(resultado, "Resultado clave actualizado."));
    }

    [HttpPut("objetivos/{id:int}/resultados/{idResultado:int}/retirar")]
    public async Task<ActionResult<ApiResponse<ObjetivoOkrResponse>>> RetirarResultadoClave(
        int id, int idResultado, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RetirarResultadoClaveCommand(id, idResultado), cancellationToken);
        return Ok(ApiResponse<ObjetivoOkrResponse>.Exito(resultado, "Resultado clave retirado."));
    }
}
