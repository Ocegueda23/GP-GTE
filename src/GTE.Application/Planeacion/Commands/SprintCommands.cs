using FluentValidation;
using GTE.Application.DTOs.Request.Planeacion;
using GTE.Application.DTOs.Responses.Planeacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Planeacion;
using MediatR;

namespace GTE.Application.Planeacion.Commands;

public record CrearSprintCommand(SprintCrearRequest Datos) : IRequest<SprintResponse>;

public class CrearSprintValidator : AbstractValidator<CrearSprintCommand>
{
    public CrearSprintValidator()
    {
        RuleFor(c => c.Datos.IdEquipo).GreaterThan(0).WithMessage("El equipo es obligatorio.");
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del sprint es obligatorio.")
            .MaximumLength(100);
        RuleFor(c => c.Datos.Objetivo).MaximumLength(500);
        RuleFor(c => c.Datos.FechaFin).GreaterThan(c => c.Datos.FechaInicio)
            .WithMessage("La fecha de fin debe ser posterior a la de inicio.");
    }
}

public class CrearSprintHandler(
    IPlaneacionRepository repositorio,
    IPlaneacionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearSprintCommand, SprintResponse>
{
    public async Task<SprintResponse> Handle(CrearSprintCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosPlaneacion.GestionarSprints, null, cancellationToken);

        var idSprint = await repositorio.CrearSprintAsync(new SprintNuevo(
            command.Datos.IdEquipo, command.Datos.Nombre.Trim(), command.Datos.Objetivo,
            command.Datos.FechaInicio, command.Datos.FechaFin), cancellationToken);

        return await consultas.ObtenerSprintAsync(idSprint, cancellationToken)
            ?? throw new NotFoundException("Sprint", idSprint);
    }
}

public record CambiarEstatusSprintCommand(int IdSprint, string Accion, string? DestinoItemsAbiertos)
    : IRequest<SprintResponse>;

public class CambiarEstatusSprintValidator : AbstractValidator<CambiarEstatusSprintCommand>
{
    public CambiarEstatusSprintValidator()
    {
        RuleFor(c => c.IdSprint).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().MaximumLength(50);
    }
}

/// <summary>
/// ACTIVAR y CERRAR del sprint.
/// Regla: solo un sprint Activo por equipo (409 accionable si ya hay otro).
/// RN-PLA-02: al cerrar, los elementos abiertos se reubican en el backlog o en
/// el siguiente sprint planeado, segun lo que pida quien cierra.
/// </summary>
public class CambiarEstatusSprintHandler(
    IPlaneacionRepository repositorio,
    IPlaneacionQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarEstatusSprintCommand, SprintResponse>
{
    public async Task<SprintResponse> Handle(
        CambiarEstatusSprintCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosPlaneacion.GestionarSprints, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoSprintAsync(command.IdSprint, cancellationToken)
            ?? throw new NotFoundException("Sprint", command.IdSprint);

        if (command.Accion == AccionesSprint.Activar)
        {
            var otroActivo = await repositorio.ObtenerSprintActivoAsync(
                estado.IdEquipo, command.IdSprint, cancellationToken);
            if (otroActivo.HasValue)
            {
                var activo = await consultas.ObtenerSprintAsync(otroActivo.Value, cancellationToken);
                throw new ConflictException(
                    $"El equipo ya tiene un sprint activo ({activo?.Nombre}). Cierralo antes de activar otro.",
                    new { idSprintActivo = otroActivo.Value, nombre = activo?.Nombre });
            }
        }

        int? idSprintDestino = null;
        if (command.Accion == AccionesSprint.Cerrar)
        {
            var destino = ResolverDestino(command.DestinoItemsAbiertos);
            if (destino == Domain.Planeacion.DestinoItemsAbiertos.SiguienteSprint)
            {
                idSprintDestino = await repositorio.ObtenerSiguienteSprintPlaneadoAsync(
                    estado.IdEquipo, command.IdSprint, cancellationToken)
                    ?? throw new BusinessException(
                        "No hay un sprint planeado al que mover los elementos abiertos. Crealo primero o envialos al backlog.");
            }
        }

        await motor.EjecutarAccionAsync(
            "Sprint", command.IdSprint, command.Accion, null, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionSprintAsync(command.IdSprint, command.Accion, cancellationToken);

        if (command.Accion == AccionesSprint.Cerrar)
        {
            await repositorio.MoverItemsAbiertosAsync(command.IdSprint, idSprintDestino, cancellationToken);
        }

        return await consultas.ObtenerSprintAsync(command.IdSprint, cancellationToken)
            ?? throw new NotFoundException("Sprint", command.IdSprint);
    }

    private static DestinoItemsAbiertos ResolverDestino(string? valor)
    {
        return string.Equals(valor, "SiguienteSprint", StringComparison.OrdinalIgnoreCase)
            ? Domain.Planeacion.DestinoItemsAbiertos.SiguienteSprint
            : Domain.Planeacion.DestinoItemsAbiertos.Backlog;
    }
}
