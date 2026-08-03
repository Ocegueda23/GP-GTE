using FluentValidation;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record CrearIncidenteCommand(IncidenteCrearRequest Datos) : IRequest<IncidenteResponse>;

public class CrearIncidenteValidator : AbstractValidator<CrearIncidenteCommand>
{
    public CrearIncidenteValidator()
    {
        RuleFor(c => c.Datos.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.IdSeveridad).GreaterThan(0).WithMessage("La severidad es obligatoria.");
        RuleFor(c => c.Datos.FechaOcurrencia).NotEqual(default(DateTime)).WithMessage("La fecha de ocurrencia es obligatoria.");
    }
}

/// <summary>
/// Alta de incidente: estatus inicial Detectado (lo fija el backend), folio de la
/// serie INC-anio. RN-OPS-01 (parcial): un incidente S1 notifica de inmediato al
/// responsable del proyecto (tblProyecto.IdResponsable), si esta configurado. El
/// escalamiento a los 30 minutos y el resto de canales quedan fuera de esta pasada
/// (necesitan Hangfire, ver PENDIENTES.md).
/// </summary>
public class CrearIncidenteHandler(
    IIncidenteRepository repositorio,
    IIncidenteQueryService consultas,
    IGeneradorFolios folios,
    IServicioNotificaciones notificaciones) : IRequestHandler<CrearIncidenteCommand, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(CrearIncidenteCommand command, CancellationToken cancellationToken)
    {
        var folio = await folios.GenerarAsync($"INC-{DateTime.Today.Year}", cancellationToken: cancellationToken);

        var idIncidente = await repositorio.CrearAsync(new IncidenteNuevo(
            folio, command.Datos.IdProyecto, command.Datos.IdSeveridad, command.Datos.Titulo.Trim(),
            command.Datos.Descripcion, command.Datos.FechaOcurrencia, command.Datos.FechaDeteccion),
            cancellationToken);

        if (command.Datos.IdSeveridad == Severidad.S1Critica)
        {
            var idResponsable = await repositorio.ObtenerResponsableProyectoAsync(command.Datos.IdProyecto, cancellationToken);
            if (idResponsable.HasValue)
            {
                await notificaciones.NotificarAsync(
                    [idResponsable.Value], $"Incidente critico {folio}", command.Datos.Titulo,
                    "Incidente", idIncidente, "/operacion/incidentes", cancellationToken);
            }
        }

        return await consultas.ObtenerPorIdAsync(idIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", idIncidente);
    }
}
