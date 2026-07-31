namespace GTE.Application.DTOs.Responses.Calidad;

public class PlanPruebaResponse
{
    public int IdPlanPrueba { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int? IdRelease { get; set; }
    public string? Release { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int TotalCasos { get; set; }
    public int CasosEjecutados { get; set; }
    public int CasosPasa { get; set; }
    public int CasosFalla { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class PasoCasoResponse
{
    public int NumeroPaso { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ResultadoEsperado { get; set; }
}

public class CasoPruebaResponse
{
    public int IdCasoPrueba { get; set; }
    public string? Folio { get; set; }
    public int IdPlanPrueba { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public string TipoPrueba { get; set; } = string.Empty;
    public int? IdWorkItem { get; set; }
    public string? FolioWorkItem { get; set; }
    public IReadOnlyList<PasoCasoResponse> Pasos { get; set; } = [];

    /// <summary>Resultado de la ultima ejecucion en el ciclo consultado.</summary>
    public int? IdEjecucion { get; set; }
    public int? IdUltimoResultado { get; set; }
    public string? UltimoResultado { get; set; }
    public string? FolioBug { get; set; }
}

public class CicloPruebaResponse
{
    public int IdCicloPrueba { get; set; }
    public int IdPlanPrueba { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public int TotalCasos { get; set; }
    public int Ejecutados { get; set; }
    public int Pasa { get; set; }
    public int Falla { get; set; }
    public int Bloqueado { get; set; }
}

/// <summary>Fila de la matriz de trazabilidad requisito - casos - resultado.</summary>
public class TrazabilidadResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int TotalCasos { get; set; }
    public int CasosPasa { get; set; }
    public int CasosFalla { get; set; }
    public bool SinCobertura { get; set; }
}
