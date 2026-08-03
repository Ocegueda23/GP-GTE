using FluentValidation;
using GTE.Application.DTOs.Request.Costeo;
using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record ActualizarPresupuestoProyectoCommand(int IdPresupuestoProyecto, PresupuestoProyectoEditarRequest Datos)
    : IRequest<PresupuestoProyectoResponse>;

public class ActualizarPresupuestoProyectoValidator : AbstractValidator<ActualizarPresupuestoProyectoCommand>
{
    public ActualizarPresupuestoProyectoValidator()
    {
        RuleFor(c => c.IdPresupuestoProyecto).GreaterThan(0);
        RuleFor(c => c.Datos.MontoAutorizado).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Datos.HorasAutorizadas).GreaterThanOrEqualTo(0);
    }
}

public class ActualizarPresupuestoProyectoHandler(
    ICosteoRepository repositorio,
    ICosteoQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarPresupuestoProyectoCommand, PresupuestoProyectoResponse>
{
    public async Task<PresupuestoProyectoResponse> Handle(ActualizarPresupuestoProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);

        await repositorio.ActualizarPresupuestoAsync(new PresupuestoProyectoEdicion(
            command.IdPresupuestoProyecto, command.Datos.MontoAutorizado, command.Datos.HorasAutorizadas),
            cancellationToken);

        return await consultas.ObtenerPresupuestoAsync(command.IdPresupuestoProyecto, cancellationToken)
            ?? throw new NotFoundException("PresupuestoProyecto", command.IdPresupuestoProyecto);
    }
}
