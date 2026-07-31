using FluentValidation;
using GTE.Application.DTOs.Request.Calidad;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Domain.Calidad;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Calidad.Commands;

/* ---------- Planes ---------- */

public record CrearPlanPruebaCommand(PlanPruebaCrearRequest Datos) : IRequest<PlanPruebaResponse>;

public class CrearPlanPruebaValidator : AbstractValidator<CrearPlanPruebaCommand>
{
    public CrearPlanPruebaValidator()
    {
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(200);
        RuleFor(c => c.Datos.Descripcion).MaximumLength(500);
    }
}

public class CrearPlanPruebaHandler(
    ICalidadRepository repositorio,
    ICalidadQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearPlanPruebaCommand, PlanPruebaResponse>
{
    public async Task<PlanPruebaResponse> Handle(CrearPlanPruebaCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(
            PermisosCalidad.GestionarPlanes, command.Datos.IdProyecto, cancellationToken);

        var id = await repositorio.CrearPlanAsync(new PlanPruebaNuevo(
            command.Datos.IdProyecto, command.Datos.IdRelease,
            command.Datos.Nombre.Trim(), command.Datos.Descripcion), cancellationToken);

        return await consultas.ObtenerPlanAsync(id, cancellationToken)
            ?? throw new NotFoundException("PlanPrueba", id);
    }
}

/* ---------- Casos ---------- */

public record CrearCasoPruebaCommand(int IdPlanPrueba, CasoPruebaCrearRequest Datos) : IRequest<int>;

public class CrearCasoPruebaValidator : AbstractValidator<CrearCasoPruebaCommand>
{
    public CrearCasoPruebaValidator()
    {
        RuleFor(c => c.IdPlanPrueba).GreaterThan(0);
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

public class CrearCasoPruebaHandler(
    ICalidadRepository repositorio,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos) : IRequestHandler<CrearCasoPruebaCommand, int>
{
    public async Task<int> Handle(CrearCasoPruebaCommand command, CancellationToken cancellationToken)
    {
        var plan = await repositorio.ObtenerEstadoPlanAsync(command.IdPlanPrueba, cancellationToken)
            ?? throw new NotFoundException("PlanPrueba", command.IdPlanPrueba);

        await permisos.ExigirPermisoAsync(PermisosCalidad.GestionarPlanes, plan.IdProyecto, cancellationToken);

        var folio = await folios.GenerarAsync("CP", cancellationToken: cancellationToken);

        var pasos = command.Datos.Pasos
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new PasoCaso(p.NumeroPaso, p.Accion.Trim(), p.ResultadoEsperado))
            .ToList();

        return await repositorio.CrearCasoAsync(new CasoPruebaNuevo(
            folio, command.IdPlanPrueba, command.Datos.Titulo.Trim(), command.Datos.Precondiciones,
            command.Datos.ResultadoEsperado, command.Datos.IdTipoPrueba,
            command.Datos.IdWorkItem, pasos), cancellationToken);
    }
}

/* ---------- Ciclos ---------- */

public record CrearCicloPruebaCommand(int IdPlanPrueba, CicloPruebaCrearRequest Datos) : IRequest<int>;

public class CrearCicloPruebaValidator : AbstractValidator<CrearCicloPruebaCommand>
{
    public CrearCicloPruebaValidator()
    {
        RuleFor(c => c.IdPlanPrueba).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del ciclo es obligatorio.")
            .MaximumLength(200);
    }
}

public class CrearCicloPruebaHandler(
    ICalidadRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<CrearCicloPruebaCommand, int>
{
    public async Task<int> Handle(CrearCicloPruebaCommand command, CancellationToken cancellationToken)
    {
        var plan = await repositorio.ObtenerEstadoPlanAsync(command.IdPlanPrueba, cancellationToken)
            ?? throw new NotFoundException("PlanPrueba", command.IdPlanPrueba);

        await permisos.ExigirPermisoAsync(PermisosCalidad.GestionarPlanes, plan.IdProyecto, cancellationToken);

        return await repositorio.CrearCicloAsync(new CicloPruebaNuevo(
            command.IdPlanPrueba, command.Datos.Nombre.Trim(),
            command.Datos.FechaInicio, command.Datos.FechaFin), cancellationToken);
    }
}

/* ---------- Ejecucion ---------- */

public record RegistrarEjecucionCommand(int IdCicloPrueba, EjecucionRegistrarRequest Datos) : IRequest<int>;

public class RegistrarEjecucionValidator : AbstractValidator<RegistrarEjecucionCommand>
{
    public RegistrarEjecucionValidator()
    {
        RuleFor(c => c.IdCicloPrueba).GreaterThan(0);
        RuleFor(c => c.Datos.IdCasoPrueba).GreaterThan(0);
        RuleFor(c => c.Datos.IdResultadoPrueba).InclusiveBetween(1, 4)
            .WithMessage("El resultado debe ser Pasa, Falla, Bloqueado o No aplica.");
        RuleFor(c => c.Datos.Observaciones)
            .NotEmpty()
            .When(c => c.Datos.IdResultadoPrueba is ResultadoPrueba.Falla or ResultadoPrueba.Bloqueado)
            .WithMessage("Describe que fallo o que bloqueo la prueba.");
    }
}

public class RegistrarEjecucionHandler(
    ICalidadRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario,
    IVerificadorPermisos permisos) : IRequestHandler<RegistrarEjecucionCommand, int>
{
    public async Task<int> Handle(RegistrarEjecucionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var caso = await repositorio.ObtenerEstadoCasoAsync(command.Datos.IdCasoPrueba, cancellationToken)
            ?? throw new NotFoundException("CasoPrueba", command.Datos.IdCasoPrueba);

        await permisos.ExigirPermisoAsync(PermisosCalidad.Ejecutar, caso.IdProyecto, cancellationToken);

        // El ciclo y el caso deben pertenecer al mismo plan
        if (!await repositorio.ExisteCicloEnPlanAsync(command.IdCicloPrueba, caso.IdPlanPrueba, cancellationToken))
        {
            throw new BusinessException("El ciclo no pertenece al plan de pruebas de ese caso.");
        }

        return await repositorio.RegistrarEjecucionAsync(new EjecucionNueva(
            command.Datos.IdCasoPrueba, command.IdCicloPrueba, usuario.IdUsuario,
            command.Datos.IdResultadoPrueba, command.Datos.Observaciones), cancellationToken);
    }
}

/* ---------- Bug desde una falla ---------- */

public record CrearBugDesdeEjecucionCommand(int IdEjecucion, BugDesdeEjecucionRequest Datos)
    : IRequest<WorkItemResponse>;

public class CrearBugDesdeEjecucionValidator : AbstractValidator<CrearBugDesdeEjecucionCommand>
{
    public CrearBugDesdeEjecucionValidator()
    {
        RuleFor(c => c.IdEjecucion).GreaterThan(0);
        RuleFor(c => c.Datos.IdPrioridad).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).MaximumLength(200);
    }
}

/// <summary>
/// Crea el bug de una ejecucion fallida reutilizando el comando de alta de WorkItem
/// (folio, historial y reglas incluidas) y lo deja vinculado a la ejecucion, para que
/// la trazabilidad prueba-defecto quede completa y no se reporte dos veces lo mismo.
/// </summary>
public class CrearBugDesdeEjecucionHandler(
    ICalidadRepository repositorio,
    ISender mediator) : IRequestHandler<CrearBugDesdeEjecucionCommand, WorkItemResponse>
{
    private const int TipoBug = 5;   // dbo.tblTipoWorkItem

    public async Task<WorkItemResponse> Handle(
        CrearBugDesdeEjecucionCommand command, CancellationToken cancellationToken)
    {
        var ejecucion = await repositorio.ObtenerEstadoEjecucionAsync(command.IdEjecucion, cancellationToken)
            ?? throw new NotFoundException("EjecucionPrueba", command.IdEjecucion);

        if (ejecucion.IdResultado != ResultadoPrueba.Falla)
        {
            throw new BusinessException("Solo se crean bugs desde ejecuciones con resultado Falla.");
        }

        var existente = await repositorio.ObtenerBugDeEjecucionAsync(command.IdEjecucion, cancellationToken);
        if (existente.HasValue)
        {
            throw new ConflictException(
                "Esta ejecucion ya tiene un bug reportado.", new { idWorkItem = existente.Value });
        }

        var descripcion = string.IsNullOrWhiteSpace(command.Datos.Descripcion)
            ? $"Detectado al ejecutar el caso: {ejecucion.TituloCaso}."
              + (string.IsNullOrWhiteSpace(ejecucion.Observaciones)
                  ? string.Empty
                  : $"\n\nObservaciones de la ejecucion: {ejecucion.Observaciones}")
            : command.Datos.Descripcion;

        var bug = await mediator.Send(new CrearWorkItemCommand(new WorkItemCrearRequest
        {
            IdProyecto = ejecucion.IdProyecto,
            IdTipoWorkItem = TipoBug,
            Titulo = string.IsNullOrWhiteSpace(command.Datos.Titulo)
                ? $"Falla en prueba: {ejecucion.TituloCaso}"
                : command.Datos.Titulo.Trim(),
            Descripcion = descripcion,
            IdPrioridad = command.Datos.IdPrioridad,
            IdAsignado = command.Datos.IdAsignado,
            FechaCompromiso = command.Datos.FechaCompromiso
        }), cancellationToken);

        await repositorio.VincularBugAsync(command.IdEjecucion, bug.IdWorkItem, cancellationToken);
        return bug;
    }
}
