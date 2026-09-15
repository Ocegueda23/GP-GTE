using FluentValidation;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.Interfaces;
using GTE.Domain.Ausencias;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Ausencias.Commands;

public record CambiarEstatusAusenciaCommand(int IdAusencia, string Accion, string? Motivo)
    : IRequest<AusenciaResponse>;

public class CambiarEstatusAusenciaValidator : AbstractValidator<CambiarEstatusAusenciaCommand>
{
    public CambiarEstatusAusenciaValidator()
    {
        RuleFor(c => c.IdAusencia).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().WithMessage("La accion es obligatoria.").MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// APROBAR/RECHAZAR exigen ADM.Ausencias (RECHAZAR ademas motivo); CANCELAR la puede
/// ejecutar el dueño o quien gestione. La transicion la resuelve el motor
/// (dbo.spCambiarEstatus): aqui solo van las reglas que el grafo no conoce.
/// </summary>
public class CambiarEstatusAusenciaHandler(
    IAusenciaRepository repositorio,
    IAusenciaQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario,
    IServicioNotificaciones notificaciones) : IRequestHandler<CambiarEstatusAusenciaCommand, AusenciaResponse>
{
    public async Task<AusenciaResponse> Handle(
        CambiarEstatusAusenciaCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", command.IdAusencia);

        var accion = command.Accion.Trim().ToUpperInvariant();

        if (AccionesAusencia.DeAprobador.Contains(accion))
        {
            await permisos.ExigirPermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken);
        }

        if (accion == AccionesAusencia.Cancelar)
        {
            var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
            var esDueno = usuario is not null && usuario.IdUsuario == estado.IdUsuario;
            if (!esDueno && !await permisos.TienePermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken))
            {
                throw new ForbiddenException("Solo puedes cancelar tus propias ausencias.");
            }
        }

        if (AccionesAusencia.ConMotivo.Contains(accion) && string.IsNullOrWhiteSpace(command.Motivo))
        {
            throw new BusinessException($"La accion {accion} requiere capturar un motivo para la persona.");
        }

        await motor.EjecutarAccionAsync("Ausencia", command.IdAusencia, accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(command.IdAusencia, accion, cancellationToken);

        var ausencia = await consultas.ObtenerPorIdAsync(command.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", command.IdAusencia);

        await NotificarDuenoAsync(accion, command.Motivo, ausencia, cancellationToken);
        return ausencia;
    }

    /// <summary>A12: aprobada o rechazada, la persona se entera; cancelar no notifica (lo hizo ella).</summary>
    private async Task NotificarDuenoAsync(
        string accion, string? motivo, AusenciaResponse ausencia, CancellationToken cancellationToken)
    {
        var periodo = $"Del {ausencia.FechaInicio:dd/MM/yyyy} al {ausencia.FechaFin:dd/MM/yyyy}.";
        var (titulo, mensaje) = accion switch
        {
            AccionesAusencia.Aprobar => ($"Tu ausencia ({ausencia.Tipo}) fue aprobada", periodo),
            AccionesAusencia.Rechazar => ($"Tu ausencia ({ausencia.Tipo}) fue rechazada", motivo ?? periodo),
            _ => (null, null)
        };
        if (titulo is null)
        {
            return;
        }

        await notificaciones.NotificarAsync(
            [ausencia.IdUsuario], titulo, mensaje, "Ausencia", ausencia.IdAusencia,
            "/ausencias", cancellationToken);
    }
}
