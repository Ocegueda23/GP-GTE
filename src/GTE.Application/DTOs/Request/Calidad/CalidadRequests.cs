namespace GTE.Application.DTOs.Request.Calidad;

public class PasoCasoRequest
{
    public int NumeroPaso { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ResultadoEsperado { get; set; }
}

/// <summary>Crea un caso y lo asigna al WorkItem en el mismo paso.</summary>
public class CasoPruebaCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public int IdTipoPrueba { get; set; } = 1;

    /// <summary>false = caso libre, solo para esta asignacion; no aparece en el catalogo reutilizable.</summary>
    public bool Reutilizable { get; set; } = true;
    public List<PasoCasoRequest> Pasos { get; set; } = [];
}

public class CasoPruebaEditarRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Precondiciones { get; set; }
    public string? ResultadoEsperado { get; set; }
    public int IdTipoPrueba { get; set; } = 1;
    public List<PasoCasoRequest> Pasos { get; set; } = [];
}

public class AsignarCasoRequest
{
    public int IdCasoPrueba { get; set; }
}

public class EjecucionRegistrarRequest
{
    public int IdCasoPrueba { get; set; }
    public int IdResultadoPrueba { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>Obligatoria cuando IdResultadoPrueba es Falla: decide si el hallazgo bloquea.</summary>
    public int? IdSeveridad { get; set; }
}
