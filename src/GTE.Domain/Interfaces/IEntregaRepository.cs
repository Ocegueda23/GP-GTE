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

    Task CrearCadenaAprobacionAsync(int idRelease, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

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

    /// <summary>Casos con resultado Falla en el ultimo ciclo que no tienen bug asociado.</summary>
    Task<IReadOnlyList<string>> ObtenerFallasSinBugAsync(int idRelease, CancellationToken cancellationToken = default);

    /// <summary>Bugs de severidad S1 o S2 abiertos ligados al contenido del release.</summary>
    Task<IReadOnlyList<string>> ObtenerBugsCriticosAbiertosAsync(int idRelease, CancellationToken cancellationToken = default);
}
