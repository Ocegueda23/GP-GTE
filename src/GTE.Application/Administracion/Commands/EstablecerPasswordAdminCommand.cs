using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Autenticacion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

/// <summary>Reset de password por un administrador: regresa la nueva password temporal una sola vez.</summary>
public record EstablecerPasswordAdminCommand(int IdUsuario) : IRequest<string>;

public class EstablecerPasswordAdminValidator : AbstractValidator<EstablecerPasswordAdminCommand>
{
    public EstablecerPasswordAdminValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
    }
}

public class EstablecerPasswordAdminHandler(
    IAutenticacionRepository autenticacion,
    IAdministracionQueryService consultas,
    IHashPassword hasher,
    IVerificadorPermisos permisos) : IRequestHandler<EstablecerPasswordAdminCommand, string>
{
    public async Task<string> Handle(EstablecerPasswordAdminCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerUsuarioAsync(command.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuario);

        var passwordTemporal = GeneradorPasswordTemporal.Generar();
        await autenticacion.EstablecerPasswordAsync(
            command.IdUsuario, hasher.Hash(passwordTemporal), true, cancellationToken);

        return passwordTemporal;
    }
}
