namespace GTE.Application.DTOs.Request.Administracion;

public class AmbienteCrearRequest
{
    public int? IdProyecto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Servidor { get; set; }
    public string? BaseDatos { get; set; }
    public int? IdResponsable { get; set; }
}

public class AmbienteEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Servidor { get; set; }
    public string? BaseDatos { get; set; }
    public int? IdResponsable { get; set; }
}
