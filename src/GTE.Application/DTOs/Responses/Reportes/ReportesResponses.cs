namespace GTE.Application.DTOs.Responses.Reportes;

/// <summary>Un registro de tiempo dentro del reporte de actividad diaria de un usuario.</summary>
public class ActividadDetalleResponse
{
    public int IdRegistroTiempo { get; set; }
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int Minutos { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>Actividad de un usuario en un dia especifico dentro del rango consultado.</summary>
public class ActividadDiaResponse
{
    public DateOnly Fecha { get; set; }
    public int MinutosDia { get; set; }
    public IReadOnlyList<ActividadDetalleResponse> Registros { get; set; } = [];
}

/// <summary>Reporte de actividad diaria de un usuario (P.Reportes): horas reales trabajadas por dia en un rango de fechas.</summary>
public class ActividadUsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int MinutosTotales { get; set; }
    public IReadOnlyList<ActividadDiaResponse> Dias { get; set; } = [];
}

// ---------- R17 Trabajo pendiente ----------

/// <summary>
/// WorkItem abierto: cualquier estatus distinto de Terminado y Cancelado. Es una foto al
/// momento de correr el reporte, no un periodo: por eso no lleva fechas de filtro.
/// </summary>
public class WorkItemPendienteResponse
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
    public int IdEstatusWorkItem { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string? Sprint { get; set; }
    public int? MinutosPresupuesto { get; set; }

    /// <summary>Minutos laborales en En Proceso (VwBandejaTrabajo.MinutosInvertidos), igual que R15.</summary>
    public int MinutosInvertidos { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaCompromiso { get; set; }

    /// <summary>De la creacion a hoy, en dias de calendario.</summary>
    public decimal DiasAbierto { get; set; }

    /// <summary>Compromiso anterior a hoy (VwBandejaTrabajo.EsVencida, compara solo fechas).</summary>
    public bool EsVencida { get; set; }

    public int RevisionesPendientes { get; set; }
}

public class WorkItemsPendientesTotalesResponse
{
    public int Items { get; set; }
    public int Vencidos { get; set; }
    public int SinAsignar { get; set; }
    public int Suspendidos { get; set; }
    public int MinutosInvertidos { get; set; }
}

/// <summary>Ticket abierto: todo lo que no esta Resuelto ni Cerrado.</summary>
public class TicketPendienteResponse
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
    public int MinutosEnAtencion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPrimeraRespuesta { get; set; }
    public DateTime? FechaLimiteResolucion { get; set; }
    public decimal DiasAbierto { get; set; }

    /// <summary>Ya paso FechaLimiteResolucion sin resolverse; null si el ticket no trae SLA.</summary>
    public bool? SlaVencido { get; set; }
}

public class TicketsPendientesTotalesResponse
{
    public int Items { get; set; }
    public int SinAsignar { get; set; }
    public int SlaVencido { get; set; }
}

/// <summary>Incidente abierto: todo lo que no esta Resuelto ni Cerrado (Mitigado sigue abierto).</summary>
public class IncidentePendienteResponse
{
    public int IdIncidente { get; set; }
    public string? Folio { get; set; }
    public string Severidad { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public int MinutosEnAtencion { get; set; }
    public int? MinutosIndisponibilidad { get; set; }
    public DateTime FechaOcurrencia { get; set; }
    public DateTime? FechaDeteccion { get; set; }

    /// <summary>De la ocurrencia a hoy, en dias de calendario.</summary>
    public decimal DiasAbierto { get; set; }
}

public class IncidentesPendientesTotalesResponse
{
    public int Items { get; set; }
    public int MinutosIndisponibilidad { get; set; }
}

public class TrabajoPendienteReporteResponse
{
    /// <summary>Momento de la foto: el reporte no tiene periodo, mide contra este instante.</summary>
    public DateTime FechaCorte { get; set; }

    public IReadOnlyList<WorkItemPendienteResponse> Items { get; set; } = [];
    public WorkItemsPendientesTotalesResponse Totales { get; set; } = new();

    /// <summary>True si los work items excedieron el tope de renglones y la lista viene recortada.</summary>
    public bool Truncado { get; set; }

    public IReadOnlyList<TicketPendienteResponse> Tickets { get; set; } = [];
    public TicketsPendientesTotalesResponse TotalesTickets { get; set; } = new();

    public IReadOnlyList<IncidentePendienteResponse> Incidentes { get; set; } = [];
    public IncidentesPendientesTotalesResponse TotalesIncidentes { get; set; } = new();

    /// <summary>Mismo criterio que R15: filtros que la seccion no puede honrar la dejan vacia con aviso.</summary>
    public IReadOnlyList<string> AvisosTickets { get; set; } = [];
    public IReadOnlyList<string> AvisosIncidentes { get; set; } = [];
}
