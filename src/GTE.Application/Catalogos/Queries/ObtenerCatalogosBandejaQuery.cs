using GTE.Application.DTOs.Responses.Catalogos;
using MediatR;

namespace GTE.Application.Catalogos.Queries;

public record ObtenerCatalogosBandejaQuery : IRequest<CatalogosBandejaResponse>;

public interface ICatalogosQueryService
{
    Task<CatalogosBandejaResponse> ObtenerCatalogosBandejaAsync(CancellationToken cancellationToken = default);
    Task<CatalogosAdministracionResponse> ObtenerCatalogosAdministracionAsync(CancellationToken cancellationToken = default);
}

public class ObtenerCatalogosBandejaHandler(ICatalogosQueryService consultas)
    : IRequestHandler<ObtenerCatalogosBandejaQuery, CatalogosBandejaResponse>
{
    public async Task<CatalogosBandejaResponse> Handle(
        ObtenerCatalogosBandejaQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCatalogosBandejaAsync(cancellationToken);
    }
}
