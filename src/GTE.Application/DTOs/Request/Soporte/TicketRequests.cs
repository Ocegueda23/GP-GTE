namespace GTE.Application.DTOs.Request.Soporte;

public class TicketCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdCategoriaTicket { get; set; }
    public int IdPrioridad { get; set; }
}

/// <summary>El frontend manda la ACCION; ASIGNAR ademas exige el agente destino.</summary>
public class CambiarEstatusTicketRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public int? IdAsignado { get; set; }
}

/// <summary>
/// Datos del WorkItem tipo Soporte que se crea al escalar. Titulo, Descripcion y
/// Prioridad se heredan del propio ticket (no se vuelven a capturar).
/// </summary>
public class EscalarTicketRequest
{
    public int IdProyecto { get; set; }
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}

public class EncuestaTicketRequest
{
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
}
