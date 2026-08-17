using GTE.Domain.WorkItems;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo WorkItems (la lectura vive en IWorkItemQueryService).</summary>
public interface IWorkItemRepository
{
    Task<int> CrearAsync(WorkItemNuevo datos, CancellationToken cancellationToken = default);

    Task ActualizarAsync(WorkItemEdicion datos, CancellationToken cancellationToken = default);

    Task<EstadoWorkItem?> ObtenerEstadoAsync(int idWorkItem, CancellationToken cancellationToken = default);

    Task<ProyectoResumen?> ObtenerProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    Task<UsuarioResumen?> ObtenerUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

    /// <summary>RN-REQ-08: minutos y puntos de presupuesto segun matriz complejidad x nivel (null si no hay fila).</summary>
    Task<PresupuestoMatriz?> ObtenerPresupuestoMatrizAsync(int idComplejidad, int idNivel, CancellationToken cancellationToken = default);

    /// <summary>Complejidad activa de menor Orden, para flujos de creacion sin captura humana
    /// de complejidad (null solo si el catalogo esta vacio).</summary>
    Task<int?> ObtenerComplejidadPorDefectoAsync(CancellationToken cancellationToken = default);

    /// <summary>RN-REQ-01: el otro item En Proceso del asignado (null si no hay).</summary>
    Task<int?> ObtenerItemEnProcesoDeAsignadoAsync(int idAsignado, int idExcluido, CancellationToken cancellationToken = default);

    /// <summary>RN-REQ-03: avance registrado y revisiones que bloquean el cierre.</summary>
    Task<ValidacionCierre> ObtenerValidacionCierreAsync(int idWorkItem, CancellationToken cancellationToken = default);

    /// <summary>Efectos posteriores a una transicion exitosa (fechas, auditoria de movimiento, bitacora).</summary>
    Task AplicarEfectosTransicionAsync(int idWorkItem, string accion, CancellationToken cancellationToken = default);

    Task<int> RegistrarTiempoAsync(
        int idWorkItem,
        int idUsuario,
        DateOnly fecha,
        int minutos,
        string? descripcion,
        CancellationToken cancellationToken = default);
}
