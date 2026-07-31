using FluentValidation;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Application.WorkItems.Commands;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Planeacion;
using MediatR;

namespace GTE.Application.Planeacion.Commands;

public record MoverTarjetaCommand(int IdWorkItem, int IdEstatusDestino) : IRequest<EstatusCambiadoResponse>;

public class MoverTarjetaValidator : AbstractValidator<MoverTarjetaCommand>
{
    public MoverTarjetaValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.IdEstatusDestino).GreaterThan(0);
    }
}

/// <summary>
/// Movimiento de tarjeta en el tablero. El tablero es una vista por estatus, asi que
/// soltar una tarjeta en otra columna se TRADUCE a la accion del grafo que lleva a ese
/// estatus: el front sigue sin decidir transiciones y todas las reglas de negocio del
/// cambio de estatus (RN-REQ-01, 02, 03) se aplican igual que en el detalle.
/// RN-PLA-04: si la columna destino tiene limite WIP alcanzado, se bloquea salvo permiso.
/// </summary>
public class MoverTarjetaHandler(
    IMotorWorkflow motor,
    IPlaneacionRepository repositorio,
    IWorkItemRepository workItems,
    IVerificadorPermisos permisos,
    ISender mediator) : IRequestHandler<MoverTarjetaCommand, EstatusCambiadoResponse>
{
    public async Task<EstatusCambiadoResponse> Handle(
        MoverTarjetaCommand command, CancellationToken cancellationToken)
    {
        var item = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (item.IdEstatus == command.IdEstatusDestino)
        {
            throw new BusinessException("El elemento ya esta en esa columna.");
        }

        // La accion la resuelve el grafo, no el front
        var acciones = await motor.ObtenerAccionesAsync("WorkItem", command.IdWorkItem, cancellationToken);
        var destinos = await motor.ObtenerDestinosAsync("WorkItem", item.IdEstatus, cancellationToken);

        var accion = destinos
            .Where(d => d.IdEstatusDestino == command.IdEstatusDestino)
            .Select(d => acciones.FirstOrDefault(a => a.Accion == d.Accion))
            .FirstOrDefault(a => a is not null)
            ?? throw new BusinessException(
                "Ese movimiento no esta permitido desde la columna actual.");

        if (accion.RequiereMotivo)
        {
            throw new BusinessException(
                $"'{accion.EtiquetaBoton}' requiere capturar un motivo: hazlo desde el detalle del elemento.");
        }

        await ValidarLimiteWipAsync(
            command.IdWorkItem, item.IdProyecto, command.IdEstatusDestino, cancellationToken);


        return await mediator.Send(
            new CambiarEstatusWorkItemCommand(command.IdWorkItem, accion.Accion, null), cancellationToken);
    }

    /// <summary>RN-PLA-04: el limite de trabajo en curso se respeta salvo permiso explicito.</summary>
    private async Task ValidarLimiteWipAsync(
        int idWorkItem, int idProyecto, int idEstatusDestino, CancellationToken cancellationToken)
    {
        // El tablero (y por lo tanto el limite WIP) es del equipo responsable del proyecto
        var idEquipo = await repositorio.ObtenerEquipoDeProyectoAsync(idProyecto, cancellationToken);
        if (idEquipo is null)
        {
            return;   // proyecto sin equipo: no hay tablero al que aplicar limites
        }

        var columnas = await repositorio.ObtenerOCrearColumnasAsync(idEquipo.Value, cancellationToken);
        var columna = columnas.FirstOrDefault(c => c.IdEstatusWorkItem == idEstatusDestino);
        if (columna?.LimiteWip is null)
        {
            return;
        }

        var enColumna = await repositorio.ContarItemsEnEstatusAsync(
            idEquipo.Value, idEstatusDestino, cancellationToken);
        if (enColumna < columna.LimiteWip.Value)
        {
            return;
        }

        if (!await permisos.TienePermisoAsync(PermisosPlaneacion.SaltarWip, idProyecto, cancellationToken))
        {
            throw new ConflictException(
                $"La columna {columna.Nombre} alcanzo su limite de {columna.LimiteWip} elementos en curso. "
                + "Termina algo antes de meter mas trabajo.",
                new { columna = columna.Nombre, limite = columna.LimiteWip, enColumna });
        }

        // Con permiso se puede exceder, pero queda registrado
        await repositorio.RegistrarSaltoWipAsync(
            idWorkItem, columna.Nombre, columna.LimiteWip.Value, enColumna, cancellationToken);
    }
}
