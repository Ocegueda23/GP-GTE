using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Comentarios.Queries;

public record ObtenerComentariosQuery(int IdWorkItem) : IRequest<IReadOnlyList<ComentarioResponse>>;

public class ObtenerComentariosHandler(IComentarioQueryService consultas)
    : IRequestHandler<ObtenerComentariosQuery, IReadOnlyList<ComentarioResponse>>
{
    public async Task<IReadOnlyList<ComentarioResponse>> Handle(
        ObtenerComentariosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync("WorkItem", query.IdWorkItem, cancellationToken);
    }
}
