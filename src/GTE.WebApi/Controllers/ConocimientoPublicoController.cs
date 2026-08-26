using GTE.Application.Common;
using GTE.Application.Conocimiento.Queries;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Domain.Conocimiento;
using GTE.WebApi.Models;
using GTE.WebApi.Seguridad;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Base de conocimiento, consumo ANONIMO (P23 expuesta al publico).
///
/// RAZON DEL [AllowAnonymous] (excepcion explicita al FallbackPolicy, exigida por
/// CLAUDE.md): el negocio decidio publicar la base de conocimiento para que clientes y
/// usuarios externos puedan consultar definiciones y procedimientos sin una cuenta de
/// GTE. Los limites de esa excepcion:
///
///   1. Solo alcanza articulos con EsPublico = 1 (Activo = 1). El default es privado y
///      marcarlo publico es una accion deliberada del autor (permiso CON.Administrar).
///   2. Un articulo interno responde 404, igual que uno inexistente: nadie puede
///      enumerar el contenido privado probando ids.
///   3. Solo LECTURA. No hay alta, edicion ni baja por esta via.
///   4. Las respuestas usan DTOs propios (ArticuloPublico*) que NO exponen autores,
///      numero de version ni el historial: metadata interna que un visitante externo no
///      tiene por que ver.
///   5. Las imagenes del texto se sirven una por una y solo si estan incrustadas en un
///      articulo publico. NO existe endpoint publico de adjuntos: por decision de
///      negocio la pagina publica muestra las imagenes del articulo pero no ofrece
///      descargar archivos.
///   6. Todo el controlador esta bajo el limitador "publico" (Program.cs): a diferencia
///      del resto de la API, estas rutas quedan expuestas a internet.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(LimitadoresTasa.Publico)]
[Route("api/v1/publico/conocimiento")]
public class ConocimientoPublicoController(IMediator mediator) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ProveedorTipos = new();

    /// <summary>Listado/buscador publico. Solo articulos marcados publicos.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ArticuloPublicoListaResponse>>>> ObtenerLista(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? texto = null,
        [FromQuery] bool? esGlosario = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroArticulos(page, pageSize, texto, esGlosario);
        var resultado = await mediator.Send(new ObtenerArticulosPublicosQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<ArticuloPublicoListaResponse>>.Exito(resultado));
    }

    /// <summary>Detalle publico. 404 si el articulo no existe o no esta marcado publico.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ArticuloPublicoResponse>>> ObtenerPorId(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerArticuloPublicoQuery(id), cancellationToken);
        return Ok(ApiResponse<ArticuloPublicoResponse>.Exito(resultado));
    }

    /// <summary>
    /// Imagen incrustada en un articulo publico. Se sirve inline (no como descarga
    /// adjunta): es parte del texto del articulo, no un archivo que se ofrezca guardar.
    /// El handler responde 404 si el GUID no corresponde a una imagen dentro de un
    /// articulo publico.
    /// </summary>
    [HttpGet("imagenes/{guid:guid}")]
    public async Task<IActionResult> ObtenerImagen(Guid guid, CancellationToken cancellationToken)
    {
        var imagen = await mediator.Send(new ObtenerImagenPublicaQuery(guid), cancellationToken);
        var tipoContenido = ProveedorTipos.TryGetContentType(imagen.NombreArchivo, out var tipo)
            ? tipo
            : "application/octet-stream";

        // Sin nombre de archivo: File(stream, tipo) responde inline, sin Content-Disposition
        // attachment, asi el navegador la pinta dentro del articulo en vez de descargarla.
        return File(imagen.Contenido, tipoContenido);
    }
}
