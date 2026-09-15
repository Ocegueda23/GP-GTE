namespace GTE.Application.DTOs.Request.Administracion;

public class EquipoCrearRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdLider { get; set; }

    /// <summary>Bloque tecnico del Centro de Mando TI; null = solo bloque comun.</summary>
    public string? AmbitoCentroMando { get; set; }
}

public class EquipoEditarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdLider { get; set; }

    /// <summary>Bloque tecnico del Centro de Mando TI; null = solo bloque comun.</summary>
    public string? AmbitoCentroMando { get; set; }
}

public class MiembroEquipoCrearRequest
{
    public int IdUsuario { get; set; }
    public string? RolEquipo { get; set; }
    public decimal PorcentajeDedicacion { get; set; } = 100;
}

public class MiembroEquipoEditarRequest
{
    public string? RolEquipo { get; set; }
    public decimal PorcentajeDedicacion { get; set; }
}
