using GTE.Domain.Common;

namespace GTE.Domain.Exceptions;

/// <summary>
/// Conflicto de estado. Se mapea a HTTP 409. Admite un detalle estructurado
/// para que el frontend pinte la lista de bloqueos sin re-consultar.
/// </summary>
public class ConflictException : ExcepcionDominio
{
    public object? Detalle { get; }

    public ConflictException(string mensaje, object? detalle = null) : base(mensaje)
    {
        Detalle = detalle;
    }
}
