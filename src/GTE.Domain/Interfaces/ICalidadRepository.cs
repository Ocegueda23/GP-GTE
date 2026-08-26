using GTE.Domain.Calidad;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Calidad (QA): catalogo de casos, su asignacion a
/// WorkItems y el registro de ejecuciones. Las fallas se reportan como hallazgo
/// (ver IRevisionRepository), no como un WorkItem nuevo.</summary>
public interface ICalidadRepository
{
    Task<int> CrearCasoAsync(CasoPruebaNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoCaso?> ObtenerEstadoCasoAsync(int idCasoPrueba, CancellationToken cancellationToken = default);

    /// <summary>Reemplaza titulo/datos y la lista completa de pasos del caso.</summary>
    Task ActualizarCasoAsync(CasoPruebaEdicion datos, CancellationToken cancellationToken = default);

    /// <summary>Baja logica (Activo = 0); las ejecuciones ya registradas se conservan.</summary>
    Task RetirarCasoAsync(int idCasoPrueba, CancellationToken cancellationToken = default);

    /// <summary>Asigna un caso reutilizable (o recien creado) a un WorkItem.</summary>
    Task AsignarCasoAsync(int idWorkItem, int idCasoPrueba, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsignacionActivaAsync(int idWorkItem, int idCasoPrueba, CancellationToken cancellationToken = default);

    /// <summary>WorkItem de una asignacion, para resolver el proyecto y validar el permiso al retirarla.</summary>
    Task<int?> ObtenerIdWorkItemDeAsignacionAsync(int idWorkItemCasoPrueba, CancellationToken cancellationToken = default);

    /// <summary>Baja logica de la asignacion; no afecta al caso ni a sus ejecuciones previas.</summary>
    Task RetirarAsignacionAsync(int idWorkItemCasoPrueba, CancellationToken cancellationToken = default);

    Task<int> RegistrarEjecucionAsync(EjecucionNueva datos, CancellationToken cancellationToken = default);

    Task<EstadoEjecucion?> ObtenerEstadoEjecucionAsync(int idEjecucion, CancellationToken cancellationToken = default);
}
