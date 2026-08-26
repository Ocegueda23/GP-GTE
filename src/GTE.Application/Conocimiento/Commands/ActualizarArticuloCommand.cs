using FluentValidation;
using GTE.Application.DTOs.Request.Conocimiento;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Conocimiento.Commands;

public record ActualizarArticuloCommand(int IdArticulo, ArticuloActualizarRequest Datos)
    : IRequest<ArticuloResponse>;

public class ActualizarArticuloValidator : AbstractValidator<ActualizarArticuloCommand>
{
    public ActualizarArticuloValidator()
    {
        RuleFor(c => c.IdArticulo).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.")
            .MaximumLength(ConstantesConocimiento.LongitudMaximaTitulo);
        RuleFor(c => c.Datos.Contenido).NotEmpty().WithMessage("El contenido es obligatorio.");
    }
}

/// <summary>
/// Edicion de articulo. El repositorio decide si genera version nueva: solo cuando el
/// contenido sanitizado quedo distinto del vigente (renombrar o cambiar los switches
/// de glosario/publico no ensucia el historial).
/// </summary>
public class ActualizarArticuloHandler(
    IConocimientoRepository repositorio,
    IConocimientoQueryService consultas,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador) : IRequestHandler<ActualizarArticuloCommand, ArticuloResponse>
{
    public async Task<ArticuloResponse> Handle(ActualizarArticuloCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosConocimiento.Administrar, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", command.IdArticulo);

        if (!estado.Activo)
        {
            throw new BusinessException("No se puede editar un articulo eliminado.");
        }

        var titulo = command.Datos.Titulo.Trim();
        if (await repositorio.ExisteTituloAsync(titulo, command.IdArticulo, cancellationToken))
        {
            throw new ConflictException($"Ya existe otro articulo con el titulo \"{titulo}\".");
        }

        var contenido = sanitizador.Sanitizar(command.Datos.Contenido);
        if (string.IsNullOrWhiteSpace(contenido))
        {
            throw new BusinessException("El contenido quedo vacio despues de sanitizarlo.");
        }

        await repositorio.ActualizarAsync(
            command.IdArticulo,
            new ArticuloActualizacion(titulo, contenido, command.Datos.EsGlosario, command.Datos.EsPublico),
            cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", command.IdArticulo);
    }
}
