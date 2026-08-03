namespace GTE.Application.DTOs.Responses.Soporte;

public class TicketResponse
{
    public int IdTicket { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int IdSolicitante { get; set; }
    public string Solicitante { get; set; } = string.Empty;
    public string? Asignado { get; set; }
    public string? Sla { get; set; }
    public DateTime? FechaLimiteRespuesta { get; set; }
    public DateTime? FechaLimiteResolucion { get; set; }
    public DateTime? FechaPrimeraRespuesta { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? Solucion { get; set; }
    public int? MinutosSolucion { get; set; }
    public string? UsuarioSolicitante { get; set; }
    public string? Locacion { get; set; }
    public int? IdWorkItemDerivado { get; set; }
    public string? FolioWorkItemDerivado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? Calificacion { get; set; }
    public string? ComentarioEncuesta { get; set; }
}

/// <summary>Eco del WorkItem creado al escalar.</summary>
public class EscalarTicketResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
}
