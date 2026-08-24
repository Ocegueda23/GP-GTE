using FluentValidation;
using GTE.Application.Common;
using GTE.Application.DTOs.Request.CatalogoGenerico;
using GTE.Application.DTOs.Responses.CatalogoGenerico;
using GTE.Application.Interfaces;
using GTE.Domain.CatalogoGenerico;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.CatalogoGenerico.Commands;

public record CrearCatalogoCommand(CatalogoCrearRequest Datos) : IRequest<ConfiguracionCatalogoResponse>;

public class CrearCatalogoValidator : AbstractValidator<CrearCatalogoCommand>
{
    public CrearCatalogoValidator()
    {
        RuleFor(c => c.Datos.Clave).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z][A-Za-z0-9_]*$")
            .WithMessage("La clave solo admite letras, numeros y guion bajo, y debe empezar con letra.");
        RuleFor(c => c.Datos.NombreTabla).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Datos.Titulo).NotEmpty().MaximumLength(200);
    }
}

/// <summary>
/// Alta de un catalogo nuevo. RN clave: NombreTabla solo se acepta si aparece en el
/// listado en VIVO de tablas reales de bdsGTE (SQL dinamico blindado) -- nunca se confia
/// en el nombre de tabla del request sin ese chequeo, sea cual sea su origen.
/// </summary>
public class CrearCatalogoHandler(
    ICatalogoGenericoRepository repositorio,
    ICatalogoGenericoQueryService consultas,
    IMotorMetadatosEsquema motorMetadatos,
    IVerificadorPermisos permisos) : IRequestHandler<CrearCatalogoCommand, ConfiguracionCatalogoResponse>
{
    public async Task<ConfiguracionCatalogoResponse> Handle(CrearCatalogoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Configurar, null, cancellationToken);

        var existente = await consultas.ObtenerPorClaveAsync(command.Datos.Clave, cancellationToken);
        if (existente is not null)
        {
            throw new ConflictException($"Ya existe un catalogo con la clave {command.Datos.Clave}.");
        }

        var tablasDisponibles = await motorMetadatos.ObtenerTablasDisponiblesAsync(cancellationToken);
        if (!tablasDisponibles.Contains(command.Datos.NombreTabla, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException($"La tabla {command.Datos.NombreTabla} no existe en bdsGTE.");
        }

        await repositorio.CrearCatalogoAsync(
            new CatalogoNuevo(command.Datos.Clave, command.Datos.NombreTabla, command.Datos.Titulo), cancellationToken);
        await repositorio.SembrarPermisosAsync(command.Datos.Clave, command.Datos.Titulo, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(command.Datos.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Datos.Clave);
        return ProyeccionesCatalogoGenerico.Proyectar(config);
    }
}

public record ActualizarConfigColumnasCommand(string Clave, List<ColumnaConfigRequest> Columnas)
    : IRequest<ConfiguracionCatalogoResponse>;

public class ActualizarConfigColumnasValidator : AbstractValidator<ActualizarConfigColumnasCommand>
{
    public ActualizarConfigColumnasValidator()
    {
        RuleFor(c => c.Clave).NotEmpty();
        RuleForEach(c => c.Columnas).ChildRules(col =>
        {
            col.RuleFor(x => x.NombreColumna).NotEmpty().MaximumLength(128);
            col.RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        });
    }
}

/// <summary>
/// Guarda la configuracion de presentacion por columna. RN clave: cada NombreColumna se
/// revalida contra el esquema REAL y actual de la tabla antes de persistirse -- de ahi en
/// adelante MotorCrudGenerico confia en los nombres guardados sin volver a consultarlos.
/// </summary>
public class ActualizarConfigColumnasHandler(
    ICatalogoGenericoRepository repositorio,
    ICatalogoGenericoQueryService consultas,
    IMotorMetadatosEsquema motorMetadatos,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarConfigColumnasCommand, ConfiguracionCatalogoResponse>
{
    public async Task<ConfiguracionCatalogoResponse> Handle(
        ActualizarConfigColumnasCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Configurar, null, cancellationToken);

        var catalogo = await consultas.ObtenerPorClaveAsync(command.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Clave);

        var columnasReales = await motorMetadatos.ObtenerColumnasAsync(catalogo.NombreTabla, cancellationToken);
        var nombresReales = columnasReales.Select(c => c.NombreColumna).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalidas = command.Columnas.Where(c => !nombresReales.Contains(c.NombreColumna)).ToList();
        if (invalidas.Count > 0)
        {
            throw new BusinessException(
                $"Las columnas {string.Join(", ", invalidas.Select(c => c.NombreColumna))} no existen en {catalogo.NombreTabla}.");
        }

        // Los 3 campos del combo FK tambien se validan en vivo contra INFORMATION_SCHEMA
        // (misma regla que NombreColumna): de aqui en adelante MotorCrudGenerico confia en
        // ellos para armar SQL dinamico sin volver a consultarlos.
        var tablasDisponiblesFk = await motorMetadatos.ObtenerTablasDisponiblesAsync(cancellationToken);
        foreach (var columna in command.Columnas.Where(c =>
            !string.IsNullOrWhiteSpace(c.TablaFk) || !string.IsNullOrWhiteSpace(c.ColumnaClaveFk)
            || !string.IsNullOrWhiteSpace(c.ColumnaMostrarFk)))
        {
            if (string.IsNullOrWhiteSpace(columna.TablaFk) || string.IsNullOrWhiteSpace(columna.ColumnaClaveFk)
                || string.IsNullOrWhiteSpace(columna.ColumnaMostrarFk))
            {
                throw new BusinessException(
                    $"El combo FK de {columna.NombreColumna} requiere tabla, columna clave y columna a mostrar.");
            }
            if (!tablasDisponiblesFk.Contains(columna.TablaFk, StringComparer.OrdinalIgnoreCase))
            {
                throw new BusinessException($"La tabla FK {columna.TablaFk} no existe en bdsGTE.");
            }
            var columnasFk = await motorMetadatos.ObtenerColumnasAsync(columna.TablaFk, cancellationToken);
            var nombresFk = columnasFk.Select(c => c.NombreColumna).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!nombresFk.Contains(columna.ColumnaClaveFk) || !nombresFk.Contains(columna.ColumnaMostrarFk))
            {
                throw new BusinessException(
                    $"La columna clave o la columna a mostrar del FK de {columna.NombreColumna} no existen en {columna.TablaFk}.");
            }
        }

        var edicion = command.Columnas.Select(c => new ColumnaConfigEdicion(
            c.NombreColumna, c.DisplayName, c.EsVisible, c.EsSoloLectura, c.EsRequerido, c.OrdinalPos,
            c.TablaFk, c.ColumnaClaveFk, c.ColumnaMostrarFk, c.EsCifrado,
            c.AutoFechaAlta, c.AutoFechaEdicion, c.AutoUsuarioAlta, c.AutoUsuarioEdicion)).ToList();

        await repositorio.ActualizarConfigColumnasAsync(catalogo.IdCatalogo, edicion, cancellationToken);
        // Idempotente: backfill de CAT.<CLAVE>.Descifrar para catalogos creados antes de que existiera.
        await repositorio.SembrarPermisosAsync(catalogo.Clave, catalogo.Titulo, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(command.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Clave);
        return ProyeccionesCatalogoGenerico.Proyectar(config);
    }
}

public record CrearRegistroCatalogoCommand(string Clave, Dictionary<string, object?> Valores)
    : IRequest<Dictionary<string, object?>>;

public class CrearRegistroCatalogoHandler(
    ICatalogoGenericoQueryService consultas,
    IMotorCrudGenerico motor,
    IVerificadorPermisos permisos,
    AuditContext auditoria) : IRequestHandler<CrearRegistroCatalogoCommand, Dictionary<string, object?>>
{
    public async Task<Dictionary<string, object?>> Handle(
        CrearRegistroCatalogoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Crear(command.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(command.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Clave);

        ValidarRequeridos(config, command.Valores);

        return await motor.CrearAsync(config, command.Valores, auditoria.Usuario, cancellationToken);
    }

    private static void ValidarRequeridos(ConfiguracionCatalogo config, Dictionary<string, object?> valores)
    {
        var faltantes = config.Columnas
            .Where(c => c.EsRequerido && !c.EsIdentity && !c.AutoFechaAlta && !c.AutoUsuarioAlta)
            .Where(c => !valores.TryGetValue(c.NombreColumna, out var v) || v is null)
            .Select(c => c.DisplayName)
            .ToList();
        if (faltantes.Count > 0)
        {
            throw new BusinessException($"Faltan campos obligatorios: {string.Join(", ", faltantes)}.");
        }
    }
}

public record ActualizarRegistroCatalogoCommand(
    string Clave, Dictionary<string, object?> ClavesPk, Dictionary<string, object?> Valores) : IRequest<Unit>;

public class ActualizarRegistroCatalogoHandler(
    ICatalogoGenericoQueryService consultas,
    IMotorCrudGenerico motor,
    IVerificadorPermisos permisos,
    AuditContext auditoria) : IRequestHandler<ActualizarRegistroCatalogoCommand, Unit>
{
    public async Task<Unit> Handle(ActualizarRegistroCatalogoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Editar(command.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(command.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Clave);

        await motor.ActualizarAsync(config, command.ClavesPk, command.Valores, auditoria.Usuario, cancellationToken);
        return Unit.Value;
    }
}

public record EliminarRegistroCatalogoCommand(string Clave, Dictionary<string, object?> ClavesPk) : IRequest<Unit>;

public class EliminarRegistroCatalogoHandler(
    ICatalogoGenericoQueryService consultas,
    IMotorCrudGenerico motor,
    IVerificadorPermisos permisos,
    AuditContext auditoria) : IRequestHandler<EliminarRegistroCatalogoCommand, Unit>
{
    public async Task<Unit> Handle(EliminarRegistroCatalogoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCatalogoGenerico.Eliminar(command.Clave), null, cancellationToken);

        var config = await consultas.ObtenerConfigCatalogoAsync(command.Clave, cancellationToken)
            ?? throw new NotFoundException("Catalogo", command.Clave);

        await motor.EliminarAsync(config, command.ClavesPk, auditoria.Usuario, cancellationToken);
        return Unit.Value;
    }
}
