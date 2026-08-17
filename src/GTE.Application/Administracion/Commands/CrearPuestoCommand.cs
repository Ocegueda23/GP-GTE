using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearPuestoCommand(PuestoCrearRequest Datos) : IRequest<PuestoResponse>;

public class CrearPuestoValidator : AbstractValidator<CrearPuestoCommand>
{
    public CrearPuestoValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del puesto es obligatorio.").MaximumLength(100);
    }
}

public class CrearPuestoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearPuestoCommand, PuestoResponse>
{
    public async Task<PuestoResponse> Handle(CrearPuestoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idPuesto = await repositorio.CrearPuestoAsync(
            new PuestoNuevo(command.Datos.Nombre.Trim(), command.Datos.IdArea), cancellationToken);

        return await consultas.ObtenerPuestoAsync(idPuesto, cancellationToken)
            ?? throw new NotFoundException("Puesto", idPuesto);
    }
}
