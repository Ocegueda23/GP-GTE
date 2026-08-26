using FluentValidation;
using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Autenticacion.Commands;

public record IniciarSuplantacionCommand(int IdUsuarioSuplantado, string Password) : IRequest<SuplantacionResultado>;

public record SuplantacionResultado(string Token, DateTime Expira, SesionResponse Sesion, string UsuarioRealNombre);

public record TerminarSuplantacionCommand : IRequest<Unit>;

public class IniciarSuplantacionValidator : AbstractValidator<IniciarSuplantacionCommand>
{
    public IniciarSuplantacionValidator()
    {
        RuleFor(c => c.IdUsuarioSuplantado).GreaterThan(0);
        RuleFor(c => c.Password).NotEmpty().WithMessage("Confirma tu contraseña para continuar.");
    }
}

/// <summary>
/// "Iniciar sesion como" (soporte), rediseño auditado del Documento Maestro §8.1: exige el
/// permiso ADM.Suplantar y re-autenticacion del suplantador con SU PROPIA contraseña (no la
/// del suplantado). El token resultante evalua permisos como el suplantado
/// (preferred_username, que es lo que lee AuditContext.Usuario/VerificadorPermisos) y lleva
/// un claim extra "actor_real" para la doble identidad en bitacora.
/// </summary>
public class IniciarSuplantacionHandler(
    IProveedorUsuarioActual proveedorUsuario,
    IAutenticacionRepository repositorio,
    IHashPassword hasher,
    IVerificadorPermisos permisos,
    ISesionQueryService sesiones,
    IEmisorTokenSesion emisor,
    AuditContext auditoria) : IRequestHandler<IniciarSuplantacionCommand, SuplantacionResultado>
{
    public async Task<SuplantacionResultado> Handle(
        IniciarSuplantacionCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Suplantar, cancellationToken: cancellationToken);

        if (auditoria.EsSuplantacion)
        {
            throw new BusinessException(
                "Ya hay una suplantacion activa; termina la actual antes de iniciar otra.");
        }

        var usuarioReal = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        if (command.IdUsuarioSuplantado == usuarioReal.IdUsuario)
        {
            throw new BusinessException("No puedes suplantarte a ti mismo.");
        }

        var credencialesReal = await repositorio.ObtenerCredencialesAsync(usuarioReal.Dominio, cancellationToken)
            ?? throw new NotFoundException("Usuario", usuarioReal.IdUsuario);

        if (credencialesReal.PasswordHash is null || !hasher.Verificar(command.Password, credencialesReal.PasswordHash))
        {
            throw new BusinessException("Tu contraseña no es correcta.");
        }

        var sesionSuplantado = await sesiones.ObtenerSesionAsync(command.IdUsuarioSuplantado, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuarioSuplantado);

        var (token, expira) = emisor.EmitirTokenSuplantacion(sesionSuplantado, usuarioReal.Dominio);

        await repositorio.RegistrarBitacoraSuplantacionAsync(
            command.IdUsuarioSuplantado, "INICIAR_SUPLANTACION",
            $"{usuarioReal.Dominio} inicio sesion como {sesionSuplantado.Dominio}", cancellationToken);

        return new SuplantacionResultado(token, expira, sesionSuplantado, usuarioReal.Nombre);
    }
}

/// <summary>
/// Cierra una suplantacion activa: solo deja rastro en bitacora (el JWT es sin estado, no
/// hay nada que revocar). El cliente vuelve a usar el token real que tenia guardado antes
/// de suplantar.
/// </summary>
public class TerminarSuplantacionHandler(
    AuditContext auditoria,
    IProveedorUsuarioActual proveedorUsuario,
    IAutenticacionRepository repositorio) : IRequestHandler<TerminarSuplantacionCommand, Unit>
{
    public async Task<Unit> Handle(TerminarSuplantacionCommand command, CancellationToken cancellationToken)
    {
        if (!auditoria.EsSuplantacion)
        {
            throw new BusinessException("No hay una suplantacion activa.");
        }

        var suplantado = await proveedorUsuario.ObtenerAsync(cancellationToken);
        await repositorio.RegistrarBitacoraSuplantacionAsync(
            suplantado?.IdUsuario ?? 0, "TERMINAR_SUPLANTACION",
            $"{auditoria.UsuarioReal} termino la suplantacion de {auditoria.Usuario}", cancellationToken);

        return Unit.Value;
    }
}
