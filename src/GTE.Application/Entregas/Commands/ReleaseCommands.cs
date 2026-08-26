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

/* ---------- Envio de sprint a release (paso aparte tras cerrar el sprint) ---------- */

public record EnviarSprintAReleaseCommand(int IdSprint, EnviarSprintAReleaseRequest Datos)
    : IRequest<ReleaseDetalleResponse>;

public class EnviarSprintAReleaseValidator : AbstractValidator<EnviarSprintAReleaseCommand>
{
    public EnviarSprintAReleaseValidator()
    {
        RuleFor(c => c.IdSprint).GreaterThan(0);
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.VersionNueva)
            .NotEmpty().WithMessage("La version es obligatoria para crear un release nuevo.")
            .MaximumLength(50)
            .Matches(@"^\d+\.\d+(\.\d+)?([-.].+)?$").WithMessage("Usa versionado semantico, por ejemplo 2.11.0.")
            .When(c => c.Datos.IdReleaseExistente is null || c.Datos.IdReleaseExistente <= 0);
    }
}

/// <summary>
/// RN-GTE-018 complementaria: en vez de que a alguien se le olvide meter a un release lo
/// que un sprint termino, este comando manda todo lo disponible de un proyecto de un
/// jalon -- a un release ya En Preparacion de ese proyecto, o a uno nuevo si no hay.
/// Recalcula los disponibles contra la BD (no confia en lo que mande el front).
/// </summary>
public class EnviarSprintAReleaseHandler(
    IEntregaRepository repositorio,
    IEntregaQueryService consultas,
    IGeneradorFolios folios,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos) : IRequestHandler<EnviarSprintAReleaseCommand, ReleaseDetalleResponse>
{
    public async Task<ReleaseDetalleResponse> Handle(
        EnviarSprintAReleaseCommand command, CancellationToken cancellationToken)
    {
        var datos = command.Datos;
        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, datos.IdProyecto, cancellationToken);

        var cobertura = await consultas.ObtenerCoberturaReleaseSprintAsync(command.IdSprint, cancellationToken);
        var proyecto = cobertura.Proyectos.FirstOrDefault(p => p.IdProyecto == datos.IdProyecto);
        if (proyecto is null || proyecto.Disponibles.Count == 0)
        {
            throw new BusinessException(
                "No hay elementos de este sprint listos para enviar a un release en ese proyecto.");
        }

        int idRelease;
        if (datos.IdReleaseExistente.HasValue)
        {
            var release = await repositorio.ObtenerEstadoAsync(datos.IdReleaseExistente.Value, cancellationToken)
                ?? throw new NotFoundException("Release", datos.IdReleaseExistente.Value);
            if (release.IdProyecto != datos.IdProyecto)
            {
                throw new BusinessException("El release indicado no pertenece a ese proyecto.");
            }
            if (release.IdEstatus != EstatusRelease.EnPreparacion)
            {
                throw new BusinessException("El contenido solo se modifica mientras el release esta En Preparacion.");
            }
            idRelease = release.IdRelease;
        }
        else
        {
            var version = datos.VersionNueva!.Trim();
            if (await repositorio.ExisteVersionAsync(datos.IdProyecto, version, cancellationToken))
            {
                throw new ConflictException($"El proyecto ya tiene un release con la version {version}.");
            }

            var infoProyecto = await workItems.ObtenerProyectoAsync(datos.IdProyecto, cancellationToken)
                ?? throw new NotFoundException("Proyecto", datos.IdProyecto);
            var folio = await folios.GenerarAsync(
                $"REL-{infoProyecto.Clave}-{DateTime.Today.Year}", 3, cancellationToken);

            idRelease = await repositorio.CrearReleaseAsync(
                new ReleaseNuevo(datos.IdProyecto, version, folio, null, null), cancellationToken);
        }

        foreach (var item in proyecto.Disponibles)
        {
            await repositorio.AgregarWorkItemAsync(idRelease, item.IdWorkItem, cancellationToken);
        }

        return await consultas.ObtenerDetalleAsync(idRelease, cancellationToken)
            ?? throw new NotFoundException("Release", idRelease);
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
/// RN-GTE-031: al release solo entran elementos Terminados y sin hallazgos de revision
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
/// RN-GTE-032: todo script SQL del release necesita su script de rollback pareado
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

public record QuitarArtefactoCommand(int IdRelease, int IdArtefacto) : IRequest<Unit>;

/// <summary>
/// Baja de un artefacto del release. Mismo criterio que QuitarContenido: solo mientras el
/// release esta En Preparacion, porque a partir de En Aprobacion las firmas se dieron sobre
/// una lista concreta de artefactos y quitar uno la invalidaria en silencio.
/// </summary>
public class QuitarArtefactoHandler(
    IEntregaRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<QuitarArtefactoCommand, Unit>
{
    public async Task<Unit> Handle(QuitarArtefactoCommand command, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerEstadoAsync(command.IdRelease, cancellationToken)
            ?? throw new NotFoundException("Release", command.IdRelease);

        await permisos.ExigirPermisoAsync(PermisosEntregas.Crear, release.IdProyecto, cancellationToken);

        if (release.IdEstatus != EstatusRelease.EnPreparacion)
        {
            throw new BusinessException(
                "Los artefactos solo se quitan mientras el release esta En Preparacion.");
        }

        var dependiente = await repositorio.QuitarArtefactoAsync(
            command.IdRelease, command.IdArtefacto, cancellationToken);
        if (dependiente is not null)
        {
            throw new BusinessException(
                $"No se puede quitar: es el script de reversa de \"{dependiente}\". "
                + "Quita primero ese artefacto o cambiale la reversa.");
        }

        return Unit.Value;
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

            // Invalida toda la cadena (incluida la fila recien rechazada, que ya quedo con su
            // firma/comentario/fecha escritos arriba): sin esto, un SOLICITAR_APROBACION
            // posterior no crea firmas nuevas para roles que ya tienen fila activa (Aprobada
            // o Rechazada) y la fila Rechazada, al no ser Pendiente, nunca se puede volver a
            // resolver -- el release queda atorado en En Aprobacion para siempre.
            await repositorio.InvalidarCadenaAprobacionAsync(idRelease, cancellationToken);
        }
        else
        {
            // RN-GTE-033: el release avanza cuando toda la cadena firmo
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
