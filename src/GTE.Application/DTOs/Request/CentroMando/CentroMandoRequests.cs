namespace GTE.Application.DTOs.Request.CentroMando;

/// <summary>
/// Ajuste del catalogo. Clave, nombre, formula y ambito NO son editables desde la UI: los
/// gobierna el script de despliegue para que el modelo siga siendo comparable entre
/// periodos. Lo que el equipo si calibra con datos reales es meta, umbral, peso, si pondera
/// y la accion sugerida.
/// </summary>
public class ActualizarIndicadorGestionRequest
{
    public decimal? Meta { get; set; }
    public decimal? UmbralAlerta { get; set; }
    public decimal Peso { get; set; }
    public bool PonderaEnScore { get; set; }
    public string? AccionSugerida { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Recalculo manual de un periodo (el mensual lo dispara Hangfire).</summary>
public class RecalcularPeriodoRequest
{
    public int Anio { get; set; }
    public int Mes { get; set; }
}
