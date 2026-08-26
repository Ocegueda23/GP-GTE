using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using MediatR;

namespace GTE.Application.Archivos.Queries;

public record ObtenerArchivosArticuloQuery(int IdArticulo) : IRequest<IReadOnlyList<ArchivoResponse>>;

public class ObtenerArchivosArticuloHandler(IArchivoQueryService consultas)
    : IRequestHandler<ObtenerArchivosArticuloQuery, IReadOnlyList<ArchivoResponse>>
{
    public async Task<IReadOnlyList<ArchivoResponse>> Handle(
        ObtenerArchivosArticuloQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerPorEntidadAsync(
            ConstantesConocimiento.EntidadArchivo, query.IdArticulo, cancellationToken);
    }
}
