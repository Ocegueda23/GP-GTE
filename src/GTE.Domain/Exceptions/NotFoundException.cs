using GTE.Domain.Common;

namespace GTE.Domain.Exceptions;

/// <summary>Recurso inexistente. Se mapea a HTTP 404.</summary>
public class NotFoundException : ExcepcionDominio
{
    public NotFoundException(string entidad, object id)
        : base($"No se encontro {entidad} con identificador {id}.")
    {
    }

    public NotFoundException(string mensaje) : base(mensaje)
    {
    }
}
