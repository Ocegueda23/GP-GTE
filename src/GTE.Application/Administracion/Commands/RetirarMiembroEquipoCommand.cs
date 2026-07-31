using FluentValidation;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarMiembroEquipoCommand(int IdEquipo, int IdEquipoMiembro) : IRequest<EquipoDetalleResponse>;

public class RetirarMiembroEquipoValidator : AbstractValidator<RetirarMiembroEquipoCommand>
{
    public RetirarMiembroEquipoValidator()
    {
        RuleFor(c => c.IdEquipo).GreaterThan(0);
        RuleFor(c => c.IdEquipoMiembro).GreaterThan(0);
    }
}

public class RetirarMiembroEquipoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarMiembroEquipoCommand, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(RetirarMiembroEquipoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        await repositorio.RetirarMiembroAsync(command.IdEquipoMiembro, cancellationToken);

        return await consultas.ObtenerEquipoAsync(command.IdEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", command.IdEquipo);
    }
}
