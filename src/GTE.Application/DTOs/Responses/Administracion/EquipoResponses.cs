namespace GTE.Application.DTOs.Responses.Administracion;

public class EquipoResponse
{
    public int IdEquipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdLider { get; set; }
    public string? Lider { get; set; }
    public int TotalMiembros { get; set; }
}

public class MiembroEquipoResponse
{
    public int IdEquipoMiembro { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? RolEquipo { get; set; }
    public decimal PorcentajeDedicacion { get; set; }
}

public class EquipoDetalleResponse
{
    public int IdEquipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdLider { get; set; }
    public string? Lider { get; set; }
    public List<MiembroEquipoResponse> Miembros { get; set; } = [];
}
