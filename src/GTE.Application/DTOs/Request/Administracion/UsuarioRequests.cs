namespace GTE.Application.DTOs.Request.Administracion;

public class UsuarioCrearRequest
{
    public string Dominio { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public int? IdPuesto { get; set; }
    public int? IdNivel { get; set; }
    public int? IdHorario { get; set; }
    public int? IdJefe { get; set; }
}

public class UsuarioEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public int? IdPuesto { get; set; }
    public int? IdNivel { get; set; }
    public int? IdHorario { get; set; }
    public int? IdJefe { get; set; }
}
