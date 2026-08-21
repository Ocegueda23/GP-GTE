using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.Interfaces;
using MediatR;

namespace GTE.Application.Calidad.Queries;

public record ObtenerCasosDisponiblesQuery(int IdProyecto) : IRequest<IReadOnlyList<CasoPruebaResponse>>;

public class ObtenerCasosDisponiblesHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerCasosDisponiblesQuery, IReadOnlyList<CasoPruebaResponse>>
{
    public async Task<IReadOnlyList<CasoPruebaResponse>> Handle(
        ObtenerCasosDisponiblesQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCasosDisponiblesAsync(query.IdProyecto, cancellationToken);
    }
}

public record ObtenerCasosAsignadosQuery(int IdWorkItem) : IRequest<IReadOnlyList<CasoAsignadoResponse>>;

public class ObtenerCasosAsignadosHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerCasosAsignadosQuery, IReadOnlyList<CasoAsignadoResponse>>
{
    public async Task<IReadOnlyList<CasoAsignadoResponse>> Handle(
        ObtenerCasosAsignadosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCasosAsignadosAsync(query.IdWorkItem, cancellationToken);
    }
}
