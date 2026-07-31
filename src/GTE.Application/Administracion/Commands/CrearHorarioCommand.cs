using FluentValidation;
using GTE.Application.DTOs.Request.Administracion;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CrearHorarioCommand(HorarioCrearRequest Datos) : IRequest<HorarioResponse>;

public class CrearHorarioValidator : AbstractValidator<CrearHorarioCommand>
{
    public CrearHorarioValidator()
    {
        RuleFor(c => c.Datos.Nombre).NotEmpty().WithMessage("El nombre del horario es obligatorio.").MaximumLength(100);
    }
}

public class CrearHorarioHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<CrearHorarioCommand, HorarioResponse>
{
    public async Task<HorarioResponse> Handle(CrearHorarioCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var idHorario = await repositorio.CrearHorarioAsync(new HorarioNuevo(command.Datos.Nombre.Trim()), cancellationToken);

        var horarios = await consultas.ObtenerHorariosAsync(cancellationToken);
        return horarios.FirstOrDefault(h => h.IdHorario == idHorario)
            ?? throw new NotFoundException("Horario", idHorario);
    }
}
