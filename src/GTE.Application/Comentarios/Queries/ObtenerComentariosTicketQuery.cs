using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Comentarios.Queries;

public record ObtenerComentariosTicketQuery(int IdTicket) : IRequest<IReadOnlyList<ComentarioResponse>>;

/// <summary>El solicitante siempre puede ver los comentarios de su propio ticket; cualquier otro
/// usuario necesita TKT.Atender (mismo criterio que CrearComentarioTicketHandler).</summary>
public class ObtenerComentariosTicketHandler(
    IComentarioQueryService consultas,
    ITicketRepository tickets,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario)
    : IRequestHandler<ObtenerComentariosTicketQuery, IReadOnlyList<ComentarioResponse>>
{
    public async Task<IReadOnlyList<ComentarioResponse>> Handle(
        ObtenerComentariosTicketQuery query, CancellationToken cancellationToken)
    {
        var estadoTicket = await tickets.ObtenerEstadoAsync(query.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", query.IdTicket);

        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken);
        if (usuarioActual?.IdUsuario != estadoTicket.IdSolicitante)
        {
            await permisos.ExigirPermisoAsync(PermisosTicket.Atender, null, cancellationToken);
        }

        return await consultas.ObtenerPorEntidadAsync("Ticket", query.IdTicket, cancellationToken);
    }
}
