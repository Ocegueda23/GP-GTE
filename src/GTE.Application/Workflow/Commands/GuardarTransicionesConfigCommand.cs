using FluentValidation;
using GTE.Application.DTOs.Request.Workflow;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using GTE.Domain.Workflow;
using MediatR;

namespace GTE.Application.Workflow.Commands;

public record GuardarTransicionesConfigCommand(string Proceso, IReadOnlyList<TransicionConfigRequest> Transiciones)
    : IRequest<Unit>;

public class GuardarTransicionesConfigValidator : AbstractValidator<GuardarTransicionesConfigCommand>
{
    public GuardarTransicionesConfigValidator()
    {
        RuleFor(c => c.Proceso).NotEmpty();
        RuleForEach(c => c.Transiciones).ChildRules(t =>
        {
            t.RuleFor(x => x.Accion).NotEmpty().MaximumLength(50);
            t.RuleFor(x => x.EtiquetaBoton).NotEmpty().WithMessage("La etiqueta del boton es obligatoria.").MaximumLength(100);
        });
    }
}

/// <summary>
/// Guarda solo los METADATOS de UI de transiciones que ya existen en el grafo
/// (dbo.tblTransicionConfig); nunca crea ni elimina flechas de dbo.tblTransicion.
/// </summary>
public class GuardarTransicionesConfigHandler(
    IWorkflowRepository repositorio, IVerificadorPermisos permisos)
    : IRequestHandler<GuardarTransicionesConfigCommand, Unit>
{
    public async Task<Unit> Handle(GuardarTransicionesConfigCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Workflows, null, cancellationToken);

        var transiciones = command.Transiciones.Select(t => new TransicionConfigEdicion(
            t.IdEstatusOrigen, t.Accion, t.EtiquetaBoton.Trim(), t.RequierePermiso,
            t.RequiereMotivo, t.EsAccionPrincipal, t.Orden)).ToList();

        await repositorio.GuardarTransicionesConfigAsync(command.Proceso, transiciones, cancellationToken);
        return Unit.Value;
    }
}
