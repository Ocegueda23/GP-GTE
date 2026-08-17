using GTE.Application.DTOs.Responses.Dashboard;
using GTE.Application.Interfaces;
using GTE.Domain.Dashboard;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Dashboard.Queries;

/// <summary>
/// Resuelve, a partir del usuario del token, los dos flags de alcance que necesita
/// <see cref="IDashboardQueryService"/>: DASH.Ejecutivo (global) y DASH.VerDepartamento.
/// Sin ninguno de los dos el QueryService igual responde -- resuelve el alcance por datos
/// (equipo liderado / jefe directo, o solo el propio usuario) -- por eso este guard nunca
/// lanza ForbiddenException para las consultas de alcance propio.
/// </summary>
file static class AlcanceDashboard
{
    public static async Task<(int IdUsuarioActual, bool Global, bool Departamento)> ResolverAsync(
        IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos, CancellationToken cancellationToken)
    {
        var actual = await proveedorUsuarioActual.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("No hay una identidad de GTE asociada a la sesion.");

        var global = await permisos.TienePermisoAsync(PermisosDashboard.VerEjecutivo, null, cancellationToken);
        var departamento = !global
            && await permisos.TienePermisoAsync(PermisosDashboard.VerDepartamento, null, cancellationToken);

        return (actual.IdUsuario, global, departamento);
    }
}

public record ObtenerDashboardQuery(
    int Anio, int Mes, int? IdProyecto, int? IdArea, int? IdUsuarioFoco) : IRequest<DashboardResponse>;

public class ObtenerDashboardHandler(
    IDashboardQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerDashboardQuery, DashboardResponse>
{
    public async Task<DashboardResponse> Handle(ObtenerDashboardQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, global, departamento) = await AlcanceDashboard.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerDashboardAsync(
            idUsuarioActual, global, departamento,
            query.Anio, query.Mes, query.IdProyecto, query.IdArea, query.IdUsuarioFoco,
            cancellationToken);
    }
}

public record ObtenerIndicadoresEmpleadoQuery(int IdUsuario, int Anio, int Mes) : IRequest<IndicadoresEmpleadoResponse>;

public class ObtenerIndicadoresEmpleadoHandler(
    IDashboardQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerIndicadoresEmpleadoQuery, IndicadoresEmpleadoResponse>
{
    public async Task<IndicadoresEmpleadoResponse> Handle(ObtenerIndicadoresEmpleadoQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, global, departamento) = await AlcanceDashboard.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerIndicadoresEmpleadoAsync(
            idUsuarioActual, global, departamento, query.IdUsuario, query.Anio, query.Mes, cancellationToken)
            ?? throw new ForbiddenException("No tienes permiso para ver los indicadores de este colaborador.");
    }
}

public record ObtenerTendenciasDashboardQuery(
    int Anio, int? IdProyecto, int? IdArea) : IRequest<IReadOnlyList<TendenciaResponse>>;

public class ObtenerTendenciasDashboardHandler(
    IDashboardQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerTendenciasDashboardQuery, IReadOnlyList<TendenciaResponse>>
{
    public async Task<IReadOnlyList<TendenciaResponse>> Handle(ObtenerTendenciasDashboardQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, global, departamento) = await AlcanceDashboard.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerTendenciasAsync(
            idUsuarioActual, global, departamento, query.Anio, query.IdProyecto, query.IdArea, cancellationToken);
    }
}

public record ObtenerFiltrosDashboardQuery : IRequest<FiltroCatalogosDashboardResponse>;

public class ObtenerFiltrosDashboardHandler(
    IDashboardQueryService consultas, IProveedorUsuarioActual proveedorUsuarioActual, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerFiltrosDashboardQuery, FiltroCatalogosDashboardResponse>
{
    public async Task<FiltroCatalogosDashboardResponse> Handle(ObtenerFiltrosDashboardQuery query, CancellationToken cancellationToken)
    {
        var (idUsuarioActual, global, departamento) = await AlcanceDashboard.ResolverAsync(proveedorUsuarioActual, permisos, cancellationToken);
        return await consultas.ObtenerFiltrosAsync(idUsuarioActual, global, departamento, cancellationToken);
    }
}
