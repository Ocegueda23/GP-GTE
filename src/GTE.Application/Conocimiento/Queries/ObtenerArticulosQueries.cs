using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Conocimiento.Queries;

/// <summary>
/// Listado/buscador interno. Sin exigir permiso: P23 esta marcada como "Todos" en el
/// Documento Maestro (seccion 5.1); basta la identidad que ya exige el FallbackPolicy.
/// </summary>
public record ObtenerArticulosQuery(FiltroArticulos Filtro) : IRequest<PagedResult<ArticuloListaResponse>>;

public class ObtenerArticulosHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerArticulosQuery, PagedResult<ArticuloListaResponse>>
{
    public async Task<PagedResult<ArticuloListaResponse>> Handle(
        ObtenerArticulosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerListaAsync(query.Filtro, cancellationToken);
    }
}

public record ObtenerArticuloQuery(int IdArticulo) : IRequest<ArticuloResponse>;

public class ObtenerArticuloHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerArticuloQuery, ArticuloResponse>
{
    public async Task<ArticuloResponse> Handle(ObtenerArticuloQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorIdAsync(query.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", query.IdArticulo);
    }
}

public record ObtenerVersionesArticuloQuery(int IdArticulo) : IRequest<IReadOnlyList<ArticuloVersionResponse>>;

public class ObtenerVersionesArticuloHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerVersionesArticuloQuery, IReadOnlyList<ArticuloVersionResponse>>
{
    public async Task<IReadOnlyList<ArticuloVersionResponse>> Handle(
        ObtenerVersionesArticuloQuery query, CancellationToken cancellationToken)
    {
        // Confirma que el articulo exista antes de devolver una lista vacia enganosa.
        _ = await consultas.ObtenerPorIdAsync(query.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", query.IdArticulo);

        return await consultas.ObtenerVersionesAsync(query.IdArticulo, cancellationToken);
    }
}

/// <summary>Contenido de una version historica, para verla sin restaurarla.</summary>
public record ObtenerVersionArticuloQuery(int IdArticulo, int Version)
    : IRequest<ArticuloVersionContenidoResponse>;

public class ObtenerVersionArticuloHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerVersionArticuloQuery, ArticuloVersionContenidoResponse>
{
    public async Task<ArticuloVersionContenidoResponse> Handle(
        ObtenerVersionArticuloQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerVersionAsync(query.IdArticulo, query.Version, cancellationToken)
            ?? throw new NotFoundException($"No se encontro la version {query.Version} del articulo {query.IdArticulo}.");
    }
}
