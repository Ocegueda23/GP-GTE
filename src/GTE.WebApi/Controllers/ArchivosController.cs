using GTE.Application.Archivos.Commands;
using GTE.Application.Archivos.Queries;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Domain.Archivos;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace GTE.WebApi.Controllers;

/// <summary>Adjuntos sobre WorkItem: subida, listado, descarga por streaming y baja del vinculo.</summary>
[ApiController]
[Route("api/v1")]
public class ArchivosController(IMediator mediator) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ProveedorTipos = new();

    [HttpGet("workitems/{idWorkItem:int}/archivos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ArchivoResponse>>>> ObtenerPorWorkItem(
        int idWorkItem, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerArchivosQuery(idWorkItem), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ArchivoResponse>>.Exito(resultado));
    }

    /// <summary>Tamano y extension se validan en el comando; este limite solo evita leer de mas del body.</summary>
    [HttpPost("workitems/{idWorkItem:int}/archivos")]
    [RequestSizeLimit(ConstantesArchivos.TamanoMaximoBytes)]
    public async Task<ActionResult<ApiResponse<ArchivoResponse>>> Subir(
        int idWorkItem, IFormFile archivo, CancellationToken cancellationToken)
    {
        var nombreArchivo = Path.GetFileName(archivo.FileName);
        if (nombreArchivo.Length > 200)
        {
            nombreArchivo = nombreArchivo[..200];
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await mediator.Send(
            new SubirArchivoCommand(idWorkItem, contenido, nombreArchivo, archivo.Length), cancellationToken);
        return Ok(ApiResponse<ArchivoResponse>.Exito(resultado, $"{resultado.NombreArchivo} adjuntado."));
    }

    /// <summary>
    /// Descarga por streaming, nunca URL directa al share. Requiere identidad valida (fallback
    /// policy global); el GUID no se puede adivinar y solo resuelve si el vinculo sigue activo.
    /// </summary>
    [HttpGet("archivos/{guid:guid}")]
    public async Task<IActionResult> Descargar(Guid guid, CancellationToken cancellationToken)
    {
        var descarga = await mediator.Send(new ObtenerDescargaQuery(guid), cancellationToken);
        var tipoContenido = ProveedorTipos.TryGetContentType(descarga.NombreArchivo, out var tipo)
            ? tipo
            : "application/octet-stream";
        return File(descarga.Contenido, tipoContenido, descarga.NombreArchivo);
    }

    /// <summary>Baja logica del vinculo. Solo quien subio el archivo puede eliminarlo.</summary>
    [HttpDelete("archivos-vinculo/{idArchivoVinculo:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Eliminar(
        int idArchivoVinculo, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarArchivoVinculoCommand(idArchivoVinculo), cancellationToken);
        return Ok(ApiResponse<object>.Exito(new { }, "Adjunto eliminado."));
    }
}
