namespace GTE.Application.DTOs.Request.WorkItems;

public class WorkItemActualizarRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CriteriosAceptacion { get; set; }
    public int IdPrioridad { get; set; }
    public int? IdComplejidad { get; set; }
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
    public decimal? PuntosHistoria { get; set; }
}
