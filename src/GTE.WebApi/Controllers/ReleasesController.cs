using GTE.Application.DTOs.Request.Entregas;
using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.Entregas.Commands;
using GTE.Application.Entregas.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Entregas: releases, contenido, artefactos, aprobaciones y despliegues.</summary>
[ApiController]
[Route("api/v1")]
public class ReleasesController(IMediator mediator) : ControllerBase
{
    [HttpGet("releases")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReleaseResponse>>>> ObtenerReleases(
        [FromQuery] int? idProyecto = null,
        [FromQuery] bool soloAbiertos = true,
        CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerReleasesQuery(idProyecto, soloAbiertos), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReleaseResponse>>.Exito(resultado));
    }

    [HttpPost("releases")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> Crear(
        [FromBody] ReleaseCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearReleaseCommand(request), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado,
            $"Release {resultado.Version} creado ({resultado.Folio})."));
    }

    [HttpGet("releases/{id:int}")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> ObtenerDetalle(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerReleaseQuery(id), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado));
    }

    /// <summary>SOLICITAR_APROBACION, CANCELAR o ROLLBACK.</summary>
    [HttpPut("releases/{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> CambiarEstatus(
        int id, [FromBody] CambiarEstatusReleaseRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CambiarEstatusReleaseCommand(id, request.Accion, request.Motivo), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado, $"El release paso a {resultado.Estatus}."));
    }

    /// <summary>Agrega elementos terminados y sin hallazgos pendientes al release.</summary>
    [HttpPost("releases/{id:int}/items")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> AgregarContenido(
        int id, [FromBody] AgregarContenidoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AgregarContenidoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado, "Contenido actualizado."));
    }

    [HttpDelete("releases/{id:int}/items/{idWorkItem:int}")]
    public async Task<ActionResult<ApiResponse<object>>> QuitarContenido(
        int id, int idWorkItem, CancellationToken cancellationToken)
    {
        await mediator.Send(new QuitarContenidoCommand(id, idWorkItem), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Elemento retirado del release."));
    }

    [HttpPost("releases/{id:int}/artefactos")]
    public async Task<ActionResult<ApiResponse<int>>> AgregarArtefacto(
        int id, [FromBody] ArtefactoAgregarRequest request, CancellationToken cancellationToken)
    {
        var idArtefacto = await mediator.Send(new AgregarArtefactoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(idArtefacto, "Artefacto registrado."));
    }

    /// <summary>Firma una aprobacion de la cadena; rechazar regresa el release a preparacion.</summary>
    [HttpPost("aprobaciones/{idAprobacion:int}/resolver")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> ResolverAprobacion(
        int idAprobacion, [FromBody] ResolverAprobacionRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ResolverAprobacionCommand(idAprobacion, request), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado,
            request.Aprobada ? "Aprobacion firmada." : "Release rechazado y devuelto a preparacion."));
    }

    /// <summary>Registra un despliegue; en produccion exige release aprobado.</summary>
    [HttpPost("releases/{id:int}/despliegues")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> RegistrarDespliegue(
        int id, [FromBody] DespliegueRegistrarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RegistrarDespliegueCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado,
            request.EsRollback ? "Rollback registrado." : "Despliegue registrado."));
    }

    [HttpPost("releases/{id:int}/notas")]
    public async Task<ActionResult<ApiResponse<string>>> GenerarNotas(
        int id, CancellationToken cancellationToken)
    {
        var notas = await mediator.Send(new GenerarNotasCommand(id), cancellationToken);
        return Ok(ApiResponse<string>.Exito(notas, "Notas de version generadas."));
    }

    [HttpGet("ambientes/matriz")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MatrizAmbienteResponse>>>> ObtenerMatriz(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMatrizAmbientesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MatrizAmbienteResponse>>.Exito(resultado));
    }
}
