using GTE.Application.DTOs.Responses.Notificaciones;
using GTE.Application.Notificaciones.Commands;
using GTE.Application.Notificaciones.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Notificaciones In-App del usuario autenticado (campana).</summary>
[ApiController]
[Route("api/v1/me/notificaciones")]
public class NotificacionesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificacionResponse>>>> Obtener(
        [FromQuery] bool soloNoLeidas, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerNotificacionesQuery(soloNoLeidas), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificacionResponse>>.Exito(resultado));
    }

    [HttpPut("{id:long}/leer")]
    public async Task<ActionResult<ApiResponse<object>>> MarcarLeida(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarcarNotificacionLeidaCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Notificacion marcada como leida."));
    }

    [HttpPut("leer-todas")]
    public async Task<ActionResult<ApiResponse<object>>> MarcarTodasLeidas(CancellationToken cancellationToken)
    {
        await mediator.Send(new MarcarTodasNotificacionesLeidasCommand(), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Notificaciones marcadas como leidas."));
    }
}
