using FluentValidation;
using GTE.Application.DTOs.Request.Okr;
using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Commands;

public record CrearObjetivoOkrCommand(ObjetivoOkrCrearRequest Datos) : IRequest<ObjetivoOkrResponse>;

public class CrearObjetivoOkrValidator : AbstractValidator<CrearObjetivoOkrCommand>
{
    public CrearObjetivoOkrValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del objetivo es obligatorio.").MaximumLength(200);
        RuleFor(c => c.Datos.Anio).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Datos.Trimestre).InclusiveBetween((byte)1, (byte)4);
        RuleFor(c => c).Must(c => c.Datos.IdProyecto.HasValue || c.Datos.IdEquipo.HasValue)
            .WithMessage("El objetivo requiere un proyecto o un equipo.");
    }
}

public class CrearObjetivoOkrHandler(
    IOkrRepository repositorio,
    IOkrQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearObjetivoOkrCommand, ObjetivoOkrResponse>
{
    public async Task<ObjetivoOkrResponse> Handle(CrearObjetivoOkrCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);

        var idObjetivo = await repositorio.CrearObjetivoAsync(new ObjetivoOkrNuevo(
            command.Datos.IdProyecto, command.Datos.IdEquipo, command.Datos.Nombre.Trim(),
            command.Datos.Descripcion, command.Datos.Anio, command.Datos.Trimestre), cancellationToken);

        return await consultas.ObtenerObjetivoAsync(idObjetivo, cancellationToken)
            ?? throw new NotFoundException("ObjetivoOkr", idObjetivo);
    }
}
