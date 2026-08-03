using FluentValidation;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.WorkItems.Commands;

public record CrearWorkItemCommand(WorkItemCrearRequest Datos) : IRequest<WorkItemResponse>;

public class CrearWorkItemValidator : AbstractValidator<CrearWorkItemCommand>
{
    public CrearWorkItemValidator()
    {
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.IdTipoWorkItem).GreaterThan(0).WithMessage("El tipo es obligatorio.");
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0).WithMessage("La prioridad es obligatoria.");
        RuleFor(c => c.Datos.PuntosHistoria).GreaterThanOrEqualTo(0).When(c => c.Datos.PuntosHistoria.HasValue);
    }
}

public class CrearWorkItemHandler(
    IWorkItemRepository repositorio,
    IWorkItemQueryService consultas,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador) : IRequestHandler<CrearWorkItemCommand, WorkItemResponse>
{
    public async Task<WorkItemResponse> Handle(CrearWorkItemCommand command, CancellationToken cancellationToken)
    {
        var datos = command.Datos;
        var descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : sanitizador.Sanitizar(datos.Descripcion);

        var proyecto = await repositorio.ObtenerProyectoAsync(datos.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", datos.IdProyecto);
        if (!proyecto.Activo)
        {
            throw new BusinessException("El proyecto esta inactivo; no admite elementos nuevos.");
        }

        // RN-REQ-04: compromiso en el pasado solo con permiso
        if (datos.FechaCompromiso.HasValue && datos.FechaCompromiso.Value.Date < DateTime.Today
            && !await permisos.TienePermisoAsync(PermisosWorkItem.ModificarCompromiso, datos.IdProyecto, cancellationToken))
        {
            throw new BusinessException("La fecha compromiso no puede ser anterior a hoy.");
        }

        // RN-REQ-08: presupuesto congelado al asignar (matriz complejidad x nivel del asignado)
        var minutosPresupuesto = await CalcularPresupuestoAsync(
            repositorio, datos.IdComplejidad, datos.IdAsignado, cancellationToken);

        var folio = await folios.GenerarAsync(proyecto.Clave, cancellationToken: cancellationToken);

        // El estatus inicial lo fija el backend (Pendiente); el repositorio siembra el historial
        var idWorkItem = await repositorio.CrearAsync(new WorkItemNuevo(
            folio, datos.IdTipoWorkItem, datos.IdPadre, datos.IdProyecto, datos.IdSolicitud,
            datos.Titulo.Trim(), descripcion, datos.CriteriosAceptacion, datos.IdPrioridad,
            datos.IdComplejidad, datos.IdAsignado, datos.IdSolicitante, datos.PuntosHistoria,
            minutosPresupuesto, datos.FechaCompromiso), cancellationToken);

        return await consultas.ObtenerPorIdAsync(idWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", idWorkItem);
    }

    internal static async Task<int?> CalcularPresupuestoAsync(
        IWorkItemRepository repositorio, int? idComplejidad, int? idAsignado, CancellationToken cancellationToken)
    {
        if (!idComplejidad.HasValue || !idAsignado.HasValue)
        {
            return null;
        }

        var usuario = await repositorio.ObtenerUsuarioAsync(idAsignado.Value, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            throw new BusinessException("El asignado no existe o esta inactivo.");
        }

        return usuario.IdNivel.HasValue
            ? await repositorio.ObtenerMinutosMatrizAsync(idComplejidad.Value, usuario.IdNivel.Value, cancellationToken)
            : null;
    }
}
