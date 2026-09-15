using FluentValidation;
using GTE.Application.DTOs.Request.NotasVersion;
using GTE.Application.DTOs.Responses.NotasVersion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.NotasVersion;
using MediatR;

namespace GTE.Application.NotasVersion.Commands;

public record CrearNotaVersionCommand(NotaVersionUpsertRequest Datos) : IRequest<NotaVersionResponse>;

public record ActualizarNotaVersionCommand(int IdNotaVersion, NotaVersionUpsertRequest Datos)
    : IRequest<NotaVersionResponse>;

public record EliminarNotaVersionCommand(int IdNotaVersion) : IRequest<Unit>;

public class CrearNotaVersionValidator : AbstractValidator<CrearNotaVersionCommand>
{
    public CrearNotaVersionValidator() => this.AplicarReglasNota(c => c.Datos);
}

public class ActualizarNotaVersionValidator : AbstractValidator<ActualizarNotaVersionCommand>
{
    public ActualizarNotaVersionValidator()
    {
        RuleFor(c => c.IdNotaVersion).GreaterThan(0);
        this.AplicarReglasNota(c => c.Datos);
    }
}

internal static class ReglasNotaVersion
{
    /// <summary>
    /// Reglas compartidas por alta y edicion. Espejo del esquema: Version NVARCHAR(20) UNIQUE,
    /// Resumen 500, Descripcion 500 NOT NULL, Modulo 100.
    /// </summary>
    public static void AplicarReglasNota<T>(
        this AbstractValidator<T> validador,
        Func<T, NotaVersionUpsertRequest> selector)
    {
        validador.RuleFor(c => selector(c).Version)
            .NotEmpty().WithMessage("La version es obligatoria.")
            .MaximumLength(20);

        validador.RuleFor(c => selector(c).FechaLiberacion)
            .NotEqual(default(DateOnly)).WithMessage("La fecha de liberacion es obligatoria.");

        validador.RuleFor(c => selector(c).Resumen).MaximumLength(500);

        validador.RuleForEach(c => selector(c).Renglones).ChildRules(renglon =>
        {
            renglon.RuleFor(r => r.Descripcion)
                .NotEmpty().WithMessage("Cada renglon necesita una descripcion.")
                .MaximumLength(500);
            renglon.RuleFor(r => r.Modulo).MaximumLength(100);
            renglon.RuleFor(r => r.IdTipoCambioVersion)
                .GreaterThan(0).WithMessage("Cada renglon necesita un tipo de cambio.");
        });
    }
}

/// <summary>
/// Alta de la nota con su arbol de renglones. La nota nace como borrador salvo que se pida
/// publicarla explicitamente; el usuario final solo ve las publicadas.
/// </summary>
public class CrearNotaVersionHandler(
    INotaVersionRepository repositorio,
    INotaVersionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearNotaVersionCommand, NotaVersionResponse>
{
    public async Task<NotaVersionResponse> Handle(
        CrearNotaVersionCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosNotasVersion.Administrar, null, cancellationToken);

        var version = command.Datos.Version.Trim();
        if (await repositorio.ExisteVersionAsync(version, null, cancellationToken))
        {
            throw new ConflictException($"Ya existe una nota para la version {version}.");
        }

        var (idNotaVersion, renglones) = await repositorio.CrearAsync(
            new NotaVersionNueva(
                version,
                command.Datos.FechaLiberacion,
                Limpiar(command.Datos.Resumen),
                command.Datos.Publicada,
                MapearRenglones(command.Datos.Renglones)),
            cancellationToken);

        return await RehidratarAsync(consultas, idNotaVersion, renglones, cancellationToken);
    }

    internal static IReadOnlyList<RenglonNotaVersion> MapearRenglones(
        IReadOnlyList<RenglonNotaVersionRequest> renglones)
        => renglones
            .Select((r, indice) => new RenglonNotaVersion(
                r.IdNotaVersionDetalle,
                r.UiId,
                r.IdTipoCambioVersion,
                Limpiar(r.Modulo),
                r.Descripcion.Trim(),
                // El orden lo fija la posicion en que llego el arreglo: el front reordena
                // arrastrando y no tiene que llevar la cuenta.
                indice))
            .ToList();

    internal static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    /// <summary>
    /// Devuelve el arbol persistido con Ids reales y le pega de vuelta el UiId de cada alta,
    /// para que el front rehidrate sin depender del orden.
    /// </summary>
    internal static async Task<NotaVersionResponse> RehidratarAsync(
        INotaVersionQueryService consultas,
        int idNotaVersion,
        IReadOnlyList<RenglonPersistido> persistidos,
        CancellationToken cancellationToken)
    {
        var nota = await consultas.ObtenerPorIdAsync(idNotaVersion, cancellationToken)
            ?? throw new NotFoundException("nota de version", idNotaVersion);

        var uiPorId = persistidos
            .Where(p => p.UiId != null)
            .ToDictionary(p => p.IdNotaVersionDetalle, p => p.UiId);

        foreach (var renglon in nota.Renglones)
        {
            if (uiPorId.TryGetValue(renglon.IdNotaVersionDetalle, out var uiId))
            {
                renglon.UiId = uiId;
            }
        }

        return nota;
    }
}

/// <summary>Guardado atomico del arbol (sync pattern), devolviendo la nota persistida.</summary>
public class ActualizarNotaVersionHandler(
    INotaVersionRepository repositorio,
    INotaVersionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarNotaVersionCommand, NotaVersionResponse>
{
    public async Task<NotaVersionResponse> Handle(
        ActualizarNotaVersionCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosNotasVersion.Administrar, null, cancellationToken);

        _ = await repositorio.ObtenerEstadoAsync(command.IdNotaVersion, cancellationToken)
            ?? throw new NotFoundException("nota de version", command.IdNotaVersion);

        var version = command.Datos.Version.Trim();
        if (await repositorio.ExisteVersionAsync(version, command.IdNotaVersion, cancellationToken))
        {
            throw new ConflictException($"Ya existe una nota para la version {version}.");
        }

        var renglones = await repositorio.ActualizarAsync(
            new NotaVersionEdicion(
                command.IdNotaVersion,
                version,
                command.Datos.FechaLiberacion,
                CrearNotaVersionHandler.Limpiar(command.Datos.Resumen),
                command.Datos.Publicada,
                CrearNotaVersionHandler.MapearRenglones(command.Datos.Renglones)),
            cancellationToken);

        return await CrearNotaVersionHandler.RehidratarAsync(
            consultas, command.IdNotaVersion, renglones, cancellationToken);
    }
}

public class EliminarNotaVersionHandler(
    INotaVersionRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<EliminarNotaVersionCommand, Unit>
{
    public async Task<Unit> Handle(EliminarNotaVersionCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosNotasVersion.Administrar, null, cancellationToken);

        _ = await repositorio.ObtenerEstadoAsync(command.IdNotaVersion, cancellationToken)
            ?? throw new NotFoundException("nota de version", command.IdNotaVersion);

        await repositorio.EliminarAsync(command.IdNotaVersion, cancellationToken);
        return Unit.Value;
    }
}
