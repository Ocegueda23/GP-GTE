using GTE.Application.Common;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.WorkItems.Queries;

public record ObtenerBandejaQuery(FiltroBandeja Filtro) : IRequest<PagedResult<BandejaItemResponse>>;

public class ObtenerBandejaHandler(IWorkItemQueryService consultas)
    : IRequestHandler<ObtenerBandejaQuery, PagedResult<BandejaItemResponse>>
{
    public async Task<PagedResult<BandejaItemResponse>> Handle(
        ObtenerBandejaQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerBandejaAsync(query.Filtro, cancellationToken);
    }
}

public record ObtenerWorkItemPorFolioQuery(string Folio) : IRequest<WorkItemResponse>;

public class ObtenerWorkItemPorFolioHandler(IWorkItemQueryService consultas)
    : IRequestHandler<ObtenerWorkItemPorFolioQuery, WorkItemResponse>
{
    public async Task<WorkItemResponse> Handle(
        ObtenerWorkItemPorFolioQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorFolioAsync(query.Folio, cancellationToken)
            ?? throw new NotFoundException("WorkItem", query.Folio);
    }
}

public record ObtenerTiemposQuery(int IdWorkItem) : IRequest<IReadOnlyList<RegistroTiempoResponse>>;

public class ObtenerTiemposHandler(IWorkItemQueryService consultas)
    : IRequestHandler<ObtenerTiemposQuery, IReadOnlyList<RegistroTiempoResponse>>
{
    public async Task<IReadOnlyList<RegistroTiempoResponse>> Handle(
        ObtenerTiemposQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerTiemposAsync(query.IdWorkItem, cancellationToken);
    }
}
