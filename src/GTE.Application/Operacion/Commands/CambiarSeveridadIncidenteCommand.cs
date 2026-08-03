using FluentValidation;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record CambiarSeveridadIncidenteCommand(int IdIncidente, CambiarSeveridadIncidenteRequest Datos)
    : IRequest<IncidenteResponse>;

public class CambiarSeveridadIncidenteValidator : AbstractValidator<CambiarSeveridadIncidenteCommand>
{
    public CambiarSeveridadIncidenteValidator()
    {
        RuleFor(c => c.IdIncidente).GreaterThan(0);
        RuleFor(c => c.Datos.IdSeveridad).GreaterThan(0);
        RuleFor(c => c.Datos.Motivo).NotEmpty().WithMessage("El motivo es obligatorio.").MaximumLength(500);
    }
}

/// <summary>
/// RN-OPS-03: degradar/escalar severidad solo con motivo registrado. No es una
/// transicion de tblTransicion (el proceso Incidente no tiene estatus de severidad) --
/// es una accion de negocio aparte, con bitacora.
/// </summary>
public class CambiarSeveridadIncidenteHandler(
    IIncidenteRepository repositorio,
    IIncidenteQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarSeveridadIncidenteCommand, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(CambiarSeveridadIncidenteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);

        _ = await repositorio.ObtenerEstadoAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);

        await repositorio.CambiarSeveridadAsync(
            command.IdIncidente, command.Datos.IdSeveridad, command.Datos.Motivo.Trim(), cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);
    }
}
