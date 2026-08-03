using FluentValidation;
using GTE.Application.DTOs.Request.Soporte;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Soporte.Commands;

public record EscalarTicketCommand(int IdTicket, EscalarTicketRequest Datos) : IRequest<EscalarTicketResponse>;

public class EscalarTicketValidator : AbstractValidator<EscalarTicketCommand>
{
    public EscalarTicketValidator()
    {
        RuleFor(c => c.IdTicket).GreaterThan(0);
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("Escalar requiere elegir el proyecto destino.");
    }
}

/// <summary>
/// Crea un WorkItem tipo Soporte reutilizando CrearWorkItemCommand (mismo patron que
/// ConvertirSolicitudHandler para la conversion de solicitudes) y lo vincula al
/// ticket. No es una transicion del motor de workflow: el ticket se queda en su
/// estatus actual (ver EstatusTicket.cs, AccionesTicket.Escalar).
/// </summary>
public class EscalarTicketHandler(
    ITicketRepository repositorio,
    IVerificadorPermisos permisos,
    ISender mediator) : IRequestHandler<EscalarTicketCommand, EscalarTicketResponse>
{
    private const int IdTipoWorkItemSoporte = 8;

    public async Task<EscalarTicketResponse> Handle(EscalarTicketCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosTicket.Atender, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);

        if (estado.IdEstatus == EstatusTicket.Cerrado)
        {
            throw new BusinessException("No se puede escalar un ticket Cerrado.");
        }
        if (estado.IdWorkItemDerivado.HasValue)
        {
            throw new BusinessException("El ticket ya tiene un elemento de trabajo vinculado.");
        }

        var titulo = $"Soporte: {estado.Titulo}";
        if (titulo.Length > 200)
        {
            titulo = titulo[..200];
        }

        var creado = await mediator.Send(new CrearWorkItemCommand(new WorkItemCrearRequest
        {
            IdProyecto = command.Datos.IdProyecto,
            IdTipoWorkItem = IdTipoWorkItemSoporte,
            Titulo = titulo,
            Descripcion = estado.Descripcion,
            IdPrioridad = estado.IdPrioridad,
            IdAsignado = command.Datos.IdAsignado,
            FechaCompromiso = command.Datos.FechaCompromiso
        }), cancellationToken);

        await repositorio.EscalarAsync(command.IdTicket, creado.IdWorkItem, cancellationToken);

        return new EscalarTicketResponse { IdWorkItem = creado.IdWorkItem, Folio = creado.Folio };
    }
}
