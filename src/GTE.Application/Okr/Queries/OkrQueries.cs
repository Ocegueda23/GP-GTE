using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Domain.Okr;
using MediatR;

namespace GTE.Application.Okr.Queries;

public record ObtenerObjetivosOkrQuery(int? IdProyecto, int? IdEquipo, int? Anio) : IRequest<IReadOnlyList<ObjetivoOkrResponse>>;

public class ObtenerObjetivosOkrHandler(IOkrQueryService consultas, IVerificadorPermisos permisos)
    : IRequestHandler<ObtenerObjetivosOkrQuery, IReadOnlyList<ObjetivoOkrResponse>>
{
    public async Task<IReadOnlyList<ObjetivoOkrResponse>> Handle(
        ObtenerObjetivosOkrQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosOkr.Gestionar, null, cancellationToken);
        return await consultas.ObtenerObjetivosAsync(query.IdProyecto, query.IdEquipo, query.Anio, cancellationToken);
    }
}
