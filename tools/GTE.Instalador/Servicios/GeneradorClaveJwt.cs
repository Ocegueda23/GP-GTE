using System.Security.Cryptography;

namespace GTE.Instalador.Servicios;

public static class GeneradorClaveJwt
{
    // 64 bytes: mismo tamano que ya se generaba a mano para Jwt__ClaveFirma, con margen
    // de sobra para HMAC-SHA256.
    public static string Generar() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
