using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record RetirarTarifaNivelCommand(int IdTarifaNivel) : IRequest<Unit>;

public class RetirarTarifaNivelValidator : AbstractValidator<RetirarTarifaNivelCommand>
{
    public RetirarTarifaNivelValidator()
    {
        RuleFor(c => c.IdTarifaNivel).GreaterThan(0);
    }
}

public class RetirarTarifaNivelHandler(
    ICosteoRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarTarifaNivelCommand, Unit>
{
    public async Task<Unit> Handle(RetirarTarifaNivelCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);
        await repositorio.RetirarTarifaNivelAsync(command.IdTarifaNivel, cancellationToken);
        return Unit.Value;
    }
}
