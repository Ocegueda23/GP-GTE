using FluentValidation;
using GTE.Application.DTOs.Request.Planeacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Planeacion;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.Planeacion.Commands;

public record ReordenarBacklogCommand(ReordenarBacklogRequest Datos) : IRequest<Unit>;

public class ReordenarBacklogValidator : AbstractValidator<ReordenarBacklogCommand>
{
    public ReordenarBacklogValidator()
    {
        RuleFor(c => c.Datos.IdsEnOrden).NotEmpty()
            .WithMessage("Envia los elementos en el orden deseado.");
        RuleFor(c => c.Datos.IdsEnOrden)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de orden tiene elementos repetidos.");
    }
}

public class ReordenarBacklogHandler(
    IPlaneacionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<ReordenarBacklogCommand, Unit>
{
    public async Task<Unit> Handle(ReordenarBacklogCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosPlaneacion.GestionarSprints, null, cancellationToken);
        await repositorio.ReordenarBacklogAsync(command.Datos.IdsEnOrden, cancellationToken);
        return Unit.Value;
    }
}

public record AsignarSprintCommand(int IdWorkItem, int? IdSprint) : IRequest<Unit>;

/// <summary>
/// Mueve un elemento al sprint o lo regresa al backlog.
/// RN-PLA-03: un elemento pertenece a un solo sprint (la columna es unica);
/// solo se admiten sprints abiertos (Planeado o Activo).
/// </summary>
public class AsignarSprintHandler(
    IPlaneacionRepository repositorio,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos) : IRequestHandler<AsignarSprintCommand, Unit>
{
    public async Task<Unit> Handle(AsignarSprintCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosPlaneacion.GestionarSprints, null, cancellationToken);

        var item = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (item.IdEstatus is EstatusWorkItem.Terminado or EstatusWorkItem.Cancelado)
        {
            throw new BusinessException(
                "Un elemento terminado o cancelado ya no se replanifica; su sprint queda como historia.");
        }

        if (command.IdSprint.HasValue)
        {
            var sprint = await repositorio.ObtenerEstadoSprintAsync(command.IdSprint.Value, cancellationToken)
                ?? throw new NotFoundException("Sprint", command.IdSprint.Value);
            if (sprint.IdEstatus == EstatusSprint.Cerrado)
            {
                throw new BusinessException("No se pueden agregar elementos a un sprint cerrado.");
            }
        }

        await repositorio.AsignarSprintAsync(command.IdWorkItem, command.IdSprint, cancellationToken);
        return Unit.Value;
    }
}
