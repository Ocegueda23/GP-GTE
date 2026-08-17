using GTE.Application.Dashboard.Queries;
using GTE.Application.DTOs.Responses.Dashboard;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Dashboard Ejecutivo de Metricas. Sin permiso especifico de acceso -- cualquier usuario
/// autenticado ve, como minimo, su propia informacion; el alcance (Global/Departamento/
/// Equipo/Personal) lo resuelve el handler segun DASH.Ejecutivo/DASH.VerDepartamento y la
/// jerarquia real (tblEquipo.IdLider / tblUsuario.IdJefe).
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> Obtener(
        [FromQuery] int anio, [FromQuery] int mes,
        [FromQuery] int? idProyecto, [FromQuery] int? idArea, [FromQuery] int? idUsuario,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ObtenerDashboardQuery(anio, mes, idProyecto, idArea, idUsuario), cancellationToken);
        return Ok(ApiResponse<DashboardResponse>.Exito(resultado));
    }

    [HttpGet("empleados/{idUsuario:int}")]
    public async Task<ActionResult<ApiResponse<IndicadoresEmpleadoResponse>>> ObtenerIndicadoresEmpleado(
        int idUsuario, [FromQuery] int anio, [FromQuery] int mes, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerIndicadoresEmpleadoQuery(idUsuario, anio, mes), cancellationToken);
        return Ok(ApiResponse<IndicadoresEmpleadoResponse>.Exito(resultado));
    }

    [HttpGet("tendencias")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TendenciaResponse>>>> ObtenerTendencias(
        [FromQuery] int anio, [FromQuery] int? idProyecto, [FromQuery] int? idArea, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerTendenciasDashboardQuery(anio, idProyecto, idArea), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TendenciaResponse>>.Exito(resultado));
    }

    [HttpGet("filtros")]
    public async Task<ActionResult<ApiResponse<FiltroCatalogosDashboardResponse>>> ObtenerFiltros(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerFiltrosDashboardQuery(), cancellationToken);
        return Ok(ApiResponse<FiltroCatalogosDashboardResponse>.Exito(resultado));
    }
}
