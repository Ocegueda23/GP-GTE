using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearEquipoCommand(EquipoCrearRequest Datos) : IRequest<EquipoDetalleResponse>;

public class CrearEquipoValidator : AbstractValidator<CrearEquipoCommand>
{
    public CrearEquipoValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del equipo es obligatorio.").MaximumLength(100);
        RuleFor(c => c.Datos.Descripcion).MaximumLength(500);
    }
}

public class CrearEquipoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearEquipoCommand, EquipoDetalleResponse>
{
    public async Task<EquipoDetalleResponse> Handle(CrearEquipoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idEquipo = await repositorio.CrearEquipoAsync(new EquipoNuevo(
            command.Datos.Nombre.Trim(), command.Datos.Descripcion, command.Datos.IdLider), cancellationToken);

        return await consultas.ObtenerEquipoAsync(idEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", idEquipo);
    }
}
