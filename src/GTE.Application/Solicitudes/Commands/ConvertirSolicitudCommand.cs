using FluentValidation;
using GTE.Application.DTOs.Request.Solicitudes;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Solicitudes;
using MediatR;

namespace GTE.Application.Solicitudes.Commands;

public record ConvertirSolicitudCommand(int IdSolicitud, ConvertirSolicitudRequest Datos)
    : IRequest<ConversionResponse>;

public class ConvertirSolicitudValidator : AbstractValidator<ConvertirSolicitudCommand>
{
    public ConvertirSolicitudValidator()
    {
        RuleFor(c => c.IdSolicitud).GreaterThan(0);
        RuleFor(c => c.Datos.Items).NotEmpty().WithMessage("La conversion requiere al menos un elemento de trabajo.");
        RuleForEach(c => c.Datos.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.UiId).NotEmpty().WithMessage("Cada item del desglose requiere uiId.");
            item.RuleFor(i => i.Titulo).NotEmpty().WithMessage("El titulo del item es obligatorio.").MaximumLength(200);
            item.RuleFor(i => i.IdTipoWorkItem).GreaterThan(0);
            item.RuleFor(i => i.IdPrioridad).GreaterThan(0);
        });
    }
}

/// <summary>
/// CONVERTIR (RN de trazabilidad): crea los WorkItems del desglose REUTILIZANDO el
/// comando de creacion (folios, presupuesto, historial, reglas) con IdSolicitud
/// e IdSolicitante de la solicitud, y despues ejecuta la transicion a Convertida.
/// Responde el eco uiId -> Id/folio reales para rehidratar el front sin depender del orden.
/// </summary>
public class ConvertirSolicitudHandler(
    ISolicitudRepository repositorio,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    ISender mediator) : IRequestHandler<ConvertirSolicitudCommand, ConversionResponse>
{
    public async Task<ConversionResponse> Handle(
        ConvertirSolicitudCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosSolicitud.Triage, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", command.IdSolicitud);

        if (estado.IdEstatus != EstatusSolicitud.Aprobada)
        {
            throw new BusinessException("Solo se convierten solicitudes en estatus Aprobada.");
        }
        if (!estado.IdProyecto.HasValue)
        {
            throw new BusinessException("La solicitud no tiene proyecto destino asignado.");
        }

        var convertidos = new List<ItemConvertidoResponse>();
        foreach (var item in command.Datos.Items)
        {
            var creado = await mediator.Send(new CrearWorkItemCommand(new WorkItemCrearRequest
            {
                IdProyecto = estado.IdProyecto.Value,
                IdTipoWorkItem = item.IdTipoWorkItem,
                Titulo = item.Titulo,
                Descripcion = item.Descripcion,
                IdPrioridad = item.IdPrioridad,
                IdAsignado = item.IdAsignado,
                IdSolicitante = estado.IdSolicitante,
                IdSolicitud = estado.IdSolicitud,
                FechaCompromiso = item.FechaCompromiso
            }), cancellationToken);

            convertidos.Add(new ItemConvertidoResponse
            {
                UiId = item.UiId,
                IdWorkItem = creado.IdWorkItem,
                Folio = creado.Folio
            });
        }

        await motor.EjecutarAccionAsync(
            "Solicitud", command.IdSolicitud, AccionesSolicitud.Convertir, null, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(
            command.IdSolicitud, AccionesSolicitud.Convertir, cancellationToken);

        return new ConversionResponse { Items = convertidos };
    }
}
