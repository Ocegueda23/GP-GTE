using GTE.Domain.Entregas;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Entregas (releases y despliegues).</summary>
public interface IEntregaRepository
{
    Task<int> CrearReleaseAsync(ReleaseNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoRelease?> ObtenerEstadoAsync(int idRelease, CancellationToken cancellationToken = default);

    Task<bool> ExisteVersionAsync(int idProyecto, string version, CancellationToken cancellationToken = default);

    Task ActualizarNotasAsync(int idRelease, string notas, CancellationToken cancellationToken = default);

    Task AplicarEfectosTransicionAsync(int idRelease, string accion, CancellationToken cancellationToken = default);

    /// <summary>Marca la fecha de liberacion al desplegar a produccion.</summary>
    Task MarcarLiberadoAsync(int idRelease, CancellationToken cancellationToken = default);

    /* Contenido */

    Task<CandidatoRelease?> ObtenerCandidatoAsync(int idWorkItem, CancellationToken cancellationToken = default);

    Task AgregarWorkItemAsync(int idRelease, int idWorkItem, CancellationToken cancellationToken = default);

    Task QuitarWorkItemAsync(int idRelease, int idWorkItem, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidatoRelease>> ObtenerContenidoAsync(int idRelease, CancellationToken cancellationToken = default);

    /* Artefactos */

    Task<int> AgregarArtefactoAsync(ArtefactoNuevo datos, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtefactoRelease>> ObtenerArtefactosAsync(int idRelease, CancellationToken cancellationToken = default);

    /* Aprobaciones */

    /// <summary>
    /// Cadena de aprobacion configurada por proyecto (Admin > Workflows). Lista vacia si el
    /// proyecto no tiene configuracion propia: en ese caso el llamador usa el default fijo
    /// (GTE.Domain.Entregas.RolesAprobacion.Cadena).
    /// </summary>
    Task<IReadOnlyList<string>> ObtenerCadenaAprobacionConfiguradaAsync(
        int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Reemplaza completa la cadena configurada del proyecto (lista vacia = volver al default fijo).</summary>
    Task GuardarCadenaAprobacionConfiguradaAsync(
        int idProyecto, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    Task CrearCadenaAprobacionAsync(int idRelease, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    /// <summary>Da de baja la cadena de aprobacion vigente (REABRIR): la siguiente
    /// SOLICITAR_APROBACION crea firmas nuevas en vez de reusar las ya resueltas.</summary>
    Task InvalidarCadenaAprobacionAsync(int idRelease, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AprobacionRelease>> ObtenerAprobacionesAsync(int idRelease, CancellationToken cancellationToken = default);

    Task ResolverAprobacionAsync(
        int idAprobacion, int idAprobador, bool aprobada, string? comentario, string firmaHash,
        CancellationToken cancellationToken = default);

    Task<AprobacionRelease?> ObtenerAprobacionAsync(int idAprobacion, CancellationToken cancellationToken = default);

    Task<int?> ObtenerIdReleaseDeAprobacionAsync(int idAprobacion, CancellationToken cancellationToken = default);

    /* Despliegues */

    Task<int> RegistrarDespliegueAsync(DespliegueNuevo datos, CancellationToken cancellationToken = default);

    Task<int?> ObtenerAmbienteProduccionAsync(int idProyecto, CancellationToken cancellationToken = default);

    /* Calidad del release (RN-QA-01) */

    /// <summary>
    /// WorkItems del contenido del release con un hallazgo (QA o code review) de severidad
    /// S1/S2 todavia sin corregir. Un item nunca probado no aparece aqui -- esa cobertura la
    /// decide QA al aprobar la fase En Pruebas del propio item, no este gate.
    /// </summary>
    Task<IReadOnlyList<string>> ObtenerHallazgosCriticosAbiertosAsync(int idRelease, CancellationToken cancellationToken = default);
}
