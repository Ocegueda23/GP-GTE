using GTE.Application.DTOs.Request.ReglasNegocio;
using GTE.Application.DTOs.Responses.ReglasNegocio;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.ReglasNegocio;
using MediatR;

namespace GTE.Application.ReglasNegocio.Queries;

/* =====================================================================
   Catalogo de un proyecto: propias + heredadas
   ===================================================================== */

public record ObtenerCatalogoProyectoQuery(int IdProyecto, ReglasNegocioFiltroRequest Filtro)
    : IRequest<CatalogoReglasProyectoResponse>;

/// <summary>
/// La consulta central del modulo: al abrir un proyecto se ven sus reglas propias y las
/// que otros proyectos declararon que lo afectan, cada una etiquetada con su origen.
/// </summary>
public class ObtenerCatalogoProyectoHandler(
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ObtenerCatalogoProyectoQuery, CatalogoReglasProyectoResponse>
{
    public async Task<CatalogoReglasProyectoResponse> Handle(
        ObtenerCatalogoProyectoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReglasNegocio.Ver, query.IdProyecto, cancellationToken);

        return await consultas.ObtenerCatalogoProyectoAsync(query.IdProyecto, query.Filtro, cancellationToken)
            ?? throw new NotFoundException("Proyecto", query.IdProyecto);
    }
}

/* =====================================================================
   Ficha de una regla
   ===================================================================== */

public record ObtenerReglaNegocioQuery(int IdRegla) : IRequest<ReglaNegocioResponse>;

public class ObtenerReglaNegocioHandler(
    IReglasNegocioQueryService consultas,
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<ObtenerReglaNegocioQuery, ReglaNegocioResponse>
{
    public async Task<ReglaNegocioResponse> Handle(
        ObtenerReglaNegocioQuery query, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(query.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", query.IdRegla);

        await permisos.ExigirPermisoAsync(PermisosReglasNegocio.Ver, estado.IdProyecto, cancellationToken);

        return await consultas.ObtenerPorIdAsync(query.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", query.IdRegla);
    }
}

/* =====================================================================
   Historial de versiones
   ===================================================================== */

public record ObtenerVersionesReglaQuery(int IdRegla) : IRequest<IReadOnlyList<ReglaVersionResponse>>;

public class ObtenerVersionesReglaHandler(
    IReglasNegocioQueryService consultas,
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerVersionesReglaQuery, IReadOnlyList<ReglaVersionResponse>>
{
    public async Task<IReadOnlyList<ReglaVersionResponse>> Handle(
        ObtenerVersionesReglaQuery query, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(query.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", query.IdRegla);

        await permisos.ExigirPermisoAsync(PermisosReglasNegocio.Ver, estado.IdProyecto, cancellationToken);

        return await consultas.ObtenerVersionesAsync(query.IdRegla, cancellationToken);
    }
}

/* =====================================================================
   Busqueda global
   ===================================================================== */

public record BuscarReglasNegocioQuery(ReglasNegocioFiltroRequest Filtro)
    : IRequest<IReadOnlyList<ReglaNegocioResumenResponse>>;

/// <summary>
/// Busqueda que cruza proyectos ("en que proyecto estaba esa regla"). El resultado se
/// acota a los proyectos donde el usuario tiene alcance: las reglas de negocio de un
/// sistema son informacion sensible del cliente interno.
/// </summary>
public class BuscarReglasNegocioHandler(
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos)
    : IRequestHandler<BuscarReglasNegocioQuery, IReadOnlyList<ReglaNegocioResumenResponse>>
{
    public async Task<IReadOnlyList<ReglaNegocioResumenResponse>> Handle(
        BuscarReglasNegocioQuery query, CancellationToken cancellationToken)
    {
        var resultados = await consultas.BuscarAsync(query.Filtro, cancellationToken);

        // El filtro de alcance se aplica aqui y no en el QueryService a proposito: la
        // evaluacion RBAC vive en un solo lugar (IVerificadorPermisos), no duplicada en
        // consultas EF. Se evalua una vez por proyecto distinto, no una por regla.
        var permitidos = new Dictionary<int, bool>();
        var visibles = new List<ReglaNegocioResumenResponse>();

        foreach (var regla in resultados)
        {
            if (!permitidos.TryGetValue(regla.IdProyectoDueno, out var puede))
            {
                puede = await permisos.TienePermisoAsync(
                    PermisosReglasNegocio.Ver, regla.IdProyectoDueno, cancellationToken);
                permitidos[regla.IdProyectoDueno] = puede;
            }

            if (puede)
            {
                visibles.Add(regla);
            }
        }

        return visibles;
    }
}

/* =====================================================================
   Ambitos y catalogos de apoyo
   ===================================================================== */

public record ObtenerAmbitosProyectoQuery(int IdProyecto) : IRequest<IReadOnlyList<AmbitoReglaResponse>>;

public class ObtenerAmbitosProyectoHandler(
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerAmbitosProyectoQuery, IReadOnlyList<AmbitoReglaResponse>>
{
    public async Task<IReadOnlyList<AmbitoReglaResponse>> Handle(
        ObtenerAmbitosProyectoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReglasNegocio.Ver, query.IdProyecto, cancellationToken);
        return await consultas.ObtenerAmbitosAsync(query.IdProyecto, cancellationToken);
    }
}

public record ObtenerCatalogosReglasQuery : IRequest<CatalogosReglasNegocioResponse>;

public class ObtenerCatalogosReglasHandler(IReglasNegocioQueryService consultas)
    : IRequestHandler<ObtenerCatalogosReglasQuery, CatalogosReglasNegocioResponse>
{
    public Task<CatalogosReglasNegocioResponse> Handle(
        ObtenerCatalogosReglasQuery query, CancellationToken cancellationToken) =>
        consultas.ObtenerCatalogosAsync(cancellationToken);
}

public record ProyectoConReglasResponse(int IdProyecto, string Clave, string Nombre, int TotalReglas);

public record ObtenerProyectosConReglasQuery : IRequest<IReadOnlyList<ProyectoConReglasResponse>>;

/// <summary>Selector de proyectos de la UI, acotado al alcance del usuario.</summary>
public class ObtenerProyectosConReglasHandler(
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerProyectosConReglasQuery, IReadOnlyList<ProyectoConReglasResponse>>
{
    public async Task<IReadOnlyList<ProyectoConReglasResponse>> Handle(
        ObtenerProyectosConReglasQuery query, CancellationToken cancellationToken)
    {
        var proyectos = await consultas.ObtenerProyectosConReglasAsync(cancellationToken);
        var visibles = new List<ProyectoConReglasResponse>();

        foreach (var proyecto in proyectos)
        {
            if (await permisos.TienePermisoAsync(
                    PermisosReglasNegocio.Ver, proyecto.IdProyecto, cancellationToken))
            {
                visibles.Add(new ProyectoConReglasResponse(
                    proyecto.IdProyecto, proyecto.Clave, proyecto.Nombre, proyecto.TotalReglas));
            }
        }

        return visibles;
    }
}
