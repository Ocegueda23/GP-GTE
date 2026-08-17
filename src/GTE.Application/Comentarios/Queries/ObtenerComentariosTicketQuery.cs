using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Comentarios.Queries;

public record ObtenerComentariosTicketQuery(int IdTicket) : IRequest<IReadOnlyList<ComentarioResponse>>;

public class ObtenerComentariosTicketHandler(IComentarioQueryService consultas)
    : IRequestHandler<ObtenerComentariosTicketQuery, IReadOnlyList<ComentarioResponse>>
{
    public async Task<IReadOnlyList<ComentarioResponse>> Handle(
        ObtenerComentariosTicketQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync("Ticket", query.IdTicket, cancellationToken);
    }
}
