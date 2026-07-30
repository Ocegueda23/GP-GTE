using GTE.Application.Common;
using GTE.Application.DTOs.Request.Solicitudes;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.Solicitudes.Commands;
using GTE.Application.Solicitudes.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Portal de solicitudes (cliente interno) y triage (lider).</summary>
[ApiController]
[Route("api/v1/solicitudes")]
public class SolicitudesController(IMediator mediator) : ControllerBase
{
    /// <summary>Crea y envia la solicitud en un paso (folio SOL-anio-NNNN).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SolicitudResponse>>> Crear(
        [FromBody] SolicitudCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearSolicitudCommand(request), cancellationToken);
        return Ok(ApiResponse<SolicitudResponse>.Exito(resultado,
            $"Solicitud {resultado.Folio} enviada correctamente."));
    }

    /// <summary>Solicitudes del usuario actual (portal).</summary>
    [HttpGet("mias")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SolicitudResponse>>>> ObtenerMias(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMisSolicitudesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SolicitudResponse>>.Exito(resultado));
    }

    /// <summary>Bandeja de triage. Sin filtro = pendientes (Enviada, En Analisis, Aprobada); estatus=-1 = todas.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SolicitudResponse>>>> ObtenerTriage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery(Name = "estatus")] int[]? estatus = null,
        [FromQuery] string? texto = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroTriage(page, pageSize, estatus, texto);
        var resultado = await mediator.Send(new ObtenerTriageQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<SolicitudResponse>>.Exito(resultado));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SolicitudResponse>>> ObtenerPorId(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerSolicitudQuery(id), cancellationToken);
        return Ok(ApiResponse<SolicitudResponse>.Exito(resultado));
    }

    [HttpGet("{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAcciones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesSolicitudQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    /// <summary>TOMAR, APROBAR (con idProyecto), RECHAZAR/DEVOLVER (con motivo), CANCELAR.</summary>
    [HttpPut("{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<SolicitudResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusSolicitudRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CambiarEstatusSolicitudCommand(
            id, request.Accion, request.Motivo, request.IdProyecto), cancellationToken);
        return Ok(ApiResponse<SolicitudResponse>.Exito(resultado,
            $"La solicitud paso a {resultado.Estatus}."));
    }

    /// <summary>Convierte la solicitud aprobada en WorkItems trazados (patron uiId).</summary>
    [HttpPost("{id:int}/convertir")]
    public async Task<ActionResult<ApiResponse<ConversionResponse>>> Convertir(
        int id, [FromBody] ConvertirSolicitudRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ConvertirSolicitudCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ConversionResponse>.Exito(resultado,
            $"Solicitud convertida en {resultado.Items.Count} elemento(s) de trabajo."));
    }
}
