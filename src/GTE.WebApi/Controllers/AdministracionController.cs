using GTE.Application.Administracion.Commands;
using GTE.Application.Administracion.Queries;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Administracion: proyectos, equipos, usuarios, roles, horarios y ambientes.</summary>
[ApiController]
[Route("api/v1")]
public class AdministracionController(IMediator mediator) : ControllerBase
{
    /* ---------- Proyectos ---------- */

    [HttpGet("proyectos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProyectoResponse>>>> ObtenerProyectos(
        [FromQuery] bool soloActivos = true, CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerProyectosQuery(soloActivos), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProyectoResponse>>.Exito(resultado));
    }

    [HttpGet("proyectos/{id:int}")]
    public async Task<ActionResult<ApiResponse<ProyectoResponse>>> ObtenerProyecto(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerProyectoQuery(id), cancellationToken);
        return Ok(ApiResponse<ProyectoResponse>.Exito(resultado));
    }

    [HttpPost("proyectos")]
    public async Task<ActionResult<ApiResponse<ProyectoResponse>>> CrearProyecto(
        [FromBody] ProyectoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearProyectoCommand(request), cancellationToken);
        return Ok(ApiResponse<ProyectoResponse>.Exito(resultado, $"Proyecto {resultado.Nombre} creado."));
    }

    [HttpPut("proyectos/{id:int}")]
    public async Task<ActionResult<ApiResponse<ProyectoResponse>>> ActualizarProyecto(
        int id, [FromBody] ProyectoEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarProyectoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ProyectoResponse>.Exito(resultado, "Proyecto actualizado."));
    }

    [HttpGet("proyectos/{id:int}/acciones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccionDisponibleResponse>>>> ObtenerAccionesProyecto(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAccionesProyectoQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccionDisponibleResponse>>.Exito(resultado));
    }

    [HttpPut("proyectos/{id:int}/estatus")]
    public async Task<ActionResult<ApiResponse<ProyectoResponse>>> CambiarEstatusProyecto(
        int id, [FromBody] CambiarEstatusProyectoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CambiarEstatusProyectoCommand(id, request.Accion), cancellationToken);
        return Ok(ApiResponse<ProyectoResponse>.Exito(resultado, $"El proyecto paso a {resultado.Estatus}."));
    }

    /* ---------- Equipos ---------- */

    [HttpGet("equipos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipoResponse>>>> ObtenerEquipos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerEquiposQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EquipoResponse>>.Exito(resultado));
    }

    [HttpGet("equipos/{id:int}")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> ObtenerEquipo(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerEquipoQuery(id), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado));
    }

    [HttpPost("equipos")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> CrearEquipo(
        [FromBody] EquipoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearEquipoCommand(request), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado, $"Equipo {resultado.Nombre} creado."));
    }

    [HttpPut("equipos/{id:int}")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> ActualizarEquipo(
        int id, [FromBody] EquipoEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarEquipoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado, "Equipo actualizado."));
    }

    [HttpPost("equipos/{id:int}/miembros")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> AgregarMiembroEquipo(
        int id, [FromBody] MiembroEquipoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AgregarMiembroEquipoCommand(id, request), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado, "Miembro agregado al equipo."));
    }

    [HttpPut("equipos/{id:int}/miembros/{idMiembro:int}")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> ActualizarMiembroEquipo(
        int id, int idMiembro, [FromBody] MiembroEquipoEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarMiembroEquipoCommand(id, idMiembro, request), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado, "Miembro actualizado."));
    }

    [HttpPut("equipos/{id:int}/miembros/{idMiembro:int}/retirar")]
    public async Task<ActionResult<ApiResponse<EquipoDetalleResponse>>> RetirarMiembroEquipo(
        int id, int idMiembro, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RetirarMiembroEquipoCommand(id, idMiembro), cancellationToken);
        return Ok(ApiResponse<EquipoDetalleResponse>.Exito(resultado, "Miembro retirado del equipo."));
    }

    /* ---------- Usuarios ---------- */

    [HttpGet("usuarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UsuarioResponse>>>> ObtenerUsuarios(
        [FromQuery] string? texto = null, [FromQuery] bool soloActivos = true,
        CancellationToken cancellationToken = default)
    {
        var resultado = await mediator.Send(new ObtenerUsuariosQuery(texto, soloActivos), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UsuarioResponse>>.Exito(resultado));
    }

    [HttpGet("usuarios/{id:int}")]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> ObtenerUsuario(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerUsuarioQuery(id), cancellationToken);
        return Ok(ApiResponse<UsuarioResponse>.Exito(resultado));
    }

    [HttpPost("usuarios")]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> CrearUsuario(
        [FromBody] UsuarioCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearUsuarioCommand(request), cancellationToken);
        return Ok(ApiResponse<UsuarioResponse>.Exito(resultado, $"Usuario {resultado.Nombre} creado."));
    }

    [HttpPut("usuarios/{id:int}")]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> ActualizarUsuario(
        int id, [FromBody] UsuarioEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarUsuarioCommand(id, request), cancellationToken);
        return Ok(ApiResponse<UsuarioResponse>.Exito(resultado, "Usuario actualizado."));
    }

    [HttpPut("usuarios/{id:int}/baja")]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> DarBajaUsuario(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new DarBajaUsuarioCommand(id), cancellationToken);
        return Ok(ApiResponse<UsuarioResponse>.Exito(resultado, $"Usuario {resultado.Nombre} dado de baja."));
    }

    /* ---------- Roles ---------- */

    [HttpGet("usuarios/{id:int}/roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolUsuarioResponse>>>> ObtenerRolesUsuario(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRolesUsuarioQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RolUsuarioResponse>>.Exito(resultado));
    }

    [HttpPost("usuarios/{id:int}/roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolUsuarioResponse>>>> AsignarRol(
        int id, [FromBody] AsignarRolRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AsignarRolCommand(id, request), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RolUsuarioResponse>>.Exito(resultado, "Rol asignado."));
    }

    [HttpPut("usuarios/{id:int}/roles/{idUsuarioRol:int}/retirar")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolUsuarioResponse>>>> RetirarRol(
        int id, int idUsuarioRol, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RetirarRolCommand(id, idUsuarioRol), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RolUsuarioResponse>>.Exito(resultado, "Rol retirado."));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolResponse>>>> ObtenerRoles(CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerRolesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RolResponse>>.Exito(resultado));
    }

    [HttpGet("roles/{id:int}/permisos")]
    public async Task<ActionResult<ApiResponse<MatrizPermisosResponse>>> ObtenerMatrizPermisos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerMatrizPermisosQuery(id), cancellationToken);
        return Ok(ApiResponse<MatrizPermisosResponse>.Exito(resultado));
    }

    /// <summary>Guardado en lote de la matriz rol-permiso: un solo PUT, no un round-trip por fila.</summary>
    [HttpPut("roles/{id:int}/permisos")]
    public async Task<ActionResult<ApiResponse<MatrizPermisosResponse>>> GuardarMatrizPermisos(
        int id, [FromBody] GuardarMatrizPermisosRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new GuardarMatrizPermisosCommand(id, request), cancellationToken);
        return Ok(ApiResponse<MatrizPermisosResponse>.Exito(resultado, "Matriz de permisos guardada."));
    }

    /* ---------- Horarios ---------- */

    [HttpGet("horarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HorarioResponse>>>> ObtenerHorarios(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerHorariosQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<HorarioResponse>>.Exito(resultado));
    }

    [HttpGet("horarios/{id:int}")]
    public async Task<ActionResult<ApiResponse<HorarioDetalleResponse>>> ObtenerHorario(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerHorarioQuery(id), cancellationToken);
        return Ok(ApiResponse<HorarioDetalleResponse>.Exito(resultado));
    }

    [HttpPost("horarios")]
    public async Task<ActionResult<ApiResponse<HorarioResponse>>> CrearHorario(
        [FromBody] HorarioCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearHorarioCommand(request), cancellationToken);
        return Ok(ApiResponse<HorarioResponse>.Exito(resultado, $"Horario {resultado.Nombre} creado."));
    }

    /// <summary>Reemplaza en una sola llamada todos los tramos del horario (soporta turnos partidos).</summary>
    [HttpPut("horarios/{id:int}/tramos")]
    public async Task<ActionResult<ApiResponse<HorarioDetalleResponse>>> GuardarTramosHorario(
        int id, [FromBody] GuardarTramosHorarioRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new GuardarTramosHorarioCommand(id, request), cancellationToken);
        return Ok(ApiResponse<HorarioDetalleResponse>.Exito(resultado, "Tramos del horario guardados."));
    }

    [HttpGet("festivos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiaFestivoResponse>>>> ObtenerFestivos(
        [FromQuery] int? idHorario, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerFestivosQuery(idHorario), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DiaFestivoResponse>>.Exito(resultado));
    }

    [HttpPost("festivos")]
    public async Task<ActionResult<ApiResponse<DiaFestivoResponse>>> CrearFestivo(
        [FromBody] DiaFestivoCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearFestivoCommand(request), cancellationToken);
        return Ok(ApiResponse<DiaFestivoResponse>.Exito(resultado, "Dia festivo agregado."));
    }

    [HttpPut("festivos/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarFestivo(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarFestivoCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Dia festivo retirado."));
    }

    /* ---------- Ambientes ---------- */

    [HttpGet("ambientes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AmbienteResponse>>>> ObtenerAmbientes(
        [FromQuery] int? idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAmbientesQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AmbienteResponse>>.Exito(resultado));
    }

    [HttpPost("ambientes")]
    public async Task<ActionResult<ApiResponse<AmbienteResponse>>> CrearAmbiente(
        [FromBody] AmbienteCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearAmbienteCommand(request), cancellationToken);
        return Ok(ApiResponse<AmbienteResponse>.Exito(resultado, $"Ambiente {resultado.Nombre} creado."));
    }

    [HttpPut("ambientes/{id:int}")]
    public async Task<ActionResult<ApiResponse<AmbienteResponse>>> ActualizarAmbiente(
        int id, [FromBody] AmbienteEditarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarAmbienteCommand(id, request), cancellationToken);
        return Ok(ApiResponse<AmbienteResponse>.Exito(resultado, "Ambiente actualizado."));
    }

    [HttpPut("ambientes/{id:int}/retirar")]
    public async Task<ActionResult<ApiResponse<object>>> RetirarAmbiente(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetirarAmbienteCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Ambiente retirado."));
    }
}
