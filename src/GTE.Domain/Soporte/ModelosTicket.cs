namespace GTE.Domain.Soporte;

/// <summary>Datos para crear un ticket (folio y estatus los fija el backend).</summary>
public record TicketNuevo(
    string Folio,
    int IdSolicitante,
    string Titulo,
    string? Descripcion,
    int? IdCategoriaTicket,
    int IdPrioridad,
    int? IdSla,
    DateTime? FechaLimiteRespuesta,
    DateTime? FechaLimiteResolucion,
    int? IdUsuarioSolicitante = null,
    int? IdLocacion = null);

/// <summary>
/// Estado minimo de un ticket para evaluar reglas. IdHorario es el del SLA del ticket: se
/// le pasa a dbo.spCambiarEstatus para que materialice los minutos laborales de cada
/// intervalo del historial. Sin el, MinutosLaborales queda NULL y todo tiempo derivado
/// del historial sale en cero.
/// </summary>
public record EstadoTicket(
    int IdTicket,
    string? Folio,
    int IdEstatus,
    int IdSolicitante,
    int? IdAsignado,
    int? IdWorkItemDerivado,
    string Titulo,
    string? Descripcion,
    int IdPrioridad,
    bool Activo,
    int? IdHorario);

/// <summary>SLA vigente para una prioridad, resuelto por el backend al crear el ticket.</summary>
public record SlaVigente(int IdSla, int MinutosRespuesta, int MinutosResolucion, int IdHorario);
