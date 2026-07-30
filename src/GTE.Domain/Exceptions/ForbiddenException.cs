using GTE.Domain.Common;

namespace GTE.Domain.Exceptions;

/// <summary>Accion no permitida en el estado actual o para el usuario. Se mapea a HTTP 403.</summary>
public class ForbiddenException : ExcepcionDominio
{
    public ForbiddenException(string mensaje) : base(mensaje)
    {
    }
}
