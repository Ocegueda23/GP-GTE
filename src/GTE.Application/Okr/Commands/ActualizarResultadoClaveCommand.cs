using FluentValidation;
using GTE.Application.DTOs.Request.Okr;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record ActualizarResultadoClaveCommand(int IdObjetivoOkr, int IdResultadoClave, ResultadoClaveEditarRequest Datos)
    : IRequest<ObjetivoOkrResponse>;

public class ActualizarResultadoClaveValidator : AbstractValidator<ActualizarResultadoClaveCommand>
{
    public ActualizarResultadoClaveValidator()
    {
        RuleFor(c => c.IdObjetivoOkr).GreaterThan(0);
        RuleFor(c => c.IdResultadoClave).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del resultado clave es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.ValorMeta).GreaterThan(0).WithMessage("La meta debe ser mayor a cero.");
        RuleFor(c => c.Datos.ValorActual).GreaterThanOrEqualTo(0);
    }
}

public class ActualizarResultadoClaveHandler(
    IOkrRepository repositorio,
    IOkrQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarResultadoClaveCommand, ObjetivoOkrResponse>
{
    public async Task<ObjetivoOkrResponse> Handle(ActualizarResultadoClaveCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);

        await repositorio.ActualizarResultadoClaveAsync(new ResultadoClaveEdicion(
            command.IdResultadoClave, command.Datos.Nombre.Trim(), command.Datos.ValorMeta,
            command.Datos.ValorActual, command.Datos.ClaveKpi), cancellationToken);

        return await consultas.ObtenerObjetivoAsync(command.IdObjetivoOkr, cancellationToken)
            ?? throw new NotFoundException("ObjetivoOkr", command.IdObjetivoOkr);
    }
}
