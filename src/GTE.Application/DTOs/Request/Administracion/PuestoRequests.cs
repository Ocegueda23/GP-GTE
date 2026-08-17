namespace GTE.Application.DTOs.Request.Administracion;

public class PuestoCrearRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int? IdArea { get; set; }
}

public class PuestoEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int? IdArea { get; set; }
}
