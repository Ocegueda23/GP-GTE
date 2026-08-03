namespace GTE.Application.DTOs.Request.Costeo;

public class TarifaNivelCrearRequest
{
    public int IdNivel { get; set; }
    public decimal CostoHora { get; set; }
    public DateOnly VigenciaDesde { get; set; }
}

public class TarifaNivelEditarRequest
{
    public decimal CostoHora { get; set; }
    public DateOnly VigenciaDesde { get; set; }
}

public class PresupuestoProyectoCrearRequest
{
    public int IdProyecto { get; set; }
    public int Anio { get; set; }
    public decimal MontoAutorizado { get; set; }
    public decimal HorasAutorizadas { get; set; }
}

public class PresupuestoProyectoEditarRequest
{
    public decimal MontoAutorizado { get; set; }
    public decimal HorasAutorizadas { get; set; }
}
