using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record AgregarMiembroEquipoCommand(int IdEquipo, MiembroEquipoCrearRequest Datos) : IRequest<EquipoDetalleResponse>;

public class AgregarMiembroEquipoValidator : AbstractValidator<AgregarMiembroEquipoCommand>
{
    public AgregarMiembroEquipoValidator()
    {
        RuleFor(c => c.IdEquipo).GreaterThan(0);
        RuleFor(c => c.Datos.IdUsuario).GreaterThan(0).WithMessage("El usuario es obligatorio.");
        RuleFor(c => c.Datos.RolEquipo).MaximumLength(100);
        RuleFor(c => c.Datos.PorcentajeDedicacion).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("El porcentaje de dedicacion debe estar entre 0 (exclusivo) y 100.");
    }
}

public class AgregarMiembroEquipoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<AgregarMiembroEquipoCommand, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(AgregarMiembroEquipoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        await repositorio.AgregarMiembroAsync(new MiembroEquipoNuevo(
            command.IdEquipo, command.Datos.IdUsuario, command.Datos.RolEquipo, command.Datos.PorcentajeDedicacion),
            cancellationToken);

        return await consultas.ObtenerEquipoAsync(command.IdEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", command.IdEquipo);
    }
}
