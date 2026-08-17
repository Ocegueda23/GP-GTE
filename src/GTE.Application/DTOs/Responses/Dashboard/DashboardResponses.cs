using GTE.Application.DTOs.Responses.Catalogos;

namespace GTE.Application.DTOs.Responses.Dashboard;

public class KpiEstadoResponse
{
    public string Estado { get; set; } = string.Empty;
    public int Total { get; set; }
    public int TotalMesAnterior { get; set; }
    public decimal? VariacionPorcentaje { get; set; }
}

public class KpiGrupoResponse
{
    public string Grupo { get; set; } = string.Empty;
    public IReadOnlyList<KpiEstadoResponse> Estados { get; set; } = [];
}

public class EmpleadoResumenResponse
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Puesto { get; set; }
    public string? UrlFoto { get; set; }
}

public class EmpleadoDelMesResponse
{
    public EmpleadoResumenResponse? Empleado { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? Puntaje { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class CargaTrabajoEmpleadoResponse
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? ProyectoPrincipal { get; set; }
    public decimal HorasAsignadas { get; set; }
    public decimal HorasConsumidas { get; set; }
    public decimal? HorasDisponibles { get; set; }
    public decimal? PorcentajeUtilizacion { get; set; }
}

/// <summary>Desglose por elementos de trabajo (conteos), estilo reporte "Carga de trabajo" del
/// GT -- complementa a <see cref="CargaTrabajoEmpleadoResponse"/>, que es por horas.</summary>
public class CargaTrabajoDetalleResponse
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Pendiente { get; set; }
    public int EnProceso { get; set; }
    public int Terminado { get; set; }
    public int Retrasos { get; set; }
    public decimal? PromedioDuracionDias { get; set; }
    public int Total { get; set; }
    public decimal? EficienciaEntrega { get; set; }
}

public class PuntajeDimensionResponse
{
    public string Dimension { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
}

public class PuntoPuntajeMensualResponse
{
    public int Mes { get; set; }
    public decimal? Puntaje { get; set; }
}

public class IndicadoresEmpleadoResponse
{
    public EmpleadoResumenResponse Empleado { get; set; } = new();
    public int Anio { get; set; }
    public int Mes { get; set; }

    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal? IndiceEficiencia { get; set; }
    public int ItemsTerminados { get; set; }
    public decimal PromedioTerminadosEquipo { get; set; }

    public int EntregasATiempo { get; set; }
    public int EntregasRetrasadas { get; set; }
    public decimal? PorcentajeCumplimientoEntregas { get; set; }

    public IReadOnlyList<PuntajeDimensionResponse> Evaluacion { get; set; } = [];
    public decimal? PuntajeMensual { get; set; }
    public IReadOnlyList<PuntoPuntajeMensualResponse> EvolucionPuntajeAnio { get; set; } = [];
}

public class RankingItemResponse
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public decimal Valor { get; set; }
}

public class RankingResponse
{
    public string Metrica { get; set; } = string.Empty;
    public IReadOnlyList<RankingItemResponse> Top10 { get; set; } = [];
    public IReadOnlyList<RankingItemResponse> Bottom10 { get; set; } = [];
}

public class ComparativoBarraResponse
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public class ComparativosResponse
{
    public IReadOnlyList<ComparativoBarraResponse> EmpleadoVsPromedioEquipo { get; set; } = [];
    public IReadOnlyList<ComparativoBarraResponse> PorArea { get; set; } = [];
    public IReadOnlyList<ComparativoBarraResponse> PorProyecto { get; set; } = [];
    public IReadOnlyList<ComparativoBarraResponse> AnioActualVsAnterior { get; set; } = [];
}

public class PuntoTendenciaResponse
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Valor { get; set; }
}

public class TendenciaResponse
{
    public string Metrica { get; set; } = string.Empty;
    public IReadOnlyList<PuntoTendenciaResponse> Puntos { get; set; } = [];
}

public class FiltroCatalogosDashboardResponse
{
    public IReadOnlyList<ProyectoItemResponse> Proyectos { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Areas { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Empleados { get; set; } = [];
    public string AlcanceVisibilidad { get; set; } = string.Empty;
}

/// <summary>
/// Payload agregado principal del Dashboard Ejecutivo -- un solo endpoint para toda la
/// carga inicial (mismo patron que GET /api/v1/mi-dia), en vez de fragmentar en muchas
/// llamadas paralelas.
/// </summary>
public class DashboardResponse
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string AlcanceVisibilidad { get; set; } = string.Empty;
    public EmpleadoDelMesResponse? EmpleadoDelMes { get; set; }
    public IReadOnlyList<EmpleadoDelMesResponse> HistoricoEmpleadoDelMes { get; set; } = [];
    public IReadOnlyList<KpiGrupoResponse> ResumenEjecutivo { get; set; } = [];
    public IReadOnlyList<CargaTrabajoEmpleadoResponse> CargaTrabajo { get; set; } = [];
    public IReadOnlyList<CargaTrabajoDetalleResponse> DesgloseCargaTrabajo { get; set; } = [];
    public IReadOnlyList<RankingResponse> Rankings { get; set; } = [];
    public ComparativosResponse Comparativos { get; set; } = new();
}
