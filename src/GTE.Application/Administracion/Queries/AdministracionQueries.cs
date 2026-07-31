using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Queries;

/* ---------- Proyectos ---------- */

public record ObtenerProyectosQuery(bool SoloActivos = true) : IRequest<IReadOnlyList<ProyectoResponse>>;

public class ObtenerProyectosHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerProyectosQuery, IReadOnlyList<ProyectoResponse>>
{
    public async Task<IReadOnlyList<ProyectoResponse>> Handle(ObtenerProyectosQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerProyectosAsync(query.SoloActivos, cancellationToken);
}

public record ObtenerProyectoQuery(int IdProyecto) : IRequest<ProyectoResponse>;

public class ObtenerProyectoHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerProyectoQuery, ProyectoResponse>
{
    public async Task<ProyectoResponse> Handle(ObtenerProyectoQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerProyectoAsync(query.IdProyecto, cancellationToken)
           ?? throw new NotFoundException("Proyecto", query.IdProyecto);
}

public record ObtenerAccionesProyectoQuery(int IdProyecto) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

/// <summary>
/// Acciones de workflow validas para el proyecto en su estatus actual: el grafo dicta
/// las transiciones; la UI solo pinta lo que llega (nunca decide el estatus destino).
/// </summary>
public class ObtenerAccionesProyectoHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IAdministracionRepository repositorio) : IRequestHandler<ObtenerAccionesProyectoQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesProyectoQuery query, CancellationToken cancellationToken)
    {
        _ = await repositorio.ObtenerEstadoProyectoAsync(query.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", query.IdProyecto);

        var acciones = await motor.ObtenerAccionesAsync("Proyecto", query.IdProyecto, cancellationToken);

        var resultado = new List<AccionDisponibleResponse>();
        foreach (var accion in acciones)
        {
            if (accion.ClavePermisoRequerida is not null
                && !await permisos.TienePermisoAsync(accion.ClavePermisoRequerida, null, cancellationToken))
            {
                continue;
            }

            resultado.Add(new AccionDisponibleResponse
            {
                Accion = accion.Accion,
                Etiqueta = accion.EtiquetaBoton,
                RequiereMotivo = accion.RequiereMotivo,
                EsAccionPrincipal = accion.EsAccionPrincipal
            });
        }

        return resultado;
    }
}

/* ---------- Equipos ---------- */

public record ObtenerEquiposQuery : IRequest<IReadOnlyList<EquipoResponse>>;

public class ObtenerEquiposHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerEquiposQuery, IReadOnlyList<EquipoResponse>>
{
    public async Task<IReadOnlyList<EquipoResponse>> Handle(ObtenerEquiposQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerEquiposAsync(cancellationToken);
}

public record ObtenerEquipoQuery(int IdEquipo) : IRequest<EquipoDetalleResponse>;

public class ObtenerEquipoHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerEquipoQuery, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(ObtenerEquipoQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerEquipoAsync(query.IdEquipo, cancellationToken)
           ?? throw new NotFoundException("Equipo", query.IdEquipo);
}

/* ---------- Usuarios ---------- */

public record ObtenerUsuariosQuery(string? Texto, bool SoloActivos = true) : IRequest<IReadOnlyList<UsuarioResponse>>;

public class ObtenerUsuariosHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerUsuariosQuery, IReadOnlyList<UsuarioResponse>>
{
    public async Task<IReadOnlyList<UsuarioResponse>> Handle(ObtenerUsuariosQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerUsuariosAsync(query.Texto, query.SoloActivos, cancellationToken);
}

public record ObtenerUsuarioQuery(int IdUsuario) : IRequest<UsuarioResponse>;

public class ObtenerUsuarioHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerUsuarioQuery, UsuarioResponse>
{
    public async Task<UsuarioResponse> Handle(ObtenerUsuarioQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerUsuarioAsync(query.IdUsuario, cancellationToken)
           ?? throw new NotFoundException("Usuario", query.IdUsuario);
}

/* ---------- Roles ---------- */

public record ObtenerRolesQuery : IRequest<IReadOnlyList<RolResponse>>;

public class ObtenerRolesHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerRolesQuery, IReadOnlyList<RolResponse>>
{
    public async Task<IReadOnlyList<RolResponse>> Handle(ObtenerRolesQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerRolesAsync(cancellationToken);
}

public record ObtenerMatrizPermisosQuery(int IdRol) : IRequest<MatrizPermisosResponse>;

public class ObtenerMatrizPermisosHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerMatrizPermisosQuery, MatrizPermisosResponse>
{
    public async Task<MatrizPermisosResponse> Handle(ObtenerMatrizPermisosQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerMatrizPermisosAsync(query.IdRol, cancellationToken)
           ?? throw new NotFoundException("Rol", query.IdRol);
}

public record ObtenerRolesUsuarioQuery(int IdUsuario) : IRequest<IReadOnlyList<RolUsuarioResponse>>;

public class ObtenerRolesUsuarioHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerRolesUsuarioQuery, IReadOnlyList<RolUsuarioResponse>>
{
    public async Task<IReadOnlyList<RolUsuarioResponse>> Handle(ObtenerRolesUsuarioQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerRolesUsuarioAsync(query.IdUsuario, cancellationToken);
}

/* ---------- Horarios ---------- */

public record ObtenerHorariosQuery : IRequest<IReadOnlyList<HorarioResponse>>;

public class ObtenerHorariosHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerHorariosQuery, IReadOnlyList<HorarioResponse>>
{
    public async Task<IReadOnlyList<HorarioResponse>> Handle(ObtenerHorariosQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerHorariosAsync(cancellationToken);
}

public record ObtenerHorarioQuery(int IdHorario) : IRequest<HorarioDetalleResponse>;

public class ObtenerHorarioHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerHorarioQuery, HorarioDetalleResponse>
{
    public async Task<HorarioDetalleResponse> Handle(ObtenerHorarioQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerHorarioAsync(query.IdHorario, cancellationToken)
           ?? throw new NotFoundException("Horario", query.IdHorario);
}

public record ObtenerFestivosQuery(int? IdHorario) : IRequest<IReadOnlyList<DiaFestivoResponse>>;

public class ObtenerFestivosHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerFestivosQuery, IReadOnlyList<DiaFestivoResponse>>
{
    public async Task<IReadOnlyList<DiaFestivoResponse>> Handle(ObtenerFestivosQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerFestivosAsync(query.IdHorario, cancellationToken);
}

/* ---------- Ambientes ---------- */

public record ObtenerAmbientesQuery(int? IdProyecto) : IRequest<IReadOnlyList<AmbienteResponse>>;

public class ObtenerAmbientesHandler(IAdministracionQueryService consultas)
    : IRequestHandler<ObtenerAmbientesQuery, IReadOnlyList<AmbienteResponse>>
{
    public async Task<IReadOnlyList<AmbienteResponse>> Handle(ObtenerAmbientesQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerAmbientesAsync(query.IdProyecto, cancellationToken);
}
