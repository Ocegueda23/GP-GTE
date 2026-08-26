using FluentValidation;
using GTE.Application.DTOs.Request.Calidad;
using GTE.Application.DTOs.Request.Revisiones;
using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.Interfaces;
using GTE.Application.Revisiones.Commands;
using GTE.Domain.Calidad;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Calidad.Commands;

/* ---------- Crear caso y asignarlo a un WorkItem ---------- */

public record CrearCasoYAsignarCommand(int IdWorkItem, CasoPruebaCrearRequest Datos) : IRequest<int>;

public class CrearCasoYAsignarValidator : AbstractValidator<CrearCasoYAsignarCommand>
{
    public CrearCasoYAsignarValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo del caso es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.IdTipoPrueba).GreaterThan(0);
        RuleForEach(c => c.Datos.Pasos).ChildRules(paso =>
        {
            paso.RuleFor(p => p.Accion).NotEmpty().WithMessage("Cada paso requiere una accion.");
            paso.RuleFor(p => p.NumeroPaso).GreaterThan(0);
        });
    }
}

/// <summary>
/// Crea un caso de prueba y lo asigna al WorkItem en el mismo paso. Si Datos.Reutilizable es
/// false, el caso solo existe para esta asignacion (no aparece en el catalogo del proyecto).
/// </summary>
public class CrearCasoYAsignarHandler(
    ICalidadRepository repositorio,
    IWorkItemRepository workItems,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos) : IRequestHandler<CrearCasoYAsignarCommand, int>
{
    public async Task<int> Handle(CrearCasoYAsignarCommand command, CancellationToken cancellationToken)
    {
        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        await permisos.ExigirPermisoAsync(PermisosCalidad.Ejecutar, estadoItem.IdProyecto, cancellationToken);

        var folio = await folios.GenerarAsync("CP", cancellationToken: cancellationToken);

        var pasos = command.Datos.Pasos
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new PasoCaso(p.NumeroPaso, p.Accion.Trim(), p.ResultadoEsperado))
            .ToList();

        var idCaso = await repositorio.CrearCasoAsync(new CasoPruebaNuevo(
            estadoItem.IdProyecto, folio, command.Datos.Titulo.Trim(), command.Datos.Precondiciones,
            command.Datos.ResultadoEsperado, command.Datos.IdTipoPrueba, command.Datos.Reutilizable, pasos),
            cancellationToken);

        await repositorio.AsignarCasoAsync(command.IdWorkItem, idCaso, cancellationToken);
        return idCaso;
    }
}

/* ---------- Asignar un caso ya existente del catalogo ---------- */

public record AsignarCasoExistenteCommand(int IdWorkItem, AsignarCasoRequest Datos) : IRequest<Unit>;

public class AsignarCasoExistenteValidator : AbstractValidator<AsignarCasoExistenteCommand>
{
    public AsignarCasoExistenteValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.IdCasoPrueba).GreaterThan(0);
    }
}

public class AsignarCasoExistenteHandler(
    ICalidadRepository repositorio,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos) : IRequestHandler<AsignarCasoExistenteCommand, Unit>
{
    public async Task<Unit> Handle(AsignarCasoExistenteCommand command, CancellationToken cancellationToken)
    {
        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        await permisos.ExigirPermisoAsync(PermisosCalidad.Ejecutar, estadoItem.IdProyecto, cancellationToken);

        var caso = await repositorio.ObtenerEstadoCasoAsync(command.Datos.IdCasoPrueba, cancellationToken)
            ?? throw new NotFoundException("CasoPrueba", command.Datos.IdCasoPrueba);

        if (caso.IdProyecto != estadoItem.IdProyecto)
        {
            throw new BusinessException("Ese caso pertenece a otro proyecto.");
        }

        if (await repositorio.ExisteAsignacionActivaAsync(command.IdWorkItem, command.Datos.IdCasoPrueba, cancellationToken))
        {
            throw new ConflictException("Ese caso ya esta asignado a este elemento.");
        }

        await repositorio.AsignarCasoAsync(command.IdWorkItem, command.Datos.IdCasoPrueba, cancellationToken);
        return Unit.Value;
    }
}

/* ---------- Retirar una asignacion ---------- */

public record RetirarAsignacionCommand(int IdWorkItemCasoPrueba) : IRequest<Unit>;

public class RetirarAsignacionValidator : AbstractValidator<RetirarAsignacionCommand>
{
    public RetirarAsignacionValidator()
    {
        RuleFor(c => c.IdWorkItemCasoPrueba).GreaterThan(0);
    }
}

public class RetirarAsignacionHandler(
    ICalidadRepository repositorio,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarAsignacionCommand, Unit>
{
    public async Task<Unit> Handle(RetirarAsignacionCommand command, CancellationToken cancellationToken)
    {
        var idWorkItem = await repositorio.ObtenerIdWorkItemDeAsignacionAsync(command.IdWorkItemCasoPrueba, cancellationToken)
            ?? throw new NotFoundException("WorkItemCasoPrueba", command.IdWorkItemCasoPrueba);

        var estadoItem = await workItems.ObtenerEstadoAsync(idWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", idWorkItem);

        await permisos.ExigirPermisoAsync(PermisosCalidad.Ejecutar, estadoItem.IdProyecto, cancellationToken);

        await repositorio.RetirarAsignacionAsync(command.IdWorkItemCasoPrueba, cancellationToken);
        return Unit.Value;
    }
}

/* ---------- Editar / retirar un caso del catalogo ---------- */

public record ActualizarCasoPruebaCommand(int IdCasoPrueba, CasoPruebaEditarRequest Datos) : IRequest<Unit>;

public class ActualizarCasoPruebaValidator : AbstractValidator<ActualizarCasoPruebaCommand>
{
    public ActualizarCasoPruebaValidator()
    {
        RuleFor(c => c.IdCasoPrueba).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo del caso es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.IdTipoPrueba).GreaterThan(0);
        RuleForEach(c => c.Datos.Pasos).ChildRules(paso =>
        {
            paso.RuleFor(p => p.Accion).NotEmpty().WithMessage("Cada paso requiere una accion.");
            paso.RuleFor(p => p.NumeroPaso).GreaterThan(0);
        });
    }
}

public class ActualizarCasoPruebaHandler(
    ICalidadRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarCasoPruebaCommand, Unit>
{
    public async Task<Unit> Handle(ActualizarCasoPruebaCommand command, CancellationToken cancellationToken)
    {
        var caso = await repositorio.ObtenerEstadoCasoAsync(command.IdCasoPrueba, cancellationToken)
            ?? throw new NotFoundException("CasoPrueba", command.IdCasoPrueba);

        await permisos.ExigirPermisoAsync(PermisosCalidad.GestionarPlanes, caso.IdProyecto, cancellationToken);

        var pasos = command.Datos.Pasos
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new PasoCaso(p.NumeroPaso, p.Accion.Trim(), p.ResultadoEsperado))
            .ToList();

        await repositorio.ActualizarCasoAsync(new CasoPruebaEdicion(
            command.IdCasoPrueba, command.Datos.Titulo.Trim(), command.Datos.Precondiciones,
            command.Datos.ResultadoEsperado, command.Datos.IdTipoPrueba, pasos), cancellationToken);

        return Unit.Value;
    }
}

public record RetirarCasoPruebaCommand(int IdCasoPrueba) : IRequest<Unit>;

public class RetirarCasoPruebaValidator : AbstractValidator<RetirarCasoPruebaCommand>
{
    public RetirarCasoPruebaValidator()
    {
        RuleFor(c => c.IdCasoPrueba).GreaterThan(0);
    }
}

public class RetirarCasoPruebaHandler(
    ICalidadRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarCasoPruebaCommand, Unit>
{
    public async Task<Unit> Handle(RetirarCasoPruebaCommand command, CancellationToken cancellationToken)
    {
        var caso = await repositorio.ObtenerEstadoCasoAsync(command.IdCasoPrueba, cancellationToken)
            ?? throw new NotFoundException("CasoPrueba", command.IdCasoPrueba);

        await permisos.ExigirPermisoAsync(PermisosCalidad.GestionarPlanes, caso.IdProyecto, cancellationToken);

        await repositorio.RetirarCasoAsync(command.IdCasoPrueba, cancellationToken);
        return Unit.Value;
    }
}

/* ---------- Registrar ejecucion ---------- */

public record RegistrarEjecucionCommand(int IdWorkItem, EjecucionRegistrarRequest Datos)
    : IRequest<EjecucionRegistradaResponse>;

public class RegistrarEjecucionValidator : AbstractValidator<RegistrarEjecucionCommand>
{
    public RegistrarEjecucionValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.IdCasoPrueba).GreaterThan(0);
        RuleFor(c => c.Datos.IdResultadoPrueba).InclusiveBetween(1, 4)
            .WithMessage("El resultado debe ser Pasa, Falla, Bloqueado o No aplica.");
        RuleFor(c => c.Datos.Observaciones)
            .NotEmpty()
            .When(c => c.Datos.IdResultadoPrueba is ResultadoPrueba.Falla or ResultadoPrueba.Bloqueado)
            .WithMessage("Describe que fallo o que bloqueo la prueba.");
        RuleFor(c => c.Datos.IdSeveridad)
            .NotNull().InclusiveBetween(1, 4)
            .When(c => c.Datos.IdResultadoPrueba == ResultadoPrueba.Falla)
            .WithMessage("Selecciona la severidad del hallazgo.");
    }
}

/// <summary>
/// Registra el resultado de correr un caso contra el WorkItem. Si el resultado es Falla,
/// crea en automatico el hallazgo (mismo mecanismo que un hallazgo de code review, via
/// CrearRevisionCommand) con la evidencia de la ejecucion ya precargada -- no se crea un
/// WorkItem nuevo: el defecto es parte de este mismo item, no pierde su trazabilidad.
/// </summary>
public class RegistrarEjecucionHandler(
    ICalidadRepository repositorio,
    IWorkItemRepository workItems,
    IProveedorUsuarioActual proveedorUsuario,
    IVerificadorPermisos permisos,
    ISender mediator) : IRequestHandler<RegistrarEjecucionCommand, EjecucionRegistradaResponse>
{
    public async Task<EjecucionRegistradaResponse> Handle(
        RegistrarEjecucionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        await permisos.ExigirPermisoAsync(PermisosCalidad.Ejecutar, estadoItem.IdProyecto, cancellationToken);

        if (!await repositorio.ExisteAsignacionActivaAsync(command.IdWorkItem, command.Datos.IdCasoPrueba, cancellationToken))
        {
            throw new BusinessException("Ese caso no esta asignado a este elemento.");
        }

        var idEjecucion = await repositorio.RegistrarEjecucionAsync(new EjecucionNueva(
            command.Datos.IdCasoPrueba, command.IdWorkItem, usuario.IdUsuario,
            command.Datos.IdResultadoPrueba, command.Datos.Observaciones), cancellationToken);

        var respuesta = new EjecucionRegistradaResponse { IdEjecucionPrueba = idEjecucion };

        if (command.Datos.IdResultadoPrueba == ResultadoPrueba.Falla)
        {
            var ejecucion = await repositorio.ObtenerEstadoEjecucionAsync(idEjecucion, cancellationToken)
                ?? throw new NotFoundException("EjecucionPrueba", idEjecucion);

            var comentario = $"Prueba fallida: {ejecucion.TituloCaso}."
                + (string.IsNullOrWhiteSpace(command.Datos.Observaciones)
                    ? string.Empty
                    : $"\n\nObservaciones: {command.Datos.Observaciones}");

            var hallazgo = await mediator.Send(new CrearRevisionCommand(command.IdWorkItem, new RevisionCrearRequest
            {
                Comentarios = comentario,
                IdSeveridad = command.Datos.IdSeveridad!.Value,
                IdEjecucionPrueba = idEjecucion
            }), cancellationToken);

            respuesta.IdRevision = hallazgo.IdRevision;
        }

        return respuesta;
    }
}
