using FluentValidation;
using GTE.Application.Common;
using GTE.Application.DTOs.Request.Entregas;
using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.Interfaces;
using GTE.Domain.Entregas;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.Entregas.Commands;

/* ---------- Alta ---------- */

public record CrearReleaseCommand(ReleaseCrearRequest Datos) : IRequest<ReleaseDetalleResponse>;

public class CrearReleaseValidator : AbstractValidator<CrearReleaseCommand>
{
    public CrearReleaseValidator()
    {
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.Version).NotEmpty().WithMessage("La version es obligatoria.")
            .MaximumLength(50)
            .Matches(@"^\d+\.\d+(\.\d+)?([-.].+)?$")
            .WithMessage("Usa versionado semantico, por ejemplo 2.11.0.");
    }
}

public class CrearReleaseHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IGeneradorFolios folios,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos) : IRequestHandler<CrearReleaseCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(CrearReleaseCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, command.Datos.IdProyecto, cancellationToken);

        var proyecto = await workItems.ObtenerProyectoAsync(command.Datos.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.Datos.IdProyecto);

        if (await repositorio.ExisteVersionAsync(command.Datos.IdProyecto, command.Datos.Version, cancellationToken))
        {
            throw new ConflictException(
                $"El proyecto ya tiene un release con la version {command.Datos.Version}.");
        }

        var folio = await folios.GenerarAsync(
            $"REL-{proyecto.Clave}-{DateTime.Today.Year}", 3, cancellationToken);

        var id = await repositorio.CrearReleaseAsync(new ReleaseNuevo(
            command.Datos.IdProyecto, command.Datos.Version.Trim(), folio,
            command.Datos.NotasVersion, command.Datos.FechaPlan), cancellationToken);

        return await consultas.ObtenerDetalleAsync(id, cancellationToken)
            ?? throw new NotFoundException("Release", id);
    }
}

/* ---------- Contenido ---------- */

public record AgregarContenidoCommand(int IdRelease, AgregarContenidoRequest Datos) : IRequest<ReleaseDetalleResponse>;

public class AgregarContenidoValidator : AbstractValidator<AgregarContenidoCommand>
{
    public AgregarContenidoValidator()
    {
        RuleFor(c => c.IdRelease).GreaterThan(0);
        RuleFor(c => c.Datos.IdsWorkItem).NotEmpty().WithMessage("Indica que elementos entran al release.");
    }
}

/// <summary>
/// RN-REL-01: al release solo entran elementos Terminados y sin hallazgos de revision
/// pendientes. El contenido se congela cuando el release pasa a aprobacion.
/// </summary>
public class AgregarContenidoHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<AgregarContenidoCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        AgregarContenidoCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, release.IdProyecto, cancellationToken);

        if (release.IdEstatus != EstatusRelease.EnPreparacion)
        {
            throw new BusinessException(
                "El contenido solo se modifica mientras el release esta En Preparacion.");
        }

        var rechazados = new List<object>();
        foreach (var idWorkItem in command.Datos.IdsWorkItem.Distinct())
        {
            var candidato = await repositorio.ObtenerCandidatoAsync(idWorkItem, cancellationToken);
            if (candidato is null)
            {
                rechazados.Add(new { idWorkItem, motivo = "No existe." });
                continue;
            }
            if (candidato.IdEstatus != EstatusWorkItem.Terminado)
            {
                rechazados.Add(new { candidato.Folio, motivo = "No esta terminado." });
                continue;
            }
            if (candidato.RevisionesPendientes > 0)
            {
                rechazados.Add(new { candidato.Folio, motivo = "Tiene hallazgos de revision sin corregir." });
                continue;
            }

            await repositorio.AgregarWorkItemAsync(command.IdRelease, idWorkItem, cancellationToken);
        }

        if (rechazados.Count > 0)
        {
            throw new ConflictException(
                "Algunos elementos no pueden entrar al release todavia.", new { rechazados });
        }

        return await consultas.ObtenerDetalleAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);
    }
}

public record QuitarContenidoCommand(int IdRelease, int IdWorkItem) : IRequest<Unit>;

public class QuitarContenidoHandler(
    IEntregaRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<QuitarContenidoCommand, Unit>
{
    public async Task<Unit> Handle(QuitarContenidoCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, release.IdProyecto, cancellationToken);

        if (release.IdEstatus != EstatusRelease.EnPreparacion)
        {
            throw new BusinessException("El contenido ya esta congelado para este release.");
        }

        await repositorio.QuitarWorkItemAsync(command.IdRelease, command.IdWorkItem, cancellationToken);
        return Unit.Value;
    }
}

/* ---------- Artefactos ---------- */

public record AgregarArtefactoCommand(int IdRelease, ArtefactoAgregarRequest Datos) : IRequest<int>;

public class AgregarArtefactoValidator : AbstractValidator<AgregarArtefactoCommand>
{
    public AgregarArtefactoValidator()
    {
        RuleFor(c => c.IdRelease).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del artefacto es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.IdTipoArtefacto).GreaterThan(0);
        RuleFor(c => c.Datos.JustificacionIrreversible).MaximumLength(500);
    }
}

/// <summary>
/// RN-REL-02: todo script SQL del release necesita su script de rollback pareado
/// o una justificacion explicita de por que el cambio es irreversible.
/// </summary>
public class AgregarArtefactoHandler(
    IEntregaRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<AgregarArtefactoCommand, int>
{
    public async Task<int> Handle(AgregarArtefactoCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, release.IdProyecto, cancellationToken);

        if (release.IdEstatus != EstatusRelease.EnPreparacion)
        {
            throw new BusinessException("Los artefactos solo se agregan mientras el release esta En Preparacion.");
        }

        return await repositorio.AgregarArtefactoAsync(new ArtefactoNuevo(
            command.IdRelease, command.Datos.Nombre.Trim(), command.Datos.IdTipoArtefacto,
            command.Datos.HashSha256, command.Datos.OrdenEjecucion,
            command.Datos.IdArtefactoRollback, command.Datos.JustificacionIrreversible), cancellationToken);
    }
}

/* ---------- Aprobaciones ---------- */

public record ResolverAprobacionCommand(int IdAprobacion, ResolverAprobacionRequest Datos)
    : IRequest<ReleaseDetalleResponse>;

public class ResolverAprobacionValidator : AbstractValidator<ResolverAprobacionCommand>
{
    public ResolverAprobacionValidator()
    {
        RuleFor(c => c.IdAprobacion).GreaterThan(0);
        RuleFor(c => c.Datos.Comentario).MaximumLength(500);
        RuleFor(c => c.Datos.Comentario).NotEmpty().When(c => !c.Datos.Aprobada)
            .WithMessage("Explica por que rechazas el release.");
    }
}

/// <summary>
/// Firma una aprobacion de la cadena. La firma es un hash de usuario, fecha UTC,
/// entidad y decision: verificable y no repudiable dentro del alcance interno.
/// Rechazar una aprobacion regresa el release a preparacion.
/// </summary>
public class ResolverAprobacionHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario,
    AuditContext auditoria) : IRequestHandler<ResolverAprobacionCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        ResolverAprobacionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var aprobacion = await repositorio.ObtenerAprobacionAsync(command.IdAprobacion, cancellationToken)
            ?? throw new NotFoundException("Aprobacion", command.IdAprobacion);

        if (aprobacion.IdEstatus != EstatusAprobacion.Pendiente)
        {
            throw new BusinessException("Esa aprobacion ya fue resuelta.");
        }

        // La aprobacion pertenece a un release: se obtiene su estado para permisos y transiciones
        var idRelease = await ObtenerIdReleaseAsync(command.IdAprobacion, cancellationToken);
        var release = await repositorio.ObtenerEstadoAsync(idRelease, cancellationToken)
            ?? throw new NotFoundException("Release", idRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Aprobar, release.IdProyecto, cancellationToken);

        if (release.IdEstatus != EstatusRelease.EnAprobacion)
        {
            throw new BusinessException("El release no esta en proceso de aprobacion.");
        }

        var firma = FirmaElectronica.Calcular(auditoria.Usuario, release.Folio ?? release.Version,
            aprobacion.RolAprobacion, command.Datos.Aprobada);

        await repositorio.ResolverAprobacionAsync(command.IdAprobacion, usuario.IdUsuario,
            command.Datos.Aprobada, command.Datos.Comentario, firma, cancellationToken);

        if (!command.Datos.Aprobada)
        {
            // Un rechazo devuelve el release a preparacion (descongela el contenido)
            await motor.EjecutarAccionAsync("Release", idRelease, AccionesRelease.Rechazar,
                command.Datos.Comentario, null, cancellationToken);
            await repositorio.AplicarEfectosTransicionAsync(idRelease, AccionesRelease.Rechazar, cancellationToken);
        }
        else
        {
            // RN-REL-03: el release avanza cuando toda la cadena firmo
            var aprobaciones = await repositorio.ObtenerAprobacionesAsync(idRelease, cancellationToken);
            if (aprobaciones.All(a => a.IdEstatus == EstatusAprobacion.Aprobada))
            {
                await motor.EjecutarAccionAsync("Release", idRelease, AccionesRelease.Aprobar,
                    null, null, cancellationToken);
                await repositorio.AplicarEfectosTransicionAsync(idRelease, AccionesRelease.Aprobar, cancellationToken);
            }
        }

        return await consultas.ObtenerDetalleAsync(idRelease, cancellationToken)
            ?? throw new NotFoundException("Release", idRelease);
    }

    private async Task<int> ObtenerIdReleaseAsync(int idAprobacion, CancellationToken cancellationToken)
    {
        var id = await repositorio.ObtenerIdReleaseDeAprobacionAsync(idAprobacion, cancellationToken);
        return id ?? throw new NotFoundException("Release de la aprobacion", idAprobacion);
    }
}
