using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarPuestoCommand(int IdPuesto, PuestoEditarRequest Datos) : IRequest<PuestoResponse>;

public class ActualizarPuestoValidator : AbstractValidator<ActualizarPuestoCommand>
{
    public ActualizarPuestoValidator()
    {
        RuleFor(c => c.IdPuesto).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del puesto es obligatorio.").MaximumLength(100);
    }
}

public class ActualizarPuestoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarPuestoCommand, PuestoResponse>
{
    public async Task<PuestoResponse> Handle(ActualizarPuestoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerPuestoAsync(command.IdPuesto, cancellationToken)
            ?? throw new NotFoundException("Puesto", command.IdPuesto);

        await repositorio.ActualizarPuestoAsync(
            new PuestoEdicion(command.IdPuesto, command.Datos.Nombre.Trim(), command.Datos.IdArea), cancellationToken);

        return await consultas.ObtenerPuestoAsync(command.IdPuesto, cancellationToken)
            ?? throw new NotFoundException("Puesto", command.IdPuesto);
    }
}
