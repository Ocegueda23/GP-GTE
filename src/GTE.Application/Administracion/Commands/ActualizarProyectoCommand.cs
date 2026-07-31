using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarProyectoCommand(int IdProyecto, ProyectoEditarRequest Datos) : IRequest<ProyectoResponse>;

public class ActualizarProyectoValidator : AbstractValidator<ActualizarProyectoCommand>
{
    public ActualizarProyectoValidator()
    {
        RuleFor(c => c.IdProyecto).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del proyecto es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.IdCategoriaProyecto).GreaterThan(0).WithMessage("La categoria es obligatoria.");
        RuleFor(c => c.Datos.FechaFinPlan).GreaterThan(c => c.Datos.FechaInicioPlan)
            .When(c => c.Datos.FechaInicioPlan.HasValue && c.Datos.FechaFinPlan.HasValue)
            .WithMessage("La fecha de fin plan debe ser posterior a la de inicio.");
    }
}

/// <summary>Edita los campos estructurales del proyecto; el estatus se cambia por separado via el motor.</summary>
public class ActualizarProyectoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarProyectoCommand, ProyectoResponse>
{
    public async Task<ProyectoResponse> Handle(ActualizarProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerProyectoAsync(command.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.IdProyecto);

        await repositorio.ActualizarProyectoAsync(new ProyectoEdicion(
            command.IdProyecto, command.Datos.Nombre.Trim(), command.Datos.IdCategoriaProyecto,
            command.Datos.IdResponsable, command.Datos.IdEquipo,
            command.Datos.FechaInicioPlan, command.Datos.FechaFinPlan, command.Datos.EsMantenimiento),
            cancellationToken);

        return await consultas.ObtenerProyectoAsync(command.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.IdProyecto);
    }
}
