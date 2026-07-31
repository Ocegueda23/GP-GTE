namespace GTE.Application.DTOs.Request.Calidad;

public class PlanPruebaCrearRequest
{
    public int IdProyecto { get; set; }
    public int? IdRelease { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class PasoCasoRequest
{
    public int NumeroPaso { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ResultadoEsperado { get; set; }
}

public class CasoPruebaCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public int IdTipoPrueba { get; set; } = 1;
    public int? IdWorkItem { get; set; }
    public List<PasoCasoRequest> Pasos { get; set; } = [];
}

public class CicloPruebaCrearRequest
{
    public string Nombre { get; set; } = string.Empty;
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
}

public class EjecucionRegistrarRequest
{
    public int IdCasoPrueba { get; set; }
    public int IdResultadoPrueba { get; set; }
    public string? Observaciones { get; set; }
}

/// <summary>Bug precargado desde una ejecucion fallida; si no se manda titulo se arma del caso.</summary>
public class BugDesdeEjecucionRequest
{
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public int IdPrioridad { get; set; } = 2;
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}
