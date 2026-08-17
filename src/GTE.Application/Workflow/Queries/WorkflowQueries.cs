using GTE.Application.DTOs.Responses.Workflow;
using GTE.Application.Interfaces;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Workflow.Queries;

public record ObtenerProcesosWorkflowQuery : IRequest<IReadOnlyList<ProcesoWorkflowResponse>>;

public class ObtenerProcesosWorkflowHandler(
    IWorkflowQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerProcesosWorkflowQuery, IReadOnlyList<ProcesoWorkflowResponse>>
{
    public async Task<IReadOnlyList<ProcesoWorkflowResponse>> Handle(
        ObtenerProcesosWorkflowQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Workflows, null, cancellationToken);
        var procesos = await consultas.ObtenerProcesosAsync(cancellationToken);
        return procesos.Select(p => new ProcesoWorkflowResponse { IdProceso = p.IdProceso, Proceso = p.Proceso })
            .ToList();
    }
}

public record ObtenerDefinicionWorkflowQuery(string Proceso) : IRequest<DefinicionWorkflowResponse>;

public class ObtenerDefinicionWorkflowHandler(
    IWorkflowQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerDefinicionWorkflowQuery, DefinicionWorkflowResponse>
{
    public async Task<DefinicionWorkflowResponse> Handle(
        ObtenerDefinicionWorkflowQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Workflows, null, cancellationToken);

        var definicion = await consultas.ObtenerDefinicionAsync(query.Proceso, cancellationToken)
            ?? throw new NotFoundException("Proceso", query.Proceso);

        return new DefinicionWorkflowResponse
        {
            Proceso = definicion.Proceso,
            Estatus = definicion.Estatus
                .Select(e => new EstatusWorkflowResponse { Id = e.Id, Descripcion = e.Descripcion })
                .ToList(),
            Transiciones = definicion.Transiciones
                .Select(t => new TransicionWorkflowResponse
                {
                    IdEstatusOrigen = t.IdEstatusOrigen,
                    EstatusOrigen = t.EstatusOrigen,
                    Accion = t.Accion,
                    IdEstatusDestino = t.IdEstatusDestino,
                    EstatusDestino = t.EstatusDestino,
                    EtiquetaBoton = t.EtiquetaBoton,
                    RequierePermiso = t.RequierePermiso,
                    RequiereMotivo = t.RequiereMotivo,
                    EsAccionPrincipal = t.EsAccionPrincipal,
                    Orden = t.Orden
                })
                .ToList()
        };
    }
}

public record ObtenerPermisosWorkflowQuery : IRequest<IReadOnlyList<PermisoWorkflowResponse>>;

public class ObtenerPermisosWorkflowHandler(
    IWorkflowQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerPermisosWorkflowQuery, IReadOnlyList<PermisoWorkflowResponse>>
{
    public async Task<IReadOnlyList<PermisoWorkflowResponse>> Handle(
        ObtenerPermisosWorkflowQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAdministracion.Workflows, null, cancellationToken);
        var permisosDisponibles = await consultas.ObtenerPermisosAsync(cancellationToken);
        return permisosDisponibles
            .Select(p => new PermisoWorkflowResponse { Clave = p.Clave, Modulo = p.Modulo, Descripcion = p.Descripcion })
            .ToList();
    }
}
