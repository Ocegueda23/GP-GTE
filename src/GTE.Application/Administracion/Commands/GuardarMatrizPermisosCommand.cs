using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record GuardarMatrizPermisosCommand(int IdRol, GuardarMatrizPermisosRequest Datos) : IRequest<MatrizPermisosResponse>;

public class GuardarMatrizPermisosValidator : AbstractValidator<GuardarMatrizPermisosCommand>
{
    public GuardarMatrizPermisosValidator()
    {
        RuleFor(c => c.IdRol).GreaterThan(0);
    }
}

/// <summary>
/// Guarda la matriz rol-permiso completa en una sola llamada (reemplazo total de
/// dbo.tblRolPermiso para el rol): corrige el round-trip por fila de la version anterior.
/// </summary>
public class GuardarMatrizPermisosHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<GuardarMatrizPermisosCommand, MatrizPermisosResponse>
{
    public async Task<MatrizPermisosResponse> Handle(GuardarMatrizPermisosCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Roles, null, cancellationToken);

        var idsUnicos = command.Datos.IdsPermiso.Distinct().ToList();
        await repositorio.GuardarMatrizPermisosAsync(command.IdRol, idsUnicos, cancellationToken);

        return await consultas.ObtenerMatrizPermisosAsync(command.IdRol, cancellationToken)
            ?? throw new NotFoundException("Rol", command.IdRol);
    }
}
