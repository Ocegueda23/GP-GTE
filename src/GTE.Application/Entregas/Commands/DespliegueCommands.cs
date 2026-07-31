using FluentValidation;
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
    }
}

/// <summary>
/// SOLICITAR_APROBACION congela el contenido y crea la cadena de firmas, validando antes
/// la calidad del release (RN-QA-01: sin fallas de prueba sin bug ni bugs S1/S2 abiertos)
/// y el rollback de los scripts (RN-REL-02). CANCELAR y ROLLBACK usan la misma puerta.
/// </summary>
public class CambiarEstatusReleaseHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarEstatusReleaseCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        CambiarEstatusReleaseCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        var permisoRequerido = command.Accion == AccionesRelease.Rollback
            ? PermisosEntregas.Desplegar
            : PermisosEntregas.Crear;
        await permisos.ExigirPermisoAsync(permisoRequerido, release.IdProyecto, cancellationToken);

        if (command.Accion == AccionesRelease.SolicitarAprobacion)
        {
            await ValidarListoParaAprobacionAsync(command.IdRelease, cancellationToken);
        }

        await motor.EjecutarAccionAsync(
            "Release", command.IdRelease, command.Accion, command.Motivo, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionAsync(command.IdRelease, command.Accion, cancellationToken);

        if (command.Accion == AccionesRelease.SolicitarAprobacion)
        {
            await repositorio.CrearCadenaAprobacionAsync(
                command.IdRelease, RolesAprobacion.Cadena, cancellationToken);
        }

        return await consultas.ObtenerDetalleAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);
    }

    private async Task ValidarListoParaAprobacionAsync(int idRelease, CancellationToken cancellationToken)
    {
        var contenido = await repositorio.ObtenerContenidoAsync(idRelease, cancellationToken);
        if (contenido.Count == 0)
        {
            throw new BusinessException("Un release sin contenido no se puede mandar a aprobacion.");
        }

        // RN-REL-02: scripts SQL sin rollback ni justificacion
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

        // RN-QA-01: calidad del release
        var fallasSinBug = await repositorio.ObtenerFallasSinBugAsync(idRelease, cancellationToken);
        var bugsCriticos = await repositorio.ObtenerBugsCriticosAbiertosAsync(idRelease, cancellationToken);
        if (fallasSinBug.Count > 0 || bugsCriticos.Count > 0)
        {
            throw new ConflictException(
                "El release no cumple los criterios de calidad para aprobacion.",
                new { fallasSinBug, bugsCriticos });
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
/// Registra un despliegue. RN-REL-03: el paso a produccion exige que el release este
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
