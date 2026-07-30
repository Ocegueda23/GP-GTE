using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Solicitudes;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Solicitudes.Queries;

public record ObtenerTriageQuery(FiltroTriage Filtro) : IRequest<PagedResult<SolicitudResponse>>;

public class ObtenerTriageHandler(ISolicitudQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerTriageQuery, PagedResult<SolicitudResponse>>
{
    public async Task<PagedResult<SolicitudResponse>> Handle(
        ObtenerTriageQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync("SOL.Triage", null, cancellationToken);
        return await consultas.ObtenerTriageAsync(query.Filtro, cancellationToken);
    }
}

public record ObtenerMisSolicitudesQuery : IRequest<IReadOnlyList<SolicitudResponse>>;

public class ObtenerMisSolicitudesHandler(
    ISolicitudQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerMisSolicitudesQuery, IReadOnlyList<SolicitudResponse>>
{
    public async Task<IReadOnlyList<SolicitudResponse>> Handle(
        ObtenerMisSolicitudesQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");
        return await consultas.ObtenerMiasAsync(usuario.IdUsuario, cancellationToken);
    }
}

public record ObtenerSolicitudQuery(int IdSolicitud) : IRequest<SolicitudResponse>;

public class ObtenerSolicitudHandler(ISolicitudQueryService consultas)
    : IRequestHandler<ObtenerSolicitudQuery, SolicitudResponse>
{
    public async Task<SolicitudResponse> Handle(
        ObtenerSolicitudQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorIdAsync(query.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", query.IdSolicitud);
    }
}

public record ObtenerAccionesSolicitudQuery(int IdSolicitud) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

/// <summary>Acciones del grafo filtradas por lo que el usuario puede ejecutar en triage.</summary>
public class ObtenerAccionesSolicitudHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    ISolicitudRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerAccionesSolicitudQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesSolicitudQuery query, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(query.IdSolicitud, cancellationToken)
            ?? throw new NotFoundException("Solicitud", query.IdSolicitud);

        var tieneTriage = await permisos.TienePermisoAsync(
            Domain.Solicitudes.PermisosSolicitud.Triage, null, cancellationToken);
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
        var esSolicitante = usuario is not null && usuario.IdUsuario == estado.IdSolicitante;

        var acciones = await motor.ObtenerAccionesAsync("Solicitud", query.IdSolicitud, cancellationToken);

        return acciones
            .Where(a => Domain.Solicitudes.AccionesSolicitud.DeTriage.Contains(a.Accion)
                ? tieneTriage
                : a.Accion != Domain.Solicitudes.AccionesSolicitud.Cancelar || esSolicitante || tieneTriage)
            .Select(a => new AccionDisponibleResponse
            {
                Accion = a.Accion,
                Etiqueta = a.EtiquetaBoton,
                RequiereMotivo = a.RequiereMotivo || Domain.Solicitudes.AccionesSolicitud.ConMotivo.Contains(a.Accion),
                EsAccionPrincipal = a.EsAccionPrincipal
            })
            .ToList();
    }
}
