namespace GTE.Application.DTOs.Responses.Administracion;

public class PuestoResponse
{
    public int IdPuesto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? IdArea { get; set; }
    public string? Area { get; set; }
}
