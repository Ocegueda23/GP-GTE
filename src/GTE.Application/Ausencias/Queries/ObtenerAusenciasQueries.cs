using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.DTOs.Responses.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Ausencias;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Ausencias.Queries;

/// <summary>Bandeja del aprobador: exige ADM.Ausencias porque ve las de todas las personas.</summary>
public record ObtenerBandejaAusenciasQuery(FiltroAusencias Filtro) : IRequest<PagedResult<AusenciaResponse>>;

public class ObtenerBandejaAusenciasHandler(
    IAusenciaQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ObtenerBandejaAusenciasQuery, PagedResult<AusenciaResponse>>
{
    public async Task<PagedResult<AusenciaResponse>> Handle(
        ObtenerBandejaAusenciasQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken);
        return await consultas.ObtenerBandejaAsync(query.Filtro, cancellationToken);
    }
}

/// <summary>Sin estatus = vigentes (Solicitada, Aprobada); [-1] = todas.</summary>
public record ObtenerMisAusenciasQuery(IReadOnlyList<int>? Estatus = null) : IRequest<IReadOnlyList<AusenciaResponse>>;

public class ObtenerMisAusenciasHandler(
    IAusenciaQueryService consultas,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerMisAusenciasQuery, IReadOnlyList<AusenciaResponse>>
{
    public async Task<IReadOnlyList<AusenciaResponse>> Handle(
        ObtenerMisAusenciasQuery query, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");
        return await consultas.ObtenerMiasAsync(usuario.IdUsuario, query.Estatus, cancellationToken);
    }
}

/// <summary>Detalle: la propia siempre; la ajena solo con ADM.Ausencias (lleva motivo personal).</summary>
public record ObtenerAusenciaQuery(int IdAusencia) : IRequest<AusenciaResponse>;

public class ObtenerAusenciaHandler(
    IAusenciaQueryService consultas,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerAusenciaQuery, AusenciaResponse>
{
    public async Task<AusenciaResponse> Handle(ObtenerAusenciaQuery query, CancellationToken cancellationToken)
    {
        var ausencia = await consultas.ObtenerPorIdAsync(query.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", query.IdAusencia);

        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
        if (usuario is null || usuario.IdUsuario != ausencia.IdUsuario)
        {
            await permisos.ExigirPermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken);
        }

        return ausencia;
    }
}

/// <summary>Acciones del grafo filtradas por lo que este usuario puede ejecutar.</summary>
public record ObtenerAccionesAusenciaQuery(int IdAusencia) : IRequest<IReadOnlyList<AccionDisponibleResponse>>;

public class ObtenerAccionesAusenciaHandler(
    IMotorWorkflow motor,
    IVerificadorPermisos permisos,
    IAusenciaRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<ObtenerAccionesAusenciaQuery, IReadOnlyList<AccionDisponibleResponse>>
{
    public async Task<IReadOnlyList<AccionDisponibleResponse>> Handle(
        ObtenerAccionesAusenciaQuery query, CancellationToken cancellationToken)
    {
        var estado = await repositorio.ObtenerEstadoAsync(query.IdAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", query.IdAusencia);

        var puedeGestionar = await permisos.TienePermisoAsync(
            PermisosAusencia.Gestionar, null, cancellationToken);
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken);
        var esDueno = usuario is not null && usuario.IdUsuario == estado.IdUsuario;

        var acciones = await motor.ObtenerAccionesAsync("Ausencia", query.IdAusencia, cancellationToken);

        return acciones
            .Where(a => AccionesAusencia.DeAprobador.Contains(a.Accion)
                ? puedeGestionar
                : a.Accion != AccionesAusencia.Cancelar || esDueno || puedeGestionar)
            .Select(a => new AccionDisponibleResponse
            {
                Accion = a.Accion,
                Etiqueta = a.EtiquetaBoton,
                RequiereMotivo = a.RequiereMotivo || AccionesAusencia.ConMotivo.Contains(a.Accion),
                EsAccionPrincipal = a.EsAccionPrincipal
            })
            .ToList();
    }
}

/// <summary>Tipos y estatus para los dropdowns de la pantalla.</summary>
public record ObtenerCatalogosAusenciaQuery : IRequest<CatalogosAusenciaResponse>;

public class ObtenerCatalogosAusenciaHandler(IAusenciaQueryService consultas)
    : IRequestHandler<ObtenerCatalogosAusenciaQuery, CatalogosAusenciaResponse>
{
    public async Task<CatalogosAusenciaResponse> Handle(
        ObtenerCatalogosAusenciaQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerCatalogosAsync(cancellationToken);
    }
}

/// <summary>Badge de pendientes por aprobar; 0 si el usuario no gestiona ausencias.</summary>
public record ContarAusenciasPendientesQuery : IRequest<int>;

public class ContarAusenciasPendientesHandler(
    IAusenciaQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<ContarAusenciasPendientesQuery, int>
{
    public async Task<int> Handle(ContarAusenciasPendientesQuery query, CancellationToken cancellationToken)
    {
        if (!await permisos.TienePermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken))
        {
            return 0;
        }
        return await consultas.ContarPendientesAsync(cancellationToken);
    }
}
