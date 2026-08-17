using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarAreaCommand(int IdArea, AreaEditarRequest Datos) : IRequest<AreaResponse>;

public class ActualizarAreaValidator : AbstractValidator<ActualizarAreaCommand>
{
    public ActualizarAreaValidator()
    {
        RuleFor(c => c.IdArea).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del area es obligatorio.").MaximumLength(100);
    }
}

public class ActualizarAreaHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarAreaCommand, AreaResponse>
{
    public async Task<AreaResponse> Handle(ActualizarAreaCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerAreaAsync(command.IdArea, cancellationToken)
            ?? throw new NotFoundException("Area", command.IdArea);

        await repositorio.ActualizarAreaAsync(
            new AreaEdicion(command.IdArea, command.Datos.Nombre.Trim()), cancellationToken);

        return await consultas.ObtenerAreaAsync(command.IdArea, cancellationToken)
            ?? throw new NotFoundException("Area", command.IdArea);
    }
}
