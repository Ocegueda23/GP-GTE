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

    /// <summary>Lo que puede entrar al release: terminados del proyecto y sin release todavia.</summary>
    [HttpGet("releases/{id:int}/candidatos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CandidatoContenidoResponse>>>> ObtenerCandidatos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCandidatosContenidoQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CandidatoContenidoResponse>>.Exito(resultado));
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

    [HttpDelete("releases/{id:int}/artefactos/{idArtefacto:int}")]
    public async Task<ActionResult<ApiResponse<object>>> QuitarArtefacto(
        int id, int idArtefacto, CancellationToken cancellationToken)
    {
        await mediator.Send(new QuitarArtefactoCommand(id, idArtefacto), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Artefacto retirado del release."));
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

    /// <summary>Paso aparte tras cerrar un sprint: que le falta a cada proyecto para no dejar nada fuera de un release.</summary>
    [HttpGet("sprints/{idSprint:int}/cobertura-release")]
    public async Task<ActionResult<ApiResponse<CoberturaReleaseSprintResponse>>> ObtenerCoberturaRelease(
        int idSprint, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCoberturaReleaseSprintQuery(idSprint), cancellationToken);
        return Ok(ApiResponse<CoberturaReleaseSprintResponse>.Exito(resultado));
    }

    /// <summary>Envia lo terminado y disponible del sprint, de un proyecto, a un release existente o nuevo.</summary>
    [HttpPost("sprints/{idSprint:int}/enviar-a-release")]
    public async Task<ActionResult<ApiResponse<ReleaseDetalleResponse>>> EnviarSprintARelease(
        int idSprint, [FromBody] EnviarSprintAReleaseRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new EnviarSprintAReleaseCommand(idSprint, request), cancellationToken);
        return Ok(ApiResponse<ReleaseDetalleResponse>.Exito(resultado,
            $"Contenido enviado al release {resultado.Version} ({resultado.Folio})."));
    }

    /// <summary>Cadena de aprobacion de releases configurada para el proyecto (vacia = usa el default fijo QA/Lider/Negocio).</summary>
    [HttpGet("proyectos/{idProyecto:int}/cadena-aprobacion")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> ObtenerCadenaAprobacion(
        int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCadenaAprobacionQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<string>>.Exito(resultado));
    }

    /// <summary>Reemplaza la cadena de aprobacion del proyecto (ADM.Workflows). Lista vacia para volver al default.</summary>
    [HttpPut("proyectos/{idProyecto:int}/cadena-aprobacion")]
    public async Task<ActionResult<ApiResponse<object>>> ConfigurarCadenaAprobacion(
        int idProyecto, [FromBody] ConfigurarCadenaAprobacionRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ConfigurarCadenaAprobacionCommand(idProyecto, request.Roles), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Cadena de aprobacion actualizada."));
    }

    [HttpGet("ambientes/matriz")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MatrizAmbienteResponse>>>> ObtenerMatriz(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMatrizAmbientesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MatrizAmbienteResponse>>.Exito(resultado));
    }
}
