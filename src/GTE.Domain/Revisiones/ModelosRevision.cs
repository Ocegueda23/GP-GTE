namespace GTE.Domain.Revisiones;

/// <summary>Hallazgo nuevo de revision (QA o code review).</summary>
public record RevisionNueva(int IdWorkItem, int IdRevisor, string Comentarios);

/// <summary>Estado de un hallazgo para evaluar reglas.</summary>
public record EstadoRevision(
    int IdRevision,
    int IdWorkItem,
    int IdEstatus,
    bool Corregido,
    int IdRevisor,
    bool Activo);
