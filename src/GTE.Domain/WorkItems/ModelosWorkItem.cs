namespace GTE.Domain.WorkItems;

/// <summary>Datos para crear un elemento de trabajo (el folio y el estatus los fija el backend).</summary>
public record WorkItemNuevo(
    string Folio,
    int IdTipoWorkItem,
    int? IdPadre,
    int IdProyecto,
    int? IdSolicitud,
    string Titulo,
    string? Descripcion,
    string? CriteriosAceptacion,
    int IdPrioridad,
    int? IdComplejidad,
    int? IdAsignado,
    int? IdSolicitante,
    decimal? PuntosHistoria,
    int? MinutosPresupuesto,
    DateTime? FechaCompromiso,
    int? IdUsuarioSolicitante = null);

/// <summary>
/// Datos editables de un elemento de trabajo. ActualizarPresupuesto distingue
/// "recalcular a MinutosPresupuesto (incluso null)" de "conservar el congelado" (RN-REQ-08).
/// </summary>
public record WorkItemEdicion(
    int IdWorkItem,
    string Titulo,
    string? Descripcion,
    string? CriteriosAceptacion,
    int IdPrioridad,
    int? IdComplejidad,
    int? IdAsignado,
    decimal? PuntosHistoria,
    bool ActualizarPresupuesto,
    int? MinutosPresupuesto,
    DateTime? FechaCompromiso);

/// <summary>Estado minimo de un item para evaluar reglas de negocio.</summary>
public record EstadoWorkItem(
    int IdWorkItem,
    string Folio,
    int IdEstatus,
    int IdProyecto,
    bool EsMantenimiento,
    int? IdAsignado,
    int? IdHorarioAsignado,
    int? IdComplejidad,
    DateTime? FechaCompromiso,
    bool Activo);

/// <summary>Resumen de proyecto para reglas y folios.</summary>
public record ProyectoResumen(int IdProyecto, string Clave, bool EsMantenimiento, bool Activo);

/// <summary>Resumen de usuario para presupuesto y materializacion de tiempos.</summary>
public record UsuarioResumen(int IdUsuario, int? IdNivel, int? IdHorario, bool Activo);

/// <summary>Hallazgo de revision que bloquea el cierre (RN-REQ-03).</summary>
public record RevisionPendiente(int IdRevision, string Revisor, string? Comentarios);

/// <summary>Resultado de validar el cierre de un item.</summary>
public record ValidacionCierre(bool TieneAvance, IReadOnlyList<RevisionPendiente> RevisionesPendientes);
