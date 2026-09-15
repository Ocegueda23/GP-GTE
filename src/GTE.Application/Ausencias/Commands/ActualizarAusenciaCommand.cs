using FluentValidation;
using GTE.Application.DTOs.Request.Ausencias;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.Interfaces;
using GTE.Domain.Ausencias;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Ausencias.Commands;

public record ActualizarAusenciaCommand(int IdAusencia, AusenciaEditarRequest Datos) : IRequest<AusenciaResponse>;

public class ActualizarAusenciaValidator : AbstractValidator<ActualizarAusenciaCommand>
{
    public ActualizarAusenciaValidator()
    {
        RuleFor(c => c.IdAusencia).GreaterThan(0);
        RuleFor(c => c.Datos.IdTipoAusencia).GreaterThan(0).WithMessage("El tipo de ausencia es obligatorio.");
        RuleFor(c => c.Datos.FechaInicio).NotEqual(default(DateOnly)).WithMessage("La fecha de inicio es obligatoria.");
        RuleFor(c => c.Datos.FechaFin).NotEqual(default(DateOnly)).WithMessage("La fecha de fin es obligatoria.");
        RuleFor(c => c.Datos)
            .Must(d => d.FechaFin >= d.FechaInicio)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
        RuleFor(c => c.Datos.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Edicion del periodo/tipo mientras la ausencia siga en Solicitada: solo el dueño o
/// quien tenga ADM.Ausencias. Una vez aprobada o rechazada ya no se edita (se cancela
/// y se registra otra), porque Planeacion y los reportes ya la estan descontando.
/// </summary>
public class ActualizarAusenciaHandler(
    IAusenciaRepository repositorio,
    IAusenciaQueryService consultas,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ActualizarAusenciaCommand, AusenciaResponse>
{
    public async Task<AusenciaResponse> Handle(
        ActualizarAusenciaCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", command.IdAusencia);

        if (estado.IdEstatus != EstatusAusencia.Solicitada)
        {
            throw new BusinessException("Solo se puede editar una ausencia mientras sigue solicitada.");
        }

        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
        var esDueno = usuario is not null && usuario.IdUsuario == estado.IdUsuario;
        if (!esDueno && !await permisos.TienePermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken))
        {
            throw new ForbiddenException("Solo puedes editar tus propias ausencias.");
        }

        var traslapes = await repositorio.ObtenerTraslapesAsync(
            estado.IdUsuario, command.Datos.FechaInicio, command.Datos.FechaFin,
            command.IdAusencia, cancellationToken);
        if (traslapes.Count > 0)
        {
            throw new ConflictException(
                "Ya existe una ausencia registrada que se cruza con ese periodo.", traslapes);
        }

        var motivo = string.IsNullOrWhiteSpace(command.Datos.Motivo) ? null : command.Datos.Motivo.Trim();

        await repositorio.ActualizarAsync(new AusenciaEdicion(
            command.IdAusencia, command.Datos.IdTipoAusencia,
            command.Datos.FechaInicio, command.Datos.FechaFin, motivo), cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", command.IdAusencia);
    }
}
