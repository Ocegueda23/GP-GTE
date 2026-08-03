using FluentValidation;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Soporte.Commands;

public record CambiarEstatusTicketCommand(
    int IdTicket, string Accion, string? Motivo, int? IdAsignado,
    string? Solucion = null, int? MinutosSolucion = null) : IRequest<TicketResponse>;

public class CambiarEstatusTicketValidator : AbstractValidator<CambiarEstatusTicketCommand>
{
    public CambiarEstatusTicketValidator()
    {
        RuleFor(c => c.IdTicket).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().WithMessage("La accion es obligatoria.").MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
        RuleFor(c => c.Solucion).MaximumLength(4000);
    }
}

/// <summary>
/// Toda transicion del proceso Ticket exige TKT.Atender (tblTransicionConfig ya lo
/// declara por fila; se revalida aqui porque ASIGNAR ademas necesita el agente
/// destino, dato que no viaja en tblTransicion). Los efectos propios de cada accion
/// (FechaPrimeraRespuesta, FechaResolucion) los aplica el repositorio despues de la
/// transicion, en AplicarEfectosTransicionAsync.
/// </summary>
public class CambiarEstatusTicketHandler(
    ITicketRepository repositorio,
    ITicketQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarEstatusTicketCommand, TicketResponse>
{
    public async Task<TicketResponse> Handle(CambiarEstatusTicketCommand command, CancellationToken cancellationToken)
    {
        _ = await repositorio.ObtenerEstadoAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);

        await permisos.ExigirPermisoAsync(PermisosTicket.Atender, null, cancellationToken);

        if (command.Accion == AccionesTicket.Asignar)
        {
            if (!command.IdAsignado.HasValue)
            {
                throw new BusinessException("Asignar un ticket requiere elegir el agente responsable.");
            }
            await repositorio.AsignarAsync(command.IdTicket, command.IdAsignado.Value, cancellationToken);
        }
        else if (command.Accion == AccionesTicket.Resolver)
        {
            if (string.IsNullOrWhiteSpace(command.Solucion) || !command.MinutosSolucion.HasValue
                || command.MinutosSolucion.Value <= 0)
            {
                throw new BusinessException("Resolver un ticket requiere capturar la solucion y el tiempo invertido.");
            }
        }

        await motor.EjecutarAccionAsync(
            "Ticket", command.IdTicket, command.Accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(
            command.IdTicket, command.Accion, command.Solucion, command.MinutosSolucion, cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);
    }
}
