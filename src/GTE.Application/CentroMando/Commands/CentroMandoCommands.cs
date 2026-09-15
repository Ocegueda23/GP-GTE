using FluentValidation;
using GTE.Application.DTOs.Request.CentroMando;
using GTE.Application.Interfaces;
using GTE.Domain.CentroMando;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.CentroMando.Commands;

/// <summary>Calibra meta, umbral, peso y accion sugerida de un indicador del catalogo.</summary>
public record ActualizarIndicadorGestionCommand(int IdIndicadorGestion, ActualizarIndicadorGestionRequest Datos)
    : IRequest<Unit>;

public class ActualizarIndicadorGestionValidator : AbstractValidator<ActualizarIndicadorGestionCommand>
{
    public ActualizarIndicadorGestionValidator()
    {
        RuleFor(c => c.IdIndicadorGestion).GreaterThan(0);
        RuleFor(c => c.Datos.Peso)
            .InclusiveBetween(0m, 100m)
            .WithMessage("El peso debe estar entre 0 y 100.");
        RuleFor(c => c.Datos.AccionSugerida)
            .MaximumLength(1000);
    }
}

public class ActualizarIndicadorGestionHandler(IVerificadorPermisos permisos, ICentroMandoRepository repositorio)
    : IRequestHandler<ActualizarIndicadorGestionCommand, Unit>
{
    public async Task<Unit> Handle(ActualizarIndicadorGestionCommand comando, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Administrar, cancellationToken: cancellationToken);

        if (!await repositorio.ExisteIndicadorAsync(comando.IdIndicadorGestion, cancellationToken))
        {
            throw new NotFoundException("IndicadorGestion", comando.IdIndicadorGestion);
        }

        await repositorio.ActualizarIndicadorAsync(
            comando.IdIndicadorGestion, comando.Datos.Meta, comando.Datos.UmbralAlerta, comando.Datos.Peso,
            comando.Datos.PonderaEnScore, comando.Datos.AccionSugerida, comando.Datos.Activo, cancellationToken);

        return Unit.Value;
    }
}

/// <summary>
/// Recalculo manual de un periodo. Exige GES.Administrar (no solo GES.Ver): reescribe las
/// evaluaciones y alertas de ese mes, asi que no es una consulta.
/// </summary>
public record RecalcularPeriodoCommand(int Anio, int Mes) : IRequest<int>;

public class RecalcularPeriodoValidator : AbstractValidator<RecalcularPeriodoCommand>
{
    public RecalcularPeriodoValidator()
    {
        RuleFor(c => c.Anio).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
    }
}

public class RecalcularPeriodoHandler(
    IVerificadorPermisos permisos,
    IMotorEvaluacionCentroMando motor,
    IGeneradorAlertasCentroMando alertas)
    : IRequestHandler<RecalcularPeriodoCommand, int>
{
    public async Task<int> Handle(RecalcularPeriodoCommand comando, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Administrar, cancellationToken: cancellationToken);

        var evaluados = await motor.RecalcularPeriodoAsync(comando.Anio, comando.Mes, cancellationToken);

        // Las alertas se regeneran en la misma operacion: recalcular sin esto dejaria en
        // pantalla alertas de una evaluacion que ya se sobreescribio.
        await alertas.GenerarAsync(comando.Anio, comando.Mes, cancellationToken);

        return evaluados;
    }
}

/// <summary>Marca una alerta como atendida (no la borra: el historial es la evidencia).</summary>
public record AtenderAlertaGestionCommand(long IdAlertaGestion) : IRequest<Unit>;

public class AtenderAlertaGestionHandler(IVerificadorPermisos permisos, ICentroMandoRepository repositorio)
    : IRequestHandler<AtenderAlertaGestionCommand, Unit>
{
    public async Task<Unit> Handle(AtenderAlertaGestionCommand comando, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Ver, cancellationToken: cancellationToken);

        if (!await repositorio.ExisteAlertaAsync(comando.IdAlertaGestion, cancellationToken))
        {
            throw new NotFoundException("AlertaGestion", comando.IdAlertaGestion);
        }

        await repositorio.MarcarAlertaAtendidaAsync(comando.IdAlertaGestion, cancellationToken);
        return Unit.Value;
    }
}
