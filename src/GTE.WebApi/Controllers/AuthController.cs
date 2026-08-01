using GTE.Application.Autenticacion.Commands;
using GTE.Application.DTOs.Request.Autenticacion;
using GTE.Application.DTOs.Responses.Autenticacion;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Application.Seguridad.Queries;
using GTE.WebApi.Models;
using GTE.WebApi.Seguridad;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

/// <summary>
/// Identidad de la sesion, login propio (usuario+contraseña) y, solo en desarrollo,
/// el atajo sin contraseña. GTE no depende de ningun proveedor de identidad externo.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IMediator mediator,
    IOptions<OpcionesAutenticacion> opciones,
    IWebHostEnvironment ambiente,
    ISesionQueryService sesiones,
    IAprovisionadorUsuarios aprovisionador,
    IEmisorTokenSesion emisor) : ControllerBase
{
    private const string CookieRefresh = "gte.refresh";

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
            emisorDesarrollo = ambiente.IsDevelopment() && opciones.Value.Desarrollo.Habilitado
        }));
    }

    /// <summary>Login real: usuario (cuenta de dominio) + contraseña.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new IniciarSesionCommand(request.Dominio, request.Password), cancellationToken);
        EstablecerCookieRefresh(resultado.RefreshTokenCrudo, resultado.RefreshExpira);
        return Ok(ApiResponse<LoginResponseDto>.Exito(new LoginResponseDto
        {
            Token = resultado.Token,
            Expira = resultado.Expira,
            Sesion = resultado.Sesion,
            RequiereCambioPassword = resultado.RequiereCambioPassword
        }, $"Sesion iniciada como {resultado.Sesion.Nombre}."));
    }

    /// <summary>Rota el refresh token (cookie HttpOnly) y emite un access token nuevo.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refrescar(CancellationToken cancellationToken)
    {
        var refreshCrudo = Request.Cookies[CookieRefresh];
        if (string.IsNullOrEmpty(refreshCrudo))
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Falla(
                ApiResponseCodes.Forbidden, "No hay sesion que refrescar."));
        }

        var resultado = await mediator.Send(new RefrescarSesionCommand(refreshCrudo), cancellationToken);
        EstablecerCookieRefresh(resultado.RefreshTokenCrudo, resultado.RefreshExpira);
        return Ok(ApiResponse<LoginResponseDto>.Exito(new LoginResponseDto
        {
            Token = resultado.Token,
            Expira = resultado.Expira,
            Sesion = resultado.Sesion,
            RequiereCambioPassword = resultado.RequiereCambioPassword
        }));
    }

    /// <summary>Revoca el refresh token y borra la cookie. Anonimo: el access token ya pudo expirar.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        var refreshCrudo = Request.Cookies[CookieRefresh];
        await mediator.Send(new CerrarSesionCommand(refreshCrudo), cancellationToken);
        Response.Cookies.Delete(CookieRefresh);
        return Ok(ApiResponse<object>.Exito(new { }, "Sesion cerrada."));
    }

    /// <summary>Cambio de contraseña por el propio usuario (exige la contraseña actual).</summary>
    [HttpPost("cambiar-password")]
    public async Task<ActionResult<ApiResponse<object>>> CambiarPassword(
        [FromBody] CambiarPasswordRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CambiarPasswordCommand(request.PasswordActual, request.PasswordNueva), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Contraseña actualizada."));
    }

    /// <summary>
    /// Emite un token firmado localmente para trabajar sin escribir contraseña.
    /// Existe SOLO en Development y solo si Jwt:Desarrollo:Habilitado esta activo:
    /// en cualquier otro caso responde 404, como si la ruta no existiera.
    /// </summary>
    [HttpPost("desarrollo/token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> EmitirTokenDesarrollo(
        [FromBody] TokenDesarrolloRequest request, CancellationToken cancellationToken)
    {
        if (!ambiente.IsDevelopment() || !opciones.Value.Desarrollo.Habilitado)
        {
            return NotFound(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.NotFound,
                "Esta instalacion usa usuario y contraseña para iniciar sesion."));
        }

        if (string.IsNullOrWhiteSpace(request.Dominio))
        {
            return BadRequest(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.ValidationError, "Indica la cuenta de dominio."));
        }

        var dominio = request.Dominio.Trim();

        // El atajo de desarrollo no crea usuarios con roles: solo firma por identidades que ya existen
        var idUsuario = await aprovisionador.ObtenerOCrearAsync(
            new IdentidadToken(dominio, null, null), cancellationToken);
        var sesion = await sesiones.ObtenerSesionAsync(idUsuario, cancellationToken);
        if (sesion is null)
        {
            return NotFound(ApiResponse<TokenResponse>.Falla(
                ApiResponseCodes.NotFound, $"No existe el usuario {dominio} en GTE."));
        }

        var (token, expira) = emisor.EmitirTokenAcceso(sesion);

        return Ok(ApiResponse<TokenResponse>.Exito(new TokenResponse
        {
            Token = token,
            Expira = expira,
            Sesion = sesion
        }, $"Sesion de desarrollo iniciada como {sesion.Nombre}."));
    }

    private void EstablecerCookieRefresh(string tokenCrudo, DateTime expira)
    {
        Response.Cookies.Append(CookieRefresh, tokenCrudo, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expira,
            Path = "/api/v1/auth"
        });
    }
}
