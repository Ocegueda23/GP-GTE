namespace GTE.Application.DTOs.Responses.Operacion;

public class IncidenteResponse
{
    public int IdIncidente { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int IdSeveridad { get; set; }
    public string Severidad { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTime FechaOcurrencia { get; set; }
    public DateTime? FechaDeteccion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public int? MinutosIndisponibilidad { get; set; }
    public string? CausaRaiz { get; set; }
    public int? IdWorkItemCorrectivo { get; set; }
    public string? FolioWorkItemCorrectivo { get; set; }
    public int? IdReleaseCausante { get; set; }
    public string? VersionReleaseCausante { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class VincularCorrectivoResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
}
