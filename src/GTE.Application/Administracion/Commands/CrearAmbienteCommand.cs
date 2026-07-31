using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearAmbienteCommand(AmbienteCrearRequest Datos) : IRequest<AmbienteResponse>;

public class CrearAmbienteValidator : AbstractValidator<CrearAmbienteCommand>
{
    public CrearAmbienteValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del ambiente es obligatorio.").MaximumLength(100);
        RuleFor(c => c.Datos.Url).MaximumLength(500);
        RuleFor(c => c.Datos.Servidor).MaximumLength(200);
        RuleFor(c => c.Datos.BaseDatos).MaximumLength(200);
    }
}

public class CrearAmbienteHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearAmbienteCommand, AmbienteResponse>
{
    public async Task<AmbienteResponse> Handle(CrearAmbienteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idAmbiente = await repositorio.CrearAmbienteAsync(new AmbienteNuevo(
            command.Datos.IdProyecto, command.Datos.Nombre.Trim(), command.Datos.Url,
            command.Datos.Servidor, command.Datos.BaseDatos, command.Datos.IdResponsable), cancellationToken);

        return await consultas.ObtenerAmbienteAsync(idAmbiente, cancellationToken)
            ?? throw new NotFoundException("Ambiente", idAmbiente);
    }
}
