using FluentValidation;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Solicitudes;
using MediatR;

namespace GTE.Application.Solicitudes.Commands;

public record CambiarEstatusSolicitudCommand(int IdSolicitud, string Accion, string? Motivo, int? IdProyecto)
    : IRequest<SolicitudResponse>;

public class CambiarEstatusSolicitudValidator : AbstractValidator<CambiarEstatusSolicitudCommand>
{
    public CambiarEstatusSolicitudValidator()
    {
        RuleFor(c => c.IdSolicitud).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().WithMessage("La accion es obligatoria.").MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Transiciones del triage: TOMAR/APROBAR/RECHAZAR/DEVOLVER exigen SOL.Triage;
/// RECHAZAR y DEVOLVER exigen motivo (se notifica al solicitante);
/// APROBAR exige y fija el proyecto destino; CANCELAR solo el solicitante o triage.
/// La conversion tiene su propio comando (ConvertirSolicitudCommand).
/// </summary>
public class CambiarEstatusSolicitudHandler(
    ISolicitudRepository repositorio,
    ISolicitudQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CambiarEstatusSolicitudCommand, SolicitudResponse>
{
    public async Task<SolicitudResponse> Handle(
        CambiarEstatusSolicitudCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);

        if (command.Accion == AccionesSolicitud.Convertir)
        {
            throw new BusinessException("La conversion se ejecuta con el endpoint de convertir (requiere el desglose de items).");
        }

        if (AccionesSolicitud.DeTriage.Contains(command.Accion))
        {
            await permisos.ExigirPermisoAsync(PermisosSolicitud.Triage, null, cancellationToken);
        }

        if (command.Accion == AccionesSolicitud.Cancelar)
        {
            var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
            var esSolicitante = usuario is not null && usuario.IdUsuario == estado.IdSolicitante;
            if (!esSolicitante && !await permisos.TienePermisoAsync(PermisosSolicitud.Triage, null, cancellationToken))
            {
                throw new ForbiddenException("Solo el solicitante puede cancelar su solicitud.");
            }
        }

        if (AccionesSolicitud.ConMotivo.Contains(command.Accion) && string.IsNullOrWhiteSpace(command.Motivo))
        {
            throw new BusinessException($"La accion {command.Accion} requiere capturar un motivo para el solicitante.");
        }

        if (command.Accion == AccionesSolicitud.Aprobar)
        {
            if (!command.IdProyecto.HasValue)
            {
                throw new BusinessException("Aprobar una solicitud requiere elegir el proyecto destino.");
            }
            await repositorio.AsignarProyectoAsync(command.IdSolicitud, command.IdProyecto.Value, cancellationToken);
        }

        await motor.EjecutarAccionAsync(
            "Solicitud", command.IdSolicitud, command.Accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(command.IdSolicitud, command.Accion, cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);
    }
}
