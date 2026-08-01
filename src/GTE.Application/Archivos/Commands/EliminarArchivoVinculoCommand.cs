using FluentValidation;
using GTE.Application.Common;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Archivos.Commands;

public record EliminarArchivoVinculoCommand(int IdArchivoVinculo) : IRequest<Unit>;

public class EliminarArchivoVinculoValidator : AbstractValidator<EliminarArchivoVinculoCommand>
{
    public EliminarArchivoVinculoValidator()
    {
        RuleFor(c => c.IdArchivoVinculo).GreaterThan(0);
    }
}

/// <summary>Baja logica del vinculo. Solo quien subio el archivo puede eliminarlo (sin admin-override en esta entrega).</summary>
public class EliminarArchivoVinculoHandler(
    IArchivoRepository repositorio,
    AuditContext auditoria) : IRequestHandler<EliminarArchivoVinculoCommand, Unit>
{
    public async Task<Unit> Handle(EliminarArchivoVinculoCommand command, CancellationToken cancellationToken)
    {
        var vinculo = await repositorio.ObtenerVinculoAsync(command.IdArchivoVinculo, cancellationToken)
            ?? throw new NotFoundException("ArchivoVinculo", command.IdArchivoVinculo);

        if (vinculo.Activo)
        {
            if (!string.Equals(vinculo.UsuarioRegistro, auditoria.Usuario, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Solo quien subio el archivo puede eliminarlo.");
            }

            await repositorio.DesvincularAsync(command.IdArchivoVinculo, cancellationToken);
        }

        return Unit.Value;
    }
}
