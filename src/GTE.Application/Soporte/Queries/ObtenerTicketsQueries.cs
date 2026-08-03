using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Soporte;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Soporte.Queries;

public record ObtenerBandejaTicketsQuery(FiltroBandejaTicket Filtro) : IRequest<PagedResult<TicketResponse>>;

public class ObtenerBandejaTicketsHandler(ITicketQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerBandejaTicketsQuery, PagedResult<TicketResponse>>
{
    public async Task<PagedResult<TicketResponse>> Handle(
        ObtenerBandejaTicketsQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosTicket.Atender, null, cancellationToken);
        return await consultas.ObtenerBandejaAsync(query.Filtro, cancellationToken);
    }
}

public record ObtenerMisTicketsQuery : IRequest<IReadOnlyList<TicketResponse>>;

public class ObtenerMisTicketsHandler(
    ITicketQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerMisTicketsQuery, IReadOnlyList<TicketResponse>>
{
    public async Task<IReadOnlyList<TicketResponse>> Handle(
        ObtenerMisTicketsQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");
        return await consultas.ObtenerMiosAsync(usuario.IdUsuario, cancellationToken);
    }
}

/// <summary>Detalle por folio (ruta /tickets/:folio de la SPA, mismo patron que WorkItem).</summary>
public record ObtenerTicketPorFolioQuery(string Folio) : IRequest<TicketResponse>;

public class ObtenerTicketPorFolioHandler(ITicketQueryService consultas)
    : IRequestHandler<ObtenerTicketPorFolioQuery, TicketResponse>
{
    public async Task<TicketResponse> Handle(ObtenerTicketPorFolioQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorFolioAsync(query.Folio, cancellationToken)
            ?? throw new NotFoundException("Ticket", query.Folio);
    }
}

public record ObtenerAccionesTicketQuery(int IdTicket) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

/// <summary>
/// Igual patron que ObtenerAccionesWorkItemQuery: el grafo (dbo.tblTransicion) dicta
/// las transiciones posibles y tblTransicionConfig el permiso por fila; el ESCALAR no
/// aparece aqui porque no es una transicion (ver EstatusTicket.cs).
/// </summary>
public class ObtenerAccionesTicketHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    ITicketRepository repositorio) : IRequestHandler<ObtenerAccionesTicketQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesTicketQuery query, CancellationToken cancellationToken)
    {
        _ = await repositorio.ObtenerEstadoAsync(query.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", query.IdTicket);

        var acciones = await motor.ObtenerAccionesAsync("Ticket", query.IdTicket, cancellationToken);

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
