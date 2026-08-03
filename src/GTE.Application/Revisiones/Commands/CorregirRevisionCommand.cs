using FluentValidation;
using GTE.Application.DTOs.Request.Revisiones;
using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Revisiones;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.Revisiones.Commands;

public record CorregirRevisionCommand(int IdRevision, RevisionCorregirRequest Datos) : IRequest<RevisionResponse>;

public class CorregirRevisionValidator : AbstractValidator<CorregirRevisionCommand>
{
    public CorregirRevisionValidator()
    {
        RuleFor(c => c.IdRevision).GreaterThan(0);
        RuleFor(c => c.Datos.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Marca un hallazgo como corregido o lo reabre.
/// RN-QA-02: reabrir un hallazgo ya corregido exige el permiso REV.Reabrir
/// (regla heredada del GT: solo un lider puede reabrir) y motivo capturado.
/// RN-REQ-05 (2026-08-02): marcar CORREGIDO un hallazgo de un WorkItem ajeno exige
/// WI.ModificarAjeno -- quien corrige deberia ser quien hizo el arreglo (el asignado
/// del WorkItem), no un tercero cualquiera. Se detecto la misma clase de hueco que
/// RegistrarTiempoCommand: este comando no tenia NINGUN gate en el camino de
/// "corregido", reproducido en vivo antes de codear.
/// </summary>
public class CorregirRevisionHandler(
    IRevisionRepository repositorio,
    IRevisionQueryService consultas,
    IWorkItemRepository workItems,
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<CorregirRevisionCommand, RevisionResponse>
{
    public async Task<RevisionResponse> Handle(CorregirRevisionCommand command, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(command.IdRevision, cancellationToken)
            ?? throw new NotFoundException("Revision", command.IdRevision);

        if (estado.Corregido == command.Datos.Corregido)
        {
            throw new BusinessException(command.Datos.Corregido
                ? "El hallazgo ya estaba marcado como corregido."
                : "El hallazgo ya estaba abierto.");
        }

        var estadoItem = await workItems.ObtenerEstadoAsync(estado.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", estado.IdWorkItem);

        if (command.Datos.Corregido)
        {
            var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken);
            var esAjeno = estadoItem.IdAsignado != usuarioActual?.IdUsuario;
            if (esAjeno)
            {
                await permisos.ExigirPermisoAsync(
                    PermisosWorkItem.ModificarAjeno, estadoItem.IdProyecto, cancellationToken);
            }
        }
        else
        {
            // RN-QA-02: reabrir es facultad del lider y siempre con motivo
            await permisos.ExigirPermisoAsync(PermisosRevision.Reabrir, estadoItem.IdProyecto, cancellationToken);
            if (string.IsNullOrWhiteSpace(command.Datos.Motivo))
            {
                throw new BusinessException("Reabrir un hallazgo requiere explicar por que no quedo resuelto.");
            }
        }

        await repositorio.EstablecerCorregidoAsync(command.IdRevision, command.Datos.Corregido, cancellationToken);

        // El hallazgo sigue su propio ciclo de vida en el motor.
        // TERMINAR solo procede desde En Proceso: si sigue Pendiente se avanza
        // primero con INICIAR en vez de forzar una transicion invalida.
        foreach (var accion in ResolverSecuencia(estado.IdEstatus, command.Datos.Corregido))
        {
            await motor.EjecutarAccionAsync(
                "Revision", command.IdRevision, accion, command.Datos.Motivo, null, cancellationToken);
            await repositorio.AplicarEfectosTransicionAsync(command.IdRevision, accion, cancellationToken);
        }

        return await consultas.ObtenerPorIdAsync(command.IdRevision, cancellationToken)
            ?? throw new NotFoundException("Revision", command.IdRevision);
    }

    private static IEnumerable<string> ResolverSecuencia(int idEstatusActual, bool corregido)
    {
        if (corregido)
        {
            if (idEstatusActual == EstatusRevision.Pendiente)
            {
                yield return AccionesRevision.Iniciar;
                yield return AccionesRevision.Terminar;
            }
            else if (idEstatusActual == EstatusRevision.EnProceso)
            {
                yield return AccionesRevision.Terminar;
            }
        }
        else if (idEstatusActual == EstatusRevision.Terminada)
        {
            yield return AccionesRevision.Reabrir;
        }
    }
}
