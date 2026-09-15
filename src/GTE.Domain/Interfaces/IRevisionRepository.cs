using GTE.Domain.Revisiones;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Revisiones.</summary>
public interface IRevisionRepository
{
    Task<int> CrearAsync(RevisionNueva datos, CancellationToken cancellationToken = default);

    Task<EstadoRevision?> ObtenerEstadoAsync(int idRevision, CancellationToken cancellationToken = default);

    /// <summary>Marca el hallazgo como corregido (o lo reabre) y registra la fecha.</summary>
    /// <summary>
    /// Cierra o reabre un hallazgo. Con esFalsoPositivo el cierre se registra como
    /// "No es un error" (Corregido sigue en 1 para que deje de bloquear) y guarda la
    /// razon; reabrir limpia ambas marcas.
    /// </summary>
    Task EstablecerCorregidoAsync(
        int idRevision, bool corregido, bool esFalsoPositivo, string? motivoDescarte,
        CancellationToken cancellationToken = default);

    Task AplicarEfectosTransicionAsync(int idRevision, string accion, CancellationToken cancellationToken = default);
}
