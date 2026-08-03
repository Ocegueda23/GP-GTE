using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Costeo.Queries;

/// <summary>
/// Guard compartido de las 3 consultas de este archivo: ver tarifas/presupuesto/costo
/// real exige RPT.Costos (dato sensible) O POR.GestionarCosteo (quien administra el
/// catalogo tambien puede verlo). Distinto de Gestionar, que exigen los Commands de
/// alta/edicion/baja -- Ver NO habilita editar.
/// </summary>
file static class GuardCosteo
{
    public static async Task ExigirVerAsync(IVerificadorPermisos permisos, CancellationToken cancellationToken)
    {
        var puedeVer = await permisos.TienePermisoAsync(PermisosCosteo.VerCostos, null, cancellationToken)
            || await permisos.TienePermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);
        if (!puedeVer)
        {
            throw new ForbiddenException("No tienes permiso para ver la informacion de costeo.");
        }
    }
}

public record ObtenerTarifasNivelQuery : IRequest<IReadOnlyList<TarifaNivelResponse>>;

public class ObtenerTarifasNivelHandler(ICosteoQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerTarifasNivelQuery, IReadOnlyList<TarifaNivelResponse>>
{
    public async Task<IReadOnlyList<TarifaNivelResponse>> Handle(
        ObtenerTarifasNivelQuery query, CancellationToken cancellationToken)
    {
        await GuardCosteo.ExigirVerAsync(permisos, cancellationToken);
        return await consultas.ObtenerTarifasAsync(cancellationToken);
    }
}

public record ObtenerPresupuestosProyectoQuery(int IdProyecto) : IRequest<IReadOnlyList<PresupuestoProyectoResponse>>;

public class ObtenerPresupuestosProyectoHandler(ICosteoQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerPresupuestosProyectoQuery, IReadOnlyList<PresupuestoProyectoResponse>>
{
    public async Task<IReadOnlyList<PresupuestoProyectoResponse>> Handle(
        ObtenerPresupuestosProyectoQuery query, CancellationToken cancellationToken)
    {
        await GuardCosteo.ExigirVerAsync(permisos, cancellationToken);
        return await consultas.ObtenerPresupuestosAsync(query.IdProyecto, cancellationToken);
    }
}

public record ObtenerCostoProyectoQuery(int IdProyecto, int Anio) : IRequest<CostoProyectoResponse>;

public class ObtenerCostoProyectoHandler(ICosteoQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerCostoProyectoQuery, CostoProyectoResponse>
{
    public async Task<CostoProyectoResponse> Handle(
        ObtenerCostoProyectoQuery query, CancellationToken cancellationToken)
    {
        await GuardCosteo.ExigirVerAsync(permisos, cancellationToken);
        return await consultas.ObtenerCostoProyectoAsync(query.IdProyecto, query.Anio, cancellationToken);
    }
}
