using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarEquipoCommand(int IdEquipo, EquipoEditarRequest Datos) : IRequest<EquipoDetalleResponse>;

public class ActualizarEquipoValidator : AbstractValidator<ActualizarEquipoCommand>
{
    public ActualizarEquipoValidator()
    {
        RuleFor(c => c.IdEquipo).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del equipo es obligatorio.").MaximumLength(100);
        RuleFor(c => c.Datos.Descripcion).MaximumLength(500);
    }
}

public class ActualizarEquipoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarEquipoCommand, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(ActualizarEquipoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerEquipoAsync(command.IdEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", command.IdEquipo);

        await repositorio.ActualizarEquipoAsync(new EquipoEdicion(
            command.IdEquipo, command.Datos.Nombre.Trim(), command.Datos.Descripcion, command.Datos.IdLider),
            cancellationToken);

        return await consultas.ObtenerEquipoAsync(command.IdEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", command.IdEquipo);
    }
}
