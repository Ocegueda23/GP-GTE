using FluentValidation;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Entregas.Commands;

/// <summary>
/// Cadena de aprobacion de releases configurable por proyecto (Documento Maestro §7.2):
/// una lista vacia significa "usar el default fijo" (GTE.Domain.Entregas.
/// RolesAprobacion.Cadena, los seis firmantes del formato) -- ver CambiarEstatusReleaseHandler.
/// Gateado por ADM.Workflows (mismo permiso del editor de procesos/transiciones, ya
/// sembrado y sin mas consumidores hasta ahora): es configuracion de proceso, no de
/// releases en si.
/// </summary>
public record ObtenerCadenaAprobacionQuery(int IdProyecto) : IRequest<IReadOnlyList<string>>;

public record ConfigurarCadenaAprobacionCommand(int IdProyecto, IReadOnlyList<string> Roles) : IRequest<Unit>;

public class ConfigurarCadenaAprobacionValidator : AbstractValidator<ConfigurarCadenaAprobacionCommand>
{
    public ConfigurarCadenaAprobacionValidator()
    {
        RuleFor(c => c.IdProyecto).GreaterThan(0);
        RuleForEach(c => c.Roles).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Roles).Must(r => r.Distinct().Count() == r.Count)
            .WithMessage("No repitas el mismo rol dos veces en la cadena.");
    }
}

public class ObtenerCadenaAprobacionHandler(IEntregaRepository repositorio)
    : IRequestHandler<ObtenerCadenaAprobacionQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        ObtenerCadenaAprobacionQuery query, CancellationToken cancellationToken)
    {
        return await repositorio.ObtenerCadenaAprobacionConfiguradaAsync(query.IdProyecto, cancellationToken);
    }
}

public class ConfigurarCadenaAprobacionHandler(
    IEntregaRepository repositorio, IVerificadorPermisos permisos)
    : IRequestHandler<ConfigurarCadenaAprobacionCommand, Unit>
{
    public async Task<Unit> Handle(ConfigurarCadenaAprobacionCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Workflows, cancellationToken: cancellationToken);
        await repositorio.GuardarCadenaAprobacionConfiguradaAsync(
            command.IdProyecto, command.Roles, cancellationToken);
        return Unit.Value;
    }
}
