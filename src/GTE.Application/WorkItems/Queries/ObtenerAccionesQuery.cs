using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.WorkItems.Queries;

public record ObtenerAccionesWorkItemQuery(int IdWorkItem) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

/// <summary>
/// Acciones de workflow validas para el usuario actual: el grafo dicta las
/// transiciones y tblTransicionConfig los permisos; la UI solo pinta lo que llega.
/// </summary>
public class ObtenerAccionesWorkItemHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IWorkItemRepository repositorio) : IRequestHandler<ObtenerAccionesWorkItemQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesWorkItemQuery query, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(query.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", query.IdWorkItem);

        var acciones = await motor.ObtenerAccionesAsync("WorkItem", query.IdWorkItem, cancellationToken);

        var resultado = new List<AccionDisponibleResponse>();
        foreach (var accion in acciones)
        {
            if (accion.ClavePermisoRequerida is not null
                && !await permisos.TienePermisoAsync(accion.ClavePermisoRequerida, estado.IdProyecto, cancellationToken))
            {
                continue;
            }

            resultado.Add(new AccionDisponibleResponse
            {
                Accion = accion.Accion,
                Etiqueta = accion.EtiquetaBoton,
                RequiereMotivo = accion.RequiereMotivo,
                EsAccionPrincipal = accion.EsAccionPrincipal
            });
        }

        return resultado;
    }
}
