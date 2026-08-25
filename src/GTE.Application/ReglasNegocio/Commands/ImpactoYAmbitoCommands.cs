using FluentValidation;
using GTE.Application.DTOs.Request.ReglasNegocio;
using GTE.Application.DTOs.Responses.ReglasNegocio;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.ReglasNegocio;
using MediatR;

namespace GTE.Application.ReglasNegocio.Commands;

/* =====================================================================
   Impactos: proyectos secundarios afectados por la regla
   ===================================================================== */

public record AgregarImpactoReglaCommand(int IdRegla, ImpactoReglaCrearRequest Datos)
    : IRequest<ReglaNegocioResponse>;

public class AgregarImpactoReglaValidator : AbstractValidator<AgregarImpactoReglaCommand>
{
    public AgregarImpactoReglaValidator()
    {
        RuleFor(c => c.IdRegla).GreaterThan(0);
        RuleFor(c => c.Datos.IdProyectoAfectado).GreaterThan(0)
            .WithMessage("Hay que indicar el proyecto afectado.");
        RuleFor(c => c.Datos.DescripcionImpacto)
            .MaximumLength(ConstantesReglasNegocio.LongitudMaximaDescripcionImpacto);
    }
}

/// <summary>
/// Declara que la regla afecta ademas a otro proyecto. La captura es explicita, proyecto
/// por proyecto: no se deriva de categoria, programa ni ningun otro criterio (decision del
/// equipo 2026-08-24).
///
/// El permiso se exige sobre el proyecto DUENO, no sobre el afectado: declarar el alcance
/// de una regla es una decision de quien la escribio.
/// </summary>
public class AgregarImpactoReglaHandler(
    IReglasNegocioRepository repositorio,
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<AgregarImpactoReglaCommand, ReglaNegocioResponse>
{
    public async Task<ReglaNegocioResponse> Handle(
        AgregarImpactoReglaCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, estado.IdProyecto, cancellationToken);

        // La BD tambien lo impide (CK_tblReglaNegocioImpacto_NoAutoImpacto), pero un 400
        // con explicacion es mejor que un 500 por violacion de constraint.
        if (command.Datos.IdProyectoAfectado == estado.IdProyecto)
        {
            throw new BusinessException(
                "La regla ya pertenece a ese proyecto: no hace falta declararlo como afectado.");
        }

        // El ambito opcional del impacto describe que flujo del proyecto AFECTADO toca la
        // regla, asi que se valida contra ese proyecto y no contra el dueno.
        await CrearReglaNegocioHandler.ValidarAmbitoAsync(
            repositorio, command.Datos.IdAmbitoRegla, command.Datos.IdProyectoAfectado, cancellationToken);

        try
        {
            await repositorio.AgregarImpactoAsync(
                new ImpactoReglaNuevo(
                    command.IdRegla, command.Datos.IdProyectoAfectado,
                    command.Datos.DescripcionImpacto, command.Datos.IdAmbitoRegla),
                cancellationToken);
        }
        catch (ConflictException)
        {
            throw new ConflictException("Ese proyecto ya estaba declarado como afectado por la regla.");
        }

        return await consultas.ObtenerPorIdAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);
    }
}

public record QuitarImpactoReglaCommand(int IdImpacto) : IRequest<Unit>;

public class QuitarImpactoReglaHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<QuitarImpactoReglaCommand, Unit>
{
    public async Task<Unit> Handle(QuitarImpactoReglaCommand command, CancellationToken cancellationToken)
    {
        var origen = await repositorio.ObtenerOrigenImpactoAsync(command.IdImpacto, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocioImpacto", command.IdImpacto);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, origen.IdProyectoDueno, cancellationToken);

        await repositorio.QuitarImpactoAsync(command.IdImpacto, cancellationToken);
        return Unit.Value;
    }
}

/* =====================================================================
   Ambitos: flujos de operacion y caracteristicas del sistema
   ===================================================================== */

public record CrearAmbitoReglaCommand(AmbitoReglaCrearRequest Datos) : IRequest<int>;

public class CrearAmbitoReglaValidator : AbstractValidator<CrearAmbitoReglaCommand>
{
    public CrearAmbitoReglaValidator()
    {
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0);
        RuleFor(c => c.Datos.IdTipoAmbitoRegla)
            .Must(t => t == TiposAmbitoRegla.FlujoDeOperacion || t == TiposAmbitoRegla.CaracteristicaDelSistema)
            .WithMessage("El ambito debe ser un flujo de operacion o una caracteristica del sistema.");
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ConstantesReglasNegocio.LongitudMaximaNombreAmbito);
    }
}

/// <summary>
/// Alta de un flujo de operacion o una caracteristica del sistema. El catalogo es PROPIO
/// de cada proyecto: los flujos de un sistema no son los de otro.
/// </summary>
public class CrearAmbitoReglaHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<CrearAmbitoReglaCommand, int>
{
    public async Task<int> Handle(CrearAmbitoReglaCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, command.Datos.IdProyecto, cancellationToken);

        return await repositorio.CrearAmbitoAsync(
            new AmbitoReglaNuevo(
                command.Datos.IdProyecto, command.Datos.IdTipoAmbitoRegla,
                command.Datos.Nombre.Trim(), command.Datos.Descripcion),
            cancellationToken);
    }
}

public record ActualizarAmbitoReglaCommand(int IdAmbito, AmbitoReglaActualizarRequest Datos) : IRequest<Unit>;

public class ActualizarAmbitoReglaValidator : AbstractValidator<ActualizarAmbitoReglaCommand>
{
    public ActualizarAmbitoReglaValidator()
    {
        RuleFor(c => c.IdAmbito).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ConstantesReglasNegocio.LongitudMaximaNombreAmbito);
    }
}

public class ActualizarAmbitoReglaHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarAmbitoReglaCommand, Unit>
{
    public async Task<Unit> Handle(ActualizarAmbitoReglaCommand command, CancellationToken cancellationToken)
    {
        var ambito = await repositorio.ObtenerProyectoAmbitoAsync(command.IdAmbito, cancellationToken)
            ?? throw new NotFoundException("AmbitoRegla", command.IdAmbito);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, ambito.IdProyecto, cancellationToken);

        await repositorio.ActualizarAmbitoAsync(
            command.IdAmbito, command.Datos.Nombre.Trim(), command.Datos.Descripcion, cancellationToken);
        return Unit.Value;
    }
}

public record EliminarAmbitoReglaCommand(int IdAmbito) : IRequest<Unit>;

/// <summary>
/// Baja logica del ambito. Se bloquea si alguna regla activa sigue apuntando a el: dejar
/// reglas colgando de un flujo dado de baja las volveria invisibles en la vista agrupada.
/// </summary>
public class EliminarAmbitoReglaHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<EliminarAmbitoReglaCommand, Unit>
{
    public async Task<Unit> Handle(EliminarAmbitoReglaCommand command, CancellationToken cancellationToken)
    {
        var ambito = await repositorio.ObtenerProyectoAmbitoAsync(command.IdAmbito, cancellationToken)
            ?? throw new NotFoundException("AmbitoRegla", command.IdAmbito);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, ambito.IdProyecto, cancellationToken);

        if (await repositorio.AmbitoTieneReglasAsync(command.IdAmbito, cancellationToken))
        {
            throw new ConflictException(
                "No se puede dar de baja: todavia hay reglas activas asignadas a este flujo o caracteristica.");
        }

        await repositorio.EliminarAmbitoAsync(command.IdAmbito, cancellationToken);
        return Unit.Value;
    }
}
