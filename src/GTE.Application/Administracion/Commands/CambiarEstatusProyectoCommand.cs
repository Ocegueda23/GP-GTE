using FluentValidation;
using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Administracion.Commands;

public record CambiarEstatusProyectoCommand(int IdProyecto, string Accion) : IRequest<ProyectoResponse>;

public class CambiarEstatusProyectoValidator : AbstractValidator<CambiarEstatusProyectoCommand>
{
    public CambiarEstatusProyectoValidator()
    {
        RuleFor(c => c.IdProyecto).GreaterThan(0);
        RuleFor(c => c.Accion).NotEmpty().MaximumLength(50);
    }
}

/// <summary>
/// AUTORIZAR/INICIAR/PAUSAR/REANUDAR/CERRAR/CANCELAR del proyecto, via el motor de
/// workflow (dbo.spCambiarEstatus, proceso "Proyecto" ya sembrado en el script 09).
/// RN-PRY-01: no se puede CERRAR con WorkItems abiertos (409 con el detalle de folios).
/// El folio del proyecto (serie PRY-anio) se genera al confirmar AUTORIZAR, nunca antes
/// de que el motor valide la transicion (para no quemar un folio en un intento invalido).
/// </summary>
public class CambiarEstatusProyectoHandler(
    IAdministracionRepository repositorio,
    IAdministracionQueryService consultas,
    IMotorWorkflow motor,
    IGeneradorFolios folios,
    IVerificadorPermisos permisos) : IRequestHandler<CambiarEstatusProyectoCommand, ProyectoResponse>
{
    public async Task<ProyectoResponse> Handle(CambiarEstatusProyectoCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Usuarios, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoProyectoAsync(command.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.IdProyecto);

        if (command.Accion == AccionesProyecto.Cerrar)
        {
            var abiertos = await repositorio.ObtenerFoliosWorkItemsAbiertosAsync(command.IdProyecto, cancellationToken);
            if (abiertos.Count > 0)
            {
                throw new ConflictException(
                    $"El proyecto tiene {abiertos.Count} elemento(s) sin terminar; no se puede cerrar.",
                    new { folios = abiertos });
            }
        }

        await motor.EjecutarAccionAsync("Proyecto", command.IdProyecto, command.Accion, null, null, cancellationToken);
        await repositorio.AplicarEfectosTransicionProyectoAsync(command.IdProyecto, command.Accion, cancellationToken);

        if (command.Accion == AccionesProyecto.Autorizar && estado.Folio is null)
        {
            var folio = await folios.GenerarAsync($"PRY-{DateTime.Today.Year}", cancellationToken: cancellationToken);
            await repositorio.AsignarFolioProyectoAsync(command.IdProyecto, folio, cancellationToken);
        }

        return await consultas.ObtenerProyectoAsync(command.IdProyecto, cancellationToken)
            ?? throw new NotFoundException("Proyecto", command.IdProyecto);
    }
}
