namespace GTE.Application.DTOs.Responses.Administracion;

public class UsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Dominio { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public int? IdPuesto { get; set; }
    public string? Puesto { get; set; }
    public int? IdNivel { get; set; }
    public string? Nivel { get; set; }
    public int? IdHorario { get; set; }
    public string? Horario { get; set; }
    public int? IdJefe { get; set; }
    public string? Jefe { get; set; }
    public bool EsExterno { get; set; }
    public DateTime? FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }
    public bool Activo { get; set; }
}
