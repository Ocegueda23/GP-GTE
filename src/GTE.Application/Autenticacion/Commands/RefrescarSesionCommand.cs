using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Autenticacion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Autenticacion.Commands;

public record RefrescarSesionCommand(string RefreshTokenCrudo) : IRequest<LoginResultado>;

public class RefrescarSesionValidator : AbstractValidator<RefrescarSesionCommand>
{
    public RefrescarSesionValidator()
    {
        RuleFor(c => c.RefreshTokenCrudo).NotEmpty();
    }
}

/// <summary>
/// Rota el refresh token en cada uso. Si el token presentado ya estaba revocado
/// (reuso de un token viejo, indicio de robo), se revocan TODOS los refresh tokens
/// del usuario como respuesta de seguridad.
/// </summary>
public class RefrescarSesionHandler(
    IAutenticacionRepository repositorio,
    IEmisorTokenSesion emisor,
    ISesionQueryService sesiones) : IRequestHandler<RefrescarSesionCommand, LoginResultado>
{
    public async Task<LoginResultado> Handle(RefrescarSesionCommand command, CancellationToken cancellationToken)
    {
        var hash = emisor.HashRefreshToken(command.RefreshTokenCrudo);
        var existente = await repositorio.ObtenerRefreshTokenAsync(hash, cancellationToken);

        if (existente is null || existente.FechaExpiracion <= DateTime.Now)
        {
            throw new ForbiddenException("La sesion expiro. Inicia sesion de nuevo.");
        }

        if (existente.Revocado)
        {
            await repositorio.RevocarTodosLosRefreshTokensAsync(existente.IdUsuario, cancellationToken);
            throw new ForbiddenException("La sesion ya no es valida. Inicia sesion de nuevo.");
        }

        await repositorio.RevocarRefreshTokenAsync(existente.IdRefreshToken, cancellationToken);

        var sesion = await sesiones.ObtenerSesionAsync(existente.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", existente.IdUsuario);

        var (token, expira) = emisor.EmitirTokenAcceso(sesion);
        var refresh = emisor.GenerarRefreshToken();
        await repositorio.GuardarRefreshTokenAsync(
            new RefreshTokenNuevo(existente.IdUsuario, refresh.TokenHash, refresh.Expira, null), cancellationToken);

        return new LoginResultado(token, expira, sesion, false, refresh.TokenCrudo, refresh.Expira);
    }
}
