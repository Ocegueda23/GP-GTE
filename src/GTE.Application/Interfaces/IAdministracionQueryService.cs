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

    /* ---------- Horarios ---------- */
    Task<IReadOnlyList<HorarioResponse>> ObtenerHorariosAsync(CancellationToken cancellationToken = default);
    Task<HorarioDetalleResponse?> ObtenerHorarioAsync(int idHorario, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiaFestivoResponse>> ObtenerFestivosAsync(int? idHorario, CancellationToken cancellationToken = default);
    Task<DiaFestivoResponse?> ObtenerFestivoAsync(int idDiaFestivo, CancellationToken cancellationToken = default);

    /* ---------- Ambientes ---------- */
    Task<IReadOnlyList<AmbienteResponse>> ObtenerAmbientesAsync(int? idProyecto, CancellationToken cancellationToken = default);
    Task<AmbienteResponse?> ObtenerAmbienteAsync(int idAmbiente, CancellationToken cancellationToken = default);
}
