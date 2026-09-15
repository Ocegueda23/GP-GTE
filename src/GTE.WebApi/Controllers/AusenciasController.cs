using GTE.Application.Ausencias.Commands;
using GTE.Application.Ausencias.Queries;
using GTE.Application.Common;
using GTE.Application.DTOs.Request.Ausencias;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Registro de ausencias (cada quien las suyas) y bandeja de aprobacion (ADM.Ausencias).</summary>
[ApiController]
[Route("api/v1/ausencias")]
public class AusenciasController(IMediator mediator) : ControllerBase
{
    /// <summary>Registra una ausencia en estatus Solicitada y avisa a quien aprueba.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AusenciaResponse>>> Crear(
        [FromBody] AusenciaCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearAusenciaCommand(request), cancellationToken);
        return Ok(ApiResponse<AusenciaResponse>.Exito(resultado, "Ausencia registrada, queda pendiente de aprobacion."));
    }

    /// <summary>Ausencias del usuario actual. Sin estatus = vigentes (Solicitada, Aprobada); estatus=-1 = todas.</summary>
    [HttpGet("mias")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AusenciaResponse>>>> ObtenerMias(
        [FromQuery(Name = "estatus")] int[]? estatus, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMisAusenciasQuery(estatus), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AusenciaResponse>>.Exito(resultado));
    }

    /// <summary>Bandeja de aprobacion. Sin filtro = pendientes (Solicitada); estatus=-1 = todas.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AusenciaResponse>>>> ObtenerBandeja(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery(Name = "estatus")] int[]? estatus = null,
        [FromQuery] int? idUsuario = null,
        [FromQuery] int? idTipoAusencia = null,
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] bool ordenDescendente = false,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroAusencias(
            page, pageSize, estatus, idUsuario, idTipoAusencia, desde, hasta, ordenarPor, ordenDescendente);
        var resultado = await mediator.Send(new ObtenerBandejaAusenciasQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<AusenciaResponse>>.Exito(resultado));
    }

    /// <summary>Cuantas ausencias esperan aprobacion (0 si el usuario no las gestiona).</summary>
    [HttpGet("pendientes/conteo")]
    public async Task<ActionResult<ApiResponse<int>>> ContarPendientes(CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ContarAusenciasPendientesQuery(), cancellationToken);
        return Ok(ApiResponse<int>.Exito(resultado));
    }

    /// <summary>Tipos de ausencia y catalogo de estatus para los filtros de la pantalla.</summary>
    [HttpGet("catalogos")]
    public async Task<ActionResult<ApiResponse<CatalogosAusenciaResponse>>> ObtenerCatalogos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCatalogosAusenciaQuery(), cancellationToken);
        return Ok(ApiResponse<CatalogosAusenciaResponse>.Exito(resultado));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AusenciaResponse>>> ObtenerPorId(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAusenciaQuery(id), cancellationToken);
        return Ok(ApiResponse<AusenciaResponse>.Exito(resultado));
    }

    /// <summary>Edita el periodo o el tipo mientras la ausencia siga en Solicitada.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AusenciaResponse>>> Actualizar(
        int id, [FromBody] AusenciaEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarAusenciaCommand(id, request), cancellationToken);
        return Ok(ApiResponse<AusenciaResponse>.Exito(resultado, "Ausencia actualizada."));
    }

    [HttpGet("{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAcciones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesAusenciaQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    /// <summary>APROBAR, RECHAZAR (con motivo) o CANCELAR. El front manda la accion, no el estatus.</summary>
    [HttpPut("{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<AusenciaResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusAusenciaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CambiarEstatusAusenciaCommand(id, request.Accion, request.Motivo), cancellationToken);
        return Ok(ApiResponse<AusenciaResponse>.Exito(resultado, $"La ausencia paso a {resultado.Estatus}."));
    }
}
