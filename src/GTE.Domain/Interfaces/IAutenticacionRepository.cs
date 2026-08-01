using GTE.Domain.Autenticacion;

namespace GTE.Domain.Interfaces;

public interface IAutenticacionRepository
{
    Task<CredencialesUsuario?> ObtenerCredencialesAsync(string dominio, CancellationToken cancellationToken = default);
    Task RegistrarIntentoFallidoAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task ResetearIntentosAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task EstablecerPasswordAsync(int idUsuario, string passwordHash, bool requiereCambio, CancellationToken cancellationToken = default);

    Task<int> GuardarRefreshTokenAsync(RefreshTokenNuevo datos, CancellationToken cancellationToken = default);
    Task<RefreshTokenValido?> ObtenerRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevocarRefreshTokenAsync(int idRefreshToken, CancellationToken cancellationToken = default);
    Task RevocarTodosLosRefreshTokensAsync(int idUsuario, CancellationToken cancellationToken = default);
}
