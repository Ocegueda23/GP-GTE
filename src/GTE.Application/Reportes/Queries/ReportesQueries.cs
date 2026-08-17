using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Reportes;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Reportes;
using MediatR;

namespace GTE.Application.Reportes.Queries;

public record ObtenerActividadUsuarioQuery(int IdUsuario, DateOnly FechaInicio, DateOnly FechaFin)
    : IRequest<ActividadUsuarioResponse>;

public class ObtenerActividadUsuarioHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerActividadUsuarioQuery, ActividadUsuarioResponse>
{
    public async Task<ActividadUsuarioResponse> Handle(
        ObtenerActividadUsuarioQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.VerActividad, null, cancellationToken);

        if (query.FechaInicio > query.FechaFin)
        {
            throw new BusinessException("La fecha de inicio no puede ser posterior a la fecha fin.");
        }

        return await consultas.ObtenerActividadUsuarioAsync(
            query.IdUsuario, query.FechaInicio, query.FechaFin, cancellationToken);
    }
}

file static class ValidacionRangoFechas
{
    public static void Exigir(DateOnly desde, DateOnly hasta)
    {
        if (desde > hasta)
        {
            throw new BusinessException("La fecha de inicio no puede ser posterior a la fecha fin.");
        }
    }
}

// ---------- R01 Productividad ----------
public record ObtenerProductividadQuery(DateOnly Desde, DateOnly Hasta, int? IdProyecto, int? IdEquipo)
    : IRequest<ProductividadReporteResponse>;

public class ObtenerProductividadHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerProductividadQuery, ProductividadReporteResponse>
{
    public async Task<ProductividadReporteResponse> Handle(ObtenerProductividadQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerProductividadAsync(query.Desde, query.Hasta, query.IdProyecto, query.IdEquipo, cancellationToken);
    }
}

// ---------- R02 Horas registradas ----------
public record ObtenerHorasRegistradasQuery(DateOnly Desde, DateOnly Hasta, int? IdEquipo)
    : IRequest<HorasRegistradasReporteResponse>;

public class ObtenerHorasRegistradasHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerHorasRegistradasQuery, HorasRegistradasReporteResponse>
{
    public async Task<HorasRegistradasReporteResponse> Handle(ObtenerHorasRegistradasQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerHorasRegistradasAsync(query.Desde, query.Hasta, query.IdEquipo, cancellationToken);
    }
}

// ---------- R03 Retrabajo ----------
public record ObtenerRetrabajoQuery(DateOnly Desde, DateOnly Hasta, int? IdProyecto) : IRequest<RetrabajoReporteResponse>;

public class ObtenerRetrabajoHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerRetrabajoQuery, RetrabajoReporteResponse>
{
    public async Task<RetrabajoReporteResponse> Handle(ObtenerRetrabajoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerRetrabajoAsync(query.Desde, query.Hasta, query.IdProyecto, cancellationToken);
    }
}

// ---------- R04 Bugs y defectos ----------
public record ObtenerBugsDefectosQuery(DateOnly Desde, DateOnly Hasta, int? IdProyecto) : IRequest<BugsDefectosReporteResponse>;

public class ObtenerBugsDefectosHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerBugsDefectosQuery, BugsDefectosReporteResponse>
{
    public async Task<BugsDefectosReporteResponse> Handle(ObtenerBugsDefectosQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerBugsDefectosAsync(query.Desde, query.Hasta, query.IdProyecto, cancellationToken);
    }
}

// ---------- R05 Releases ----------
public record ObtenerReleasesReporteQuery(DateOnly Desde, DateOnly Hasta, int? IdProyecto) : IRequest<ReleasesReporteResponse>;

public class ObtenerReleasesReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerReleasesReporteQuery, ReleasesReporteResponse>
{
    public async Task<ReleasesReporteResponse> Handle(ObtenerReleasesReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerReleasesAsync(query.Desde, query.Hasta, query.IdProyecto, cancellationToken);
    }
}

// ---------- R06 Riesgos ----------
public record ObtenerRiesgosReporteQuery(int? IdProyecto) : IRequest<RiesgosReporteResponse>;

public class ObtenerRiesgosReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerRiesgosReporteQuery, RiesgosReporteResponse>
{
    public async Task<RiesgosReporteResponse> Handle(ObtenerRiesgosReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        return await consultas.ObtenerRiesgosAsync(query.IdProyecto, cancellationToken);
    }
}

// ---------- R07 Solicitantes ----------
public record ObtenerSolicitantesReporteQuery(DateOnly Desde, DateOnly Hasta) : IRequest<SolicitantesReporteResponse>;

public class ObtenerSolicitantesReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerSolicitantesReporteQuery, SolicitantesReporteResponse>
{
    public async Task<SolicitantesReporteResponse> Handle(ObtenerSolicitantesReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerSolicitantesAsync(query.Desde, query.Hasta, cancellationToken);
    }
}

// ---------- R08 Costos ----------
public record ObtenerCostosReporteQuery(int Anio, int? IdProyecto) : IRequest<CostosReporteResponse>;

public class ObtenerCostosReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerCostosReporteQuery, CostosReporteResponse>
{
    public async Task<CostosReporteResponse> Handle(ObtenerCostosReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.VerCostos, null, cancellationToken);
        return await consultas.ObtenerCostosAsync(query.Anio, query.IdProyecto, cancellationToken);
    }
}

// ---------- R09 Rentabilidad ----------
public record ObtenerRentabilidadReporteQuery(int Anio) : IRequest<RentabilidadReporteResponse>;

public class ObtenerRentabilidadReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerRentabilidadReporteQuery, RentabilidadReporteResponse>
{
    public async Task<RentabilidadReporteResponse> Handle(ObtenerRentabilidadReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.VerCostos, null, cancellationToken);
        return await consultas.ObtenerRentabilidadAsync(query.Anio, cancellationToken);
    }
}

// ---------- R10 SLA ----------
public record ObtenerSlaReporteQuery(DateOnly Desde, DateOnly Hasta) : IRequest<SlaReporteResponse>;

public class ObtenerSlaReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerSlaReporteQuery, SlaReporteResponse>
{
    public async Task<SlaReporteResponse> Handle(ObtenerSlaReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerSlaAsync(query.Desde, query.Hasta, cancellationToken);
    }
}

// ---------- R11 KPIs / DORA ----------
public record ObtenerKpisHistoricosQuery(int Anio, int? AnioComparativo) : IRequest<KpisHistoricosReporteResponse>;

public class ObtenerKpisHistoricosHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerKpisHistoricosQuery, KpisHistoricosReporteResponse>
{
    public async Task<KpisHistoricosReporteResponse> Handle(ObtenerKpisHistoricosQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        return await consultas.ObtenerKpisHistoricosAsync(query.Anio, query.AnioComparativo, cancellationToken);
    }
}

// ---------- R12 Carga de trabajo ----------
public record ObtenerCargaTrabajoReporteQuery(int? IdEquipo) : IRequest<CargaTrabajoReporteResponse>;

public class ObtenerCargaTrabajoReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerCargaTrabajoReporteQuery, CargaTrabajoReporteResponse>
{
    public async Task<CargaTrabajoReporteResponse> Handle(ObtenerCargaTrabajoReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        return await consultas.ObtenerCargaTrabajoAsync(query.IdEquipo, cancellationToken);
    }
}

// ---------- R13 Flujo (CFD) ----------
public record ObtenerFlujoReporteQuery(DateOnly Desde, DateOnly Hasta, int IdProyecto) : IRequest<FlujoReporteResponse>;

public class ObtenerFlujoReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerFlujoReporteQuery, FlujoReporteResponse>
{
    public async Task<FlujoReporteResponse> Handle(ObtenerFlujoReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.Ver, null, cancellationToken);
        ValidacionRangoFechas.Exigir(query.Desde, query.Hasta);
        return await consultas.ObtenerFlujoAsync(query.Desde, query.Hasta, query.IdProyecto, cancellationToken);
    }
}

// ---------- R14 Auditoria ----------
public record ObtenerAuditoriaReporteQuery(
    DateOnly? Desde, DateOnly? Hasta, string? Usuario, string? Entidad, int Page, int PageSize)
    : IRequest<PagedResult<AuditoriaItemResponse>>;

public class ObtenerAuditoriaReporteHandler(IReportesQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerAuditoriaReporteQuery, PagedResult<AuditoriaItemResponse>>
{
    public async Task<PagedResult<AuditoriaItemResponse>> Handle(ObtenerAuditoriaReporteQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosReportes.VerAuditoria, null, cancellationToken);
        return await consultas.ObtenerAuditoriaAsync(
            query.Desde, query.Hasta, query.Usuario, query.Entidad, query.Page, query.PageSize, cancellationToken);
    }
}
