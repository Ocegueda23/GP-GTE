using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Application.Seguridad.Queries;
using GTE.WebApi.Models;
using GTE.WebApi.Seguridad;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GTE.WebApi.Controllers;

public class TokenDesarrolloRequest
{
    /// <summary>Cuenta de dominio que se quiere simular (debe existir en GTE).</summary>
    public string Dominio { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public SesionResponse Sesion { get; set; } = new();
}

/// <summary>Identidad de la sesion y, solo en desarrollo, emision local de tokens.</summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IMediator mediator,
    IOptions<OpcionesAutenticacion> opciones,
    IWebHostEnvironment ambiente,
    ISesionQueryService sesiones,
    IAprovisionadorUsuarios aprovisionador,
    ClaveFirmaDesarrollo? claveDesarrollo = null) : ControllerBase
{
    /// <summary>Perfil, roles y permisos del usuario autenticado.</summary>
    [HttpGet("sesion")]
    public async Task<ActionResult<ApiResponse<SesionResponse>>> ObtenerSesion(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerSesionQuery(), cancellationToken);
        return Ok(ApiResponse<SesionResponse>.Exito(resultado));
    }

    /// <summary>Como iniciar sesion en este despliegue (lo consulta la pantalla de login).</summary>
    [HttpGet("configuracion")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> ObtenerConfiguracion()
    {
        return Ok(ApiResponse<object>.Exito(new
        {
            identidadExterna = opciones.Value.TieneIdentidadExterna,
            authority = opciones.Value.Authority,
            audience = opciones.Value.Audience,
            emisorDesarrollo = claveDesarrollo is not null
        }));
    }

    /// <summary>
    /// Emite un token firmado localmente para trabajar sin tenant de Entra ID.
    /// Existe SOLO en Development y solo si Jwt:Desarrollo:Habilitado esta activo:
    /// en cualquier otro caso responde 404, como si la ruta no existiera.
    /// </summary>
    [HttpPost("desarrollo/token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> EmitirTokenDesarrollo(
        [FromBody] TokenDesarrolloRequest request, CancellationToken cancellationToken)
    {
        if (!ambiente.IsDevelopment() || claveDesarrollo is null)
        {
            return NotFound(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.NotFound,
                "Esta instalacion usa el proveedor de identidad corporativo para iniciar sesion."));
        }

        if (string.IsNullOrWhiteSpace(request.Dominio))
        {
            return BadRequest(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.ValidationError, "Indica la cuenta de dominio."));
        }

        var dominio = request.Dominio.Trim();

        // El emisor local no crea usuarios: solo firma por identidades que ya existen
        var idUsuario = await aprovisionador.ObtenerOCrearAsync(
            new IdentidadToken(dominio, null, null), cancellationToken);
        var sesion = await sesiones.ObtenerSesionAsync(idUsuario, cancellationToken);
        if (sesion is null)
        {
            return NotFound(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.NotFound, $"No existe el usuario {dominio} en GTE."));
        }

        var expira = DateTime.UtcNow.AddMinutes(claveDesarrollo.Opciones.MinutosVigencia);
        var credenciales = new SigningCredentials(
            claveDesarrollo.Clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: claveDesarrollo.Opciones.Issuer,
            audience: opciones.Value.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, sesion.IdUsuario.ToString()),
                new Claim("preferred_username", sesion.Dominio),
                new Claim("name", sesion.Nombre),
                new Claim(ClaimTypes.Email, sesion.Correo ?? string.Empty)
            ],
            expires: expira,
            signingCredentials: credenciales);

        return Ok(ApiResponse<TokenResponse>.Exito(new TokenResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expira = expira,
            Sesion = sesion
        }, $"Sesion de desarrollo iniciada como {sesion.Nombre}."));
    }
}

/// <summary>Nombres de claims estandar usados al emitir tokens de desarrollo.</summary>
internal static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
}
