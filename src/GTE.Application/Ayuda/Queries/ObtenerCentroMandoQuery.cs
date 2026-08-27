using GTE.Application.Interfaces;
using GTE.Domain.Ayuda;
using GTE.Domain.Exceptions;
using MediatR;

namespace GTE.Application.Ayuda.Queries;

/// <summary>
/// Centro de Mando TI: documento de referencia del modelo de indicadores con el que se
/// evalua a los responsables de area. Exige <see cref="PermisosAyuda.VerCentroMando"/>
/// (sembrado solo para el rol Administrador), a diferencia del Manual de usuario que es
/// para todos.
/// </summary>
public record ObtenerCentroMandoQuery : IRequest<string>;

public class ObtenerCentroMandoHandler(
    IVerificadorPermisos permisos, IProveedorDocumentosAyuda documentos)
    : IRequestHandler<ObtenerCentroMandoQuery, string>
{
    public async Task<string> Handle(ObtenerCentroMandoQuery query, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosAyuda.VerCentroMando, cancellationToken: cancellationToken);

        return await documentos.ObtenerHtmlAsync(ConstantesAyuda.DocumentoCentroMando, cancellationToken)
            ?? throw new NotFoundException("DocumentoAyuda", ConstantesAyuda.DocumentoCentroMando);
    }
}
