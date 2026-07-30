using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Revisiones.Queries;

public record ObtenerRevisionesQuery(int IdWorkItem) : IRequest<IReadOnlyList<RevisionResponse>>;

public class ObtenerRevisionesHandler(IRevisionQueryService consultas)
    : IRequestHandler<ObtenerRevisionesQuery, IReadOnlyList<RevisionResponse>>
{
    public async Task<IReadOnlyList<RevisionResponse>> Handle(
        ObtenerRevisionesQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorWorkItemAsync(query.IdWorkItem, cancellationToken);
    }
}
