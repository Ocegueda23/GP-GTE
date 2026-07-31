using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarMiembroEquipoCommand(int IdEquipo, int IdEquipoMiembro, MiembroEquipoEditarRequest Datos)
    : IRequest<EquipoDetalleResponse>;

public class ActualizarMiembroEquipoValidator : AbstractValidator<ActualizarMiembroEquipoCommand>
{
    public ActualizarMiembroEquipoValidator()
    {
        RuleFor(c => c.IdEquipo).GreaterThan(0);
        RuleFor(c => c.IdEquipoMiembro).GreaterThan(0);
        RuleFor(c => c.Datos.RolEquipo).MaximumLength(100);
        RuleFor(c => c.Datos.PorcentajeDedicacion).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("El porcentaje de dedicacion debe estar entre 0 (exclusivo) y 100.");
    }
}

public class ActualizarMiembroEquipoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarMiembroEquipoCommand, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(ActualizarMiembroEquipoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        await repositorio.ActualizarMiembroAsync(new MiembroEquipoEdicion(
            command.IdEquipoMiembro, command.Datos.RolEquipo, command.Datos.PorcentajeDedicacion), cancellationToken);

        return await consultas.ObtenerEquipoAsync(command.IdEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", command.IdEquipo);
    }
}
