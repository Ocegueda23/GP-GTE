using FluentValidation;
using GTE.Application.DTOs.Request.Solicitudes;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Solicitudes;
using MediatR;

namespace GTE.Application.Solicitudes.Commands;

public record CrearSolicitudCommand(SolicitudCrearRequest Datos) : IRequest<SolicitudResponse>;

public class CrearSolicitudValidator : AbstractValidator<CrearSolicitudCommand>
{
    public CrearSolicitudValidator()
    {
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.IdTipoSolicitud).GreaterThan(0).WithMessage("El tipo de solicitud es obligatorio.");
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0).WithMessage("La prioridad es obligatoria.");
        RuleFor(c => c.Datos.JustificacionNegocio).MaximumLength(500);
        RuleFor(c => c.Datos.FechaDeseada)
            .Must(f => !f.HasValue || f.Value.Date >= DateTime.Today)
            .WithMessage("La fecha deseada no puede ser anterior a hoy.");
    }
}

/// <summary>
/// El portal crea y ENVIA en un solo paso: alta en Borrador (estatus lo fija el
/// backend, historial ALTA), folio de la serie SOL-anio y transicion ENVIAR
/// por el motor. El solicitante es el usuario del token.
/// </summary>
public class CrearSolicitudHandler(
    ISolicitudRepository repositorio,
    ISolicitudQueryService consultas,
    IGeneradorFolios folios,
    IMotorWorkflow motor,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CrearSolicitudCommand, SolicitudResponse>
{
    public async Task<SolicitudResponse> Handle(CrearSolicitudCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var folio = await folios.GenerarAsync($"SOL-{DateTime.Today.Year}", cancellationToken: cancellationToken);

        var idSolicitud = await repositorio.CrearAsync(new SolicitudNueva(
            folio, usuario.IdUsuario, command.Datos.Titulo.Trim(), command.Datos.Descripcion,
            command.Datos.IdTipoSolicitud, command.Datos.IdPrioridad,
            command.Datos.FechaDeseada, command.Datos.JustificacionNegocio,
            command.Datos.IdUsuarioSolicitante), cancellationToken);

        await motor.EjecutarAccionAsync(
            "Solicitud", idSolicitud, AccionesSolicitud.Enviar, null, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(idSolicitud, AccionesSolicitud.Enviar, cancellationToken);

        return await consultas.ObtenerPorIdAsync(idSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", idSolicitud);
    }
}
