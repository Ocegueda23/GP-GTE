using GTE.Application.DTOs.Responses.NotasVersion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.NotasVersion;
using MediatR;

namespace GTE.Application.NotasVersion.Queries;

/// <summary>
/// Historial que ve el usuario final. SIN permiso a proposito: cualquier usuario autenticado
/// debe poder ver que trae la version que esta usando. Solo devuelve notas publicadas.
/// </summary>
public record ObtenerNotasVersionPublicadasQuery : IRequest<IReadOnlyList<NotaVersionResponse>>;

public class ObtenerNotasVersionPublicadasHandler(INotaVersionQueryService consultas)
    : IRequestHandler<ObtenerNotasVersionPublicadasQuery, IReadOnlyList<NotaVersionResponse>>
{
    public async Task<IReadOnlyList<NotaVersionResponse>> Handle(
        ObtenerNotasVersionPublicadasQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerPublicadasAsync(cancellationToken);
}

/// <summary>Listado de administracion: incluye borradores, por eso si exige permiso.</summary>
public record ObtenerNotasVersionQuery : IRequest<IReadOnlyList<NotaVersionListaResponse>>;

public class ObtenerNotasVersionHandler(INotaVersionQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerNotasVersionQuery, IReadOnlyList<NotaVersionListaResponse>>
{
    public async Task<IReadOnlyList<NotaVersionListaResponse>> Handle(
        ObtenerNotasVersionQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosNotasVersion.Administrar, null, cancellationToken);
        return await consultas.ObtenerTodasAsync(cancellationToken);
    }
}

public record ObtenerNotaVersionQuery(int IdNotaVersion) : IRequest<NotaVersionResponse>;

public class ObtenerNotaVersionHandler(INotaVersionQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerNotaVersionQuery, NotaVersionResponse>
{
    public async Task<NotaVersionResponse> Handle(
        ObtenerNotaVersionQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosNotasVersion.Administrar, null, cancellationToken);
        return await consultas.ObtenerPorIdAsync(query.IdNotaVersion, cancellationToken)
            ?? throw new NotFoundException("nota de version", query.IdNotaVersion);
    }
}

public record ObtenerTiposCambioVersionQuery : IRequest<IReadOnlyList<TipoCambioVersionResponse>>;

public class ObtenerTiposCambioVersionHandler(INotaVersionQueryService consultas)
    : IRequestHandler<ObtenerTiposCambioVersionQuery, IReadOnlyList<TipoCambioVersionResponse>>
{
    public async Task<IReadOnlyList<TipoCambioVersionResponse>> Handle(
        ObtenerTiposCambioVersionQuery query, CancellationToken cancellationToken)
        => await consultas.ObtenerTiposCambioAsync(cancellationToken);
}
