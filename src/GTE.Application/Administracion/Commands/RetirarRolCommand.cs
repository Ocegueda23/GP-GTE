using FluentValidation;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarRolCommand(int IdUsuario, int IdUsuarioRol) : IRequest<IReadOnlyList<RolUsuarioResponse>>;

public class RetirarRolValidator : AbstractValidator<RetirarRolCommand>
{
    public RetirarRolValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
        RuleFor(c => c.IdUsuarioRol).GreaterThan(0);
    }
}

public class RetirarRolHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarRolCommand, IReadOnlyList<RolUsuarioResponse>>
{
    public async Task<IReadOnlyList<RolUsuarioResponse>> Handle(RetirarRolCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Roles, null, cancellationToken);

        await repositorio.RetirarRolAsync(command.IdUsuarioRol, cancellationToken);

        return await consultas.ObtenerRolesUsuarioAsync(command.IdUsuario, cancellationToken);
    }
}
