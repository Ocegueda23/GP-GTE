using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirArchivoRevisionCommand(int IdRevision, Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoResponse>;

public class SubirArchivoRevisionValidator : AbstractValidator<SubirArchivoRevisionCommand>
{
    public SubirArchivoRevisionValidator()
    {
        RuleFor(c => c.IdRevision).GreaterThan(0);
        RuleFor(c => c.NombreArchivo).NotEmpty();
        RuleFor(c => c.TamanoBytes).GreaterThan(0)
            .LessThanOrEqualTo(ConstantesArchivos.TamanoMaximoBytes)
            .WithMessage(
                $"El archivo excede el tamano maximo permitido ({ConstantesArchivos.TamanoMaximoBytes / 1024 / 1024} MB).");
        RuleFor(c => c.NombreArchivo)
            .Must(nombre => ConstantesArchivos.ExtensionesPermitidas.Contains(Path.GetExtension(nombre)))
            .WithMessage("Tipo de archivo no permitido.");
    }
}

/// <summary>Mismo patron que SubirArchivoCommand (WorkItem), validando contra el hallazgo (Revision).</summary>
public class SubirArchivoRevisionHandler(
    IArchivoRepository repositorio,
    IArchivoQueryService consultas,
    IRevisionRepository revisiones,
    IAlmacenArchivos almacen) : IRequestHandler<SubirArchivoRevisionCommand, ArchivoResponse>
{
    public async Task<ArchivoResponse> Handle(SubirArchivoRevisionCommand command, CancellationToken cancellationToken)
    {
        var estado = await revisiones.ObtenerEstadoAsync(command.IdRevision, cancellationToken)
            ?? throw new NotFoundException("Revision", command.IdRevision);

        if (!estado.Activo)
        {
            throw new BusinessException("No se pueden adjuntar archivos a un hallazgo eliminado.");
        }

        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        var vinculo = await repositorio.VincularAsync(
            new ArchivoNuevo(
                "Revision", command.IdRevision, guardado.GuidArchivo, guardado.NombreArchivo,
                guardado.Extension, guardado.TamanoBytes, guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return await consultas.ObtenerPorVinculoAsync(vinculo.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", vinculo.IdArchivoVinculo);
    }
}
