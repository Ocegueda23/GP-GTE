using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record AsignarAccesoProyectoCommand(int IdProyecto, AsignarAccesoProyectoRequest Datos)
    : IRequest<IReadOnlyList<AccesoProyectoResponse>>;

public class AsignarAccesoProyectoValidator : AbstractValidator<AsignarAccesoProyectoCommand>
{
    public AsignarAccesoProyectoValidator()
    {
        RuleFor(c => c.IdProyecto).GreaterThan(0);
        RuleFor(c => c.Datos.IdUsuario).GreaterThan(0).WithMessage("La persona es obligatoria.");
        RuleFor(c => c.Datos.IdRol).GreaterThan(0).WithMessage("El rol es obligatorio.");
    }
}

/// <summary>
/// Da a una persona un rol acotado a este proyecto. Escribe la MISMA tabla que la pantalla
/// de usuarios (tblUsuarioRol con IdProyecto), no un catalogo paralelo de permisos por
/// proyecto: hay una sola fuente de verdad del acceso. El alcance lo fija el backend con el
/// id de la ruta, asi que desde aqui es imposible crear un acceso global.
/// </summary>
public class AsignarAccesoProyectoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos)
    : IRequestHandler<AsignarAccesoProyectoCommand, IReadOnlyList<AccesoProyectoResponse>>
{
    public async Task<IReadOnlyList<AccesoProyectoResponse>> Handle(
        AsignarAccesoProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(
            PermisosAdministracion.Roles, command.IdProyecto, cancellationToken);

        _ = await repositorio.ObtenerEstadoProyectoAsync(command.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.IdProyecto);

        var usuario = await consultas.ObtenerUsuarioAsync(command.Datos.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.Datos.IdUsuario);
        if (!usuario.Activo)
        {
            throw new BusinessException("La persona esta dada de baja; no se le pueden dar accesos.");
        }

        var roles = await consultas.ObtenerRolesAsync(cancellationToken);
        var rol = roles.FirstOrDefault(r => r.IdRol == command.Datos.IdRol)
            ?? throw new NotFoundException("Rol", command.Datos.IdRol);

        var vigentes = await consultas.ObtenerAccesosProyectoAsync(command.IdProyecto, cancellationToken);
        if (vigentes.Any(a => a.IdUsuario == command.Datos.IdUsuario && a.IdRol == command.Datos.IdRol))
        {
            throw new ConflictException(
                $"{usuario.Nombre} ya tiene el rol {rol.Nombre} en este proyecto.",
                new { idUsuario = command.Datos.IdUsuario, idRol = command.Datos.IdRol });
        }

        await repositorio.AsignarRolAsync(
            new RolAsignadoNuevo(command.Datos.IdUsuario, command.Datos.IdRol, command.IdProyecto),
            cancellationToken);

        return await consultas.ObtenerAccesosProyectoAsync(command.IdProyecto, cancellationToken);
    }
}
