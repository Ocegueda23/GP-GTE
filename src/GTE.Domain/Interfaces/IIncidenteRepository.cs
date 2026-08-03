using GTE.Domain.Operacion;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Incidentes (Operacion).</summary>
public interface IIncidenteRepository
{
    /// <summary>Crea el incidente en Detectado y siembra el historial (ALTA).</summary>
    Task<int> CrearAsync(IncidenteNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoIncidente?> ObtenerEstadoAsync(int idIncidente, CancellationToken cancellationToken = default);

    /// <summary>IdUsuario responsable del proyecto (tblProyecto.IdResponsable), para notificar de inmediato un S1.</summary>
    Task<int?> ObtenerResponsableProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Valida que el release exista y pertenezca al mismo proyecto del incidente.</summary>
    Task<bool> ExisteReleaseEnProyectoAsync(int idRelease, int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Titulo/Descripcion/CausaRaiz/MinutosIndisponibilidad/FechaDeteccion. No toca el estatus.</summary>
    Task ActualizarAsync(int idIncidente, IncidenteActualizacion datos, CancellationToken cancellationToken = default);

    /// <summary>RN-OPS-03: cambio de severidad con motivo obligatorio (auditado en bitacora).</summary>
    Task CambiarSeveridadAsync(int idIncidente, int idSeveridad, string motivo, CancellationToken cancellationToken = default);

    /// <summary>Vincula el WorkItem correctivo creado. No cambia el estatus del incidente.</summary>
    Task VincularCorrectivoAsync(int idIncidente, int idWorkItem, CancellationToken cancellationToken = default);

    /// <summary>Vincula un release ya existente como causante. No cambia el estatus del incidente.</summary>
    Task VincularReleaseCausanteAsync(int idIncidente, int idRelease, CancellationToken cancellationToken = default);

    /// <summary>Auditoria de movimiento + bitacora tras una transicion exitosa; fija FechaResolucion en RESOLVER.</summary>
    Task AplicarEfectosTransicionAsync(int idIncidente, string accion, CancellationToken cancellationToken = default);
}
