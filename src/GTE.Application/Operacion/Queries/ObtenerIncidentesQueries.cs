using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Queries;

public record ObtenerBandejaIncidentesQuery(FiltroBandejaIncidente Filtro) : IRequest<PagedResult<IncidenteResponse>>;

public class ObtenerBandejaIncidentesHandler(IIncidenteQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerBandejaIncidentesQuery, PagedResult<IncidenteResponse>>
{
    public async Task<PagedResult<IncidenteResponse>> Handle(
        ObtenerBandejaIncidentesQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);
        return await consultas.ObtenerBandejaAsync(query.Filtro, cancellationToken);
    }
}

/// <summary>Detalle por folio (ruta /operacion/incidentes/:folio de la SPA).</summary>
public record ObtenerIncidentePorFolioQuery(string Folio) : IRequest<IncidenteResponse>;

public class ObtenerIncidentePorFolioHandler(IIncidenteQueryService consultas)
    : IRequestHandler<ObtenerIncidentePorFolioQuery, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(ObtenerIncidentePorFolioQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorFolioAsync(query.Folio, cancellationToken)
            ?? throw new NotFoundException("Incidente", query.Folio);
    }
}

public record ObtenerAccionesIncidenteQuery(int IdIncidente) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

/// <summary>Mismo patron generico que ObtenerAccionesTicketQuery/ObtenerAccionesWorkItemQuery.</summary>
public class ObtenerAccionesIncidenteHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IIncidenteRepository repositorio) : IRequestHandler<ObtenerAccionesIncidenteQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesIncidenteQuery query, CancellationToken cancellationToken)
    {
        _ = await repositorio.ObtenerEstadoAsync(query.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", query.IdIncidente);

        var acciones = await motor.ObtenerAccionesAsync("Incidente", query.IdIncidente, cancellationToken);

        var resultado = new List<AccionDisponibleResponse>();
        foreach (var accion in acciones)
        {
            if (accion.ClavePermisoRequerida is not null
                && !await permisos.TienePermisoAsync(accion.ClavePermisoRequerida, null, cancellationToken))
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
