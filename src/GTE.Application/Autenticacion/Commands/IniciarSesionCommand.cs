using FluentValidation;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Domain.Autenticacion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Autenticacion.Commands;

public record IniciarSesionCommand(string Dominio, string Password) : IRequest<LoginResultado>;

public record LoginResultado(
    string Token,
    DateTime Expira,
    SesionResponse Sesion,
    bool RequiereCambioPassword,
    string RefreshTokenCrudo,
    DateTime RefreshExpira);

public class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(c => c.Dominio).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

/// <summary>
/// Login propio de GTE (sin proveedor externo): verifica la contraseña con BCrypt y
/// emite el mismo JWT que ya usa el emisor de desarrollo. El mensaje de error es
/// generico tanto si el usuario no existe como si la contraseña es incorrecta, para
/// no revelar cuentas validas (enumeracion de usuarios).
/// </summary>
public class IniciarSesionHandler(
    IAutenticacionRepository repositorio,
    IHashPassword hasher,
    IEmisorTokenSesion emisor,
    ISesionQueryService sesiones) : IRequestHandler<IniciarSesionCommand, LoginResultado>
{
    private const string MensajeInvalido = "Usuario o contraseña incorrectos.";

    public async Task<LoginResultado> Handle(IniciarSesionCommand command, CancellationToken cancellationToken)
    {
        var credenciales = await repositorio.ObtenerCredencialesAsync(command.Dominio.Trim(), cancellationToken);

        if (credenciales is null || !credenciales.Activo || credenciales.PasswordHash is null)
        {
            throw new BusinessException(MensajeInvalido);
        }

        if (credenciales.BloqueadoHasta.HasValue && credenciales.BloqueadoHasta.Value > DateTime.Now)
        {
            throw new ForbiddenException(
                $"Cuenta bloqueada temporalmente. Intenta de nuevo despues de las {credenciales.BloqueadoHasta.Value:HH:mm}.");
        }

        if (!hasher.Verificar(command.Password, credenciales.PasswordHash))
        {
            await repositorio.RegistrarIntentoFallidoAsync(credenciales.IdUsuario, cancellationToken);
            throw new BusinessException(MensajeInvalido);
        }

        await repositorio.ResetearIntentosAsync(credenciales.IdUsuario, cancellationToken);

        var sesion = await sesiones.ObtenerSesionAsync(credenciales.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", credenciales.IdUsuario);

        var (token, expira) = emisor.EmitirTokenAcceso(sesion);
        var refresh = emisor.GenerarRefreshToken();
        await repositorio.GuardarRefreshTokenAsync(
            new RefreshTokenNuevo(credenciales.IdUsuario, refresh.TokenHash, refresh.Expira, null), cancellationToken);

        return new LoginResultado(
            token, expira, sesion, credenciales.RequiereCambioPassword, refresh.TokenCrudo, refresh.Expira);
    }
}
