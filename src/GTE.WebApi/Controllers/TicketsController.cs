using GTE.Application.Common;
using GTE.Application.DTOs.Request.Soporte;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.Soporte.Commands;
using GTE.Application.Soporte.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Mesa de ayuda: portal de tickets (cualquier usuario) y bandeja de agentes (TKT.Atender).</summary>
[ApiController]
[Route("api/v1/tickets")]
public class TicketsController(IMediator mediator) : ControllerBase
{
    /// <summary>Crea el ticket en Nuevo (folio TKT-anio-NNNN, SLA resuelto por prioridad).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> Crear(
        [FromBody] TicketCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearTicketCommand(request), cancellationToken);
        return Ok(ApiResponse<TicketResponse>.Exito(resultado,
            $"Ticket {resultado.Folio} registrado correctamente."));
    }

    /// <summary>Tickets del usuario actual (portal).</summary>
    [HttpGet("mios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketResponse>>>> ObtenerMios(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMisTicketsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TicketResponse>>.Exito(resultado));
    }

    /// <summary>Bandeja de mesa de ayuda. Sin filtro = abiertos (todos menos Cerrado); estatus=-1 = todos.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TicketResponse>>>> ObtenerBandeja(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery(Name = "estatus")] int[]? estatus = null,
        [FromQuery] string? texto = null,
        [FromQuery] int? idAsignado = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroBandejaTicket(page, pageSize, estatus, texto, idAsignado);
        var resultado = await mediator.Send(new ObtenerBandejaTicketsQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<TicketResponse>>.Exito(resultado));
    }

    /// <summary>Detalle por folio (ruta /tickets/:folio de la SPA, mismo patron que WorkItem).</summary>
    [HttpGet("{folio}")]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> ObtenerPorFolio(
        string folio, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTicketPorFolioQuery(folio), cancellationToken);
        return Ok(ApiResponse<TicketResponse>.Exito(resultado));
    }

    [HttpGet("{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAcciones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesTicketQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    /// <summary>ASIGNAR (con idAsignado), INICIAR_ATENCION, ESPERAR_USUARIO, REANUDAR, RESOLVER, CERRAR, REABRIR.</summary>
    [HttpPut("{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusTicketRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CambiarEstatusTicketCommand(
            id, request.Accion, request.Motivo, request.IdAsignado), cancellationToken);
        return Ok(ApiResponse<TicketResponse>.Exito(resultado,
            $"El ticket paso a {resultado.Estatus}."));
    }

    /// <summary>Crea un WorkItem tipo Soporte y lo vincula; el ticket no cambia de estatus.</summary>
    [HttpPost("{id:int}/escalar")]
    public async Task<ActionResult<ApiResponse<EscalarTicketResponse>>> Escalar(
        int id, [FromBody] EscalarTicketRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new EscalarTicketCommand(id, request), cancellationToken);
        return Ok(ApiResponse<EscalarTicketResponse>.Exito(resultado,
            $"Ticket escalado a {resultado.Folio}."));
    }

    /// <summary>Encuesta de satisfaccion (solo el solicitante, ticket Resuelto o Cerrado).</summary>
    [HttpPost("{id:int}/encuesta")]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> RegistrarEncuesta(
        int id, [FromBody] EncuestaTicketRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RegistrarEncuestaTicketCommand(id, request), cancellationToken);
        return Ok(ApiResponse<TicketResponse>.Exito(resultado, "Gracias por tu calificacion."));
    }
}
