namespace GTE.Application.DTOs.Request.Okr;

public class ObjetivoOkrCrearRequest
{
    public int? IdProyecto { get; set; }
    public int? IdEquipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Anio { get; set; }
    public byte Trimestre { get; set; }
}

public class ObjetivoOkrEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class ResultadoClaveCrearRequest
{
    public string Nombre { get; set; } = string.Empty;
    public decimal ValorMeta { get; set; }
    public string? ClaveKpi { get; set; }
}

public class ResultadoClaveEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public decimal ValorMeta { get; set; }
    public decimal ValorActual { get; set; }
    public string? ClaveKpi { get; set; }
}
