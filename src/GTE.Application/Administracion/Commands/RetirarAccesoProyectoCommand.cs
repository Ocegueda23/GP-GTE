using FluentValidation;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record RetirarAccesoProyectoCommand(int IdProyecto, int IdUsuarioRol)
    : IRequest<IReadOnlyList<AccesoProyectoResponse>>;

public class RetirarAccesoProyectoValidator : AbstractValidator<RetirarAccesoProyectoCommand>
{
    public RetirarAccesoProyectoValidator()
    {
        RuleFor(c => c.IdProyecto).GreaterThan(0);
        RuleFor(c => c.IdUsuarioRol).GreaterThan(0);
    }
}

/// <summary>
/// Retira un acceso del proyecto (baja logica de la fila de tblUsuarioRol: la asignacion se
/// conserva como historia). Se valida que la asignacion sea de ESTE proyecto para que desde
/// la pestana de un proyecto no se pueda quitar un rol global ni el acceso de otro proyecto.
/// </summary>
public class RetirarAccesoProyectoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos)
    : IRequestHandler<RetirarAccesoProyectoCommand, IReadOnlyList<AccesoProyectoResponse>>
{
    public async Task<IReadOnlyList<AccesoProyectoResponse>> Handle(
        RetirarAccesoProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(
            PermisosAdministracion.Roles, command.IdProyecto, cancellationToken);

        var vigentes = await consultas.ObtenerAccesosProyectoAsync(command.IdProyecto, cancellationToken);
        if (vigentes.All(a => a.IdUsuarioRol != command.IdUsuarioRol))
        {
            throw new NotFoundException("Acceso", command.IdUsuarioRol);
        }

        await repositorio.RetirarRolAsync(command.IdUsuarioRol, cancellationToken);

        return await consultas.ObtenerAccesosProyectoAsync(command.IdProyecto, cancellationToken);
    }
}
