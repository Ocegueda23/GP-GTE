namespace GTE.Application.DTOs.Request.WorkItems;

public class WorkItemCrearRequest
{
    public int IdProyecto { get; set; }
    public int IdTipoWorkItem { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CriteriosAceptacion { get; set; }
    public int IdPrioridad { get; set; }
    public int? IdComplejidad { get; set; }
    public int? IdAsignado { get; set; }
    public int? IdSolicitante { get; set; }
    public int? IdPadre { get; set; }
    public int? IdSolicitud { get; set; }
    public DateTime? FechaCompromiso { get; set; }
    public decimal? PuntosHistoria { get; set; }
}
