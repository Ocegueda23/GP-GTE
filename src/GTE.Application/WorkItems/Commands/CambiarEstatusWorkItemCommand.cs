using FluentValidation;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.WorkItems.Commands;

public record CambiarEstatusWorkItemCommand(int IdWorkItem, string Accion, string? Motivo)
    : IRequest<EstatusCambiadoResponse>;

public class CambiarEstatusWorkItemValidator : AbstractValidator<CambiarEstatusWorkItemCommand>
{
    public CambiarEstatusWorkItemValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().WithMessage("La accion es obligatoria.").MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Unica puerta de cambio de estatus de WorkItems: valida las reglas de negocio
/// que el motor no conoce (RN-REQ-01/02/03) y delega la transicion al motor.
/// </summary>
public class CambiarEstatusWorkItemHandler(
    IWorkItemRepository repositorio,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    INotificadorTiempoReal notificador,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CambiarEstatusWorkItemCommand, EstatusCambiadoResponse>
{
    private const string Proceso = "WorkItem";

    public async Task<EstatusCambiadoResponse> Handle(
        CambiarEstatusWorkItemCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken);

        // RN-QA-04/05: aprobar (TERMINAR) o rechazar (RECHAZAR_QA) la fase de pruebas
        // desde En Pruebas es un rol de REVISION, no de dueño -- lo normal es que quien
        // aprueba/rechaza sea distinto del asignado, asi que el gate de "item ajeno" de
        // abajo NO aplica aqui (se reemplaza por la validacion especifica de autoaprobacion).
        var esRevisionPruebas = estado.IdEstatus == EstatusWorkItem.EnPruebas
            && (command.Accion == AccionesWorkItem.Terminar || command.Accion == AccionesWorkItem.RechazarQa);

        // RN-REQ-05: item ajeno (asignado a otra persona O SIN asignar) solo con permiso --
        // mismo gate que ActualizarWorkItemCommand, aplicado tambien a cambios de estatus
        // (INICIAR/TERMINAR/etc.), no solo a la edicion de campos. Sin asignar cuenta como
        // ajeno (decision del equipo 2026-08-02): nadie "toma" trabajo del backlog solo con
        // INICIAR, un Lider/Admin lo asigna primero.
        var esAjeno = !esRevisionPruebas && estado.IdAsignado != usuarioActual?.IdUsuario;
        if (esAjeno)
        {
            await permisos.ExigirPermisoAsync(PermisosWorkItem.ModificarAjeno, estado.IdProyecto, cancellationToken);
        }

        // La accion debe existir en el grafo para el estatus actual
        var acciones = await motor.ObtenerAccionesAsync(Proceso, command.IdWorkItem, cancellationToken);
        var accion = acciones.FirstOrDefault(a => a.Accion == command.Accion)
            ?? throw new BusinessException(
                $"La accion {command.Accion} no esta permitida desde el estatus actual.");

        // Permiso y motivo configurados por datos (tblTransicionConfig)
        if (accion.ClavePermisoRequerida is not null)
        {
            await permisos.ExigirPermisoAsync(accion.ClavePermisoRequerida, estado.IdProyecto, cancellationToken);
        }
        if (accion.RequiereMotivo && string.IsNullOrWhiteSpace(command.Motivo))
        {
            throw new BusinessException($"La accion {command.Accion} requiere capturar un motivo.");
        }

        var horarioAsignado = await ObtenerHorarioAsignadoAsync(estado, cancellationToken);

        if (esRevisionPruebas)
        {
            await ValidarRevisionPruebasAsync(estado, usuarioActual, command.Accion, cancellationToken);
        }

        switch (command.Accion)
        {
            case AccionesWorkItem.Iniciar:
            case AccionesWorkItem.Reanudar:
                await ValidarInicioAsync(estado, horarioAsignado, cancellationToken);
                break;
            case AccionesWorkItem.Terminar:
                await ValidarCierreAsync(estado, cancellationToken);
                break;
        }

        var resultado = await motor.EjecutarAccionAsync(
            Proceso, command.IdWorkItem, command.Accion, command.Motivo, horarioAsignado, cancellationToken);

        await repositorio.AplicarEfectosTransicionAsync(command.IdWorkItem, command.Accion, cancellationToken);

        // Cubre tanto los botones de accion del detalle como el drag del kanban
        // (MoverTarjetaCommand delega en este mismo comando).
        await notificador.NotificarWorkItemActualizadoAsync(
            command.IdWorkItem, resultado.IdEstatusNuevo, cancellationToken);

        return new EstatusCambiadoResponse
        {
            IdEstatusAnterior = resultado.IdEstatusAnterior,
            IdEstatusNuevo = resultado.IdEstatusNuevo,
            Estatus = resultado.DescripcionEstatusNuevo
        };
    }

    private async Task<int?> ObtenerHorarioAsignadoAsync(EstadoWorkItem estado, CancellationToken cancellationToken)
    {
        if (!estado.IdAsignado.HasValue)
        {
            return null;
        }

        var asignado = await repositorio.ObtenerUsuarioAsync(estado.IdAsignado.Value, cancellationToken);
        return asignado?.IdHorario;
    }

    /// <summary>RN-REQ-01 y RN-REQ-02.</summary>
    private async Task ValidarInicioAsync(EstadoWorkItem estado, int? horarioAsignado, CancellationToken cancellationToken)
    {
        // RN-REQ-02: iniciar exige fecha compromiso capturada
        if (!estado.FechaCompromiso.HasValue)
        {
            throw new BusinessException("No se puede iniciar sin fecha compromiso capturada.");
        }

        // RN-REQ-01: una sola tarea En Proceso por persona; la anterior se suspende
        // automaticamente registrando el historial EN EL ITEM SUSPENDIDO
        // (corrige el defecto historico del GT)
        if (estado.IdAsignado.HasValue)
        {
            var otroItem = await repositorio.ObtenerItemEnProcesoDeAsignadoAsync(
                estado.IdAsignado.Value, estado.IdWorkItem, cancellationToken);
            if (otroItem.HasValue)
            {
                await motor.EjecutarAccionAsync(
                    Proceso, otroItem.Value, AccionesWorkItem.Suspender,
                    $"Suspendido automaticamente al iniciar {estado.Folio}",
                    horarioAsignado, cancellationToken);
                await repositorio.AplicarEfectosTransicionAsync(
                    otroItem.Value, AccionesWorkItem.Suspender, cancellationToken);
            }
        }
    }

    /// <summary>
    /// RN-QA-04: aprobar (TERMINAR) o rechazar (RECHAZAR_QA) la fase de pruebas desde En
    /// Pruebas exige (a) no ser el propio asignado (no autoaprobacion/autorechazo) y (b),
    /// solo para rechazar, que ya exista un hallazgo pendiente registrado -- rechazar con
    /// un simple motivo de texto sin hallazgo vinculado ya no procede. El permiso
    /// WI.AprobarPruebas en si se exige por datos (tblTransicionConfig.RequierePermiso),
    /// no aqui. Bypass acotado con WI.OmitirValidacionCierre, igual que ValidarCierreAsync.
    /// </summary>
    private async Task ValidarRevisionPruebasAsync(
        EstadoWorkItem estado, UsuarioActual? usuarioActual, string accion, CancellationToken cancellationToken)
    {
        if (await permisos.TienePermisoAsync(
                PermisosWorkItem.OmitirValidacionCierre, estado.IdProyecto, cancellationToken))
        {
            return;
        }

        if (estado.IdAsignado.HasValue && estado.IdAsignado == usuarioActual?.IdUsuario)
        {
            throw new BusinessException("No puedes aprobar ni rechazar las pruebas de tu propio elemento.");
        }

        if (accion == AccionesWorkItem.RechazarQa)
        {
            var validacion = await repositorio.ObtenerValidacionCierreAsync(estado.IdWorkItem, cancellationToken);
            if (validacion.RevisionesPendientes.Count == 0)
            {
                throw new BusinessException(
                    "No se puede rechazar sin un hallazgo registrado: reporta el hallazgo primero.");
            }
        }
    }

    /// <summary>
    /// RN-REQ-03: cierre con avance, sin revisiones pendientes; mantenimiento con permiso.
    /// Bypass acotado (decision del equipo 2026-08-02, WI.OmitirValidacionCierre, solo
    /// Administrador por seed): permite terminar sin estas validaciones. El resto de
    /// reglas de negocio del proceso (RN-REQ-01/02, ownership) no se saltan aqui.
    /// </summary>
    private async Task ValidarCierreAsync(EstadoWorkItem estado, CancellationToken cancellationToken)
    {
        if (await permisos.TienePermisoAsync(
                PermisosWorkItem.OmitirValidacionCierre, estado.IdProyecto, cancellationToken))
        {
            return;
        }

        var validacion = await repositorio.ObtenerValidacionCierreAsync(estado.IdWorkItem, cancellationToken);

        if (validacion.RevisionesPendientes.Count > 0)
        {
            throw new ConflictException(
                "No se puede terminar el elemento: tiene revisiones pendientes de corregir.",
                new { revisionesPendientes = validacion.RevisionesPendientes });
        }

        if (!validacion.TieneAvance)
        {
            throw new BusinessException(
                "No se puede terminar el elemento sin avance registrado (tiempo o subtareas terminadas).");
        }

        if (estado.EsMantenimiento)
        {
            await permisos.ExigirPermisoAsync(
                PermisosWorkItem.TerminarMantenimiento, estado.IdProyecto, cancellationToken);
        }
    }
}
