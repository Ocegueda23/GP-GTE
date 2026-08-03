namespace GTE.Application.DTOs.Responses.WorkItems;

/// <summary>Detalle de un elemento de trabajo con nombres resueltos y flags calculados.</summary>
public class WorkItemResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CriteriosAceptacion { get; set; }
    public int IdProyecto { get; set; }
    public string ClaveProyecto { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public bool EsMantenimiento { get; set; }
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int IdPrioridad { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public int? IdComplejidad { get; set; }
    public int? IdAsignado { get; set; }
    public string? Asignado { get; set; }
    public string? Solicitante { get; set; }
    public int? IdSprint { get; set; }
    public string? Sprint { get; set; }
    public decimal? PuntosHistoria { get; set; }
    public int? MinutosPresupuesto { get; set; }
    public int? MinutosInvertidos { get; set; }
    public DateTime? FechaCompromiso { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool EsVencida { get; set; }
    public int RevisionesPendientes { get; set; }
}
