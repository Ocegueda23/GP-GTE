using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarPuestoCommand(int IdPuesto) : IRequest<Unit>;

public class RetirarPuestoValidator : AbstractValidator<RetirarPuestoCommand>
{
    public RetirarPuestoValidator()
    {
        RuleFor(c => c.IdPuesto).GreaterThan(0);
    }
}

public class RetirarPuestoHandler(
    IAdministracionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarPuestoCommand, Unit>
{
    public async Task<Unit> Handle(RetirarPuestoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);
        await repositorio.RetirarPuestoAsync(command.IdPuesto, cancellationToken);
        return Unit.Value;
    }
}
