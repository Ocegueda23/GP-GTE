using GTE.Domain.Workflow;

namespace GTE.Application.Interfaces;

public interface IWorkflowQueryService
{
    Task<IReadOnlyList<ProcesoResumen>> ObtenerProcesosAsync(CancellationToken cancellationToken = default);

    /// <summary>Null si el proceso no existe en dbo.tblProceso.</summary>
    Task<DefinicionWorkflow?> ObtenerDefinicionAsync(string proceso, CancellationToken cancellationToken = default);

    /// <summary>Catalogo completo de permisos activos, para el selector "permiso requerido" del editor.</summary>
    Task<IReadOnlyList<PermisoResumen>> ObtenerPermisosAsync(CancellationToken cancellationToken = default);
}
