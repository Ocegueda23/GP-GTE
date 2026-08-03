using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record RetirarPresupuestoProyectoCommand(int IdPresupuestoProyecto) : IRequest<Unit>;

public class RetirarPresupuestoProyectoValidator : AbstractValidator<RetirarPresupuestoProyectoCommand>
{
    public RetirarPresupuestoProyectoValidator()
    {
        RuleFor(c => c.IdPresupuestoProyecto).GreaterThan(0);
    }
}

public class RetirarPresupuestoProyectoHandler(
    ICosteoRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarPresupuestoProyectoCommand, Unit>
{
    public async Task<Unit> Handle(RetirarPresupuestoProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);
        await repositorio.RetirarPresupuestoAsync(command.IdPresupuestoProyecto, cancellationToken);
        return Unit.Value;
    }
}
