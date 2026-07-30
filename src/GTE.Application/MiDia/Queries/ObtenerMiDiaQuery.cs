using GTE.Application.DTOs.Responses.MiDia;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.MiDia.Queries;

public record ObtenerMiDiaQuery : IRequest<MiDiaResponse>;

public class ObtenerMiDiaHandler(
    IMiDiaQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerMiDiaQuery, MiDiaResponse>
{
    public async Task<MiDiaResponse> Handle(ObtenerMiDiaQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        return await consultas.ObtenerAsync(usuario.IdUsuario, usuario.Nombre, cancellationToken);
    }
}
