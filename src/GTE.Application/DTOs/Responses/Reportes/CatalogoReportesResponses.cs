namespace GTE.Application.DTOs.Responses.Reportes;

// ---------- R01 Productividad por persona/equipo ----------
public class ProductividadPersonaResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int ItemsTerminados { get; set; }
    public decimal PuntosTotales { get; set; }
    public decimal PorcentajeATiempo { get; set; }
    public decimal? EficienciaPorcentaje { get; set; }
}

public class ProductividadReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public IReadOnlyList<ProductividadPersonaResponse> Personas { get; set; } = [];
}

// ---------- R02 Horas registradas ----------
public class HorasDiaResponse
{
    public DateOnly Fecha { get; set; }
    public int Minutos { get; set; }
    public bool EsAusencia { get; set; }
}

public class HorasUsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int MinutosTotales { get; set; }
    public IReadOnlyList<HorasDiaResponse> Dias { get; set; } = [];
}

public class HorasRegistradasReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public IReadOnlyList<HorasUsuarioResponse> Usuarios { get; set; } = [];
}

// ---------- R03 Retrabajo ----------
public class RetrabajoDetalleResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int MinutosCorreccion { get; set; }
    public int MinutosTotales { get; set; }
    public decimal Porcentaje { get; set; }
}

/// <summary>"Reaperturas" sin dato disponible -- no existe contador de veces reabierto (mismo gap documentado en el Dashboard Ejecutivo P18).</summary>
public class RetrabajoReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public bool ReaperturasSinDatos { get; set; } = true;
    public IReadOnlyList<RetrabajoDetalleResponse> Detalle { get; set; } = [];
}

// ---------- R04 Bugs y defectos ----------
public class BugsProyectoResponse
{
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int TotalBugs { get; set; }
    public int TotalItems { get; set; }
    public decimal DensidadPorcentaje { get; set; }
    public decimal AgingPromedioDias { get; set; }
    public int Escapados { get; set; }
    public decimal TasaEscapePorcentaje { get; set; }
}

public class BugsDefectosReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public IReadOnlyList<BugsProyectoResponse> Proyectos { get; set; } = [];
}

// ---------- R05 Versiones/Releases ----------
public class ReleaseReporteItemResponse
{
    public int IdRelease { get; set; }
    public string? Folio { get; set; }
    public string Version { get; set; } = string.Empty;
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public DateTime? FechaLiberacion { get; set; }
    public decimal? DiasAprobacion { get; set; }
    public int ItemsIncluidos { get; set; }
}

public class ReleasesReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int TotalReleases { get; set; }
    public decimal FrecuenciaPorSemana { get; set; }
    public IReadOnlyList<ReleaseReporteItemResponse> Releases { get; set; } = [];
}

// ---------- R06 Riesgos ----------
public class RiesgoReporteItemResponse
{
    public int IdRiesgo { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public byte Probabilidad { get; set; }
    public byte Impacto { get; set; }
    public byte Exposicion { get; set; }
    public string Estatus { get; set; } = string.Empty;
}

public class RiesgosReporteResponse
{
    public int TotalExpuestos { get; set; }
    public int TotalMitigados { get; set; }
    public int TotalMaterializados { get; set; }
    public int TotalCerrados { get; set; }
    public IReadOnlyList<RiesgoReporteItemResponse> Riesgos { get; set; } = [];
}

// ---------- R07 Clientes/solicitantes ----------
public class SolicitudesAreaResponse
{
    public string Area { get; set; } = string.Empty;
    public int TotalSolicitudes { get; set; }
    public decimal? TiempoTriagePromedioDias { get; set; }
    public decimal? TiempoEntregaPromedioDias { get; set; }
}

/// <summary>Satisfaccion sin datos: no existe encuesta de satisfaccion ligada a Solicitudes (solo a Tickets).</summary>
public class SolicitantesReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public bool SatisfaccionSinDatos { get; set; } = true;
    public IReadOnlyList<SolicitudesAreaResponse> PorArea { get; set; } = [];
}

// ---------- R08 Costos ----------
public class CostoProyectoMesResponse
{
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int Mes { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoReal { get; set; }
}

public class CostoDesarrolladorResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public decimal HorasReales { get; set; }
    public decimal CostoReal { get; set; }
}

public class CostosReporteResponse
{
    public int Anio { get; set; }
    public IReadOnlyList<CostoProyectoMesResponse> PorProyectoMes { get; set; } = [];
    public IReadOnlyList<CostoDesarrolladorResponse> PorDesarrollador { get; set; } = [];
}

// ---------- R09 Rentabilidad ----------
public class RentabilidadProyectoResponse
{
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public decimal? MontoAutorizado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal? PorcentajeConsumido { get; set; }
    public string Semaforo { get; set; } = string.Empty;
}

public class RentabilidadReporteResponse
{
    public int Anio { get; set; }
    public IReadOnlyList<RentabilidadProyectoResponse> Proyectos { get; set; } = [];
}

// ---------- R10 SLA ----------
public class SlaPrioridadResponse
{
    public string Prioridad { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int DentroSla { get; set; }
    public decimal CumplimientoPorcentaje { get; set; }
}

public class SlaAgenteResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int DentroSla { get; set; }
    public decimal CumplimientoPorcentaje { get; set; }
    public int Incumplimientos { get; set; }
}

public class SlaReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal? Csat { get; set; }
    public IReadOnlyList<SlaPrioridadResponse> PorPrioridad { get; set; } = [];
    public IReadOnlyList<SlaAgenteResponse> PorAgente { get; set; } = [];
}

// ---------- R11 KPIs / DORA ----------
public class PuntoKpiResponse
{
    public DateOnly Fecha { get; set; }
    public decimal Valor { get; set; }
}

public class KpiHistoricoResponse
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public IReadOnlyList<PuntoKpiResponse> SerieAnioActual { get; set; } = [];
    public IReadOnlyList<PuntoKpiResponse> SerieAnioComparativo { get; set; } = [];
}

public class KpisHistoricosReporteResponse
{
    public int Anio { get; set; }
    public int? AnioComparativo { get; set; }
    public IReadOnlyList<KpiHistoricoResponse> Kpis { get; set; } = [];
}

// ---------- R12 Carga de trabajo ----------
public class CargaTrabajoPersonaResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int Wip { get; set; }
    public decimal? PorcentajeOcupacion { get; set; }
}

/// <summary>PorcentajeOcupacion sin dato cuando la persona no tiene capacidad registrada en un sprint activo (tblCapacidadSprint).</summary>
public class CargaTrabajoReporteResponse
{
    public IReadOnlyList<CargaTrabajoPersonaResponse> Personas { get; set; } = [];
}

// ---------- R13 Flujo (CFD) ----------
public class PuntoFlujoResponse
{
    public DateOnly Fecha { get; set; }
    public IReadOnlyDictionary<string, int> ConteoPorEstatus { get; set; } = new Dictionary<string, int>();
}

public class FlujoReporteResponse
{
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public IReadOnlyList<string> Estatus { get; set; } = [];
    public IReadOnlyList<PuntoFlujoResponse> Puntos { get; set; } = [];
}

// ---------- R14 Auditoria ----------
public class AuditoriaItemResponse
{
    public long IdBitacora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? Entidad { get; set; }
    public int? IdEntidad { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}
