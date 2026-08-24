using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Conocimiento.Commands;

public record EliminarArticuloCommand(int IdArticulo) : IRequest;

public class EliminarArticuloValidator : AbstractValidator<EliminarArticuloCommand>
{
    public EliminarArticuloValidator()
    {
        RuleFor(c => c.IdArticulo).GreaterThan(0);
    }
}

/// <summary>Baja LOGICA (Activo = 0): un articulo no es un borrador, su historial se conserva.</summary>
public class EliminarArticuloHandler(
    IConocimientoRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<EliminarArticuloCommand>
{
    public async Task Handle(EliminarArticuloCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosConocimiento.Administrar, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdArticulo, cancellationToken)
            ?? throw new NotFoundException("ArticuloConocimiento", command.IdArticulo);

        if (!estado.Activo)
        {
            throw new BusinessException("El articulo ya estaba eliminado.");
        }

        await repositorio.EliminarAsync(command.IdArticulo, cancellationToken);
    }
}
