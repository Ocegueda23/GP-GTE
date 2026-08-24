using GTE.Application.Archivos.Commands;
using GTE.Application.Archivos.Queries;
using GTE.Application.Common;
using GTE.Application.Conocimiento.Commands;
using GTE.Application.Conocimiento.Queries;
using GTE.Application.DTOs.Request.Conocimiento;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Domain.Archivos;
using GTE.Domain.Conocimiento;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Base de conocimiento (P23), consumo INTERNO. La lectura solo exige identidad (P23 es
/// "Todos" en el Documento Maestro, seccion 5.1); escribir exige CON.Administrar, que se
/// valida en cada handler. El consumo anonimo vive aparte, en ConocimientoPublicoController.
/// </summary>
[ApiController]
[Route("api/v1/conocimiento")]
public class ConocimientoController(IMediator mediator) : ControllerBase
{
    /// <summary>Listado/buscador. esGlosario sin valor = articulos y terminos juntos.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ArticuloListaResponse>>>> ObtenerLista(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? texto = null,
        [FromQuery] bool? esGlosario = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = new FiltroArticulos(page, pageSize, texto, esGlosario);
        var resultado = await mediator.Send(new ObtenerArticulosQuery(filtro), cancellationToken);
        return Ok(ApiResponse<PagedResult<ArticuloListaResponse>>.Exito(resultado));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ArticuloResponse>>> ObtenerPorId(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerArticuloQuery(id), cancellationToken);
        return Ok(ApiResponse<ArticuloResponse>.Exito(resultado));
    }

    /// <summary>Historial completo; la version vigente viene marcada con EsVersionActual.</summary>
    [HttpGet("{id:int}/versiones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ArticuloVersionResponse>>>> ObtenerVersiones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerVersionesArticuloQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ArticuloVersionResponse>>.Exito(resultado));
    }

    /// <summary>Contenido de una version historica, para verla sin restaurarla.</summary>
    [HttpGet("{id:int}/versiones/{version:int}")]
    public async Task<ActionResult<ApiResponse<ArticuloVersionContenidoResponse>>> ObtenerVersion(
        int id, int version, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerVersionArticuloQuery(id, version), cancellationToken);
        return Ok(ApiResponse<ArticuloVersionContenidoResponse>.Exito(resultado));
    }

    /// <summary>Alta (CON.Administrar). Nace en version 1; el HTML se sanitiza en el backend.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ArticuloResponse>>> Crear(
        [FromBody] ArticuloCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearArticuloCommand(request), cancellationToken);
        return Ok(ApiResponse<ArticuloResponse>.Exito(resultado, $"\"{resultado.Titulo}\" se guardo correctamente."));
    }

    /// <summary>Edicion (CON.Administrar). Genera version nueva solo si el contenido cambio.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ArticuloResponse>>> Actualizar(
        int id, [FromBody] ArticuloActualizarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarArticuloCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ArticuloResponse>.Exito(resultado, $"\"{resultado.Titulo}\" se actualizo correctamente."));
    }

    /// <summary>Baja logica (CON.Administrar). Deja de estar publicado en el mismo movimiento.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Eliminar(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarArticuloCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Articulo eliminado."));
    }

    /// <summary>Adjuntos del articulo (incluye las imagenes que el editor pega en el texto).</summary>
    [HttpGet("{id:int}/archivos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ArchivoResponse>>>> ObtenerArchivos(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerArchivosArticuloQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ArchivoResponse>>.Exito(resultado));
    }

    /// <summary>Tamano y extension se validan en el comando; este limite solo evita leer de mas del body.</summary>
    [HttpPost("{id:int}/archivos")]
    [RequestSizeLimit(ConstantesArchivos.TamanoMaximoBytes)]
    public async Task<ActionResult<ApiResponse<ArchivoResponse>>> SubirArchivo(
        int id, IFormFile archivo, CancellationToken cancellationToken)
    {
        var nombreArchivo = Path.GetFileName(archivo.FileName);
        if (nombreArchivo.Length > 200)
        {
            nombreArchivo = nombreArchivo[..200];
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await mediator.Send(
            new SubirArchivoArticuloCommand(id, contenido, nombreArchivo, archivo.Length), cancellationToken);
        return Ok(ApiResponse<ArchivoResponse>.Exito(resultado, $"{resultado.NombreArchivo} adjuntado."));
    }
}
