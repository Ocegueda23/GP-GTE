using GTE.Domain.Entregas;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Entregas (releases y despliegues).</summary>
public interface IEntregaRepository
{
    Task<int> CrearReleaseAsync(ReleaseNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoRelease?> ObtenerEstadoAsync(int idRelease, CancellationToken cancellationToken = default);

    Task<bool> ExisteVersionAsync(int idProyecto, string version, CancellationToken cancellationToken = default);

    Task ActualizarNotasAsync(int idRelease, string notas, CancellationToken cancellationToken = default);

    /// <summary>Instructivo de despliegue del release en HTML enriquecido; nulo lo limpia.</summary>
    Task ActualizarInstruccionesAsync(
        int idRelease, string? instrucciones, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lider responsable de sacar la entrega. Nulo lo desasigna. Es un usuario del sistema
    /// para que el listado de releases pueda filtrarse por lider sin depender de como se
    /// escribio el nombre.
    /// </summary>
    Task AsignarLiderAsync(
        int idRelease, int? idLiderAsignado, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Baja logica del artefacto y de su vinculo con el release. Devuelve el nombre del
    /// artefacto que lo usa como reversa si existe, y en ese caso NO borra nada: quitarlo
    /// dejaria a ese otro artefacto sin rollback y bloqueado por RN-GTE-032 sin explicacion.
    /// </summary>
    Task<string?> QuitarArtefactoAsync(
        int idRelease, int idArtefacto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un artefacto del release. Devuelve false si el artefacto no pertenece al
    /// release (o ya se dio de baja); el gate de estatus lo valida el handler.
    /// </summary>
    Task<bool> EditarArtefactoAsync(ArtefactoEditar datos, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtefactoRelease>> ObtenerArtefactosAsync(int idRelease, CancellationToken cancellationToken = default);

    /* Respaldos previos al despliegue */

    Task<int> AgregarRespaldoAsync(RespaldoNuevo datos, CancellationToken cancellationToken = default);

    /// <summary>Actualiza un respaldo. Devuelve false si no pertenece al release.</summary>
    Task<bool> EditarRespaldoAsync(RespaldoEditar datos, CancellationToken cancellationToken = default);

    /// <summary>Baja logica del respaldo. Devuelve false si no pertenece al release.</summary>
    Task<bool> QuitarRespaldoAsync(
        int idRelease, int idRespaldo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RespaldoRelease>> ObtenerRespaldosAsync(
        int idRelease, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Cierra como Omitidas las firmas que seguian Pendientes al autorizar el release
    /// (AUTORIZAR), dejando escrito el autorizador, su motivo y su firma electronica.
    /// Devuelve cuantas se omitieron. Las ya resueltas no se tocan.
    /// </summary>
    Task<int> OmitirAprobacionesPendientesAsync(
        int idRelease, int idAutorizador, string motivo, string firmaHash,
        CancellationToken cancellationToken = default);

    Task<AprobacionRelease?> ObtenerAprobacionAsync(int idAprobacion, CancellationToken cancellationToken = default);

    Task<int?> ObtenerIdReleaseDeAprobacionAsync(int idAprobacion, CancellationToken cancellationToken = default);

    /* Despliegues */

    Task<int> RegistrarDespliegueAsync(DespliegueNuevo datos, CancellationToken cancellationToken = default);

    Task<int?> ObtenerAmbienteProduccionAsync(int idProyecto, CancellationToken cancellationToken = default);

    /* Calidad del release (RN-GTE-025) */

    /// <summary>
    /// WorkItems del contenido del release con un hallazgo (QA o code review) de severidad
    /// S1/S2 todavia sin corregir. Un item nunca probado no aparece aqui -- esa cobertura la
    /// decide QA al aprobar la fase En Pruebas del propio item, no este gate.
    /// </summary>
    Task<IReadOnlyList<string>> ObtenerHallazgosCriticosAbiertosAsync(int idRelease, CancellationToken cancellationToken = default);
}
