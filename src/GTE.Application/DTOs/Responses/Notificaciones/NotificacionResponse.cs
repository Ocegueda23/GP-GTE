namespace GTE.Application.DTOs.Responses.Notificaciones;

public class NotificacionResponse
{
    public long IdNotificacion { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
    public string? Entidad { get; set; }
    public int? IdEntidad { get; set; }
    public string? Url { get; set; }
    public bool Leida { get; set; }
    public DateTime? FechaLeida { get; set; }
    public DateTime FechaRegistro { get; set; }
}
