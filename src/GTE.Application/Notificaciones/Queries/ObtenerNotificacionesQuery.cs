using GTE.Application.DTOs.Responses.Notificaciones;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Notificaciones.Queries;

public record ObtenerNotificacionesQuery(bool SoloNoLeidas) : IRequest<IReadOnlyList<NotificacionResponse>>;

public class ObtenerNotificacionesHandler(
    INotificacionQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario)
    : IRequestHandler<ObtenerNotificacionesQuery, IReadOnlyList<NotificacionResponse>>
{
    public async Task<IReadOnlyList<NotificacionResponse>> Handle(
        ObtenerNotificacionesQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        return await consultas.ObtenerPorUsuarioAsync(usuario.IdUsuario, query.SoloNoLeidas, cancellationToken);
    }
}
