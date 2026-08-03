using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record RetirarObjetivoOkrCommand(int IdObjetivoOkr) : IRequest<Unit>;

public class RetirarObjetivoOkrValidator : AbstractValidator<RetirarObjetivoOkrCommand>
{
    public RetirarObjetivoOkrValidator()
    {
        RuleFor(c => c.IdObjetivoOkr).GreaterThan(0);
    }
}

public class RetirarObjetivoOkrHandler(
    IOkrRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarObjetivoOkrCommand, Unit>
{
    public async Task<Unit> Handle(RetirarObjetivoOkrCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);
        await repositorio.RetirarObjetivoAsync(command.IdObjetivoOkr, cancellationToken);
        return Unit.Value;
    }
}
