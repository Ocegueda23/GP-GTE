using GTE.Application.DTOs.Request.Workflow;
using GTE.Application.DTOs.Responses.Workflow;
using GTE.Application.Workflow.Commands;
using GTE.Application.Workflow.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Editor de Workflows (P21, permiso ADM.Workflows): grafo de tblProceso/tblTransicion, solo lectura, mas edicion de metadatos de UI (tblTransicionConfig).</summary>
[ApiController]
[Route("api/v1/workflow")]
public class WorkflowController(IMediator mediator) : ControllerBase
{
    [HttpGet("procesos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProcesoWorkflowResponse>>>> ObtenerProcesos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerProcesosWorkflowQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProcesoWorkflowResponse>>.Exito(resultado));
    }

    [HttpGet("permisos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermisoWorkflowResponse>>>> ObtenerPermisos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerPermisosWorkflowQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermisoWorkflowResponse>>.Exito(resultado));
    }

    [HttpGet("{proceso}/definicion")]
    public async Task<ActionResult<ApiResponse<DefinicionWorkflowResponse>>> ObtenerDefinicion(
        string proceso, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerDefinicionWorkflowQuery(proceso), cancellationToken);
        return Ok(ApiResponse<DefinicionWorkflowResponse>.Exito(resultado));
    }

    [HttpPut("{proceso}/transiciones")]
    public async Task<ActionResult<ApiResponse<object>>> GuardarTransiciones(
        string proceso, [FromBody] List<TransicionConfigRequest> request, CancellationToken cancellationToken)
    {
        await mediator.Send(new GuardarTransicionesConfigCommand(proceso, request), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Configuracion de transiciones guardada."));
    }
}
