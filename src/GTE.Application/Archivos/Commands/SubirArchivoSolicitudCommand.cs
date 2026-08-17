using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirArchivoSolicitudCommand(int IdSolicitud, Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoResponse>;

public class SubirArchivoSolicitudValidator : AbstractValidator<SubirArchivoSolicitudCommand>
{
    public SubirArchivoSolicitudValidator()
    {
        RuleFor(c => c.IdSolicitud).GreaterThan(0);
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

/// <summary>Mismo patron que SubirArchivoCommand (WorkItem), validando contra Solicitud en vez de WorkItem.</summary>
public class SubirArchivoSolicitudHandler(
    IArchivoRepository repositorio,
    IArchivoQueryService consultas,
    ISolicitudRepository solicitudes,
    IAlmacenArchivos almacen) : IRequestHandler<SubirArchivoSolicitudCommand, ArchivoResponse>
{
    public async Task<ArchivoResponse> Handle(SubirArchivoSolicitudCommand command, CancellationToken cancellationToken)
    {
        var estado = await solicitudes.ObtenerEstadoAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);

        if (!estado.Activo)
        {
            throw new BusinessException("No se pueden adjuntar archivos a una solicitud eliminada.");
        }

        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        var vinculo = await repositorio.VincularAsync(
            new ArchivoNuevo(
                "Solicitud", command.IdSolicitud, guardado.GuidArchivo, guardado.NombreArchivo,
                guardado.Extension, guardado.TamanoBytes, guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return await consultas.ObtenerPorVinculoAsync(vinculo.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", vinculo.IdArchivoVinculo);
    }
}
