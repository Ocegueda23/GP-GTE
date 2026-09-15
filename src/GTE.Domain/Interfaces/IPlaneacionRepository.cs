using GTE.Domain.Planeacion;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA de Planeacion: sprints, backlog y tableros.</summary>
public interface IPlaneacionRepository
{
    Task<int> CrearSprintAsync(SprintNuevo datos, string folio, CancellationToken cancellationToken = default);

    Task EditarSprintAsync(int idSprint, SprintEdicion datos, CancellationToken cancellationToken = default);

    Task<EstadoSprint?> ObtenerEstadoSprintAsync(int idSprint, CancellationToken cancellationToken = default);

    Task AsignarLiderSprintAsync(int idSprint, int? idLider, CancellationToken cancellationToken = default);

    /// <summary>Sprint Activo del lider (solo puede haber uno).</summary>
    Task<int?> ObtenerSprintActivoPorLiderAsync(int idLider, int idExcluido, CancellationToken cancellationToken = default);

    /// <summary>Siguiente sprint Planeado del lider por fecha de inicio.</summary>
    Task<int?> ObtenerSiguienteSprintPlaneadoPorLiderAsync(int idLider, int idSprintActual, CancellationToken cancellationToken = default);

    Task AplicarEfectosTransicionSprintAsync(int idSprint, string accion, CancellationToken cancellationToken = default);

    /// <summary>Mueve los elementos abiertos del sprint al backlog o al sprint destino. Devuelve cuantos movio.</summary>
    Task<int> MoverItemsAbiertosAsync(int idSprint, int? idSprintDestino, CancellationToken cancellationToken = default);

    Task AsignarSprintAsync(int idWorkItem, int? idSprint, CancellationToken cancellationToken = default);

    /// <summary>Persiste el orden del backlog de una lista de elementos (drag and drop).</summary>
    Task ReordenarBacklogAsync(IReadOnlyList<int> idsEnOrden, CancellationToken cancellationToken = default);

    /// <summary>Miembros de todos los equipos que encabeza el lider (TblEquipo.IdLider), sin duplicar.</summary>
    Task<IReadOnlyList<MiembroEquipo>> ObtenerMiembrosPorLiderAsync(int idLider, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AusenciaAprobada>> ObtenerAusenciasAprobadasPorLiderAsync(
        int idLider, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);

    /// <summary>Columnas del tablero del equipo; las crea con el mapeo estandar si no existe.</summary>
    Task<IReadOnlyList<ColumnaTablero>> ObtenerOCrearColumnasAsync(int idEquipo, CancellationToken cancellationToken = default);

    /// <summary>Equipo responsable del proyecto (null si el proyecto no tiene equipo asignado).</summary>
    Task<int?> ObtenerEquipoDeProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Elementos abiertos del equipo en un estatus, para evaluar el limite WIP.</summary>
    Task<int> ContarItemsEnEstatusAsync(int idEquipo, int idEstatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deja rastro de que alguien excedio el limite WIP con permiso (RN-GTE-020):
    /// el limite se puede saltar, pero no en silencio.
    /// </summary>
    Task RegistrarSaltoWipAsync(
        int idWorkItem, string columna, int limite, int enColumna, CancellationToken cancellationToken = default);
}
