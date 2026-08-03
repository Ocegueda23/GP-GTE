using GTE.Application.Common;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Application.WorkItems.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Modulo WorkItems: bandeja, detalle, ciclo de vida, tiempo.
/// TODO Fase 0 pendiente: activar [Authorize] cuando el tenant de Entra ID
/// este configurado; mientras, la auditoria registra la identidad disponible.
/// </summary>
[ApiController]
[Route("api/v1/workitems")]
public class WorkItemsController(IMediator mediator) : ControllerBase
{
    /// <summary>Bandeja de trabajo. Sin filtro de estatus = abiertos; estatus=-1 = todos.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BandejaItemResponse>>>> ObtenerBandeja(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery(Name = "estatus")] int[]? estatus = null,
        [FromQuery] int? idProyecto = null,
        [FromQuery] int? idAsignado = null,
        [FromQuery] int? idTipo = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool soloVencidas = false,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] bool ordenDescendente = false,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroBandeja(
            page, pageSize, estatus, idProyecto, idAsignado, idTipo, texto, soloVencidas, ordenarPor, ordenDescendente);
        var resultado = await mediator.Send(new ObtenerBandejaQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<BandejaItemResponse>>.Exito(resultado));
    }

    [HttpGet("{folio}")]
    public async Task<ActionResult<ApiResponse<WorkItemResponse>>> ObtenerPorFolio(
        string folio, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerWorkItemPorFolioQuery(folio), cancellationToken);
        return Ok(ApiResponse<WorkItemResponse>.Exito(resultado));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkItemResponse>>> Crear(
        [FromBody] WorkItemCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearWorkItemCommand(request), cancellationToken);
        return Ok(ApiResponse<WorkItemResponse>.Exito(resultado,
            $"Elemento {resultado.Folio} creado correctamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkItemResponse>>> Actualizar(
        int id, [FromBody] WorkItemActualizarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarWorkItemCommand(id, request), cancellationToken);
        return Ok(ApiResponse<WorkItemResponse>.Exito(resultado));
    }

    /// <summary>El body manda la ACCION del grafo; el estatus destino lo decide el motor.</summary>
    [HttpPut("{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<EstatusCambiadoResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CambiarEstatusWorkItemCommand(id, request.Accion, request.Motivo), cancellationToken);
        return Ok(ApiResponse<EstatusCambiadoResponse>.Exito(resultado,
            $"El elemento paso a {resultado.Estatus}."));
    }

    /// <summary>Acciones de workflow validas para el usuario actual (pinta los botones).</summary>
    [HttpGet("{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAcciones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesWorkItemQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    [HttpPost("{id:int}/tiempo")]
    public async Task<ActionResult<ApiResponse<int>>> RegistrarTiempo(
        int id, [FromBody] RegistrarTiempoRequest request, CancellationToken cancellationToken)
    {
        var idRegistro = await mediator.Send(new RegistrarTiempoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(idRegistro, "Tiempo registrado correctamente."));
    }

    [HttpGet("{id:int}/tiempo")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RegistroTiempoResponse>>>> ObtenerTiempos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTiemposQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RegistroTiempoResponse>>.Exito(resultado));
    }

    /// <summary>Subtareas (WorkItems hijos) de este elemento.</summary>
    [HttpGet("{id:int}/hijos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkItemHijoResponse>>>> ObtenerHijos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerHijosQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkItemHijoResponse>>.Exito(resultado));
    }
}
