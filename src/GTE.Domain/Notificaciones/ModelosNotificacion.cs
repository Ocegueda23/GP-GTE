namespace GTE.Domain.Notificaciones;

/// <summary>Notificacion nueva para un usuario. El canal InApp es el unico implementado hoy.</summary>
public record NotificacionNueva(
    int IdUsuario,
    string Titulo,
    string? Mensaje,
    string? Entidad,
    int? IdEntidad,
    string? Url);
