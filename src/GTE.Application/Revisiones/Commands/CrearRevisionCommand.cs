using FluentValidation;
using GTE.Application.DTOs.Request.Revisiones;
using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Revisiones;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.Revisiones.Commands;

public record CrearRevisionCommand(int IdWorkItem, RevisionCrearRequest Datos) : IRequest<RevisionResponse>;

public class CrearRevisionValidator : AbstractValidator<CrearRevisionCommand>
{
    public CrearRevisionValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Comentarios).NotEmpty()
            .WithMessage("Describe el hallazgo para que quien corrija sepa que ajustar.");
    }
}

/// <summary>
/// Reporta un hallazgo de revision (QA o code review).
/// RN-QA-03: si el elemento ya estaba Terminado, el hallazgo lo reabre a
/// Correccion a traves del motor (transicion Terminado-Correccion por RECHAZAR_QA).
/// </summary>
public class CrearRevisionHandler(
    IRevisionRepository repositorio,
    IRevisionQueryService consultas,
    IWorkItemRepository workItems,
    IMotorWorkflow motor,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CrearRevisionCommand, RevisionResponse>
{
    public async Task<RevisionResponse> Handle(CrearRevisionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (estadoItem.IdEstatus == EstatusWorkItem.Cancelado || !estadoItem.Activo)
        {
            throw new BusinessException("No se pueden reportar hallazgos en un elemento cancelado o eliminado.");
        }

        var idRevision = await repositorio.CrearAsync(
            new RevisionNueva(command.IdWorkItem, usuario.IdUsuario, command.Datos.Comentarios.Trim()),
            cancellationToken);

        // RN-QA-03: un hallazgo sobre trabajo ya cerrado lo regresa a Correccion
        if (estadoItem.IdEstatus == EstatusWorkItem.Terminado)
        {
            await motor.EjecutarAccionAsync(
                "WorkItem", command.IdWorkItem, AccionesWorkItem.RechazarQa,
                $"Hallazgo de revision reportado por {usuario.Nombre}", null, cancellationToken);
            await workItems.AplicarEfectosTransicionAsync(
                command.IdWorkItem, AccionesWorkItem.RechazarQa, cancellationToken);
        }

        return await consultas.ObtenerPorIdAsync(idRevision, cancellationToken)
            ?? throw new NotFoundException("Revision", idRevision);
    }
}
