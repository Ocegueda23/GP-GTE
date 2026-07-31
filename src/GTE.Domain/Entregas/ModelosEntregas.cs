namespace GTE.Domain.Entregas;

public record ReleaseNuevo(int IdProyecto, string Version, string? Folio, string? NotasVersion, DateOnly? FechaPlan);

public record EstadoRelease(
    int IdRelease,
    int IdProyecto,
    string Version,
    string? Folio,
    int IdEstatus,
    bool Activo);

public record ArtefactoNuevo(
    int IdRelease,
    string Nombre,
    int IdTipoArtefacto,
    string? HashSha256,
    int? OrdenEjecucion,
    int? IdArtefactoRollback,
    string? JustificacionIrreversible);

/// <summary>Artefacto del release con su pareja de rollback, para validar RN-REL-02.</summary>
public record ArtefactoRelease(
    int IdReleaseArtefacto,
    int IdArtefacto,
    string Nombre,
    int IdTipoArtefacto,
    int? OrdenEjecucion,
    int? IdArtefactoRollback,
    string? JustificacionIrreversible);

public record AprobacionRelease(
    int IdAprobacion,
    string RolAprobacion,
    int IdEstatus,
    int IdAprobador,
    string? Comentario);

public record DespliegueNuevo(int IdRelease, int IdAmbiente, int IdEjecutor, bool EsRollback, string? Bitacora);

/// <summary>Elemento candidato a entrar al release (RN-REL-01 exige Terminado y revisado).</summary>
public record CandidatoRelease(int IdWorkItem, string Folio, string Titulo, int IdEstatus, bool Revisado, int RevisionesPendientes);
