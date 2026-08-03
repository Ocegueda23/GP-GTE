using FluentValidation;
using GTE.Application.DTOs.Request.Okr;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record ActualizarObjetivoOkrCommand(int IdObjetivoOkr, ObjetivoOkrEditarRequest Datos) : IRequest<ObjetivoOkrResponse>;

public class ActualizarObjetivoOkrValidator : AbstractValidator<ActualizarObjetivoOkrCommand>
{
    public ActualizarObjetivoOkrValidator()
    {
        RuleFor(c => c.IdObjetivoOkr).GreaterThan(0);
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del objetivo es obligatorio.").MaximumLength(200);
    }
}

public class ActualizarObjetivoOkrHandler(
    IOkrRepository repositorio,
    IOkrQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ActualizarObjetivoOkrCommand, ObjetivoOkrResponse>
{
    public async Task<ObjetivoOkrResponse> Handle(ActualizarObjetivoOkrCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);

        await repositorio.ActualizarObjetivoAsync(new ObjetivoOkrEdicion(
            command.IdObjetivoOkr, command.Datos.Nombre.Trim(), command.Datos.Descripcion), cancellationToken);

        return await consultas.ObtenerObjetivoAsync(command.IdObjetivoOkr, cancellationToken)
            ?? throw new NotFoundException("ObjetivoOkr", command.IdObjetivoOkr);
    }
}
