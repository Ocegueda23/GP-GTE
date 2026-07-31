using GTE.Application.DTOs.Responses.Catalogos;
using MediatR;

namespace GTE.Application.Catalogos.Queries;

public record ObtenerCatalogosAdministracionQuery : IRequest<CatalogosAdministracionResponse>;

public class ObtenerCatalogosAdministracionHandler(ICatalogosQueryService consultas)
    : IRequestHandler<ObtenerCatalogosAdministracionQuery, CatalogosAdministracionResponse>
{
    public async Task<CatalogosAdministracionResponse> Handle(
        ObtenerCatalogosAdministracionQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCatalogosAdministracionAsync(cancellationToken);
    }
}
