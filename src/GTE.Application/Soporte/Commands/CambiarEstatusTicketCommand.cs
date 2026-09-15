using System.Net;
using FluentValidation;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.Interfaces;
using GTE.Domain.Comentarios;
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
    IVerificadorPermisos permisos,
    IComentarioRepository comentarios,
    IServicioNotificaciones notificaciones) : IRequestHandler<CambiarEstatusTicketCommand, TicketResponse>
{
    public async Task<TicketResponse> Handle(CambiarEstatusTicketCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdTicket, cancellationToken)
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

        // El horario del SLA va al motor para que dbo.spCambiarEstatus materialice los
        // minutos laborales del intervalo que cierra: de ahi sale el tiempo de atencion.
        await motor.EjecutarAccionAsync(
            "Ticket", command.IdTicket, command.Accion, command.Motivo, estado.IdHorario, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(
            command.IdTicket, command.Accion, command.Solucion, command.MinutosSolucion, cancellationToken);

        if (command.Accion == AccionesTicket.EsperarUsuario && !string.IsNullOrWhiteSpace(command.Motivo))
        {
            await AvisarEsperaUsuarioAsync(estado, command.Motivo, cancellationToken);
        }

        return await consultas.ObtenerPorIdAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);
    }

    /// <summary>
    /// El motivo de ESPERAR_USUARIO se publica tambien como comentario (visible en el hilo
    /// del ticket para el solicitante y el agente, no solo en el historial de estatus) y se
    /// notifica al solicitante -- sin esto, nadie se enteraba de que se esperaba respuesta.
    /// </summary>
    private async Task AvisarEsperaUsuarioAsync(
        EstadoTicket estado, string motivo, CancellationToken cancellationToken)
    {
        var html = $"<p>{WebUtility.HtmlEncode(motivo)}</p>";
        await comentarios.CrearAsync(new ComentarioNuevo("Ticket", estado.IdTicket, html, null), cancellationToken);

        await notificaciones.NotificarAsync(
            [estado.IdSolicitante],
            $"Tu ticket {estado.Folio} esta en espera de tu respuesta",
            motivo,
            "Ticket", estado.IdTicket, $"/tickets/{estado.Folio}", cancellationToken);
    }
}
