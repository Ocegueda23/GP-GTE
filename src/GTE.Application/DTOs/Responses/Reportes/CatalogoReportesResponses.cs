using GTE.Application.Common;

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

// ---------- R15 Detalle de actividades terminadas ----------
public class ActividadTerminadaResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string? Equipo { get; set; }
    public string? Asignado { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public string? Sprint { get; set; }
    public string? Release { get; set; }

    /// <summary>
    /// Minutos LABORALES que el item estuvo en estatus En Proceso, no lo que el usuario
    /// capturo a mano: viene de VwBandejaTrabajo.MinutosInvertidos, que suma
    /// tblHistorialEstatus.MinutosLaborales (materializados por spCambiarEstatus con
    /// dbo.fnMinutosLaborales) de los intervalos en estatus 2. Lo capturado a mano vive en
    /// tblRegistroTiempo y es lo que reporta R02 Horas registradas; las dos cifras miden
    /// cosas distintas y no tienen por que coincidir.
    /// </summary>
    public int MinutosInvertidos { get; set; }

    /// <summary>Lo que la persona capturo a mano en tblRegistroTiempo (el reloj declarado).</summary>
    public int MinutosRegistrados { get; set; }

    /// <summary>
    /// Registrado menos invertido. Negativo = se capturo menos tiempo del que el item estuvo
    /// En Proceso (lo tipico cuando no se registra); positivo = se capturo mas, que suele
    /// significar trabajo hecho sin mover el estatus.
    /// </summary>
    public int DiferenciaMinutos { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaCompromiso { get; set; }

    /// <summary>Resolucion: de la creacion al fin, en dias de calendario.</summary>
    public decimal? DiasNaturalesResolucion { get; set; }

    /// <summary>Resolucion en minutos habiles segun el horario del asignado; null si no tiene horario configurado.</summary>
    public int? MinutosLaboralesResolucion { get; set; }

    /// <summary>Espera: de la creacion al inicio de trabajo (cuanto estuvo en cola).</summary>
    public decimal? DiasNaturalesEspera { get; set; }

    public int? MinutosLaboralesEspera { get; set; }

    /// <summary>FechaFin dentro del compromiso; null si el item no tenia compromiso.</summary>
    public bool? EntregadoATiempo { get; set; }
}

public class ActividadesTerminadasTotalesResponse
{
    public int Items { get; set; }
    public int MinutosInvertidos { get; set; }
    public int MinutosRegistrados { get; set; }
    public int DiferenciaMinutos { get; set; }

    /// <summary>
    /// Cuantos de los items del periodo traen al menos un registro de tiempo capturado. Es el
    /// termometro de si el equipo esta registrando su tiempo: sin esto, un total de registrado
    /// muy bajo no distingue "nadie captura" de "unos pocos capturan mucho".
    /// </summary>
    public int ItemsConRegistro { get; set; }
    public decimal? PromedioDiasNaturalesResolucion { get; set; }
    public int? PromedioMinutosLaboralesResolucion { get; set; }
    public decimal? PromedioDiasNaturalesEspera { get; set; }
    public int? PromedioMinutosLaboralesEspera { get; set; }
    public decimal? PorcentajeATiempo { get; set; }
}

/// <summary>
/// Ticket resuelto o cerrado en el periodo. Va en su propia seccion, no mezclado con los
/// WorkItems: un ticket no tiene equipo ni proyecto, y su tiempo declarado no existe
/// (tblRegistroTiempo solo cuelga de WorkItem).
/// </summary>
public class TicketTerminadoResponse
{
    public int IdTicket { get; set; }
    public string? Folio { get; set; }
    public string? Categoria { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public string? Asignado { get; set; }

    /// <summary>Reloj del estatus: minutos laborales en En Atencion (analogo de En Proceso).</summary>
    public int MinutosEnAtencion { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPrimeraRespuesta { get; set; }
    public DateTime? FechaResolucion { get; set; }

    /// <summary>Espera: de la creacion a la primera respuesta.</summary>
    public decimal? DiasNaturalesEspera { get; set; }

    public decimal? DiasNaturalesResolucion { get; set; }
    public int? MinutosLaboralesResolucion { get; set; }

    /// <summary>FechaResolucion dentro de FechaLimiteResolucion; null si el ticket no traia SLA.</summary>
    public bool? DentroDeSla { get; set; }
}

/// <summary>
/// Incidente resuelto o cerrado en el periodo. No tiene asignado ni equipo, asi que no se
/// puede atribuir a una persona: se reporta por proyecto y severidad.
/// </summary>
public class IncidenteTerminadoResponse
{
    public int IdIncidente { get; set; }
    public string? Folio { get; set; }
    public string Severidad { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public string? CausaRaiz { get; set; }

    /// <summary>Reloj del estatus: minutos laborales en En Atencion.</summary>
    public int MinutosEnAtencion { get; set; }

    /// <summary>Indisponibilidad declarada del servicio: NO es esfuerzo, es impacto.</summary>
    public int? MinutosIndisponibilidad { get; set; }

    public DateTime FechaOcurrencia { get; set; }
    public DateTime? FechaDeteccion { get; set; }
    public DateTime? FechaResolucion { get; set; }

    /// <summary>De la ocurrencia a la deteccion: cuanto tardamos en enterarnos.</summary>
    public decimal? DiasNaturalesDeteccion { get; set; }

    /// <summary>De la ocurrencia a la resolucion.</summary>
    public decimal? DiasNaturalesResolucion { get; set; }
}

public class TicketsTerminadosTotalesResponse
{
    public int Items { get; set; }
    public int MinutosEnAtencion { get; set; }
    public decimal? PromedioDiasNaturalesResolucion { get; set; }
    public decimal? PorcentajeDentroDeSla { get; set; }
}

public class IncidentesTerminadosTotalesResponse
{
    public int Items { get; set; }
    public int MinutosEnAtencion { get; set; }
    public int MinutosIndisponibilidad { get; set; }
    public decimal? PromedioDiasNaturalesResolucion { get; set; }
}

public class ActividadesTerminadasReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public IReadOnlyList<ActividadTerminadaResponse> Items { get; set; } = [];
    public ActividadesTerminadasTotalesResponse Totales { get; set; } = new();

    /// <summary>True si el rango excedio el tope de renglones y la lista viene recortada.</summary>
    public bool Truncado { get; set; }

    public IReadOnlyList<TicketTerminadoResponse> Tickets { get; set; } = [];
    public TicketsTerminadosTotalesResponse TotalesTickets { get; set; } = new();

    public IReadOnlyList<IncidenteTerminadoResponse> Incidentes { get; set; } = [];
    public IncidentesTerminadosTotalesResponse TotalesIncidentes { get; set; } = new();

    /// <summary>
    /// Filtros que se pidieron y que esa seccion no puede honrar porque la entidad no tiene
    /// ese dato (equipo en tickets/incidentes, asignado en incidentes, proyecto en tickets).
    /// Cuando pasa, la seccion viene vacia y el front explica por que en vez de mentir con
    /// una tabla en blanco.
    /// </summary>
    public IReadOnlyList<string> AvisosTickets { get; set; } = [];
    public IReadOnlyList<string> AvisosIncidentes { get; set; } = [];
}

// ---------- R16 Gantt de actividades ----------

/// <summary>
/// Una barra del Gantt. FechaInicio nunca es nula (una actividad sin iniciar no se "realizo" y
/// no entra al reporte); FechaFin nula significa que la actividad sigue abierta y el front
/// dibuja la barra hasta hoy con el borde abierto.
/// </summary>
public class GanttActividadResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int? IdAsignado { get; set; }
    public string? Asignado { get; set; }
    public int IdEstatusWorkItem { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}

public class GanttActividadesReporteResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }

    /// <summary>Renglones de la pagina en curso, ya ordenados por la llave de agrupacion pedida.</summary>
    public PagedResult<GanttActividadResponse> Pagina { get; set; } = new();

    /// <summary>Cuantas actividades del filtro completo (no solo de la pagina) siguen sin fecha de fin.</summary>
    public int TotalEnProgreso { get; set; }
}
