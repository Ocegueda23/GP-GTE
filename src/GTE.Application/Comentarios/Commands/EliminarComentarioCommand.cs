using FluentValidation;
using GTE.Application.Common;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Comentarios.Commands;

public record EliminarComentarioCommand(int IdComentario) : IRequest<Unit>;

public class EliminarComentarioValidator : AbstractValidator<EliminarComentarioCommand>
{
    public EliminarComentarioValidator()
    {
        RuleFor(c => c.IdComentario).GreaterThan(0);
    }
}

/// <summary>Baja logica. Solo quien escribio el comentario puede eliminarlo (sin admin-override en esta entrega).</summary>
public class EliminarComentarioHandler(
    IComentarioRepository repositorio,
    AuditContext auditoria) : IRequestHandler<EliminarComentarioCommand, Unit>
{
    public async Task<Unit> Handle(EliminarComentarioCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdComentario, cancellationToken)
            ?? throw new NotFoundException("Comentario", command.IdComentario);

        if (estado.Activo)
        {
            if (!string.Equals(estado.UsuarioRegistro, auditoria.Usuario, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Solo quien escribio el comentario puede eliminarlo.");
            }

            await repositorio.EliminarAsync(command.IdComentario, cancellationToken);
        }

        return Unit.Value;
    }
}
