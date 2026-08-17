using FluentValidation;
using GTE.Application.DTOs.Request.Solicitudes;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Solicitudes;
using MediatR;

namespace GTE.Application.Solicitudes.Commands;

public record ActualizarSolicitudCommand(int IdSolicitud, SolicitudEditarRequest Datos) : IRequest<SolicitudResponse>;

public class ActualizarSolicitudValidator : AbstractValidator<ActualizarSolicitudCommand>
{
    public ActualizarSolicitudValidator()
    {
        RuleFor(c => c.IdSolicitud).GreaterThan(0);
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
/// Edita titulo/descripcion/tipo/prioridad/fecha deseada/justificacion mientras la solicitud
/// sigue activa en revision (Enviada/EnAnalisis/Aprobada) -- una vez Convertida, Rechazada o
/// Cancelada ya no admite cambios. Mismo gate de "ajeno" que el resto del sistema: el propio
/// solicitante siempre puede editar la suya, cualquier otro necesita SOL.Triage.
/// </summary>
public class ActualizarSolicitudHandler(
    ISolicitudRepository repositorio,
    ISolicitudQueryService consultas,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ActualizarSolicitudCommand, SolicitudResponse>
{
    private static readonly int[] EstatusEditables =
    [
        EstatusSolicitud.Enviada, EstatusSolicitud.EnAnalisis, EstatusSolicitud.Aprobada
    ];

    public async Task<SolicitudResponse> Handle(ActualizarSolicitudCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);

        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var esAjena = estado.IdSolicitante != usuarioActual.IdUsuario;
        if (esAjena)
        {
            await permisos.ExigirPermisoAsync(PermisosSolicitud.Triage, null, cancellationToken);
        }

        if (!EstatusEditables.Contains(estado.IdEstatus))
        {
            throw new BusinessException("La solicitud ya no admite cambios en su estatus actual.");
        }

        var descripcion = string.IsNullOrWhiteSpace(command.Datos.Descripcion)
            ? null : sanitizador.Sanitizar(command.Datos.Descripcion);

        await repositorio.ActualizarAsync(new SolicitudEdicion(
            command.IdSolicitud, command.Datos.Titulo.Trim(), descripcion, command.Datos.IdTipoSolicitud,
            command.Datos.IdPrioridad, command.Datos.FechaDeseada, command.Datos.JustificacionNegocio,
            command.Datos.IdUsuarioSolicitante), cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);
    }
}
