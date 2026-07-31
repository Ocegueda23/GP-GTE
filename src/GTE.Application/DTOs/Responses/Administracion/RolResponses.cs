namespace GTE.Application.DTOs.Responses.Administracion;

public class RolResponse
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public int TotalPermisos { get; set; }
}

public class PermisoMatrizItemResponse
{
    public int IdPermiso { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Asignado { get; set; }
}

public class MatrizPermisosResponse
{
    public int IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
    public List<PermisoMatrizItemResponse> Permisos { get; set; } = [];
}

public class RolUsuarioResponse
{
    public int IdUsuarioRol { get; set; }
    public int IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
    public int? IdProyecto { get; set; }
    public string? Proyecto { get; set; }
    public int? IdEquipo { get; set; }
    public string? Equipo { get; set; }
}
