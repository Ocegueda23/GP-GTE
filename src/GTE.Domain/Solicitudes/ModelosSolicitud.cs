namespace GTE.Domain.Solicitudes;

/// <summary>Datos para crear una solicitud (folio y estatus los fija el backend).</summary>
public record SolicitudNueva(
    string Folio,
    int IdSolicitante,
    string Titulo,
    string? Descripcion,
    int IdTipoSolicitud,
    int IdPrioridad,
    DateTime? FechaDeseada,
    string? JustificacionNegocio,
    int? IdUsuarioSolicitante = null);

/// <summary>Estado minimo de una solicitud para evaluar reglas.</summary>
public record EstadoSolicitud(
    int IdSolicitud,
    string? Folio,
    int IdEstatus,
    int? IdProyecto,
    int IdSolicitante,
    string Titulo,
    bool Activo,
    int? IdUsuarioSolicitante = null);
