using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Queries;

public record ObtenerArchivosSolicitudQuery(int IdSolicitud) : IRequest<IReadOnlyList<ArchivoResponse>>;

public class ObtenerArchivosSolicitudHandler(IArchivoQueryService consultas)
    : IRequestHandler<ObtenerArchivosSolicitudQuery, IReadOnlyList<ArchivoResponse>>
{
    public async Task<IReadOnlyList<ArchivoResponse>> Handle(
        ObtenerArchivosSolicitudQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync("Solicitud", query.IdSolicitud, cancellationToken);
    }
}
