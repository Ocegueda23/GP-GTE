using GTE.Application.Common;
using GTE.Application.DTOs.Request.CatalogoGenerico;
using GTE.Application.DTOs.Responses.CatalogoGenerico;
using GTE.Application.Interfaces;
using GTE.Domain.CatalogoGenerico;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.CatalogoGenerico.Queries;

/// <summary>Catalogos activos que el usuario actual puede ver (CAT.&lt;CLAVE&gt;.Ver).</summary>
public record ObtenerCatalogosQuery : IRequest<IReadOnlyList<CatalogoResumenResponse>>;

public class ObtenerCatalogosHandler(ICatalogoGenericoQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerCatalogosQuery, IReadOnlyList<CatalogoResumenResponse>>
{
    public async Task<IReadOnlyList<CatalogoResumenResponse>> Handle(
        ObtenerCatalogosQuery query, CancellationToken cancellationToken)
    {
        var catalogos = await consultas.ObtenerCatalogosAsync(cancellationToken);

        var visibles = new List<CatalogoResumenResponse>();
        foreach (var catalogo in catalogos)
        {
            if (await permisos.TienePermisoAsync(PermisosCatalogoGenerico.Ver(catalogo.Clave), null, cancellationToken))
            {
                visibles.Add(new CatalogoResumenResponse
                {
                    IdCatalogo = catalogo.IdCatalogo,
                    Clave = catalogo.Clave,
                    NombreTabla = catalogo.NombreTabla,
                    Titulo = catalogo.Titulo
                });
            }
        }
        return visibles;
    }
}

public record ObtenerConfigCatalogoQuery(string Clave) : IRequest<ConfiguracionCatalogoResponse>;

public class ObtenerConfigCatalogoHandler(ICatalogoGenericoQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerConfigCatalogoQuery, ConfiguracionCatalogoResponse>
{
    public async Task<ConfiguracionCatalogoResponse> Handle(
        ObtenerConfigCatalogoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Ver(query.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(query.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", query.Clave);

        return ProyeccionesCatalogoGenerico.Proyectar(config);
    }
}

public record ListarRegistrosCatalogoQuery(string Clave, ListarRegistrosRequest Filtros)
    : IRequest<PagedResult<Dictionary<string, object?>>>;

public class ListarRegistrosCatalogoHandler(
    ICatalogoGenericoQueryService consultas, IMotorCrudGenerico motor, IVerificadorPermisos permisos)
    : IRequestHandler<ListarRegistrosCatalogoQuery, PagedResult<Dictionary<string, object?>>>
{
    public async Task<PagedResult<Dictionary<string, object?>>> Handle(
        ListarRegistrosCatalogoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Ver(query.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(query.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", query.Clave);

        var filtros = new FiltrosListadoCatalogo(
            query.Filtros.Texto,
            query.Filtros.FiltrosColumna.Select(f => new FiltroColumnaDiscreto(f.NombreColumna, f.Valores)).ToList(),
            query.Filtros.Fecha is null
                ? null
                : new FiltroFecha(query.Filtros.Fecha.NombreColumna, query.Filtros.Fecha.Desde, query.Filtros.Fecha.Hasta),
            query.Filtros.OrdenarPor,
            query.Filtros.OrdenDescendente);

        return await motor.ListarAsync(config, filtros, query.Filtros.Pagina, query.Filtros.TamanoPagina, cancellationToken);
    }
}

public record ObtenerValoresDistintosQuery(string Clave, string NombreColumna) : IRequest<IReadOnlyList<string>>;

public class ObtenerValoresDistintosHandler(
    ICatalogoGenericoQueryService consultas, IMotorCrudGenerico motor, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerValoresDistintosQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        ObtenerValoresDistintosQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Ver(query.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(query.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", query.Clave);

        return await motor.ObtenerValoresDistintosAsync(config, query.NombreColumna, cancellationToken);
    }
}

public record DescifrarValorQuery(string Clave, DescifrarValorRequest Datos) : IRequest<string?>;

/// <summary>
/// Descifrado puntual y explicito de una columna cifrada. Nunca ocurre automaticamente
/// (ListarAsync jamas desprotege); requiere el permiso dedicado CAT.&lt;CLAVE&gt;.Descifrar y
/// deja bitacora siempre, incluso si el valor resulta null.
/// </summary>
public class DescifrarValorHandler(
    ICatalogoGenericoQueryService consultas,
    IMotorCrudGenerico motor,
    IServicioCifradoColumna cifrado,
    ICatalogoGenericoRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<DescifrarValorQuery, string?>
{
    public async Task<string?> Handle(DescifrarValorQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Descifrar(query.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(query.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", query.Clave);

        var columna = config.Columnas.FirstOrDefault(c =>
            string.Equals(c.NombreColumna, query.Datos.NombreColumna, StringComparison.OrdinalIgnoreCase));
        if (columna is null || !columna.EsCifrado)
        {
            throw new BusinessException($"La columna {query.Datos.NombreColumna} no esta marcada como cifrada.");
        }

        var fila = await motor.ObtenerPorPkAsync(config, query.Datos.ClavesPk, cancellationToken)
            ?? throw new NotFoundException("Registro", query.Clave);

        await repositorio.RegistrarDescifradoAsync(query.Clave, columna.NombreColumna, cancellationToken);

        var valorProtegido = fila.GetValueOrDefault(columna.NombreColumna) as string;
        return valorProtegido is null ? null : cifrado.Desproteger(columna.NombreColumna, valorProtegido);
    }
}

public record ObtenerOpcionesFkQuery(string Clave, string NombreColumna) : IRequest<IReadOnlyList<OpcionFkResponse>>;

public class ObtenerOpcionesFkHandler(
    ICatalogoGenericoQueryService consultas, IMotorCrudGenerico motor, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerOpcionesFkQuery, IReadOnlyList<OpcionFkResponse>>
{
    public async Task<IReadOnlyList<OpcionFkResponse>> Handle(
        ObtenerOpcionesFkQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Ver(query.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(query.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", query.Clave);

        var columna = config.Columnas.FirstOrDefault(c =>
            string.Equals(c.NombreColumna, query.NombreColumna, StringComparison.OrdinalIgnoreCase));
        if (columna is null || columna.TablaFk is null || columna.ColumnaClaveFk is null || columna.ColumnaMostrarFk is null)
        {
            throw new BusinessException($"La columna {query.NombreColumna} no tiene combo FK configurado.");
        }

        var opciones = await motor.ObtenerOpcionesFkAsync(
            columna.TablaFk, columna.ColumnaClaveFk, columna.ColumnaMostrarFk, cancellationToken);
        return opciones.Select(o => new OpcionFkResponse { Valor = o.Valor, Etiqueta = o.Etiqueta }).ToList();
    }
}

/* ---------- Admin: metadatos de esquema (para dar de alta catalogos nuevos) ---------- */

public record ObtenerTablasDisponiblesQuery : IRequest<IReadOnlyList<string>>;

public class ObtenerTablasDisponiblesHandler(IMotorMetadatosEsquema motorMetadatos, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerTablasDisponiblesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        ObtenerTablasDisponiblesQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Configurar, null, cancellationToken);
        return await motorMetadatos.ObtenerTablasDisponiblesAsync(cancellationToken);
    }
}

public record ObtenerColumnasEsquemaQuery(string NombreTabla) : IRequest<IReadOnlyList<ColumnaEsquemaResponse>>;

public class ObtenerColumnasEsquemaHandler(IMotorMetadatosEsquema motorMetadatos, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerColumnasEsquemaQuery, IReadOnlyList<ColumnaEsquemaResponse>>
{
    public async Task<IReadOnlyList<ColumnaEsquemaResponse>> Handle(
        ObtenerColumnasEsquemaQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Configurar, null, cancellationToken);

        var columnas = await motorMetadatos.ObtenerColumnasAsync(query.NombreTabla, cancellationToken);
        return columnas.Select(c => new ColumnaEsquemaResponse
        {
            NombreColumna = c.NombreColumna,
            TipoSql = c.TipoSql,
            EsNulable = c.EsNulable,
            LongitudMaxima = c.LongitudMaxima,
            EsPk = c.EsPk,
            EsIdentity = c.EsIdentity
        }).ToList();
    }
}
