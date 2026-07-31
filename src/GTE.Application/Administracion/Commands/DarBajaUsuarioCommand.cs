using FluentValidation;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record DarBajaUsuarioCommand(int IdUsuario) : IRequest<UsuarioResponse>;

public class DarBajaUsuarioValidator : AbstractValidator<DarBajaUsuarioCommand>
{
    public DarBajaUsuarioValidator()
    {
        RuleFor(c => c.IdUsuario).GreaterThan(0);
    }
}

public class DarBajaUsuarioHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<DarBajaUsuarioCommand, UsuarioResponse>
{
    public async Task<UsuarioResponse> Handle(DarBajaUsuarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        await repositorio.DarBajaUsuarioAsync(command.IdUsuario, cancellationToken);

        return await consultas.ObtenerUsuarioAsync(command.IdUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuario", command.IdUsuario);
    }
}
