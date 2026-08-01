using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Queries;

/// <summary>Contenido listo para transmitir por streaming; el controller resuelve el Content-Type por extension.</summary>
public record DescargaArchivo(Stream Contenido, string NombreArchivo, string? Extension);

public record ObtenerDescargaQuery(Guid GuidArchivo) : IRequest<DescargaArchivo>;

public class ObtenerDescargaHandler(
    IArchivoRepository repositorio,
    IAlmacenArchivos almacen) : IRequestHandler<ObtenerDescargaQuery, DescargaArchivo>
{
    public async Task<DescargaArchivo> Handle(ObtenerDescargaQuery query, CancellationToken cancellationToken)
    {
        var metadatos = await repositorio.ObtenerDescargaAsync(query.GuidArchivo, cancellationToken)
            ?? throw new NotFoundException("Archivo", query.GuidArchivo);

        var contenido = await almacen.ObtenerAsync(metadatos.GuidArchivo, cancellationToken);
        return new DescargaArchivo(contenido, metadatos.NombreArchivo, metadatos.Extension);
    }
}
