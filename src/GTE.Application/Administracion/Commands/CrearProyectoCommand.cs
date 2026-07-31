using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearProyectoCommand(ProyectoCrearRequest Datos) : IRequest<ProyectoResponse>;

public class CrearProyectoValidator : AbstractValidator<CrearProyectoCommand>
{
    public CrearProyectoValidator()
    {
        RuleFor(c => c.Datos.Clave).NotEmpty().WithMessage("La clave del proyecto es obligatoria.").MaximumLength(20);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del proyecto es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.IdCategoriaProyecto).GreaterThan(0).WithMessage("La categoria es obligatoria.");
        RuleFor(c => c.Datos.FechaFinPlan).GreaterThan(c => c.Datos.FechaInicioPlan)
            .When(c => c.Datos.FechaInicioPlan.HasValue && c.Datos.FechaFinPlan.HasValue)
            .WithMessage("La fecha de fin plan debe ser posterior a la de inicio.");
    }
}

/// <summary>El estatus inicial (Propuesto) y el folio (se asigna al AUTORIZAR) los fija el backend.</summary>
public class CrearProyectoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearProyectoCommand, ProyectoResponse>
{
    public async Task<ProyectoResponse> Handle(CrearProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idProyecto = await repositorio.CrearProyectoAsync(new ProyectoNuevo(
            command.Datos.Clave.Trim(), command.Datos.Nombre.Trim(), command.Datos.IdPrograma,
            command.Datos.IdCategoriaProyecto, command.Datos.IdResponsable, command.Datos.IdEquipo,
            command.Datos.FechaInicioPlan, command.Datos.FechaFinPlan, command.Datos.EsMantenimiento),
            cancellationToken);

        return await consultas.ObtenerProyectoAsync(idProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", idProyecto);
    }
}
