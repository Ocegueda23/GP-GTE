namespace GTE.Application.DTOs.Responses.Administracion;

public class ProyectoResponse
{
    public int IdProyecto { get; set; }
    public string? Folio { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int? IdPrograma { get; set; }
    public string? Programa { get; set; }
    public int IdCategoriaProyecto { get; set; }
    public string CategoriaProyecto { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int? IdResponsable { get; set; }
    public string? Responsable { get; set; }
    public int? IdEquipo { get; set; }
    public string? Equipo { get; set; }
    public DateTime? FechaInicioPlan { get; set; }
    public DateTime? FechaFinPlan { get; set; }
    public DateTime? FechaInicioReal { get; set; }
    public DateTime? FechaFinReal { get; set; }
    public bool EsMantenimiento { get; set; }
}
