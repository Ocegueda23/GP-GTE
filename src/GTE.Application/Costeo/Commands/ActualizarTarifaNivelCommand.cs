using FluentValidation;
using GTE.Application.DTOs.Request.Costeo;
using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record ActualizarTarifaNivelCommand(int IdTarifaNivel, TarifaNivelEditarRequest Datos) : IRequest<TarifaNivelResponse>;

public class ActualizarTarifaNivelValidator : AbstractValidator<ActualizarTarifaNivelCommand>
{
    public ActualizarTarifaNivelValidator()
    {
        RuleFor(c => c.IdTarifaNivel).GreaterThan(0);
        RuleFor(c => c.Datos.CostoHora).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Datos.VigenciaDesde).NotEqual(default(DateOnly)).WithMessage("La vigencia es obligatoria.");
    }
}

public class ActualizarTarifaNivelHandler(
    ICosteoRepository repositorio,
    ICosteoQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarTarifaNivelCommand, TarifaNivelResponse>
{
    public async Task<TarifaNivelResponse> Handle(ActualizarTarifaNivelCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);

        await repositorio.ActualizarTarifaNivelAsync(new TarifaNivelEdicion(
            command.IdTarifaNivel, command.Datos.CostoHora, command.Datos.VigenciaDesde), cancellationToken);

        return await consultas.ObtenerTarifaAsync(command.IdTarifaNivel, cancellationToken)
            ?? throw new NotFoundException("TarifaNivel", command.IdTarifaNivel);
    }
}
