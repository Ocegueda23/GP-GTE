using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.IndicadoresEjecutivos.Commands;

public record GuardarLayoutDashboardEjecutivoCommand(string LayoutJson) : IRequest;

/// <summary>El IdUsuario nunca viaja en el payload -- siempre sale de la identidad del token.</summary>
public class GuardarLayoutDashboardEjecutivoHandler(
    IIndicadoresEjecutivosRepository repositorio, IProveedorUsuarioActual proveedorUsuarioActual)
    : IRequestHandler<GuardarLayoutDashboardEjecutivoCommand>
{
    public async Task Handle(GuardarLayoutDashboardEjecutivoCommand command, CancellationToken cancellationToken)
    {
        var actual = await proveedorUsuarioActual.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("No hay una identidad de GTE asociada a la sesion.");

        await repositorio.GuardarLayoutAsync(actual.IdUsuario, command.LayoutJson, cancellationToken);
    }
}
