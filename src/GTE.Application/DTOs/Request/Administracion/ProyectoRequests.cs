namespace GTE.Application.DTOs.Request.Administracion;

public class ProyectoCrearRequest
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int? IdPrograma { get; set; }
    public int IdCategoriaProyecto { get; set; }
    public int? IdResponsable { get; set; }
    public int? IdEquipo { get; set; }
    public DateTime? FechaInicioPlan { get; set; }
    public DateTime? FechaFinPlan { get; set; }
    public bool EsMantenimiento { get; set; }
}

public class ProyectoEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int IdCategoriaProyecto { get; set; }
    public int? IdResponsable { get; set; }
    public int? IdEquipo { get; set; }
    public DateTime? FechaInicioPlan { get; set; }
    public DateTime? FechaFinPlan { get; set; }
    public bool EsMantenimiento { get; set; }
}

public class CambiarEstatusProyectoRequest
{
    public string Accion { get; set; } = string.Empty;
}
