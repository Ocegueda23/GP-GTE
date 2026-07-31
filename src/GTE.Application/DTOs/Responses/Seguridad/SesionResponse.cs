namespace GTE.Application.DTOs.Responses.Seguridad;

/// <summary>Identidad y capacidades del usuario autenticado (alimenta la UI).</summary>
public class SesionResponse
{
    public int IdUsuario { get; set; }
    public string Dominio { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Puesto { get; set; }
    public string? Nivel { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>Claves de permiso; la UI las usa para ocultar opciones, nunca como control real.</summary>
    public IReadOnlyList<string> Permisos { get; set; } = [];

    /// <summary>Equipos a los que pertenece, para acotar tableros y sprints.</summary>
    public IReadOnlyList<int> Equipos { get; set; } = [];

    /// <summary>true cuando el usuario existe pero nadie le ha asignado roles todavia.</summary>
    public bool SinRoles => Roles.Count == 0;
}
