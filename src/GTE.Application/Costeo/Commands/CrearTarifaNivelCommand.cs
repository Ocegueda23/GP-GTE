using FluentValidation;
using GTE.Application.DTOs.Request.Costeo;
using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Domain.Costeo;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Costeo.Commands;

public record CrearTarifaNivelCommand(TarifaNivelCrearRequest Datos) : IRequest<TarifaNivelResponse>;

public class CrearTarifaNivelValidator : AbstractValidator<CrearTarifaNivelCommand>
{
    public CrearTarifaNivelValidator()
    {
        RuleFor(c => c.Datos.IdNivel).GreaterThan(0).WithMessage("El nivel es obligatorio.");
        RuleFor(c => c.Datos.CostoHora).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Datos.VigenciaDesde).NotEqual(default(DateOnly)).WithMessage("La vigencia es obligatoria.");
    }
}

public class CrearTarifaNivelHandler(
    ICosteoRepository repositorio,
    ICosteoQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearTarifaNivelCommand, TarifaNivelResponse>
{
    public async Task<TarifaNivelResponse> Handle(CrearTarifaNivelCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCosteo.Gestionar, null, cancellationToken);

        var idTarifaNivel = await repositorio.CrearTarifaNivelAsync(new TarifaNivelNueva(
            command.Datos.IdNivel, command.Datos.CostoHora, command.Datos.VigenciaDesde), cancellationToken);

        return await consultas.ObtenerTarifaAsync(idTarifaNivel, cancellationToken)
            ?? throw new NotFoundException("TarifaNivel", idTarifaNivel);
    }
}
