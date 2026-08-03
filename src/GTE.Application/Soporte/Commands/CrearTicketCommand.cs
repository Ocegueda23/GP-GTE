using FluentValidation;
using GTE.Application.DTOs.Request.Soporte;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Soporte.Commands;

public record CrearTicketCommand(TicketCrearRequest Datos) : IRequest<TicketResponse>;

public class CrearTicketValidator : AbstractValidator<CrearTicketCommand>
{
    public CrearTicketValidator()
    {
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0).WithMessage("La prioridad es obligatoria.");
        RuleFor(c => c.Datos.Descripcion).MaximumLength(4000);
    }
}

/// <summary>
/// Alta de ticket: estatus inicial Nuevo (lo fija el backend), folio de la serie
/// TKT-anio, y fechas limite de SLA calculadas por prioridad con el calendario laboral
/// del horario del SLA (si hay uno configurado para esa prioridad; si no, el ticket se
/// crea sin fechas limite en vez de fallar el alta).
/// </summary>
public class CrearTicketHandler(
    ITicketRepository repositorio,
    ITicketQueryService consultas,
    IGeneradorFolios folios,
    ICalendarioLaboral calendario,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CrearTicketCommand, TicketResponse>
{
    public async Task<TicketResponse> Handle(CrearTicketCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var folio = await folios.GenerarAsync($"TKT-{DateTime.Today.Year}", cancellationToken: cancellationToken);

        var sla = await repositorio.ObtenerSlaVigenteAsync(command.Datos.IdPrioridad, cancellationToken);
        DateTime? fechaLimiteRespuesta = null;
        DateTime? fechaLimiteResolucion = null;
        if (sla is not null)
        {
            var ahora = DateTime.Now;
            fechaLimiteRespuesta = await calendario.SumarMinutosLaboralesAsync(
                ahora, sla.MinutosRespuesta, sla.IdHorario, cancellationToken);
            fechaLimiteResolucion = await calendario.SumarMinutosLaboralesAsync(
                ahora, sla.MinutosResolucion, sla.IdHorario, cancellationToken);
        }

        var idTicket = await repositorio.CrearAsync(new TicketNuevo(
            folio, usuario.IdUsuario, command.Datos.Titulo.Trim(), command.Datos.Descripcion,
            command.Datos.IdCategoriaTicket, command.Datos.IdPrioridad, sla?.IdSla,
            fechaLimiteRespuesta, fechaLimiteResolucion), cancellationToken);

        return await consultas.ObtenerPorIdAsync(idTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", idTicket);
    }
}
