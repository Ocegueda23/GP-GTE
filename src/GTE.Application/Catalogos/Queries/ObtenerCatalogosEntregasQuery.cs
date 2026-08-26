using GTE.Application.DTOs.Responses.Catalogos;
using MediatR;

namespace GTE.Application.Catalogos.Queries;

public record ObtenerCatalogosEntregasQuery : IRequest<CatalogosEntregasResponse>;

public class ObtenerCatalogosEntregasHandler(ICatalogosQueryService consultas)
    : IRequestHandler<ObtenerCatalogosEntregasQuery, CatalogosEntregasResponse>
{
    public async Task<CatalogosEntregasResponse> Handle(
        ObtenerCatalogosEntregasQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCatalogosEntregasAsync(cancellationToken);
    }
}
