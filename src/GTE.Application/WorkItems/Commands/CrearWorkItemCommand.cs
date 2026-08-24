using FluentValidation;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Archivos;
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
        // Sin NotNull aqui a proposito: este comando tambien lo reutilizan flujos internos
        // sin momento de captura humana (ConvertirSolicitudCommand, VincularCorrectivoIncidenteCommand,
        // EscalarTicketCommand, CalidadCommands al crear un bug desde una ejecucion fallida) --
        // ver CrearWorkItemHandler.Handle, que rellena un default cuando viene null. La UI de
        // alta manual (NuevoItemModal.tsx) SI la exige como campo obligatorio, para que una
        // persona la elija a conciencia en el camino principal.
    }
}

public class CrearWorkItemHandler(
    IWorkItemRepository repositorio,
    IWorkItemQueryService consultas,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador,
    IProveedorUsuarioActual proveedorUsuario,
    IArchivoRepository archivos) : IRequestHandler<CrearWorkItemCommand, WorkItemResponse>
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
        if (proyecto.IdEstatusProyecto is EstatusProyecto.Cerrado or EstatusProyecto.Cancelado)
        {
            throw new BusinessException("El proyecto esta cerrado; no admite elementos nuevos.");
        }

        // Proyecto administrado: crear elementos exige un permiso especifico ademas del
        // flujo normal; en proyectos no administrados no cambia nada (decision del equipo).
        if (proyecto.Administrado)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.CrearEnAdministrado, datos.IdProyecto, cancellationToken);
        }

        // RN-REQ-05: agregar una subtarea a un elemento ajeno (asignado a otra persona
        // o sin asignar) cuenta como "modificar" el padre -- mismo gate que
        // RegistrarTiempoCommand/CambiarEstatusWorkItemCommand. Sin asignar cuenta como
        // ajeno (decision del equipo 2026-08-02).
        if (datos.IdPadre.HasValue)
        {
            var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken)
                ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");
            var estadoPadre = await repositorio.ObtenerEstadoAsync(datos.IdPadre.Value, cancellationToken)
                ?? throw new NotFoundException("WorkItem", datos.IdPadre.Value);

            var esAjeno = estadoPadre.IdAsignado != usuarioActual.IdUsuario;
            if (esAjeno)
            {
                await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarAjeno, estadoPadre.IdProyecto, cancellationToken);
            }
        }

        // RN-REQ-04: compromiso en el pasado solo con permiso
        if (datos.FechaCompromiso.HasValue && datos.FechaCompromiso.Value.Date < DateTime.Today
            && !await permisos.TienePermisoAsync(PermisosWorkItem.ModificarCompromiso, datos.IdProyecto, cancellationToken))
        {
            throw new BusinessException("La fecha compromiso no puede ser anterior a hoy.");
        }

        // La complejidad nunca queda vacia: si quien crea el item no la trae (flujos
        // internos que no tienen un momento de captura humana), se usa la de menor Orden
        // activa como default -- asi RN-REQ-08 siempre tiene con que calcular presupuesto,
        // sin bloquear esos flujos ni obligarlos a construir su propio selector.
        var idComplejidadEfectiva = datos.IdComplejidad
            ?? await repositorio.ObtenerComplejidadPorDefectoAsync(cancellationToken);

        // RN-REQ-08: presupuesto (minutos + puntos de historia) congelado al asignar
        // (matriz complejidad x nivel del asignado)
        var (minutosPresupuesto, puntosHistoria) = await CalcularPresupuestoAsync(
            repositorio, idComplejidadEfectiva, datos.IdAsignado, cancellationToken);

        var folio = await folios.GenerarAsync(proyecto.Clave, cancellationToken: cancellationToken);

        // El estatus inicial lo fija el backend (Pendiente); el repositorio siembra el historial
        var idWorkItem = await repositorio.CrearAsync(new WorkItemNuevo(
            folio, datos.IdTipoWorkItem, datos.IdPadre, datos.IdProyecto, datos.IdSolicitud,
            datos.Titulo.Trim(), descripcion, datos.CriteriosAceptacion, datos.IdPrioridad,
            idComplejidadEfectiva, datos.IdAsignado, datos.IdSolicitante, puntosHistoria,
            minutosPresupuesto, datos.FechaCompromiso, datos.IdUsuarioSolicitante), cancellationToken);

        // Las imagenes pegadas durante el alta se subieron en borrador (sin vinculo, porque la
        // entidad aun no tenia Id): ahora que existe, se adjuntan.
        await archivos.VincularBorradoresAsync(
            "WorkItem", idWorkItem, ReferenciasImagenes.ObtenerGuids(descripcion), cancellationToken);

        return await consultas.ObtenerPorIdAsync(idWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", idWorkItem);
    }

    /// <summary>
    /// RN-REQ-08: minutos de presupuesto y puntos de historia se derivan siempre de
    /// tblMatrizPresupuesto (complejidad x nivel del asignado) -- nunca se capturan a mano.
    /// Sin asignado (o sin nivel capturado en el asignado) no hay como resolver la matriz,
    /// ambos quedan en null hasta que se asigne.
    /// </summary>
    internal static async Task<(int? Minutos, decimal? Puntos)> CalcularPresupuestoAsync(
        IWorkItemRepository repositorio, int? idComplejidad, int? idAsignado, CancellationToken cancellationToken)
    {
        if (!idComplejidad.HasValue || !idAsignado.HasValue)
        {
            return (null, null);
        }

        var usuario = await repositorio.ObtenerUsuarioAsync(idAsignado.Value, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            throw new BusinessException("El asignado no existe o esta inactivo.");
        }

        if (!usuario.IdNivel.HasValue)
        {
            return (null, null);
        }

        var presupuesto = await repositorio.ObtenerPresupuestoMatrizAsync(idComplejidad.Value, usuario.IdNivel.Value, cancellationToken);
        return (presupuesto?.Minutos, presupuesto?.Puntos);
    }
}
