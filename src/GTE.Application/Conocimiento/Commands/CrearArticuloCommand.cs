using FluentValidation;
using GTE.Application.DTOs.Request.Conocimiento;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Application.Interfaces;
using GTE.Domain.Archivos;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Conocimiento.Commands;

public record CrearArticuloCommand(ArticuloCrearRequest Datos) : IRequest<ArticuloResponse>;

public class CrearArticuloValidator : AbstractValidator<CrearArticuloCommand>
{
    public CrearArticuloValidator()
    {
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.")
            .MaximumLength(ConstantesConocimiento.LongitudMaximaTitulo);
        RuleFor(c => c.Datos.Contenido).NotEmpty().WithMessage("El contenido es obligatorio.");
    }
}

/// <summary>
/// Alta de articulo o termino del glosario. El contenido HTML se sanitiza antes de
/// guardarse (nunca se confia en el front) y nace en version 1.
/// </summary>
public class CrearArticuloHandler(
    IConocimientoRepository repositorio,
    IConocimientoQueryService consultas,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador,
    IArchivoRepository archivos) : IRequestHandler<CrearArticuloCommand, ArticuloResponse>
{
    public async Task<ArticuloResponse> Handle(CrearArticuloCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosConocimiento.Administrar, null, cancellationToken);

        var titulo = command.Datos.Titulo.Trim();
        if (await repositorio.ExisteTituloAsync(titulo, null, cancellationToken))
        {
            throw new ConflictException($"Ya existe un articulo con el titulo \"{titulo}\".");
        }

        var contenido = sanitizador.Sanitizar(command.Datos.Contenido);
        if (string.IsNullOrWhiteSpace(contenido))
        {
            throw new BusinessException("El contenido quedo vacio despues de sanitizarlo.");
        }

        var idArticulo = await repositorio.CrearAsync(
            new ArticuloNuevo(titulo, contenido, command.Datos.EsGlosario, command.Datos.EsPublico),
            cancellationToken);

        // Las imagenes pegadas durante el alta se subieron en borrador (sin vinculo, porque el
        // articulo aun no tenia Id): ahora que existe, se adjuntan. Asi el alta con imagen
        // queda en una sola version, sin obligar a guardar, reabrir y volver a guardar.
        await archivos.VincularBorradoresAsync(
            ConstantesConocimiento.EntidadArchivo, idArticulo,
            ReferenciasImagenes.ObtenerGuids(contenido), cancellationToken);

        return await consultas.ObtenerPorIdAsync(idArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", idArticulo);
    }
}
