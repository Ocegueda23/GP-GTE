using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record AsignarRolCommand(int IdUsuario, AsignarRolRequest Datos) : IRequest<IReadOnlyList<RolUsuarioResponse>>;

public class AsignarRolValidator : AbstractValidator<AsignarRolCommand>
{
    public AsignarRolValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
        RuleFor(c => c.Datos.IdRol).GreaterThan(0).WithMessage("El rol es obligatorio.");
    }
}

public class AsignarRolHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<AsignarRolCommand, IReadOnlyList<RolUsuarioResponse>>
{
    public async Task<IReadOnlyList<RolUsuarioResponse>> Handle(AsignarRolCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Roles, null, cancellationToken);

        await repositorio.AsignarRolAsync(
            new RolAsignadoNuevo(command.IdUsuario, command.Datos.IdRol, command.Datos.IdProyecto), cancellationToken);

        return await consultas.ObtenerRolesUsuarioAsync(command.IdUsuario, cancellationToken);
    }
}
