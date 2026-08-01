namespace GTE.Domain.Autenticacion;

public record CredencialesUsuario(
    int IdUsuario,
    string Dominio,
    string Nombre,
    string? PasswordHash,
    bool RequiereCambioPassword,
    int IntentosFallidos,
    DateTime? BloqueadoHasta,
    bool Activo);

public record RefreshTokenNuevo(int IdUsuario, string TokenHash, DateTime FechaExpiracion, string? IpOrigen);

public record RefreshTokenValido(int IdRefreshToken, int IdUsuario, DateTime FechaExpiracion, bool Revocado);
