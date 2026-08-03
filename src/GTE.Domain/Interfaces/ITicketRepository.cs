using GTE.Domain.Soporte;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Tickets (Mesa de ayuda).</summary>
public interface ITicketRepository
{
    /// <summary>Crea el ticket en Nuevo y siembra el historial (ALTA).</summary>
    Task<int> CrearAsync(TicketNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoTicket?> ObtenerEstadoAsync(int idTicket, CancellationToken cancellationToken = default);

    /// <summary>SLA activo configurado para la prioridad (null si no hay uno configurado).</summary>
    Task<SlaVigente?> ObtenerSlaVigenteAsync(int idPrioridad, CancellationToken cancellationToken = default);

    /// <summary>Al ASIGNAR se fija el agente responsable (antes de la transicion).</summary>
    Task AsignarAsync(int idTicket, int idAsignado, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auditoria de movimiento + bitacora tras una transicion exitosa, mas los efectos
    /// propios de la accion: FechaPrimeraRespuesta en INICIAR_ATENCION (solo si aun es
    /// null), FechaResolucion en RESOLVER, y se limpia en REABRIR.
    /// </summary>
    Task AplicarEfectosTransicionAsync(int idTicket, string accion, CancellationToken cancellationToken = default);

    /// <summary>Vincula el WorkItem creado al escalar. El ticket no cambia de estatus.</summary>
    Task EscalarAsync(int idTicket, int idWorkItem, CancellationToken cancellationToken = default);

    /// <summary>Alta de la encuesta de satisfaccion (unica por ticket).</summary>
    Task RegistrarEncuestaAsync(int idTicket, int calificacion, string? comentario, CancellationToken cancellationToken = default);
}
