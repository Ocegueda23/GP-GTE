using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Queries;

public record ObtenerArchivosRevisionQuery(int IdRevision) : IRequest<IReadOnlyList<ArchivoResponse>>;

public class ObtenerArchivosRevisionHandler(IArchivoQueryService consultas)
    : IRequestHandler<ObtenerArchivosRevisionQuery, IReadOnlyList<ArchivoResponse>>
{
    public async Task<IReadOnlyList<ArchivoResponse>> Handle(
        ObtenerArchivosRevisionQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync("Revision", query.IdRevision, cancellationToken);
    }
}
