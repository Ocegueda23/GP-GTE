using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record ActualizarUsuarioCommand(int IdUsuario, UsuarioEditarRequest Datos) : IRequest<UsuarioResponse>;

public class ActualizarUsuarioValidator : AbstractValidator<ActualizarUsuarioCommand>
{
    public ActualizarUsuarioValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.Correo).MaximumLength(200).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.Datos.Correo))
            .WithMessage("El correo no tiene un formato valido.");
    }
}

/// <summary>
/// RN-ADM-01: un usuario no puede ser su propio jefe ni formar ciclos en la jerarquia.
/// El rol Administrador no cortocircuita esta validacion (RN-ADM-02).
/// </summary>
public class ActualizarUsuarioHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarUsuarioCommand, UsuarioResponse>
{
    public async Task<UsuarioResponse> Handle(ActualizarUsuarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        _ = await consultas.ObtenerUsuarioAsync(command.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuario);

        if (command.Datos.IdJefe.HasValue)
        {
            if (command.Datos.IdJefe.Value == command.IdUsuario)
            {
                throw new BusinessException("Un usuario no puede ser su propio jefe.");
            }

            if (await repositorio.FormariaCicloJerarquiaAsync(command.IdUsuario, command.Datos.IdJefe.Value, cancellationToken))
            {
                throw new BusinessException("Ese jefe formaria un ciclo en la jerarquia.");
            }
        }

        await repositorio.ActualizarUsuarioAsync(new UsuarioEdicion(
            command.IdUsuario, command.Datos.Nombre.Trim(), command.Datos.Correo,
            command.Datos.IdPuesto, command.Datos.IdNivel, command.Datos.IdHorario, command.Datos.IdJefe),
            cancellationToken);

        return await consultas.ObtenerUsuarioAsync(command.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuario);
    }
}
