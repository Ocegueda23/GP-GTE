using System.Text.RegularExpressions;
using FluentValidation;
using GTE.Application.DTOs.Request.Revisiones;
using GTE.Application.DTOs.Responses.Revisiones;
using GTE.Application.Interfaces;
using GTE.Domain.Calidad;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Revisiones;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.Revisiones.Commands;

public record CrearRevisionCommand(int IdWorkItem, RevisionCrearRequest Datos) : IRequest<RevisionResponse>;

public class CrearRevisionValidator : AbstractValidator<CrearRevisionCommand>
{
    public CrearRevisionValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Comentarios).NotEmpty()
            .WithMessage("Describe el hallazgo para que quien corrija sepa que ajustar.");
        RuleFor(c => c.Datos.IdSeveridad).InclusiveBetween(1, 4)
            .WithMessage("Selecciona la severidad del hallazgo.");
    }
}

/// <summary>
/// Reporta un hallazgo de revision (QA o code review).
/// RN-QA-03: si el elemento ya estaba Terminado, un hallazgo S1/S2 lo reabre a Correccion
/// a traves del motor (transicion Terminado-Correccion por RECHAZAR_QA); uno S3/S4 queda
/// registrado sin reabrir nada -- la severidad decide que bloquea y que no.
/// </summary>
public class CrearRevisionHandler(
    IRevisionRepository repositorio,
    IRevisionQueryService consultas,
    IWorkItemRepository workItems,
    IMotorWorkflow motor,
    IProveedorUsuarioActual proveedorUsuario,
    ISanitizadorHtml sanitizador) : IRequestHandler<CrearRevisionCommand, RevisionResponse>
{
    public async Task<RevisionResponse> Handle(CrearRevisionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var estadoItem = await workItems.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (estadoItem.IdEstatus == EstatusWorkItem.Cancelado || !estadoItem.Activo)
        {
            throw new BusinessException("No se pueden reportar hallazgos en un elemento cancelado o eliminado.");
        }

        var comentarios = sanitizador.Sanitizar(command.Datos.Comentarios);
        var textoSinEtiquetas = Regex.Replace(comentarios, "<[^>]*>", string.Empty).Trim();
        if (textoSinEtiquetas.Length == 0 && !comentarios.Contains("<img", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("El hallazgo quedo vacio despues de limpiar el formato.");
        }

        var idRevision = await repositorio.CrearAsync(
            new RevisionNueva(
                command.IdWorkItem, usuario.IdUsuario, comentarios,
                command.Datos.IdSeveridad, command.Datos.IdEjecucionPrueba),
            cancellationToken);

        // RN-QA-03: solo un hallazgo bloqueante (S1/S2) sobre trabajo ya cerrado lo regresa a Correccion
        var esBloqueante = command.Datos.IdSeveridad <= Severidad.S2Alta;
        if (esBloqueante && estadoItem.IdEstatus == EstatusWorkItem.Terminado)
        {
            await motor.EjecutarAccionAsync(
                "WorkItem", command.IdWorkItem, AccionesWorkItem.RechazarQa,
                $"Hallazgo de revision reportado por {usuario.Nombre}", null, cancellationToken);
            await workItems.AplicarEfectosTransicionAsync(
                command.IdWorkItem, AccionesWorkItem.RechazarQa, cancellationToken);
        }

        return await consultas.ObtenerPorIdAsync(idRevision, cancellationToken)
            ?? throw new NotFoundException("Revision", idRevision);
    }
}
