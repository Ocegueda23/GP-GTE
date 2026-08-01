using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirArchivoCommand(int IdWorkItem, Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoResponse>;

public class SubirArchivoValidator : AbstractValidator<SubirArchivoCommand>
{
    public SubirArchivoValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
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

/// <summary>Guarda el binario en el almacen y crea el archivo + vinculo con el WorkItem en una sola operacion.</summary>
public class SubirArchivoHandler(
    IArchivoRepository repositorio,
    IArchivoQueryService consultas,
    IWorkItemRepository workItems,
    IAlmacenArchivos almacen) : IRequestHandler<SubirArchivoCommand, ArchivoResponse>
{
    public async Task<ArchivoResponse> Handle(SubirArchivoCommand command, CancellationToken cancellationToken)
    {
        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (!estadoItem.Activo)
        {
            throw new BusinessException("No se pueden adjuntar archivos a un elemento eliminado.");
        }

        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        var vinculo = await repositorio.VincularAsync(
            new ArchivoNuevo(
                "WorkItem", command.IdWorkItem, guardado.GuidArchivo, guardado.NombreArchivo,
                guardado.Extension, guardado.TamanoBytes, guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return await consultas.ObtenerPorVinculoAsync(vinculo.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", vinculo.IdArchivoVinculo);
    }
}
