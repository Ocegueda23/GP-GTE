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
    string? JustificacionIrreversible,
    string? InstruccionesImplementacion,
    string? VersionArtefacto);

/// <summary>
/// Cambios a un artefacto ya registrado en el release. Solo se admite mientras el release
/// sigue En Preparacion: despues la Solicitud de despliegue ya se mando a firmar y el
/// contenido de la entrega queda congelado.
/// </summary>
public record ArtefactoEditar(
    int IdRelease,
    int IdArtefacto,
    string Nombre,
    int IdTipoArtefacto,
    string? HashSha256,
    int? OrdenEjecucion,
    int? IdArtefactoRollback,
    string? JustificacionIrreversible,
    string? InstruccionesImplementacion,
    string? VersionArtefacto);

/// <summary>Artefacto del release con su pareja de rollback, para validar RN-GTE-032.</summary>
public record ArtefactoRelease(
    int IdReleaseArtefacto,
    int IdArtefacto,
    string Nombre,
    int IdTipoArtefacto,
    int? OrdenEjecucion,
    int? IdArtefactoRollback,
    string? JustificacionIrreversible,
    string? InstruccionesImplementacion,
    string? VersionArtefacto);

/// <summary>
/// Respaldo que se tiene que tomar antes de desplegar: la base de datos, el servicio, el
/// sitio o la ubicacion exacta. Es un apartado propio de la Solicitud de despliegue y el
/// gate de aprobacion exige al menos uno (ver ValidarListoParaAprobacionAsync).
/// </summary>
public record RespaldoNuevo(int IdRelease, int IdTipoRespaldo, string Descripcion);

/// <summary>Cambios a un respaldo ya registrado; igual que los artefactos, solo En Preparacion.</summary>
public record RespaldoEditar(int IdRelease, int IdReleaseRespaldo, int IdTipoRespaldo, string Descripcion);

public record RespaldoRelease(
    int IdReleaseRespaldo,
    int IdTipoRespaldo,
    string Tipo,
    string Descripcion);

public record AprobacionRelease(
    int IdAprobacion,
    string RolAprobacion,
    int IdEstatus,
    int IdAprobador,
    string? Comentario);

public record DespliegueNuevo(int IdRelease, int IdAmbiente, int IdEjecutor, bool EsRollback, string? Bitacora);

/// <summary>Elemento candidato a entrar al release (RN-GTE-031 exige Terminado y revisado).</summary>
public record CandidatoRelease(int IdWorkItem, string Folio, string Titulo, int IdEstatus, bool Revisado, int RevisionesPendientes);
