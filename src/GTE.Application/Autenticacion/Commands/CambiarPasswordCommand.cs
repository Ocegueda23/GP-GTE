using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Autenticacion.Commands;

public record CambiarPasswordCommand(string PasswordActual, string PasswordNueva) : IRequest<Unit>;

public class CambiarPasswordValidator : AbstractValidator<CambiarPasswordCommand>
{
    public CambiarPasswordValidator()
    {
        RuleFor(c => c.PasswordNueva).MinimumLength(8)
            .WithMessage("La contraseña nueva debe tener al menos 8 caracteres.");
        RuleFor(c => c).Must(c => c.PasswordNueva != c.PasswordActual)
            .WithMessage("La contraseña nueva debe ser diferente a la actual.");
    }
}

/// <summary>Cambio de contraseña por el propio usuario: exige la contraseña actual.</summary>
public class CambiarPasswordHandler(
    IAutenticacionRepository repositorio,
    IHashPassword hasher,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CambiarPasswordCommand, Unit>
{
    public async Task<Unit> Handle(CambiarPasswordCommand command, CancellationToken cancellationToken)
    {
        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var credenciales = await repositorio.ObtenerCredencialesAsync(usuarioActual.Dominio, cancellationToken)
            ?? throw new NotFoundException("Usuario", usuarioActual.IdUsuario);

        if (credenciales.PasswordHash is null || !hasher.Verificar(command.PasswordActual, credenciales.PasswordHash))
        {
            throw new BusinessException("La contraseña actual no es correcta.");
        }

        await repositorio.EstablecerPasswordAsync(
            usuarioActual.IdUsuario, hasher.Hash(command.PasswordNueva), false, cancellationToken);
        return Unit.Value;
    }
}
