using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Notificaciones.Commands;

public record MarcarNotificacionLeidaCommand(long IdNotificacion) : IRequest<Unit>;

public class MarcarNotificacionLeidaValidator : AbstractValidator<MarcarNotificacionLeidaCommand>
{
    public MarcarNotificacionLeidaValidator()
    {
        RuleFor(c => c.IdNotificacion).GreaterThan(0);
    }
}

public class MarcarNotificacionLeidaHandler(
    INotificacionRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario)
    : IRequestHandler<MarcarNotificacionLeidaCommand, Unit>
{
    public async Task<Unit> Handle(MarcarNotificacionLeidaCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        await repositorio.MarcarLeidaAsync(command.IdNotificacion, usuario.IdUsuario, cancellationToken);
        return Unit.Value;
    }
}
