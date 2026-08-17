using GTE.Application.DTOs.Responses.Catalogos;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Catalogos.Queries;

public record ObtenerCatalogosBandejaQuery : IRequest<CatalogosBandejaResponse>;

public interface ICatalogosQueryService
{
    Task<CatalogosBandejaResponse> ObtenerCatalogosBandejaAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task<CatalogosAdministracionResponse> ObtenerCatalogosAdministracionAsync(CancellationToken cancellationToken = default);
}

public class ObtenerCatalogosBandejaHandler(ICatalogosQueryService consultas, IProveedorUsuarioActual proveedorUsuario)
    : IRequestHandler<ObtenerCatalogosBandejaQuery, CatalogosBandejaResponse>
{
    public async Task<CatalogosBandejaResponse> Handle(
        ObtenerCatalogosBandejaQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");
        return await consultas.ObtenerCatalogosBandejaAsync(usuario.IdUsuario, cancellationToken);
    }
}
