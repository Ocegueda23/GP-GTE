using GTE.Domain.Workflow;

namespace GTE.Domain.Interfaces;

public interface IWorkflowRepository
{
    /// <summary>
    /// Upsert por (Proceso, IdEstatusOrigen, Accion): actualiza la config si ya existe,
    /// la crea si no. Nunca crea ni elimina filas de dbo.tblTransicion (el grafo estructural
    /// se define por script SQL, ver InterfloClaude.md seccion 9.3 -- "no tocar CambiarST").
    /// </summary>
    Task GuardarTransicionesConfigAsync(
        string proceso, IReadOnlyList<TransicionConfigEdicion> transiciones, CancellationToken cancellationToken = default);
}
