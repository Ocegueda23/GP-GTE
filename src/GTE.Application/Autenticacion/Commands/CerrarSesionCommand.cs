using GTE.Application.Interfaces;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Autenticacion.Commands;

public record CerrarSesionCommand(string? RefreshTokenCrudo) : IRequest<Unit>;

public class CerrarSesionHandler(
    IAutenticacionRepository repositorio, IEmisorTokenSesion emisor) : IRequestHandler<CerrarSesionCommand, Unit>
{
    public async Task<Unit> Handle(CerrarSesionCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshTokenCrudo))
        {
            var hash = emisor.HashRefreshToken(command.RefreshTokenCrudo);
            var existente = await repositorio.ObtenerRefreshTokenAsync(hash, cancellationToken);
            if (existente is not null && !existente.Revocado)
            {
                await repositorio.RevocarRefreshTokenAsync(existente.IdRefreshToken, cancellationToken);
            }
        }
        return Unit.Value;
    }
}
