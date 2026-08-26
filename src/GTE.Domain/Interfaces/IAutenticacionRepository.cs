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

    /// <summary>Bitacora de INICIAR_SUPLANTACION/TERMINAR_SUPLANTACION (doble identidad via AuditContext).</summary>
    Task RegistrarBitacoraSuplantacionAsync(
        int idUsuarioSuplantado, string accion, string? detalle, CancellationToken cancellationToken = default);
}
