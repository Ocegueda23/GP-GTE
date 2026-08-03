using FluentValidation;
using GTE.Application.DTOs.Request.Okr;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record CrearResultadoClaveCommand(int IdObjetivoOkr, ResultadoClaveCrearRequest Datos) : IRequest<ObjetivoOkrResponse>;

public class CrearResultadoClaveValidator : AbstractValidator<CrearResultadoClaveCommand>
{
    public CrearResultadoClaveValidator()
    {
        RuleFor(c => c.IdObjetivoOkr).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del resultado clave es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.ValorMeta).GreaterThan(0).WithMessage("La meta debe ser mayor a cero.");
    }
}

/// <summary>Devuelve el objetivo completo (con todos sus resultados clave) para que el front rehidrate sin re-consultar aparte.</summary>
public class CrearResultadoClaveHandler(
    IOkrRepository repositorio,
    IOkrQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearResultadoClaveCommand, ObjetivoOkrResponse>
{
    public async Task<ObjetivoOkrResponse> Handle(CrearResultadoClaveCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);

        await repositorio.CrearResultadoClaveAsync(new ResultadoClaveNuevo(
            command.IdObjetivoOkr, command.Datos.Nombre.Trim(), command.Datos.ValorMeta,
            command.Datos.ClaveKpi), cancellationToken);

        return await consultas.ObtenerObjetivoAsync(command.IdObjetivoOkr, cancellationToken)
            ?? throw new NotFoundException("ObjetivoOkr", command.IdObjetivoOkr);
    }
}
