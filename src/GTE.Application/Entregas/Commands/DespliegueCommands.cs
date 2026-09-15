using FluentValidation;
using GTE.Application.Common;
using GTE.Application.DTOs.Request.Entregas;
using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.Interfaces;
using GTE.Domain.Entregas;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Entregas.Commands;

/* ---------- Solicitar aprobacion ---------- */

public record CambiarEstatusReleaseCommand(int IdRelease, string Accion, string? Motivo)
    : IRequest<ReleaseDetalleResponse>;

public class CambiarEstatusReleaseValidator : AbstractValidator<CambiarEstatusReleaseCommand>
{
    public CambiarEstatusReleaseValidator()
    {
        RuleFor(c => c.IdRelease).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Motivo).MaximumLength(500);
        RuleFor(c => c.Motivo).NotEmpty().When(c => c.Accion == AccionesRelease.Reabrir)
            .WithMessage("Explica por que reabres el release.");
        RuleFor(c => c.Motivo).NotEmpty().When(c => c.Accion == AccionesRelease.Autorizar)
            .WithMessage("Explica por que autorizas el release sin recabar las firmas.");
    }
}

/// <summary>
/// SOLICITAR_APROBACION congela el contenido y crea la cadena de firmas, validando antes
/// la calidad del release (RN-GTE-025: sin fallas de prueba sin bug ni bugs S1/S2 abiertos)
/// y el rollback de los scripts (RN-GTE-032). CANCELAR y ROLLBACK usan la misma puerta.
/// REABRIR regresa un release ya Aprobado a preparacion (para agregar contenido o
/// artefactos que hicieron falta) e invalida la cadena de firmas vigente: como deshace
/// aprobaciones ya puestas, exige el mismo permiso que firmar, no el de solo preparar.
/// AUTORIZAR salta la cadena completa (En Aprobacion -> Aprobado) dando por cubiertas las
/// firmas que seguian pendientes; es la unica accion que abre REL.Autorizar, un acceso
/// aparte de REL.Aprobar justamente porque dispensa firmas en vez de ponerlas.
/// </summary>
public class CambiarEstatusReleaseHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario,
    AuditContext auditoria) : IRequestHandler<CambiarEstatusReleaseCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        CambiarEstatusReleaseCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        var permisoRequerido = command.Accion switch
        {
            AccionesRelease.Rollback => PermisosEntregas.Desplegar,
            AccionesRelease.Reabrir => PermisosEntregas.Aprobar,
            AccionesRelease.Autorizar => PermisosEntregas.Autorizar,
            _ => PermisosEntregas.Crear,
        };
        await permisos.ExigirPermisoAsync(permisoRequerido, release.IdProyecto, cancellationToken);

        if (command.Accion == AccionesRelease.Autorizar)
        {
            await AutorizarSaltandoFirmasAsync(release, command.Motivo!, cancellationToken);
        }
        else if (command.Accion == AccionesRelease.SolicitarAprobacion)
        {
            await ValidarListoParaAprobacionAsync(command.IdRelease, cancellationToken);

            // La cadena se crea ANTES de mover el estatus: si esto falla (usuario no
            // resuelto, conflicto al guardar, etc.) el release se queda en En Preparacion,
            // donde SOLICITAR_APROBACION se puede reintentar, en vez de avanzar a En
            // Aprobacion sin firmantes y sin forma de arreglarlo desde la UI (REABRIR solo
            // aplica desde Aprobado).
            var cadenaConfigurada = await repositorio.ObtenerCadenaAprobacionConfiguradaAsync(
                release.IdProyecto, cancellationToken);
            await repositorio.CrearCadenaAprobacionAsync(
                command.IdRelease,
                cadenaConfigurada.Count > 0 ? cadenaConfigurada : RolesAprobacion.Cadena,
                cancellationToken);
        }
        else if (command.Accion == AccionesRelease.Reabrir)
        {
            // Misma razon: invalidar la cadena vieja antes de mover el estatus, para no
            // dejar un release en En Preparacion con firmas viejas todavia activas.
            await repositorio.InvalidarCadenaAprobacionAsync(command.IdRelease, cancellationToken);
        }

        await motor.EjecutarAccionAsync(
            "Release", command.IdRelease, command.Accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(command.IdRelease, command.Accion, cancellationToken);

        return await consultas.ObtenerDetalleAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);
    }

    /// <summary>
    /// Cierra las firmas pendientes como Omitidas y las deja atribuidas al autorizador,
    /// con su motivo y su firma electronica. Se hace ANTES de mover el estatus, igual que
    /// el armado de la cadena: si falla, el release se queda En Aprobacion y la
    /// autorizacion se puede reintentar, en vez de quedar Aprobado con firmas colgando.
    /// </summary>
    private async Task AutorizarSaltandoFirmasAsync(
        EstadoRelease release, string motivo, CancellationToken cancellationToken)
    {
        if (release.IdEstatus != EstatusRelease.EnAprobacion)
        {
            throw new BusinessException(
                "Solo se autoriza un release que ya esta En Aprobacion. "
                + "Solicita primero la aprobacion para congelar el contenido.");
        }

        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var firma = FirmaElectronica.Calcular(
            auditoria.Usuario, release.Folio ?? release.Version, RolAutorizacion, true);

        await repositorio.OmitirAprobacionesPendientesAsync(
            release.IdRelease, usuario.IdUsuario, motivo, firma, cancellationToken);
    }

    /// <summary>Rol con el que se firma la autorizacion; no es parte de la cadena de la Solicitud.</summary>
    private const string RolAutorizacion = "Autorizacion de release";

    private async Task ValidarListoParaAprobacionAsync(int idRelease, CancellationToken cancellationToken)
    {
        // El instructivo general es lo que lee quien despliega: sin el, la Solicitud de
        // despliegue se firma sin decir que hay que hacer, y el que ejecuta en produccion
        // se queda adivinando. Va junto a los otros gates, no como aviso suelto en la UI.
        var detalle = await consultas.ObtenerDetalleAsync(idRelease, cancellationToken)
            ?? throw new NotFoundException("Release", idRelease);
        if (string.IsNullOrWhiteSpace(detalle.InstruccionesImplementacion))
        {
            throw new BusinessException(
                "Captura las instrucciones generales de implementacion antes de mandar el "
                + "release a aprobacion.");
        }

        var contenido = await repositorio.ObtenerContenidoAsync(idRelease, cancellationToken);
        if (contenido.Count == 0)
        {
            throw new BusinessException("Un release sin contenido no se puede mandar a aprobacion.");
        }

        // La Solicitud de despliegue se manda a firmar diciendo que se respalda antes de
        // tocar produccion; un release sin ningun respaldo capturado deja ese apartado en
        // blanco y no hay a que responsabilizar si algo se tiene que revertir a mano.
        var respaldos = await repositorio.ObtenerRespaldosAsync(idRelease, cancellationToken);
        if (respaldos.Count == 0)
        {
            throw new BusinessException(
                "Hay que capturar al menos un respaldo (base de datos, servicio, sitio o ubicacion) "
                + "antes de mandar el release a aprobacion.");
        }

        // RN-GTE-032: scripts SQL sin rollback ni justificacion
        var artefactos = await repositorio.ObtenerArtefactosAsync(idRelease, cancellationToken);
        var sinRollback = artefactos
            .Where(a => a.IdTipoArtefacto == TipoArtefacto.ScriptSql
                        && a.IdArtefactoRollback is null
                        && string.IsNullOrWhiteSpace(a.JustificacionIrreversible))
            .Select(a => a.Nombre)
            .ToList();
        if (sinRollback.Count > 0)
        {
            throw new ConflictException(
                "Hay scripts SQL sin script de rollback ni justificacion de irreversibilidad.",
                new { scripts = sinRollback });
        }

        // RN-GTE-025: calidad del release -- ningun item del contenido puede tener un
        // hallazgo (QA o code review) de severidad S1/S2 sin corregir. La cobertura de
        // pruebas es responsabilidad de QA al aprobar la fase En Pruebas de cada item,
        // no de este gate.
        var hallazgosCriticos = await repositorio.ObtenerHallazgosCriticosAbiertosAsync(idRelease, cancellationToken);
        if (hallazgosCriticos.Count > 0)
        {
            throw new ConflictException(
                "El release no cumple los criterios de calidad para aprobacion.",
                new { hallazgosCriticos });
        }
    }
}

/* ---------- Despliegue ---------- */

public record RegistrarDespliegueCommand(int IdRelease, DespliegueRegistrarRequest Datos)
    : IRequest<ReleaseDetalleResponse>;

public class RegistrarDespliegueValidator : AbstractValidator<RegistrarDespliegueCommand>
{
    public RegistrarDespliegueValidator()
    {
        RuleFor(c => c.IdRelease).GreaterThan(0);
        RuleFor(c => c.Datos.IdAmbiente).GreaterThan(0).WithMessage("El ambiente es obligatorio.");
    }
}

/// <summary>
/// Registra un despliegue. RN-GTE-033: el paso a produccion exige que el release este
/// Aprobado (toda la cadena firmada) y mueve el release a Liberado; un rollback lo
/// deja en Revertido. Ambos casos van por el motor de estatus.
/// </summary>
public class RegistrarDespliegueHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<RegistrarDespliegueCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        RegistrarDespliegueCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Desplegar, release.IdProyecto, cancellationToken);

        var idAmbienteProd = await repositorio.ObtenerAmbienteProduccionAsync(release.IdProyecto, cancellationToken);
        var esProduccion = idAmbienteProd.HasValue && command.Datos.IdAmbiente == idAmbienteProd.Value;

        if (esProduccion && !command.Datos.EsRollback)
        {
            if (release.IdEstatus != EstatusRelease.Aprobado)
            {
                var aprobaciones = await repositorio.ObtenerAprobacionesAsync(command.IdRelease, cancellationToken);
                var faltantes = aprobaciones
                    .Where(a => a.IdEstatus != EstatusAprobacion.Aprobada)
                    .Select(a => a.RolAprobacion)
                    .ToList();
                throw new ConflictException(
                    "Produccion solo recibe releases aprobados por toda la cadena.",
                    new { estatusActual = release.IdEstatus, firmasFaltantes = faltantes });
            }
        }

        await repositorio.RegistrarDespliegueAsync(new DespliegueNuevo(
            command.IdRelease, command.Datos.IdAmbiente, usuario.IdUsuario,
            command.Datos.EsRollback, command.Datos.Bitacora), cancellationToken);

        // El estatus del release solo cambia en produccion
        if (esProduccion && command.Datos.Exitoso)
        {
            var accion = command.Datos.EsRollback
                ? AccionesRelease.Rollback
                : AccionesRelease.DesplegarProd;

            await motor.EjecutarAccionAsync(
                "Release", command.IdRelease, accion, command.Datos.Bitacora, null, cancellationToken);
            await repositorio.AplicarEfectosTransicionAsync(command.IdRelease, accion, cancellationToken);

            if (!command.Datos.EsRollback)
            {
                await repositorio.MarcarLiberadoAsync(command.IdRelease, cancellationToken);
            }
        }

        return await consultas.ObtenerDetalleAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);
    }
}

/* ---------- Notas de version ---------- */

public record GenerarNotasCommand(int IdRelease) : IRequest<string>;

public class GenerarNotasHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<GenerarNotasCommand, string>
{
    public async Task<string> Handle(GenerarNotasCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, release.IdProyecto, cancellationToken);

        var notas = await consultas.GenerarNotasAsync(command.IdRelease, cancellationToken);
        await repositorio.ActualizarNotasAsync(command.IdRelease, notas, cancellationToken);
        return notas;
    }
}
