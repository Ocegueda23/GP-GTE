using GTE.Domain.Planeacion;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA de Planeacion: sprints, backlog y tableros.</summary>
public interface IPlaneacionRepository
{
    Task<int> CrearSprintAsync(SprintNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoSprint?> ObtenerEstadoSprintAsync(int idSprint, CancellationToken cancellationToken = default);

    /// <summary>Sprint Activo del equipo (solo puede haber uno).</summary>
    Task<int?> ObtenerSprintActivoAsync(int idEquipo, int idExcluido, CancellationToken cancellationToken = default);

    /// <summary>Siguiente sprint Planeado del equipo por fecha de inicio.</summary>
    Task<int?> ObtenerSiguienteSprintPlaneadoAsync(int idEquipo, int idSprintActual, CancellationToken cancellationToken = default);

    Task AplicarEfectosTransicionSprintAsync(int idSprint, string accion, CancellationToken cancellationToken = default);

    /// <summary>Mueve los elementos abiertos del sprint al backlog o al sprint destino. Devuelve cuantos movio.</summary>
    Task<int> MoverItemsAbiertosAsync(int idSprint, int? idSprintDestino, CancellationToken cancellationToken = default);

    Task AsignarSprintAsync(int idWorkItem, int? idSprint, CancellationToken cancellationToken = default);

    /// <summary>Persiste el orden del backlog de una lista de elementos (drag and drop).</summary>
    Task ReordenarBacklogAsync(IReadOnlyList<int> idsEnOrden, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MiembroEquipo>> ObtenerMiembrosEquipoAsync(int idEquipo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AusenciaAprobada>> ObtenerAusenciasAprobadasAsync(
        int idEquipo, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);

    /// <summary>Columnas del tablero del equipo; las crea con el mapeo estandar si no existe.</summary>
    Task<IReadOnlyList<ColumnaTablero>> ObtenerOCrearColumnasAsync(int idEquipo, CancellationToken cancellationToken = default);

    /// <summary>Equipo responsable del proyecto (null si el proyecto no tiene equipo asignado).</summary>
    Task<int?> ObtenerEquipoDeProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Elementos abiertos del equipo en un estatus, para evaluar el limite WIP.</summary>
    Task<int> ContarItemsEnEstatusAsync(int idEquipo, int idEstatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deja rastro de que alguien excedio el limite WIP con permiso (RN-PLA-04):
    /// el limite se puede saltar, pero no en silencio.
    /// </summary>
    Task RegistrarSaltoWipAsync(
        int idWorkItem, string columna, int limite, int enColumna, CancellationToken cancellationToken = default);
}
