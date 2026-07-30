namespace GTE.Application.DTOs.Responses.Revisiones;

public class RevisionResponse
{
    public int IdRevision { get; set; }
    public int IdWorkItem { get; set; }
    public string FolioWorkItem { get; set; } = string.Empty;
    public string Revisor { get; set; } = string.Empty;
    public string? Comentarios { get; set; }
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public bool Corregido { get; set; }
    public DateTime? FechaCorreccion { get; set; }
    public DateTime FechaRegistro { get; set; }
}
