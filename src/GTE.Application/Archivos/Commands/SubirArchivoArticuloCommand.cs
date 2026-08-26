using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirArchivoArticuloCommand(int IdArticulo, Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoResponse>;

public class SubirArchivoArticuloValidator : AbstractValidator<SubirArchivoArticuloCommand>
{
    public SubirArchivoArticuloValidator()
    {
        RuleFor(c => c.IdArticulo).GreaterThan(0);
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

/// <summary>
/// Adjunto de un articulo de la base de conocimiento: mismo almacen y misma tabla de
/// vinculos generica que WorkItem/Solicitud/Revision, solo cambia el discriminador de
/// Entidad. Exige CON.Administrar (adjuntar es editar el articulo).
/// </summary>
public class SubirArchivoArticuloHandler(
    IArchivoRepository repositorio,
    IArchivoQueryService consultas,
    IConocimientoRepository conocimiento,
    IVerificadorPermisos permisos,
    IAlmacenArchivos almacen) : IRequestHandler<SubirArchivoArticuloCommand, ArchivoResponse>
{
    public async Task<ArchivoResponse> Handle(
        SubirArchivoArticuloCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosConocimiento.Administrar, null, cancellationToken);

        var estado = await conocimiento.ObtenerEstadoAsync(command.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", command.IdArticulo);

        if (!estado.Activo)
        {
            throw new BusinessException("No se pueden adjuntar archivos a un articulo eliminado.");
        }

        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        var vinculo = await repositorio.VincularAsync(
            new ArchivoNuevo(
                ConstantesConocimiento.EntidadArchivo, command.IdArticulo, guardado.GuidArchivo,
                guardado.NombreArchivo, guardado.Extension, guardado.TamanoBytes,
                guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return await consultas.ObtenerPorVinculoAsync(vinculo.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", vinculo.IdArchivoVinculo);
    }
}
