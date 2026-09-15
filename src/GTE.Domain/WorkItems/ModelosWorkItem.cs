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
    int? IdUsuarioSolicitante = null,
    int? IdSprint = null);

/// <summary>
/// Datos editables de un elemento de trabajo. ActualizarPresupuesto distingue
/// "recalcular a MinutosPresupuesto (incluso null)" de "conservar el congelado" (RN-GTE-015).
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
    DateTime? FechaCompromiso,
    int? IdSprint);

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
    bool Activo,
    bool Administrado,
    int IdCategoriaProyecto,
    int? IdSprint);

/// <summary>Resumen de proyecto para reglas y folios.</summary>
public record ProyectoResumen(
    int IdProyecto, string Clave, bool EsMantenimiento, bool Activo, int IdEstatusProyecto,
    bool Administrado);

/// <summary>Resumen de usuario para presupuesto y materializacion de tiempos.</summary>
public record UsuarioResumen(int IdUsuario, int? IdNivel, int? IdHorario, bool Activo);

/// <summary>RN-GTE-015: fila de tblMatrizPresupuesto (complejidad x nivel) -- minutos y puntos
/// de historia se congelan juntos al asignar/reasignar o cambiar complejidad.</summary>
public record PresupuestoMatriz(int Minutos, decimal? Puntos);

/// <summary>Hallazgo de revision que bloquea el cierre (RN-GTE-010).</summary>
public record RevisionPendiente(int IdRevision, string Revisor, string? Comentarios);

/// <summary>Resultado de validar el cierre de un item.</summary>
public record ValidacionCierre(bool TieneAvance, IReadOnlyList<RevisionPendiente> RevisionesPendientes);
