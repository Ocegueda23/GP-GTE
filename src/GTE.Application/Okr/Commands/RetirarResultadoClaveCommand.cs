using FluentValidation;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record RetirarResultadoClaveCommand(int IdObjetivoOkr, int IdResultadoClave) : IRequest<ObjetivoOkrResponse>;

public class RetirarResultadoClaveValidator : AbstractValidator<RetirarResultadoClaveCommand>
{
    public RetirarResultadoClaveValidator()
    {
        RuleFor(c => c.IdObjetivoOkr).GreaterThan(0);
        RuleFor(c => c.IdResultadoClave).GreaterThan(0);
    }
}

public class RetirarResultadoClaveHandler(
    IOkrRepository repositorio,
    IOkrQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<RetirarResultadoClaveCommand, ObjetivoOkrResponse>
{
    public async Task<ObjetivoOkrResponse> Handle(RetirarResultadoClaveCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);
        await repositorio.RetirarResultadoClaveAsync(command.IdResultadoClave, cancellationToken);

        return await consultas.ObtenerObjetivoAsync(command.IdObjetivoOkr, cancellationToken)
            ?? throw new NotFoundException("ObjetivoOkr", command.IdObjetivoOkr);
    }
}
