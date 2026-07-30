namespace GTE.Application.DTOs.Responses.WorkItems;

/// <summary>Fila de la bandeja de trabajo (proyeccion de vwBandejaTrabajo).</summary>
public class BandejaItemResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string ClaveProyecto { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string? Asignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
    public bool EsVencida { get; set; }
    public decimal? PuntosHistoria { get; set; }
    public int? MinutosPresupuesto { get; set; }
    public int? MinutosInvertidos { get; set; }
    public int RevisionesPendientes { get; set; }
}
