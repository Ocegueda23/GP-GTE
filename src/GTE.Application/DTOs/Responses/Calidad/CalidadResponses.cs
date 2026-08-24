namespace GTE.Application.DTOs.Responses.Calidad;

public class PasoCasoResponse
{
    public int NumeroPaso { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ResultadoEsperado { get; set; }
}

/// <summary>Caso del catalogo reutilizable de un proyecto.</summary>
public class CasoPruebaResponse
{
    public int IdCasoPrueba { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public string TipoPrueba { get; set; } = string.Empty;
    public IReadOnlyList<PasoCasoResponse> Pasos { get; set; } = [];
}

/// <summary>Caso asignado a un WorkItem, con el resultado de su ultima ejecucion contra el.</summary>
public class CasoAsignadoResponse
{
    public int IdWorkItemCasoPrueba { get; set; }
    public int IdCasoPrueba { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TipoPrueba { get; set; } = string.Empty;
    public IReadOnlyList<PasoCasoResponse> Pasos { get; set; } = [];

    public int? IdEjecucion { get; set; }
    public int? IdUltimoResultado { get; set; }
    public string? UltimoResultado { get; set; }
    public DateTime? FechaUltimaEjecucion { get; set; }
}

/// <summary>Si el resultado fue Falla, IdRevision trae el hallazgo creado en automatico.</summary>
public class EjecucionRegistradaResponse
{
    public int IdEjecucionPrueba { get; set; }
    public int? IdRevision { get; set; }
}

/// <summary>Catalogo completo de casos de un proyecto (incluye retirados y no reutilizables),
/// para la pantalla de administracion del catalogo.</summary>
public class CasoAdminResponse
{
    public int IdCasoPrueba { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public int IdTipoPrueba { get; set; }
    public string TipoPrueba { get; set; } = string.Empty;
    public bool Reutilizable { get; set; }
    public bool Activo { get; set; }
    public int TotalAsignaciones { get; set; }
    public IReadOnlyList<PasoCasoResponse> Pasos { get; set; } = [];
}
