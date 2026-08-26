using GTE.Application.DTOs.Responses.MiDia;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Solicitudes;
using MediatR;

namespace GTE.Application.MiDia.Queries;

public record ObtenerMiDiaQuery : IRequest<MiDiaResponse>;

public class ObtenerMiDiaHandler(
    IMiDiaQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario,
    IVerificadorPermisos permisos) : IRequestHandler<ObtenerMiDiaQuery, MiDiaResponse>
{
    public async Task<MiDiaResponse> Handle(ObtenerMiDiaQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var puedeTriage = await permisos.TienePermisoAsync(PermisosSolicitud.Triage, null, cancellationToken);

        return await consultas.ObtenerAsync(usuario.IdUsuario, usuario.Nombre, puedeTriage, cancellationToken);
    }
}
