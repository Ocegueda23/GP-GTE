using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Calidad.Queries;

public record ObtenerPlanesQuery(int? IdProyecto) : IRequest<IReadOnlyList<PlanPruebaResponse>>;

public class ObtenerPlanesHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerPlanesQuery, IReadOnlyList<PlanPruebaResponse>>
{
    public async Task<IReadOnlyList<PlanPruebaResponse>> Handle(
        ObtenerPlanesQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPlanesAsync(query.IdProyecto, cancellationToken);
    }
}

public record ObtenerPlanQuery(int IdPlanPrueba) : IRequest<PlanPruebaResponse>;

public class ObtenerPlanHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerPlanQuery, PlanPruebaResponse>
{
    public async Task<PlanPruebaResponse> Handle(ObtenerPlanQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPlanAsync(query.IdPlanPrueba, cancellationToken)
            ?? throw new NotFoundException("PlanPrueba", query.IdPlanPrueba);
    }
}

public record ObtenerCiclosQuery(int IdPlanPrueba) : IRequest<IReadOnlyList<CicloPruebaResponse>>;

public class ObtenerCiclosHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerCiclosQuery, IReadOnlyList<CicloPruebaResponse>>
{
    public async Task<IReadOnlyList<CicloPruebaResponse>> Handle(
        ObtenerCiclosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCiclosAsync(query.IdPlanPrueba, cancellationToken);
    }
}

public record ObtenerCasosQuery(int IdPlanPrueba, int? IdCicloPrueba) : IRequest<IReadOnlyList<CasoPruebaResponse>>;

public class ObtenerCasosHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerCasosQuery, IReadOnlyList<CasoPruebaResponse>>
{
    public async Task<IReadOnlyList<CasoPruebaResponse>> Handle(
        ObtenerCasosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCasosAsync(query.IdPlanPrueba, query.IdCicloPrueba, cancellationToken);
    }
}

public record ObtenerTrazabilidadQuery(int IdPlanPrueba) : IRequest<IReadOnlyList<TrazabilidadResponse>>;

public class ObtenerTrazabilidadHandler(ICalidadQueryService consultas)
    : IRequestHandler<ObtenerTrazabilidadQuery, IReadOnlyList<TrazabilidadResponse>>
{
    public async Task<IReadOnlyList<TrazabilidadResponse>> Handle(
        ObtenerTrazabilidadQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerTrazabilidadAsync(query.IdPlanPrueba, cancellationToken);
    }
}
