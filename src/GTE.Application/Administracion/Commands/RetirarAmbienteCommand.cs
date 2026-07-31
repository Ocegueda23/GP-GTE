using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarAmbienteCommand(int IdAmbiente) : IRequest<Unit>;

public class RetirarAmbienteValidator : AbstractValidator<RetirarAmbienteCommand>
{
    public RetirarAmbienteValidator()
    {
        RuleFor(c => c.IdAmbiente).GreaterThan(0);
    }
}

public class RetirarAmbienteHandler(
    IAdministracionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarAmbienteCommand, Unit>
{
    public async Task<Unit> Handle(RetirarAmbienteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);
        await repositorio.RetirarAmbienteAsync(command.IdAmbiente, cancellationToken);
        return Unit.Value;
    }
}
