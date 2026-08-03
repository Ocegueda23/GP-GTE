namespace GTE.Application.DTOs.Responses.Okr;

public class ResultadoClaveResponse
{
    public int IdResultadoClave { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ValorMeta { get; set; }
    public decimal ValorActual { get; set; }
    public string? ClaveKpi { get; set; }
}

public class ObjetivoOkrResponse
{
    public int IdObjetivoOkr { get; set; }
    public int? IdProyecto { get; set; }
    public string? Proyecto { get; set; }
    public int? IdEquipo { get; set; }
    public string? Equipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Anio { get; set; }
    public byte Trimestre { get; set; }
    public IReadOnlyList<ResultadoClaveResponse> ResultadosClave { get; set; } = [];
}
