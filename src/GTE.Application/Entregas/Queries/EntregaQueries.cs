using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Entregas.Queries;

public record ObtenerReleasesQuery(int? IdProyecto, bool SoloAbiertos) : IRequest<IReadOnlyList<ReleaseResponse>>;

public class ObtenerReleasesHandler(IEntregaQueryService consultas)
    : IRequestHandler<ObtenerReleasesQuery, IReadOnlyList<ReleaseResponse>>
{
    public async Task<IReadOnlyList<ReleaseResponse>> Handle(
        ObtenerReleasesQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerReleasesAsync(query.IdProyecto, query.SoloAbiertos, cancellationToken);
    }
}

public record ObtenerReleaseQuery(int IdRelease) : IRequest<ReleaseDetalleResponse>;

public class ObtenerReleaseHandler(IEntregaQueryService consultas)
    : IRequestHandler<ObtenerReleaseQuery, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(ObtenerReleaseQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerDetalleAsync(query.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", query.IdRelease);
    }
}

public record ObtenerMatrizAmbientesQuery : IRequest<IReadOnlyList<MatrizAmbienteResponse>>;

public class ObtenerMatrizAmbientesHandler(IEntregaQueryService consultas)
    : IRequestHandler<ObtenerMatrizAmbientesQuery, IReadOnlyList<MatrizAmbienteResponse>>
{
    public async Task<IReadOnlyList<MatrizAmbienteResponse>> Handle(
        ObtenerMatrizAmbientesQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerMatrizAmbientesAsync(cancellationToken);
    }
}
