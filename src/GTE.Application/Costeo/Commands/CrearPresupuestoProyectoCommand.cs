using FluentValidation;
using GTE.Application.DTOs.Request.Costeo;
using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record CrearPresupuestoProyectoCommand(PresupuestoProyectoCrearRequest Datos) : IRequest<PresupuestoProyectoResponse>;

public class CrearPresupuestoProyectoValidator : AbstractValidator<CrearPresupuestoProyectoCommand>
{
    public CrearPresupuestoProyectoValidator()
    {
        RuleFor(c => c.Datos.IdProyecto).GreaterThan(0).WithMessage("El proyecto es obligatorio.");
        RuleFor(c => c.Datos.Anio).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Datos.MontoAutorizado).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Datos.HorasAutorizadas).GreaterThanOrEqualTo(0);
    }
}

public class CrearPresupuestoProyectoHandler(
    ICosteoRepository repositorio,
    ICosteoQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearPresupuestoProyectoCommand, PresupuestoProyectoResponse>
{
    public async Task<PresupuestoProyectoResponse> Handle(CrearPresupuestoProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);

        var idPresupuesto = await repositorio.CrearPresupuestoAsync(new PresupuestoProyectoNuevo(
            command.Datos.IdProyecto, command.Datos.Anio, command.Datos.MontoAutorizado,
            command.Datos.HorasAutorizadas), cancellationToken);

        return await consultas.ObtenerPresupuestoAsync(idPresupuesto, cancellationToken)
            ?? throw new NotFoundException("PresupuestoProyecto", idPresupuesto);
    }
}
