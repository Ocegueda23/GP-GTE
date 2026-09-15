using GTE.Application.DTOs.Responses.Administracion;

namespace GTE.Application.Interfaces;

public interface IAdministracionQueryService
{
    /* ---------- Proyectos ---------- */
    Task<IReadOnlyList<ProyectoResponse>> ObtenerProyectosAsync(bool soloActivos, CancellationToken cancellationToken = default);
    Task<ProyectoResponse?> ObtenerProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    /* ---------- Equipos ---------- */
    Task<IReadOnlyList<EquipoResponse>> ObtenerEquiposAsync(CancellationToken cancellationToken = default);
    Task<EquipoDetalleResponse?> ObtenerEquipoAsync(int idEquipo, CancellationToken cancellationToken = default);

    /* ---------- Usuarios ---------- */
    Task<IReadOnlyList<UsuarioResponse>> ObtenerUsuariosAsync(string? texto, bool soloActivos, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ObtenerUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

    /* ---------- Roles ---------- */
    Task<IReadOnlyList<RolResponse>> ObtenerRolesAsync(CancellationToken cancellationToken = default);
    Task<MatrizPermisosResponse?> ObtenerMatrizPermisosAsync(int idRol, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolUsuarioResponse>> ObtenerRolesUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

    /// <summary>Accesos vigentes de un proyecto: las asignaciones de tblUsuarioRol acotadas a el.</summary>
    Task<IReadOnlyList<AccesoProyectoResponse>> ObtenerAccesosProyectoAsync(
        int idProyecto, CancellationToken cancellationToken = default);

    /* ---------- Horarios ---------- */
    Task<IReadOnlyList<HorarioResponse>> ObtenerHorariosAsync(CancellationToken cancellationToken = default);
    Task<HorarioDetalleResponse?> ObtenerHorarioAsync(int idHorario, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiaFestivoResponse>> ObtenerFestivosAsync(int? idHorario, CancellationToken cancellationToken = default);
    Task<DiaFestivoResponse?> ObtenerFestivoAsync(int idDiaFestivo, CancellationToken cancellationToken = default);

    /* ---------- Ambientes ---------- */
    Task<IReadOnlyList<AmbienteResponse>> ObtenerAmbientesAsync(int? idProyecto, CancellationToken cancellationToken = default);
    Task<AmbienteResponse?> ObtenerAmbienteAsync(int idAmbiente, CancellationToken cancellationToken = default);

    /* ---------- Areas ---------- */
    Task<IReadOnlyList<AreaResponse>> ObtenerAreasAsync(CancellationToken cancellationToken = default);
    Task<AreaResponse?> ObtenerAreaAsync(int idArea, CancellationToken cancellationToken = default);

    /* ---------- Puestos ---------- */
    Task<IReadOnlyList<PuestoResponse>> ObtenerPuestosAsync(CancellationToken cancellationToken = default);
    Task<PuestoResponse?> ObtenerPuestoAsync(int idPuesto, CancellationToken cancellationToken = default);
}
