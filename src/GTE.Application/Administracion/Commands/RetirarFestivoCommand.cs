using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarFestivoCommand(int IdDiaFestivo) : IRequest<Unit>;

public class RetirarFestivoValidator : AbstractValidator<RetirarFestivoCommand>
{
    public RetirarFestivoValidator()
    {
        RuleFor(c => c.IdDiaFestivo).GreaterThan(0);
    }
}

public class RetirarFestivoHandler(
    IAdministracionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarFestivoCommand, Unit>
{
    public async Task<Unit> Handle(RetirarFestivoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);
        await repositorio.RetirarFestivoAsync(command.IdDiaFestivo, cancellationToken);
        return Unit.Value;
    }
}
