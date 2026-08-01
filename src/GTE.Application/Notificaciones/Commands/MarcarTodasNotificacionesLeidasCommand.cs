using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Notificaciones.Commands;

public record MarcarTodasNotificacionesLeidasCommand : IRequest<Unit>;

public class MarcarTodasNotificacionesLeidasHandler(
    INotificacionRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<MarcarTodasNotificacionesLeidasCommand, Unit>
{
    public async Task<Unit> Handle(
        MarcarTodasNotificacionesLeidasCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        await repositorio.MarcarTodasLeidasAsync(usuario.IdUsuario, cancellationToken);
        return Unit.Value;
    }
}
