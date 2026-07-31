using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearFestivoCommand(DiaFestivoCrearRequest Datos) : IRequest<DiaFestivoResponse>;

public class CrearFestivoValidator : AbstractValidator<CrearFestivoCommand>
{
    public CrearFestivoValidator()
    {
        RuleFor(c => c.Datos.Descripcion).NotEmpty().WithMessage("La descripcion del festivo es obligatoria.").MaximumLength(200);
    }
}

public class CrearFestivoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearFestivoCommand, DiaFestivoResponse>
{
    public async Task<DiaFestivoResponse> Handle(CrearFestivoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idFestivo = await repositorio.CrearFestivoAsync(new DiaFestivoNuevo(
            command.Datos.Fecha, command.Datos.Descripcion.Trim(), command.Datos.IdHorario), cancellationToken);

        return await consultas.ObtenerFestivoAsync(idFestivo, cancellationToken)
            ?? throw new NotFoundException("DiaFestivo", idFestivo);
    }
}
