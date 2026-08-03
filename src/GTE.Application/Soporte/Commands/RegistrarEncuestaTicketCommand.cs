using FluentValidation;
using GTE.Application.DTOs.Request.Soporte;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Soporte.Commands;

public record RegistrarEncuestaTicketCommand(int IdTicket, EncuestaTicketRequest Datos) : IRequest<TicketResponse>;

public class RegistrarEncuestaTicketValidator : AbstractValidator<RegistrarEncuestaTicketCommand>
{
    public RegistrarEncuestaTicketValidator()
    {
        RuleFor(c => c.IdTicket).GreaterThan(0);
        RuleFor(c => c.Datos.Calificacion).InclusiveBetween(1, 5).WithMessage("La calificacion debe ser entre 1 y 5.");
        RuleFor(c => c.Datos.Comentario).MaximumLength(500);
    }
}

/// <summary>Solo el solicitante original califica, y solo cuando el ticket ya se atendio (Resuelto/Cerrado).</summary>
public class RegistrarEncuestaTicketHandler(
    ITicketRepository repositorio,
    ITicketQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<RegistrarEncuestaTicketCommand, TicketResponse>
{
    public async Task<TicketResponse> Handle(RegistrarEncuestaTicketCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);

        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
        if (usuario is null || usuario.IdUsuario != estado.IdSolicitante)
        {
            throw new ForbiddenException("Solo el solicitante del ticket puede calificarlo.");
        }
        if (estado.IdEstatus != EstatusTicket.Resuelto && estado.IdEstatus != EstatusTicket.Cerrado)
        {
            throw new BusinessException("Solo se puede calificar un ticket Resuelto o Cerrado.");
        }

        await repositorio.RegistrarEncuestaAsync(
            command.IdTicket, command.Datos.Calificacion, command.Datos.Comentario, cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);
    }
}
