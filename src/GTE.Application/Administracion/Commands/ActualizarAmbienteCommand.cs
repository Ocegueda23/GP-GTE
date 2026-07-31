using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarAmbienteCommand(int IdAmbiente, AmbienteEditarRequest Datos) : IRequest<AmbienteResponse>;

public class ActualizarAmbienteValidator : AbstractValidator<ActualizarAmbienteCommand>
{
    public ActualizarAmbienteValidator()
    {
        RuleFor(c => c.IdAmbiente).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del ambiente es obligatorio.").MaximumLength(100);
        RuleFor(c => c.Datos.Url).MaximumLength(500);
        RuleFor(c => c.Datos.Servidor).MaximumLength(200);
        RuleFor(c => c.Datos.BaseDatos).MaximumLength(200);
    }
}

public class ActualizarAmbienteHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarAmbienteCommand, AmbienteResponse>
{
    public async Task<AmbienteResponse> Handle(ActualizarAmbienteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerAmbienteAsync(command.IdAmbiente, cancellationToken)
            ?? throw new NotFoundException("Ambiente", command.IdAmbiente);

        await repositorio.ActualizarAmbienteAsync(new AmbienteEdicion(
            command.IdAmbiente, command.Datos.Nombre.Trim(), command.Datos.Url,
            command.Datos.Servidor, command.Datos.BaseDatos, command.Datos.IdResponsable), cancellationToken);

        return await consultas.ObtenerAmbienteAsync(command.IdAmbiente, cancellationToken)
            ?? throw new NotFoundException("Ambiente", command.IdAmbiente);
    }
}
