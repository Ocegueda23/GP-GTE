using FluentValidation;
using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Archivos;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record SubirFotoUsuarioCommand(int IdUsuario, Stream Contenido, string NombreArchivo, long TamanoBytes)
    : IRequest<ArchivoResponse>;

public class SubirFotoUsuarioValidator : AbstractValidator<SubirFotoUsuarioCommand>
{
    public SubirFotoUsuarioValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
        RuleFor(c => c.NombreArchivo).NotEmpty();
        RuleFor(c => c.TamanoBytes).GreaterThan(0)
            .LessThanOrEqualTo(ConstantesArchivos.TamanoMaximoBytes)
            .WithMessage(
                $"La foto excede el tamano maximo permitido ({ConstantesArchivos.TamanoMaximoBytes / 1024 / 1024} MB).");
        RuleFor(c => c.NombreArchivo)
            .Must(nombre => ConstantesArchivos.ExtensionesImagenPermitidas.Contains(Path.GetExtension(nombre)))
            .WithMessage("La foto de perfil debe ser una imagen (png, jpg o webp).");
    }
}

/// <summary>
/// Reemplaza la foto de perfil del usuario: a diferencia de los adjuntos de WorkItem/Solicitud
/// (que pueden acumularse), un usuario solo tiene una foto activa a la vez -- se desvincula
/// cualquier foto anterior antes de vincular la nueva.
/// </summary>
public class SubirFotoUsuarioHandler(
    IArchivoRepository repositorio,
    IArchivoQueryService consultasArchivo,
    IAdministracionQueryService consultasUsuario,
    IAlmacenArchivos almacen,
    IVerificadorPermisos permisos) : IRequestHandler<SubirFotoUsuarioCommand, ArchivoResponse>
{
    public async Task<ArchivoResponse> Handle(SubirFotoUsuarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultasUsuario.ObtenerUsuarioAsync(command.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuario);

        var fotosPrevias = await consultasArchivo.ObtenerPorEntidadAsync("Usuario", command.IdUsuario, cancellationToken);
        foreach (var foto in fotosPrevias)
        {
            await repositorio.DesvincularAsync(foto.IdArchivoVinculo, cancellationToken);
        }

        var guardado = await almacen.GuardarAsync(command.Contenido, command.NombreArchivo, cancellationToken);

        var vinculo = await repositorio.VincularAsync(
            new ArchivoNuevo(
                "Usuario", command.IdUsuario, guardado.GuidArchivo, guardado.NombreArchivo,
                guardado.Extension, guardado.TamanoBytes, guardado.RutaRelativa, guardado.HashSha256),
            cancellationToken);

        return await consultasArchivo.ObtenerPorVinculoAsync(vinculo.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", vinculo.IdArchivoVinculo);
    }
}
