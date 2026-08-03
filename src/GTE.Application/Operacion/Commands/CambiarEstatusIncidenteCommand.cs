using FluentValidation;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record CambiarEstatusIncidenteCommand(int IdIncidente, string Accion, string? Motivo)
    : IRequest<IncidenteResponse>;

public class CambiarEstatusIncidenteValidator : AbstractValidator<CambiarEstatusIncidenteCommand>
{
    public CambiarEstatusIncidenteValidator()
    {
        RuleFor(c => c.IdIncidente).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().WithMessage("La accion es obligatoria.").MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Toda transicion del proceso Incidente exige INC.Gestionar. RN-OPS-02: CERRAR con
/// severidad S1/S2 exige CausaRaiz ya capturada (via ActualizarIncidenteCommand antes de
/// cerrar). FechaResolucion la fija el repositorio en RESOLVER
/// (AplicarEfectosTransicionAsync).
/// </summary>
public class CambiarEstatusIncidenteHandler(
    IIncidenteRepository repositorio,
    IIncidenteQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarEstatusIncidenteCommand, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(CambiarEstatusIncidenteCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);

        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);

        if (command.Accion == AccionesIncidente.Cerrar
            && (estado.IdSeveridad == Severidad.S1Critica || estado.IdSeveridad == Severidad.S2Alta)
            && string.IsNullOrWhiteSpace(estado.CausaRaiz))
        {
            throw new BusinessException("Cerrar un incidente S1/S2 requiere capturar la causa raiz.");
        }

        await motor.EjecutarAccionAsync(
            "Incidente", command.IdIncidente, command.Accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(command.IdIncidente, command.Accion, cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);
    }
}
