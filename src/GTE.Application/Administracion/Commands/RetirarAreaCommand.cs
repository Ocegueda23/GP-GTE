using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarAreaCommand(int IdArea) : IRequest<Unit>;

public class RetirarAreaValidator : AbstractValidator<RetirarAreaCommand>
{
    public RetirarAreaValidator()
    {
        RuleFor(c => c.IdArea).GreaterThan(0);
    }
}

public class RetirarAreaHandler(
    IAdministracionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarAreaCommand, Unit>
{
    public async Task<Unit> Handle(RetirarAreaCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);
        await repositorio.RetirarAreaAsync(command.IdArea, cancellationToken);
        return Unit.Value;
    }
}
