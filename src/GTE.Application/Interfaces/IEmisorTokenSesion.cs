using GTE.Application.DTOs.Responses.Seguridad;

namespace GTE.Application.Interfaces;

public record RefreshTokenGenerado(string TokenCrudo, string TokenHash, DateTime Expira);

/// <summary>
/// Clave de firma HMAC compartida por todo el sistema (atajo de desarrollo y login real).
/// Se guarda como bytes crudos (no como SymmetricSecurityKey) para que este contrato viva
/// en Application sin arrastrar el paquete de IdentityModel hasta aqui.
/// </summary>
public record ClaveFirmaGte(byte[] Clave, string Issuer, string Audience, int MinutosVigenciaAcceso);

/// <summary>
/// Emisor unico del JWT propio de GTE (mismo mecanismo para el atajo de desarrollo y
/// para el login real): centraliza la forma del token para que ambos caminos produzcan
/// exactamente los mismos reclamos.
/// </summary>
public interface IEmisorTokenSesion
{
    (string Token, DateTime Expira) EmitirTokenAcceso(SesionResponse sesion);
    RefreshTokenGenerado GenerarRefreshToken();
    string HashRefreshToken(string tokenCrudo);
}
