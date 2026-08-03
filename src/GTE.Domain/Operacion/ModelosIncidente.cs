namespace GTE.Domain.Operacion;

/// <summary>Datos para crear un incidente (folio y estatus los fija el backend).</summary>
public record IncidenteNuevo(
    string Folio,
    int IdProyecto,
    int IdSeveridad,
    string Titulo,
    string? Descripcion,
    DateTime FechaOcurrencia,
    DateTime? FechaDeteccion);

/// <summary>Datos editables de un incidente fuera del flujo de estatus.</summary>
public record IncidenteActualizacion(
    string Titulo,
    string? Descripcion,
    string? CausaRaiz,
    int? MinutosIndisponibilidad,
    DateTime? FechaDeteccion);

/// <summary>Estado minimo de un incidente para evaluar reglas.</summary>
public record EstadoIncidente(
    int IdIncidente,
    string? Folio,
    int IdProyecto,
    int IdEstatus,
    int IdSeveridad,
    string Titulo,
    string? Descripcion,
    string? CausaRaiz,
    int? IdWorkItemCorrectivo,
    int? IdReleaseCausante,
    bool Activo);
