using GTE.Application.DTOs.Responses.Revisiones;

namespace GTE.Application.Interfaces;

public interface IRevisionQueryService
{
    Task<IReadOnlyList<RevisionResponse>> ObtenerPorWorkItemAsync(
        int idWorkItem, CancellationToken cancellationToken = default);

    Task<RevisionResponse?> ObtenerPorIdAsync(int idRevision, CancellationToken cancellationToken = default);
}
