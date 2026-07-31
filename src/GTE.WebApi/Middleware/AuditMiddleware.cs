using System.Security.Claims;
using GTE.Application.Common;

namespace GTE.WebApi.Middleware;

/// <summary>
/// Llena el AuditContext por request desde el JWT (identidad, nombre, correo, IP).
/// Debe registrarse DESPUES de UseAuthentication para que los claims existan.
/// La auditoria siempre sale del token, nunca del payload.
/// </summary>
public class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext contexto, AuditContext auditoria)
    {
        var usuario = contexto.User;

        // Sin identidad se queda vacio: no se inventa un usuario para la auditoria
        auditoria.Usuario =
            usuario.FindFirstValue("preferred_username")
            ?? usuario.FindFirstValue(ClaimTypes.Upn)
            ?? usuario.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        auditoria.NombreCompleto =
            usuario.FindFirstValue("name")
            ?? usuario.FindFirstValue(ClaimTypes.GivenName);
        auditoria.Correo =
            usuario.FindFirstValue(ClaimTypes.Email)
            ?? usuario.FindFirstValue("email");
        auditoria.Ip = contexto.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        auditoria.Endpoint = $"{contexto.Request.Method} {contexto.Request.Path}";

        await next(contexto);
    }
}
