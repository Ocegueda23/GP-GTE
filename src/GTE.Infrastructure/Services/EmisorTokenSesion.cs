using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Domain.Autenticacion;
using Microsoft.IdentityModel.Tokens;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Emisor unico del JWT propio de GTE. La misma clave y la misma forma de reclamos las
/// usa tanto el atajo de desarrollo como el login real: AuditMiddleware/
/// IProveedorUsuarioActual no distinguen entre uno y otro.
/// </summary>
public class EmisorTokenSesion(ClaveFirmaGte clave) : IEmisorTokenSesion
{
    public (string Token, DateTime Expira) EmitirTokenAcceso(SesionResponse sesion)
    {
        var expira = DateTime.UtcNow.AddMinutes(clave.MinutosVigenciaAcceso);
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(clave.Clave), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: clave.Issuer,
            audience: clave.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, sesion.IdUsuario.ToString()),
                new Claim("preferred_username", sesion.Dominio),
                new Claim("name", sesion.Nombre),
                new Claim(ClaimTypes.Email, sesion.Correo ?? string.Empty)
            ],
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public RefreshTokenGenerado GenerarRefreshToken()
    {
        var crudo = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expira = DateTime.Now.AddHours(ConstantesAutenticacion.HorasVigenciaRefresh);
        return new RefreshTokenGenerado(crudo, HashRefreshToken(crudo), expira);
    }

    public string HashRefreshToken(string tokenCrudo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenCrudo));
        return Convert.ToBase64String(bytes);
    }
}
