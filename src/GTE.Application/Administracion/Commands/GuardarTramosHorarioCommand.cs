using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record GuardarTramosHorarioCommand(int IdHorario, GuardarTramosHorarioRequest Datos) : IRequest<HorarioDetalleResponse>;

public class GuardarTramosHorarioValidator : AbstractValidator<GuardarTramosHorarioCommand>
{
    public GuardarTramosHorarioValidator()
    {
        RuleFor(c => c.IdHorario).GreaterThan(0);
        RuleForEach(c => c.Datos.Tramos).ChildRules(tramo =>
        {
            tramo.RuleFor(t => t.DiaSemana).InclusiveBetween((byte)1, (byte)7)
                .WithMessage("El dia de la semana debe estar entre 1 (lunes) y 7 (domingo).");
            tramo.RuleFor(t => t.HoraFin).GreaterThan(t => t.HoraInicio)
                .WithMessage("La hora de fin del tramo debe ser posterior a la de inicio.");
        });
    }
}

/// <summary>Reemplaza en una sola llamada todos los tramos del horario (guardado en lote).</summary>
public class GuardarTramosHorarioHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<GuardarTramosHorarioCommand, HorarioDetalleResponse>
{
    public async Task<HorarioDetalleResponse> Handle(GuardarTramosHorarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerHorarioAsync(command.IdHorario, cancellationToken)
            ?? throw new NotFoundException("Horario", command.IdHorario);

        var duplicados = command.Datos.Tramos
            .GroupBy(t => (t.DiaSemana, t.HoraInicio))
            .Any(g => g.Count() > 1);
        if (duplicados)
        {
            throw new BusinessException("Hay tramos repetidos con el mismo dia y hora de inicio.");
        }

        var tramos = command.Datos.Tramos
            .Select(t => new TramoHorario(t.DiaSemana, t.HoraInicio, t.HoraFin))
            .ToList();
        await repositorio.GuardarTramosHorarioAsync(command.IdHorario, tramos, cancellationToken);

        return await consultas.ObtenerHorarioAsync(command.IdHorario, cancellationToken)
            ?? throw new NotFoundException("Horario", command.IdHorario);
    }
}
