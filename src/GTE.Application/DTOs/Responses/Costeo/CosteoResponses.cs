namespace GTE.Application.DTOs.Responses.Costeo;

public class TarifaNivelResponse
{
    public int IdTarifaNivel { get; set; }
    public int IdNivel { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public decimal CostoHora { get; set; }
    public DateOnly VigenciaDesde { get; set; }
}

public class PresupuestoProyectoResponse
{
    public int IdPresupuestoProyecto { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal MontoAutorizado { get; set; }
    public decimal HorasAutorizadas { get; set; }
}

/// <summary>Costo real agregado de un usuario dentro de un proyecto/anio.</summary>
public class CostoUsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public decimal Minutos { get; set; }
    public decimal Horas { get; set; }
    public decimal Costo { get; set; }
}

/// <summary>Reporte de costo real vs presupuesto de un proyecto en un anio.</summary>
public class CostoProyectoResponse
{
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal MontoAutorizado { get; set; }
    public decimal HorasAutorizadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoReal { get; set; }
    public IReadOnlyList<CostoUsuarioResponse> DetallePorUsuario { get; set; } = [];
}
