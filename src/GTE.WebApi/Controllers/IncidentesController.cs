using GTE.Application.Common;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.Operacion.Commands;
using GTE.Application.Operacion.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Operacion: bandeja y detalle de incidentes (permiso INC.Gestionar).</summary>
[ApiController]
[Route("api/v1/incidentes")]
public class IncidentesController(IMediator mediator) : ControllerBase
{
    /// <summary>Crea el incidente en Detectado (folio INC-anio-NNNN). S1 notifica al responsable del proyecto.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> Crear(
        [FromBody] IncidenteCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearIncidenteCommand(request), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado,
            $"Incidente {resultado.Folio} registrado correctamente."));
    }

    /// <summary>Bandeja de incidentes. Sin filtro = abiertos (todos menos Cerrado); estatus=-1 = todos.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<IncidenteResponse>>>> ObtenerBandeja(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery(Name = "estatus")] int[]? estatus = null,
        [FromQuery] int? idSeveridad = null,
        [FromQuery] int? idProyecto = null,
        [FromQuery] string? texto = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroBandejaIncidente(page, pageSize, estatus, idSeveridad, idProyecto, texto);
        var resultado = await mediator.Send(new ObtenerBandejaIncidentesQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<IncidenteResponse>>.Exito(resultado));
    }

    /// <summary>Detalle por folio (ruta /operacion/incidentes/:folio de la SPA).</summary>
    [HttpGet("{folio}")]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> ObtenerPorFolio(
        string folio, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerIncidentePorFolioQuery(folio), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado));
    }

    [HttpGet("{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAcciones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesIncidenteQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    /// <summary>Titulo, descripcion, causa raiz, minutos de indisponibilidad, fecha de deteccion. No toca el estatus.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> Actualizar(
        int id, [FromBody] IncidenteActualizarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarIncidenteCommand(id, request), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado, "Incidente actualizado correctamente."));
    }

    /// <summary>ATENDER, MITIGAR, RESOLVER, CERRAR (CERRAR valida causa raiz en S1/S2).</summary>
    [HttpPut("{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusIncidenteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CambiarEstatusIncidenteCommand(
            id, request.Accion, request.Motivo), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado,
            $"El incidente paso a {resultado.Estatus}."));
    }

    /// <summary>RN-OPS-03: cambio de severidad con motivo obligatorio.</summary>
    [HttpPut("{id:int}/severidad")]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> CambiarSeveridad(
        int id, [FromBody] CambiarSeveridadIncidenteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CambiarSeveridadIncidenteCommand(id, request), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado,
            $"Severidad actualizada a {resultado.Severidad}."));
    }

    /// <summary>Crea un WorkItem tipo Correccion y lo vincula; el incidente no cambia de estatus.</summary>
    [HttpPost("{id:int}/correctivo")]
    public async Task<ActionResult<ApiResponse<VincularCorrectivoResponse>>> VincularCorrectivo(
        int id, [FromBody] VincularCorrectivoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new VincularCorrectivoIncidenteCommand(id, request), cancellationToken);
        return Ok(ApiResponse<VincularCorrectivoResponse>.Exito(resultado,
            $"Incidente vinculado a {resultado.Folio}."));
    }

    /// <summary>Vincula un release ya existente como causante.</summary>
    [HttpPost("{id:int}/release-causante")]
    public async Task<ActionResult<ApiResponse<IncidenteResponse>>> VincularReleaseCausante(
        int id, [FromBody] VincularReleaseCausanteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new VincularReleaseCausanteIncidenteCommand(id, request), cancellationToken);
        return Ok(ApiResponse<IncidenteResponse>.Exito(resultado, "Release causante vinculado correctamente."));
    }
}
