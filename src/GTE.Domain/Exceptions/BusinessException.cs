using GTE.Domain.Common;

namespace GTE.Domain.Exceptions;

/// <summary>Regla de negocio violada. Se mapea a HTTP 400.</summary>
public class BusinessException : ExcepcionDominio
{
    public BusinessException(string mensaje) : base(mensaje)
    {
    }
}
