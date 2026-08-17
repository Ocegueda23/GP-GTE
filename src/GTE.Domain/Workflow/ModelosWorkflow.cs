namespace GTE.Domain.Workflow;

public record ProcesoResumen(int IdProceso, string Proceso);

public record EstatusWorkflowItem(int Id, string Descripcion);

/// <summary>
/// Una flecha del grafo (dbo.tblTransicion) con sus metadatos de UI (dbo.tblTransicionConfig,
/// unida en memoria por Proceso+IdEstatusOrigen+Accion -- no hay FK real entre ambas).
/// Si la transicion no tiene fila de config todavia, los metadatos llegan con default
/// (etiqueta = la accion tal cual, sin permiso, sin motivo, no principal, orden 0).
/// </summary>
public record TransicionWorkflow(
    int IdEstatusOrigen, string EstatusOrigen, string Accion, int IdEstatusDestino, string EstatusDestino,
    string EtiquetaBoton, string? RequierePermiso, bool RequiereMotivo, bool EsAccionPrincipal, int Orden);

public record DefinicionWorkflow(
    string Proceso, IReadOnlyList<EstatusWorkflowItem> Estatus, IReadOnlyList<TransicionWorkflow> Transiciones);

/// <summary>Metadatos de UI editables de una transicion YA existente en el grafo (nunca crea/borra flechas).</summary>
public record TransicionConfigEdicion(
    int IdEstatusOrigen, string Accion, string EtiquetaBoton, string? RequierePermiso,
    bool RequiereMotivo, bool EsAccionPrincipal, int Orden);

public record PermisoResumen(string Clave, string Modulo, string? Descripcion);
