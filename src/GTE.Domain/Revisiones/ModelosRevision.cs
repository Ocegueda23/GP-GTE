namespace GTE.Domain.Revisiones;

/// <summary>
/// Hallazgo nuevo de revision (QA o code review). IdSeveridad decide si bloquea: S1/S2
/// impiden Terminar el WorkItem y bloquean la aprobacion de un release que lo incluya;
/// S3/S4 quedan registrados sin bloquear. IdEjecucionPrueba es NULL en hallazgos de code
/// review sin una prueba de por medio.
/// </summary>
public record RevisionNueva(int IdWorkItem, int IdRevisor, string Comentarios, int IdSeveridad, int? IdEjecucionPrueba);

/// <summary>Estado de un hallazgo para evaluar reglas.</summary>
public record EstadoRevision(
    int IdRevision,
    int IdWorkItem,
    int IdEstatus,
    bool Corregido,
    int IdRevisor,
    bool Activo);
