using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Seguridad.Queries;

public record ObtenerSesionQuery : IRequest<SesionResponse>;

/// <summary>
/// Devuelve la identidad del usuario autenticado. Si es su primer inicio de sesion
/// con una identidad valida, su usuario se crea aqui (aprovisionamiento JIT) y
/// nace sin roles: la UI le avisa que necesita que administracion lo habilite.
/// </summary>
public class ObtenerSesionHandler(
    ISesionQueryService consultas,
    IAprovisionadorUsuarios aprovisionador,
    AuditContext auditoria) : IRequestHandler<ObtenerSesionQuery, SesionResponse>
{
    public async Task<SesionResponse> Handle(ObtenerSesionQuery query, CancellationToken cancellationToken)
    {
        if (!auditoria.TieneIdentidad)
        {
            throw new ForbiddenException("El token no trae una identidad utilizable.");
        }

        var idUsuario = await aprovisionador.ObtenerOCrearAsync(
            new IdentidadToken(auditoria.Usuario, auditoria.NombreCompleto, auditoria.Correo),
            cancellationToken);

        return await consultas.ObtenerSesionAsync(idUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", idUsuario);
    }
}
