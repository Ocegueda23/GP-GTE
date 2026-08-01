using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Autenticacion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearUsuarioCommand(UsuarioCrearRequest Datos) : IRequest<UsuarioCreadoResponse>;

public class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioValidator()
    {
        RuleFor(c => c.Datos.Dominio).NotEmpty().WithMessage("La cuenta de dominio es obligatoria.").MaximumLength(100);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.Correo).MaximumLength(200).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.Datos.Correo))
            .WithMessage("El correo no tiene un formato valido.");
    }
}

/// <summary>
/// Alta manual de usuario. No aplica RN-ADM-01 (ciclo de jerarquia): un usuario recien
/// creado no puede formar parte de un ciclo porque todavia nadie lo referencia como jefe.
/// Genera una password temporal (el usuario debe cambiarla en su primer login) y la
/// regresa una sola vez en la respuesta: no se puede volver a consultar despues.
/// </summary>
public class CrearUsuarioHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IAutenticacionRepository autenticacion,
    IHashPassword hasher,
    IVerificadorPermisos permisos) : IRequestHandler<CrearUsuarioCommand, UsuarioCreadoResponse>
{
    public async Task<UsuarioCreadoResponse> Handle(CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idUsuario = await repositorio.CrearUsuarioAsync(new UsuarioNuevo(
            command.Datos.Dominio.Trim(), command.Datos.Nombre.Trim(), command.Datos.Correo,
            command.Datos.IdPuesto, command.Datos.IdNivel, command.Datos.IdHorario, command.Datos.IdJefe),
            cancellationToken);

        var passwordTemporal = GeneradorPasswordTemporal.Generar();
        await autenticacion.EstablecerPasswordAsync(idUsuario, hasher.Hash(passwordTemporal), true, cancellationToken);

        var usuario = await consultas.ObtenerUsuarioAsync(idUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", idUsuario);

        return new UsuarioCreadoResponse
        {
            IdUsuario = usuario.IdUsuario,
            Dominio = usuario.Dominio,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            IdPuesto = usuario.IdPuesto,
            Puesto = usuario.Puesto,
            IdNivel = usuario.IdNivel,
            Nivel = usuario.Nivel,
            IdHorario = usuario.IdHorario,
            Horario = usuario.Horario,
            IdJefe = usuario.IdJefe,
            Jefe = usuario.Jefe,
            EsExterno = usuario.EsExterno,
            FechaAlta = usuario.FechaAlta,
            FechaBaja = usuario.FechaBaja,
            Activo = usuario.Activo,
            PasswordTemporal = passwordTemporal
        };
    }
}
