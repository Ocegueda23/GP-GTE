using GTE.Application.DTOs.Responses.IndicadoresEjecutivos;
using GTE.Application.Interfaces;
using GTE.Domain.Dashboard;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.IndicadoresEjecutivos.Queries;

/// <summary>
/// A diferencia del Dashboard de colaborador individual (sin bloqueo, alcance por datos),
/// el Dashboard Ejecutivo P18 expone costo/rentabilidad de proyecto y DORA de equipo --
/// no hay un "alcance personal" razonable aqui, asi que sin DASH.Ejecutivo ni
/// DASH.VerDepartamento se bloquea con 403.
/// </summary>
file static class AlcanceIndicadoresEjecutivos
{
    public static async Task<(int IdUsuarioActual, bool Global)> ResolverAsync(
        IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos, CancellationToken cancellationToken)
    {
        var actual = await proveedorUsuarioActual.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("No hay una identidad de GTE asociada a la sesion.");

        var global = await permisos.TienePermisoAsync(PermisosDashboard.VerEjecutivo, null, cancellationToken);
        if (!global && !await permisos.TienePermisoAsync(PermisosDashboard.VerDepartamento, null, cancellationToken))
        {
            throw new ForbiddenException("No tienes permiso para ver el Dashboard Ejecutivo.");
        }

        return (actual.IdUsuario, global);
    }
}

public record ObtenerIndicadoresEjecutivosQuery(
    int Anio, int Mes, int? IdEquipo, int? IdProyecto) : IRequest<IndicadoresEjecutivosResponse>;

public class ObtenerIndicadoresEjecutivosHandler(
    IIndicadoresEjecutivosQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerIndicadoresEjecutivosQuery, IndicadoresEjecutivosResponse>
{
    public async Task<IndicadoresEjecutivosResponse> Handle(ObtenerIndicadoresEjecutivosQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, global) = await AlcanceIndicadoresEjecutivos.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerAsync(
            idUsuarioActual, global, query.Anio, query.Mes, query.IdEquipo, query.IdProyecto, cancellationToken);
    }
}

public record ObtenerLayoutDashboardEjecutivoQuery : IRequest<string?>;

public class ObtenerLayoutDashboardEjecutivoHandler(
    IIndicadoresEjecutivosQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerLayoutDashboardEjecutivoQuery, string?>
{
    public async Task<string?> Handle(ObtenerLayoutDashboardEjecutivoQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, _) = await AlcanceIndicadoresEjecutivos.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerLayoutAsync(idUsuarioActual, cancellationToken);
    }
}
