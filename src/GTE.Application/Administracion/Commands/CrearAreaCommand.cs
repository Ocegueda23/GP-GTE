using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearAreaCommand(AreaCrearRequest Datos) : IRequest<AreaResponse>;

public class CrearAreaValidator : AbstractValidator<CrearAreaCommand>
{
    public CrearAreaValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del area es obligatorio.").MaximumLength(100);
    }
}

public class CrearAreaHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearAreaCommand, AreaResponse>
{
    public async Task<AreaResponse> Handle(CrearAreaCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idArea = await repositorio.CrearAreaAsync(new AreaNueva(command.Datos.Nombre.Trim()), cancellationToken);

        return await consultas.ObtenerAreaAsync(idArea, cancellationToken)
            ?? throw new NotFoundException("Area", idArea);
    }
}
