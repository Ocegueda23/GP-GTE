using GTE.Application.DTOs.Responses.CentroMando;
using GTE.Application.Interfaces;
using GTE.Domain.CentroMando;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.CentroMando.Queries;

/// <summary>Dashboard ejecutivo del Centro de Mando TI.</summary>
public record ObtenerCentroMandoQuery(int Anio, int Mes) : IRequest<CentroMandoResponse>;

public class ObtenerCentroMandoHandler(IVerificadorPermisos permisos, ICentroMandoQueryService consultas)
    : IRequestHandler<ObtenerCentroMandoQuery, CentroMandoResponse>
{
    public async Task<CentroMandoResponse> Handle(ObtenerCentroMandoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Ver, cancellationToken: cancellationToken);
        return await consultas.ObtenerCentroMandoAsync(query.Anio, query.Mes, cancellationToken);
    }
}

/// <summary>Dashboard individual de un responsable.</summary>
public record ObtenerEvaluacionResponsableQuery(int IdEquipo, int Anio, int Mes)
    : IRequest<EvaluacionResponsableResponse>;

public class ObtenerEvaluacionResponsableHandler(IVerificadorPermisos permisos, ICentroMandoQueryService consultas)
    : IRequestHandler<ObtenerEvaluacionResponsableQuery, EvaluacionResponsableResponse>
{
    public async Task<EvaluacionResponsableResponse> Handle(
        ObtenerEvaluacionResponsableQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Ver, cancellationToken: cancellationToken);

        return await consultas.ObtenerEvaluacionAsync(query.IdEquipo, query.Anio, query.Mes, cancellationToken)
            ?? throw new NotFoundException("EvaluacionEquipo", $"{query.IdEquipo}/{query.Anio}-{query.Mes:00}");
    }
}

/// <summary>Catalogo de indicadores; ambito null devuelve todos.</summary>
public record ObtenerCatalogoIndicadoresQuery(string? Ambito) : IRequest<IReadOnlyList<IndicadorGestionResponse>>;

public class ObtenerCatalogoIndicadoresHandler(IVerificadorPermisos permisos, ICentroMandoQueryService consultas)
    : IRequestHandler<ObtenerCatalogoIndicadoresQuery, IReadOnlyList<IndicadorGestionResponse>>
{
    public async Task<IReadOnlyList<IndicadorGestionResponse>> Handle(
        ObtenerCatalogoIndicadoresQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Ver, cancellationToken: cancellationToken);
        return await consultas.ObtenerCatalogoAsync(query.Ambito, cancellationToken);
    }
}

public record ObtenerAlertasGestionQuery(int Anio, int Mes, bool SoloVigentes)
    : IRequest<IReadOnlyList<AlertaGestionResponse>>;

public class ObtenerAlertasGestionHandler(IVerificadorPermisos permisos, ICentroMandoQueryService consultas)
    : IRequestHandler<ObtenerAlertasGestionQuery, IReadOnlyList<AlertaGestionResponse>>
{
    public async Task<IReadOnlyList<AlertaGestionResponse>> Handle(
        ObtenerAlertasGestionQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosCentroMando.Ver, cancellationToken: cancellationToken);
        return await consultas.ObtenerAlertasAsync(query.Anio, query.Mes, query.SoloVigentes, cancellationToken);
    }
}
