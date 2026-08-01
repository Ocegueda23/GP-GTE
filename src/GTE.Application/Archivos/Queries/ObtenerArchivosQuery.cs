using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Queries;

public record ObtenerArchivosQuery(int IdWorkItem) : IRequest<IReadOnlyList<ArchivoResponse>>;

public class ObtenerArchivosHandler(IArchivoQueryService consultas)
    : IRequestHandler<ObtenerArchivosQuery, IReadOnlyList<ArchivoResponse>>
{
    public async Task<IReadOnlyList<ArchivoResponse>> Handle(
        ObtenerArchivosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync("WorkItem", query.IdWorkItem, cancellationToken);
    }
}
