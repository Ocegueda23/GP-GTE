using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirArchivoBorradorCommand(Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoBorradorResponse>;

public class SubirArchivoBorradorValidator : AbstractValidator<SubirArchivoBorradorCommand>
{
    public SubirArchivoBorradorValidator()
    {
        RuleFor(c => c.NombreArchivo).NotEmpty();
        RuleFor(c => c.TamanoBytes).GreaterThan(0)
            .LessThanOrEqualTo(ConstantesArchivos.TamanoMaximoBytes)
            .WithMessage(
                $"El archivo excede el tamano maximo permitido ({ConstantesArchivos.TamanoMaximoBytes / 1024 / 1024} MB).");
        // Solo imagen: el borrador existe para las imagenes pegadas en el editor enriquecido.
        // Los adjuntos normales siguen exigiendo una entidad ya creada.
        RuleFor(c => c.NombreArchivo)
            .Must(nombre => ConstantesArchivos.ExtensionesImagenPermitidas.Contains(Path.GetExtension(nombre)))
            .WithMessage("Solo se pueden pegar imagenes antes de guardar.");
    }
}

/// <summary>
/// Sube una imagen pegada en un formulario de ALTA, cuando la entidad destino todavia no
/// tiene Id: guarda el binario y crea tblArchivo sin vinculo. El vinculo se crea al guardar
/// la entidad, que es la que sabe a que se adjunta. Sin esto, el alta con imagen obligaba a
/// guardar, reabrir y volver a guardar, dejando dos versiones del contenido.
///
/// No exige un permiso concreto porque todavia no hay entidad sobre la cual evaluarlo; basta
/// la identidad valida que impone la FallbackPolicy. El archivo huerfano solo lo puede leer
/// quien lo subio y lo recoge el job de purga si el alta se abandona.
/// </summary>
public class SubirArchivoBorradorHandler(
    IArchivoRepository repositorio,
    IAlmacenArchivos almacen) : IRequestHandler<SubirArchivoBorradorCommand, ArchivoBorradorResponse>
{
    public async Task<ArchivoBorradorResponse> Handle(
        SubirArchivoBorradorCommand command, CancellationToken cancellationToken)
    {
        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        await repositorio.CrearBorradorAsync(
            new ArchivoBorrador(
                guardado.GuidArchivo, guardado.NombreArchivo, guardado.Extension,
                guardado.TamanoBytes, guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return new ArchivoBorradorResponse
        {
            GuidArchivo = guardado.GuidArchivo,
            NombreArchivo = guardado.NombreArchivo,
            TamanoBytes = guardado.TamanoBytes
        };
    }
}
