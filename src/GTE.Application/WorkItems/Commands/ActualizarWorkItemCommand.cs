using FluentValidation;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.WorkItems.Commands;

public record ActualizarWorkItemCommand(int IdWorkItem, WorkItemActualizarRequest Datos) : IRequest<WorkItemResponse>;

public class ActualizarWorkItemValidator : AbstractValidator<ActualizarWorkItemCommand>
{
    public ActualizarWorkItemValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0).WithMessage("La prioridad es obligatoria.");
        RuleFor(c => c.Datos.PuntosHistoria).GreaterThanOrEqualTo(0).When(c => c.Datos.PuntosHistoria.HasValue);
    }
}

public class ActualizarWorkItemHandler(
    IWorkItemRepository repositorio,
    IWorkItemQueryService consultas,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario,
    ISanitizadorHtml sanitizador) : IRequestHandler<ActualizarWorkItemCommand, WorkItemResponse>
{
    public async Task<WorkItemResponse> Handle(ActualizarWorkItemCommand command, CancellationToken cancellationToken)
    {
        var datos = command.Datos;
        var descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : sanitizador.Sanitizar(datos.Descripcion);
        var estado = await repositorio.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        // RN-REQ-05: item terminado solo con permiso
        if (estado.IdEstatus == EstatusWorkItem.Terminado)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarTerminado, estado.IdProyecto, cancellationToken);
        }

        // RN-REQ-05: item ajeno (asignado a otra persona O SIN asignar) solo con permiso --
        // decision del equipo 2026-08-02: una tarea sin asignar no es "de nadie que la pueda
        // tocar libremente", se trata igual que ajena (evita que cualquiera tome trabajo del
        // backlog sin que un Lider/Admin con WI.ModificarAjeno la asigne primero).
        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken);
        var esAjeno = estado.IdAsignado != usuarioActual?.IdUsuario;
        if (esAjeno)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarAjeno, estado.IdProyecto, cancellationToken);
        }

        // RN-REQ-04: mover el compromiso al pasado solo con permiso
        var compromisoCambio = datos.FechaCompromiso != estado.FechaCompromiso;
        if (compromisoCambio && datos.FechaCompromiso.HasValue
            && datos.FechaCompromiso.Value.Date < DateTime.Today)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarCompromiso, estado.IdProyecto, cancellationToken);
        }

        // Cambiar complejidad exige permiso (regla heredada del GT)
        var complejidadCambio = datos.IdComplejidad != estado.IdComplejidad;
        if (complejidadCambio)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarComplejidad, estado.IdProyecto, cancellationToken);
        }

        // RN-REQ-08: el presupuesto solo se recalcula al reasignar o cambiar complejidad
        var asignadoCambio = datos.IdAsignado != estado.IdAsignado;
        int? minutosPresupuesto = null;
        var recalcularPresupuesto = complejidadCambio || asignadoCambio;
        if (recalcularPresupuesto)
        {
            minutosPresupuesto = await CrearWorkItemHandler.CalcularPresupuestoAsync(
                repositorio, datos.IdComplejidad, datos.IdAsignado, cancellationToken);
        }

        await repositorio.ActualizarAsync(new WorkItemEdicion(
            command.IdWorkItem, datos.Titulo.Trim(), descripcion, datos.CriteriosAceptacion,
            datos.IdPrioridad, datos.IdComplejidad, datos.IdAsignado, datos.PuntosHistoria,
            recalcularPresupuesto, minutosPresupuesto, datos.FechaCompromiso), cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);
    }
}
