using FluentValidation;
using GTE.Application.DTOs.Request.Comentarios;
using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using GTE.Domain.Comentarios;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Comentarios.Commands;

public record CrearComentarioCommand(int IdWorkItem, ComentarioCrearRequest Datos) : IRequest<ComentarioResponse>;

public class CrearComentarioValidator : AbstractValidator<CrearComentarioCommand>
{
    public CrearComentarioValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Contenido).NotEmpty()
            .WithMessage("Escribe algo antes de comentar.");
    }
}

/// <summary>Comenta sobre un WorkItem. El contenido se sanitiza antes de guardarse (nunca se confia en el HTML del front).</summary>
public class CrearComentarioHandler(
    IComentarioRepository repositorio,
    IComentarioQueryService consultas,
    IWorkItemRepository workItems,
    ISanitizadorHtml sanitizador) : IRequestHandler<CrearComentarioCommand, ComentarioResponse>
{
    public async Task<ComentarioResponse> Handle(CrearComentarioCommand command, CancellationToken cancellationToken)
    {
        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (!estadoItem.Activo)
        {
            throw new BusinessException("No se puede comentar un elemento eliminado.");
        }

        if (command.Datos.IdComentarioPadre.HasValue)
        {
            var padre = await repositorio.ObtenerEstadoAsync(
                command.Datos.IdComentarioPadre.Value, cancellationToken);
            if (padre is null || !padre.Activo)
            {
                throw new NotFoundException("Comentario", command.Datos.IdComentarioPadre.Value);
            }
        }

        var html = sanitizador.Sanitizar(command.Datos.Contenido);
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new BusinessException("El comentario quedo vacio despues de limpiar el formato.");
        }

        var idComentario = await repositorio.CrearAsync(
            new ComentarioNuevo("WorkItem", command.IdWorkItem, html, command.Datos.IdComentarioPadre),
            cancellationToken);

        return await consultas.ObtenerPorIdAsync(idComentario, cancellationToken)
            ?? throw new NotFoundException("Comentario", idComentario);
    }
}
