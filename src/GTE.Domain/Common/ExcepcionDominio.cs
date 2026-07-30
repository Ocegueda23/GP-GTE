namespace GTE.Domain.Common;

/// <summary>
/// Excepcion base del dominio. El GlobalExceptionMiddleware mapea sus derivadas a HTTP.
/// </summary>
public abstract class ExcepcionDominio : Exception
{
    protected ExcepcionDominio(string mensaje) : base(mensaje)
    {
    }
}
