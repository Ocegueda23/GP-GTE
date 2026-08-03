using FluentValidation;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record VincularCorrectivoIncidenteCommand(int IdIncidente, VincularCorrectivoRequest Datos)
    : IRequest<VincularCorrectivoResponse>;

public class VincularCorrectivoIncidenteValidator : AbstractValidator<VincularCorrectivoIncidenteCommand>
{
    public VincularCorrectivoIncidenteValidator()
    {
        RuleFor(c => c.IdIncidente).GreaterThan(0);
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0).WithMessage("La prioridad del correctivo es obligatoria.");
    }
}

/// <summary>
/// Crea un WorkItem tipo Correccion reutilizando CrearWorkItemCommand (mismo patron
/// que EscalarTicketHandler) y lo vincula al incidente. No cambia el estatus del
/// incidente.
/// </summary>
public class VincularCorrectivoIncidenteHandler(
    IIncidenteRepository repositorio,
    IVerificadorPermisos permisos,
    ISender mediator) : IRequestHandler<VincularCorrectivoIncidenteCommand, VincularCorrectivoResponse>
{
    public async Task<VincularCorrectivoResponse> Handle(VincularCorrectivoIncidenteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);

        if (estado.IdWorkItemCorrectivo.HasValue)
        {
            throw new BusinessException("El incidente ya tiene un elemento de trabajo correctivo vinculado.");
        }

        var titulo = $"Correccion: {estado.Titulo}";
        if (titulo.Length > 200)
        {
            titulo = titulo[..200];
        }

        var creado = await mediator.Send(new CrearWorkItemCommand(new WorkItemCrearRequest
        {
            IdProyecto = estado.IdProyecto,
            IdTipoWorkItem = EstatusIncidente.IdTipoWorkItemCorreccion,
            Titulo = titulo,
            Descripcion = estado.Descripcion,
            IdPrioridad = command.Datos.IdPrioridad,
            IdAsignado = command.Datos.IdAsignado,
            FechaCompromiso = command.Datos.FechaCompromiso
        }), cancellationToken);

        await repositorio.VincularCorrectivoAsync(command.IdIncidente, creado.IdWorkItem, cancellationToken);

        return new VincularCorrectivoResponse { IdWorkItem = creado.IdWorkItem, Folio = creado.Folio };
    }
}
