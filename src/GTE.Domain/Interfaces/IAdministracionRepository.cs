using GTE.Domain.Administracion;

namespace GTE.Domain.Interfaces;

public interface IAdministracionRepository
{
    /* ---------- Proyectos ---------- */
    Task<int> CrearProyectoAsync(ProyectoNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarProyectoAsync(ProyectoEdicion datos, CancellationToken cancellationToken = default);
    Task<EstadoProyecto?> ObtenerEstadoProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);
    Task AsignarFolioProyectoAsync(int idProyecto, string folio, CancellationToken cancellationToken = default);
    Task AplicarEfectosTransicionProyectoAsync(int idProyecto, string accion, CancellationToken cancellationToken = default);

    /// <summary>RN-PRY-01: folios de WorkItems activos y sin terminar del proyecto (para el 409 de CERRAR).</summary>
    Task<IReadOnlyList<string>> ObtenerFoliosWorkItemsAbiertosAsync(int idProyecto, CancellationToken cancellationToken = default);

    /* ---------- Equipos ---------- */
    Task<int> CrearEquipoAsync(EquipoNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarEquipoAsync(EquipoEdicion datos, CancellationToken cancellationToken = default);
    Task<int> AgregarMiembroAsync(MiembroEquipoNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarMiembroAsync(MiembroEquipoEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarMiembroAsync(int idEquipoMiembro, CancellationToken cancellationToken = default);

    /* ---------- Usuarios ---------- */
    Task<int> CrearUsuarioAsync(UsuarioNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarUsuarioAsync(UsuarioEdicion datos, CancellationToken cancellationToken = default);
    Task DarBajaUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// RN-ADM-01: valida con CTE recursivo si asignar idJefePropuesto como jefe de
    /// idUsuario formaria un ciclo en la jerarquia. No valida el caso "es su propio jefe"
    /// (eso se revisa antes, comparando los dos ids directamente).
    /// </summary>
    Task<bool> FormariaCicloJerarquiaAsync(int idUsuario, int idJefePropuesto, CancellationToken cancellationToken = default);

    /* ---------- Roles ---------- */
    Task<int> AsignarRolAsync(RolAsignadoNuevo datos, CancellationToken cancellationToken = default);
    Task RetirarRolAsync(int idUsuarioRol, CancellationToken cancellationToken = default);

    /// <summary>Reemplazo completo de dbo.tblRolPermiso para un rol, en una sola transaccion.</summary>
    Task GuardarMatrizPermisosAsync(int idRol, IReadOnlyList<int> idsPermiso, CancellationToken cancellationToken = default);

    /* ---------- Horarios ---------- */
    Task<int> CrearHorarioAsync(HorarioNuevo datos, CancellationToken cancellationToken = default);

    /// <summary>Reemplazo completo de los tramos de un horario, en una sola transaccion.</summary>
    Task GuardarTramosHorarioAsync(int idHorario, IReadOnlyList<TramoHorario> tramos, CancellationToken cancellationToken = default);

    Task<int> CrearFestivoAsync(DiaFestivoNuevo datos, CancellationToken cancellationToken = default);
    Task RetirarFestivoAsync(int idDiaFestivo, CancellationToken cancellationToken = default);

    /* ---------- Ambientes ---------- */
    Task<int> CrearAmbienteAsync(AmbienteNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarAmbienteAsync(AmbienteEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarAmbienteAsync(int idAmbiente, CancellationToken cancellationToken = default);
}
