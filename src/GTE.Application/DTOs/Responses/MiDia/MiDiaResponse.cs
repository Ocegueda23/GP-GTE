using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.DTOs.Responses.WorkItems;

namespace GTE.Application.DTOs.Responses.MiDia;

/// <summary>
/// Item de la vista personal con la accion que lo pone En Proceso desde su
/// estatus actual, resuelta por el MOTOR (INICIAR o REANUDAR segun el grafo).
/// El front pinta y manda lo que recibe: nunca deduce la transicion.
/// </summary>
public class MiDiaItemResponse : BandejaItemResponse
{
    public string? AccionInicio { get; set; }
    public string? EtiquetaAccionInicio { get; set; }
}

/// <summary>
/// Vista personal del dia: todo lo que una persona necesita para decidir
/// que hacer, en una sola llamada.
/// </summary>
public class MiDiaResponse
{
    public string Usuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }

    /// <summary>El unico item En Proceso del usuario (RN-GTE-008); null si no esta trabajando en nada.</summary>
    public MiDiaItemResponse? EnProceso { get; set; }

    public IReadOnlyList<MiDiaItemResponse> Vencidas { get; set; } = [];
    public IReadOnlyList<MiDiaItemResponse> ParaHoy { get; set; } = [];
    public IReadOnlyList<MiDiaItemResponse> Proximas { get; set; } = [];

    /// <summary>Minutos registrados por el usuario el dia de hoy.</summary>
    public int MinutosHoy { get; set; }

    /// <summary>Total de elementos abiertos asignados al usuario.</summary>
    public int TotalAbiertos { get; set; }

    /// <summary>Tickets asignados al usuario como agente (no Cerrado).</summary>
    public IReadOnlyList<TicketResponse> TicketsAsignados { get; set; } = [];

    /// <summary>Incidentes no cerrados en proyectos donde el usuario es responsable.</summary>
    public IReadOnlyList<IncidenteResponse> IncidentesRelevantes { get; set; } = [];

    /// <summary>Solicitudes levantadas por el usuario que siguen pendientes de resolucion.</summary>
    public IReadOnlyList<SolicitudResponse> SolicitudesPendientes { get; set; } = [];

    /// <summary>Cuantas solicitudes esperan revision en todo el sistema; 0 si el usuario no tiene SOL.Triage.</summary>
    public int TriagePendientes { get; set; }

    /// <summary>Releases no liberados/cancelados en proyectos donde el usuario es responsable.</summary>
    public IReadOnlyList<ReleaseResponse> ReleasesRelevantes { get; set; } = [];
}
