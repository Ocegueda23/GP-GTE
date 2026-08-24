using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Conocimiento.Queries;

/// <summary>
/// Listado para el consumo ANONIMO: solo articulos con EsPublico = 1. Handlers aparte
/// de los internos a proposito -- el filtro de publicidad vive en su propia consulta,
/// no en un parametro opcional que alguien pueda olvidar de pasar.
/// </summary>
public record ObtenerArticulosPublicosQuery(FiltroArticulos Filtro)
    : IRequest<PagedResult<ArticuloPublicoListaResponse>>;

public class ObtenerArticulosPublicosHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerArticulosPublicosQuery, PagedResult<ArticuloPublicoListaResponse>>
{
    public async Task<PagedResult<ArticuloPublicoListaResponse>> Handle(
        ObtenerArticulosPublicosQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerListaPublicaAsync(query.Filtro, cancellationToken);
    }
}

/// <summary>
/// Detalle anonimo. Un articulo que existe pero NO es publico responde 404, igual que
/// uno inexistente: un visitante externo no debe poder distinguir entre "no existe" y
/// "existe pero es interno" (evita enumerar el contenido privado por id).
/// </summary>
public record ObtenerArticuloPublicoQuery(int IdArticulo) : IRequest<ArticuloPublicoResponse>;

public class ObtenerArticuloPublicoHandler(IConocimientoQueryService consultas)
    : IRequestHandler<ObtenerArticuloPublicoQuery, ArticuloPublicoResponse>
{
    public async Task<ArticuloPublicoResponse> Handle(
        ObtenerArticuloPublicoQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPublicoPorIdAsync(query.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", query.IdArticulo);
    }
}

/// <summary>
/// Descarga anonima de una IMAGEN incrustada en un articulo publico. No existe un
/// equivalente publico para adjuntos sueltos: por decision de negocio la pagina publica
/// muestra las imagenes del texto pero no ofrece descargar archivos.
/// </summary>
public record ObtenerImagenPublicaQuery(Guid GuidArchivo) : IRequest<DescargaImagenPublica>;

/// <summary>Contenido listo para transmitir; el controller resuelve el Content-Type por extension.</summary>
public record DescargaImagenPublica(Stream Contenido, string NombreArchivo, string? Extension);

public class ObtenerImagenPublicaHandler(
    IConocimientoQueryService consultas,
    IArchivoRepository archivos,
    IAlmacenArchivos almacen) : IRequestHandler<ObtenerImagenPublicaQuery, DescargaImagenPublica>
{
    public async Task<DescargaImagenPublica> Handle(
        ObtenerImagenPublicaQuery query, CancellationToken cancellationToken)
    {
        // La autorizacion va PRIMERO y es la unica puerta: si el GUID no es una imagen
        // incrustada en un articulo publico, responde 404 sin tocar el almacen.
        if (!await consultas.EsImagenDeArticuloPublicoAsync(query.GuidArchivo, cancellationToken))
        {
            throw new NotFoundException("Archivo", query.GuidArchivo);
        }

        var metadatos = await archivos.ObtenerDescargaAsync(query.GuidArchivo, cancellationToken)
            ?? throw new NotFoundException("Archivo", query.GuidArchivo);

        var contenido = await almacen.ObtenerAsync(metadatos.GuidArchivo, cancellationToken);
        return new DescargaImagenPublica(contenido, metadatos.NombreArchivo, metadatos.Extension);
    }
}
