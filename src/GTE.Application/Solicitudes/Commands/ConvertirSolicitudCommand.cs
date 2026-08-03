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
/// Notifica al solicitante igual que Aprobar/Rechazar/Devolver en
/// CambiarEstatusSolicitudCommand (ahi no cubre Convertir a proposito, tiene su
/// propio comando por el desglose de items); tambien notifica al usuario
/// asignado de cada item del desglose que traiga IdAsignado (no todos lo
/// traen -- el triage puede dejarlo sin asignar).
/// </summary>
public class ConvertirSolicitudHandler(
    ISolicitudRepository repositorio,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IServicioNotificaciones notificaciones,
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
                IdUsuarioSolicitante = estado.IdUsuarioSolicitante,
                FechaCompromiso = item.FechaCompromiso
            }), cancellationToken);

            convertidos.Add(new ItemConvertidoResponse
            {
                UiId = item.UiId,
                IdWorkItem = creado.IdWorkItem,
                Folio = creado.Folio
            });

            if (item.IdAsignado.HasValue)
            {
                await notificaciones.NotificarAsync(
                    [item.IdAsignado.Value], $"Se te asigno el elemento de trabajo {creado.Folio}",
                    item.Titulo, "WorkItem", creado.IdWorkItem, $"/wi/{creado.Folio}", cancellationToken);
            }
        }

        await motor.EjecutarAccionAsync(
            "Solicitud", command.IdSolicitud, AccionesSolicitud.Convertir, null, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(
            command.IdSolicitud, AccionesSolicitud.Convertir, cancellationToken);

        var folios = string.Join(", ", convertidos.Select(c => c.Folio));
        var mensaje = convertidos.Count == 1
            ? $"Se genero el elemento de trabajo {folios}."
            : $"Se generaron los elementos de trabajo: {folios}.";
        await notificaciones.NotificarAsync(
            [estado.IdSolicitante], $"Tu solicitud {estado.Titulo} fue convertida en trabajo", mensaje,
            "Solicitud", command.IdSolicitud, "/solicitudes", cancellationToken);

        return new ConversionResponse { Items = convertidos };
    }
}
