using GTE.Domain.Solicitudes;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Solicitudes.</summary>
public interface ISolicitudRepository
{
    /// <summary>Crea la solicitud en Borrador y siembra el historial (ALTA).</summary>
    Task<int> CrearAsync(SolicitudNueva datos, CancellationToken cancellationToken = default);

    Task<EstadoSolicitud?> ObtenerEstadoAsync(int idSolicitud, CancellationToken cancellationToken = default);

    /// <summary>Al APROBAR se fija el proyecto destino (antes de la transicion).</summary>
    Task AsignarProyectoAsync(int idSolicitud, int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Auditoria de movimiento + bitacora tras una transicion exitosa.</summary>
    Task AplicarEfectosTransicionAsync(int idSolicitud, string accion, CancellationToken cancellationToken = default);
}
