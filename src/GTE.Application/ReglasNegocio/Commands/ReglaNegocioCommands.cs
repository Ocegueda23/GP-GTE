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
   Alta
   ===================================================================== */

public record CrearReglaNegocioCommand(ReglaNegocioCrearRequest Datos) : IRequest<ReglaNegocioResponse>;

public class CrearReglaNegocioValidator : AbstractValidator<CrearReglaNegocioCommand>
{
    public CrearReglaNegocioValidator()
    {
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0)
            .WithMessage("La regla debe pertenecer a un proyecto.");
        // La clave ya no se valida: no la captura nadie, la genera el backend.
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ConstantesReglasNegocio.LongitudMaximaNombre);
        RuleFor(c => c.Datos.Enunciado).NotEmpty().WithMessage("El enunciado es obligatorio.");
        RuleFor(c => c.Datos.IdEstadoReglaNegocio).InclusiveBetween(1, 4)
            .WithMessage("El estado de la regla no es valido.");
        RuleFor(c => c.Datos.MensajeError).MaximumLength(ConstantesReglasNegocio.LongitudMaximaMensajeError);
        RuleFor(c => c.Datos.UbicacionCodigo).MaximumLength(ConstantesReglasNegocio.LongitudMaximaUbicacionCodigo);
        RuleFor(c => c.Datos)
            .Must(d => d.FechaVigenciaHasta is null || d.FechaVigenciaDesde is null
                       || d.FechaVigenciaHasta >= d.FechaVigenciaDesde)
            .WithMessage("La vigencia final no puede ser anterior a la inicial.");
    }
}

/// <summary>
/// Alta de una regla en su proyecto dueno.
///
/// La clave se forma SOLA: RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO DE 3 DIGITOS}. El
/// consecutivo lo entrega dbo.spGenerarFolio (serie "RN-{CLAVE}"), que es el motor de
/// folios del sistema y toma ROWLOCK/UPDLOCK/HOLDLOCK: dos altas simultaneas en el mismo
/// proyecto no pueden sacar el mismo numero. NO se usa MAX()+1 justamente por eso.
/// </summary>
public class CrearReglaNegocioHandler(
    IReglasNegocioRepository repositorio,
    IReglasNegocioQueryService consultas,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos) : IRequestHandler<CrearReglaNegocioCommand, ReglaNegocioResponse>
{
    /// <summary>
    /// Reintentos por si la serie de folio quedo desfasada de las claves reales (por
    /// ejemplo, datos cargados por script que no pasaron por spGenerarFolio). Cada
    /// reintento consume un consecutivo y prueba el siguiente; la UNIQUE de BD es la
    /// garantia final.
    /// </summary>
    private const int IntentosClave = 5;

    public async Task<ReglaNegocioResponse> Handle(
        CrearReglaNegocioCommand command, CancellationToken cancellationToken)
    {
        var datos = command.Datos;
        await permisos.ExigirPermisoAsync(PermisosReglasNegocio.Administrar, datos.IdProyecto, cancellationToken);

        var claveProyecto = await repositorio.ObtenerClaveProyectoAsync(datos.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", datos.IdProyecto);

        var clave = await GenerarClaveAsync(datos.IdProyecto, claveProyecto, cancellationToken);

        await ValidarAmbitoAsync(repositorio, datos.IdAmbitoRegla, datos.IdProyecto, cancellationToken);

        var idRegla = await repositorio.CrearAsync(
            new ReglaNegocioNueva(
                datos.IdProyecto, clave, datos.Nombre.Trim(), datos.Enunciado, datos.Justificacion,
                datos.IdAmbitoRegla, datos.IdEstadoReglaNegocio, datos.MensajeError,
                datos.PermisoBypass, datos.UbicacionCodigo,
                datos.FechaVigenciaDesde, datos.FechaVigenciaHasta),
            cancellationToken);

        return await consultas.ObtenerPorIdAsync(idRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", idRegla);
    }

    private async Task<string> GenerarClaveAsync(
        int idProyecto, string claveProyecto, CancellationToken cancellationToken)
    {
        var serie = ClaveReglaNegocio.SerieDeProyecto(claveProyecto);

        for (var intento = 0; intento < IntentosClave; intento++)
        {
            var candidata = await folios.GenerarAsync(
                serie, ClaveReglaNegocio.DigitosConsecutivo, cancellationToken);

            if (!await repositorio.ExisteClaveAsync(idProyecto, candidata, null, cancellationToken))
            {
                return candidata;
            }
        }

        throw new ConflictException(
            $"No se pudo generar una clave libre para la serie {serie}. "
            + "Revisa el consecutivo de esa serie en dbo.tblFolio.");
    }

    /// <summary>
    /// El ambito tiene que ser del MISMO proyecto que la regla: tblAmbitoRegla es catalogo
    /// propio de cada proyecto, y dejar que una regla apunte al flujo de otro sistema
    /// romperia la lectura por proyecto sin que ninguna FK lo impida.
    /// </summary>
    internal static async Task ValidarAmbitoAsync(
        IReglasNegocioRepository repositorio, int? idAmbito, int idProyecto, CancellationToken cancellationToken)
    {
        if (idAmbito is null)
        {
            return;
        }

        var ambito = await repositorio.ObtenerProyectoAmbitoAsync(idAmbito.Value, cancellationToken)
            ?? throw new NotFoundException("AmbitoRegla", idAmbito.Value);

        if (ambito.IdProyecto != idProyecto)
        {
            throw new BusinessException(
                "El flujo o caracteristica seleccionado pertenece a otro proyecto.");
        }
    }
}

/* =====================================================================
   Edicion
   ===================================================================== */

public record ActualizarReglaNegocioCommand(int IdRegla, ReglaNegocioActualizarRequest Datos)
    : IRequest<ReglaNegocioResponse>;

public class ActualizarReglaNegocioValidator : AbstractValidator<ActualizarReglaNegocioCommand>
{
    public ActualizarReglaNegocioValidator()
    {
        RuleFor(c => c.IdRegla).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ConstantesReglasNegocio.LongitudMaximaNombre);
        RuleFor(c => c.Datos.Enunciado).NotEmpty().WithMessage("El enunciado es obligatorio.");
        RuleFor(c => c.Datos.IdEstadoReglaNegocio).InclusiveBetween(1, 4)
            .WithMessage("El estado de la regla no es valido.");
        RuleFor(c => c.Datos.MensajeError).MaximumLength(ConstantesReglasNegocio.LongitudMaximaMensajeError);
        RuleFor(c => c.Datos.UbicacionCodigo).MaximumLength(ConstantesReglasNegocio.LongitudMaximaUbicacionCodigo);
        RuleFor(c => c.Datos)
            .Must(d => d.FechaVigenciaHasta is null || d.FechaVigenciaDesde is null
                       || d.FechaVigenciaHasta >= d.FechaVigenciaDesde)
            .WithMessage("La vigencia final no puede ser anterior a la inicial.");
    }
}

/// <summary>
/// Edicion de la regla. Se exige el permiso sobre el proyecto DUENO: una regla heredada se
/// lee en el proyecto secundario pero solo se edita en su origen, que es lo que garantiza
/// que nunca existan dos versiones del mismo enunciado.
/// El versionado lo decide el repositorio: solo sube version si el texto cambio.
/// </summary>
public class ActualizarReglaNegocioHandler(
    IReglasNegocioRepository repositorio,
    IReglasNegocioQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarReglaNegocioCommand, ReglaNegocioResponse>
{
    public async Task<ReglaNegocioResponse> Handle(
        ActualizarReglaNegocioCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, estado.IdProyecto, cancellationToken);

        var datos = command.Datos;
        await CrearReglaNegocioHandler.ValidarAmbitoAsync(
            repositorio, datos.IdAmbitoRegla, estado.IdProyecto, cancellationToken);

        await repositorio.ActualizarAsync(
            command.IdRegla,
            new ReglaNegocioActualizacion(
                datos.Nombre.Trim(), datos.Enunciado, datos.Justificacion, datos.IdAmbitoRegla,
                datos.IdEstadoReglaNegocio, datos.MensajeError, datos.PermisoBypass,
                datos.UbicacionCodigo, datos.FechaVigenciaDesde, datos.FechaVigenciaHasta,
                datos.MotivoCambio),
            cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);
    }
}

/* =====================================================================
   Derogacion y reactivacion
   ===================================================================== */

public record DerogarReglaNegocioCommand(int IdRegla, DateOnly? FechaHasta) : IRequest<Unit>;

/// <summary>
/// Derogar una regla es baja logica (Activo = 0) mas estado Derogada y fecha de fin de
/// vigencia. Nunca borrado fisico: una regla derogada sigue explicando por que el sistema
/// se comporto como se comporto en su momento.
/// </summary>
public class DerogarReglaNegocioHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<DerogarReglaNegocioCommand, Unit>
{
    public async Task<Unit> Handle(DerogarReglaNegocioCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, estado.IdProyecto, cancellationToken);

        if (!estado.Activo)
        {
            throw new BusinessException("La regla ya estaba derogada.");
        }

        var fecha = command.FechaHasta ?? DateOnly.FromDateTime(DateTime.Today);
        await repositorio.DerogarAsync(command.IdRegla, fecha, cancellationToken);
        return Unit.Value;
    }
}

public record ReactivarReglaNegocioCommand(int IdRegla) : IRequest<Unit>;

public class ReactivarReglaNegocioHandler(
    IReglasNegocioRepository repositorio,
    IVerificadorPermisos permisos) : IRequestHandler<ReactivarReglaNegocioCommand, Unit>
{
    public async Task<Unit> Handle(ReactivarReglaNegocioCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdRegla, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocio", command.IdRegla);

        await permisos.ExigirPermisoAsync(
            PermisosReglasNegocio.Administrar, estado.IdProyecto, cancellationToken);

        if (estado.Activo)
        {
            throw new BusinessException("La regla ya estaba vigente.");
        }

        await repositorio.ReactivarAsync(command.IdRegla, cancellationToken);
        return Unit.Value;
    }
}
