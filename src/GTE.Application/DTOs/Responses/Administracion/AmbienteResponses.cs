namespace GTE.Application.DTOs.Responses.Administracion;

public class AmbienteResponse
{
    public int IdAmbiente { get; set; }
    public int? IdProyecto { get; set; }
    public string? Proyecto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Servidor { get; set; }
    public string? BaseDatos { get; set; }
    public int? IdResponsable { get; set; }
    public string? Responsable { get; set; }
}
