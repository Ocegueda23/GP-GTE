using FluentValidation;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record ActualizarIncidenteCommand(int IdIncidente, IncidenteActualizarRequest Datos) : IRequest<IncidenteResponse>;

public class ActualizarIncidenteValidator : AbstractValidator<ActualizarIncidenteCommand>
{
    public ActualizarIncidenteValidator()
    {
        RuleFor(c => c.IdIncidente).GreaterThan(0);
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.MinutosIndisponibilidad).GreaterThanOrEqualTo(0)
            .When(c => c.Datos.MinutosIndisponibilidad.HasValue);
    }
}

/// <summary>Edita campos propios (titulo, descripcion, causa raiz, minutos de
/// indisponibilidad, fecha de deteccion) sin tocar el estatus del incidente.</summary>
public class ActualizarIncidenteHandler(
    IIncidenteRepository repositorio,
    IIncidenteQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarIncidenteCommand, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(ActualizarIncidenteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);

        _ = await repositorio.ObtenerEstadoAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);

        await repositorio.ActualizarAsync(command.IdIncidente, new IncidenteActualizacion(
            command.Datos.Titulo.Trim(), command.Datos.Descripcion, command.Datos.CausaRaiz,
            command.Datos.MinutosIndisponibilidad, command.Datos.FechaDeteccion), cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);
    }
}
