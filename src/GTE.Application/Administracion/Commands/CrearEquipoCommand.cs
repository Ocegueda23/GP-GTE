using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.CentroMando;
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
        // Lista cerrada: un ambito con typo dejaria al equipo sin su bloque tecnico de
        // indicadores y nadie se enteraria (el motor simplemente no lo evalua).
        RuleFor(c => c.Datos.AmbitoCentroMando)
            .Must(a => a is null || AmbitoCentroMando.EsTecnicoValido(a))
            .WithMessage("El ambito del Centro de Mando no es valido.");
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
            command.Datos.Nombre.Trim(), command.Datos.Descripcion, command.Datos.IdLider,
            command.Datos.AmbitoCentroMando), cancellationToken);

        return await consultas.ObtenerEquipoAsync(idEquipo, cancellationToken)
            ?? throw new NotFoundException("Equipo", idEquipo);
    }
}
