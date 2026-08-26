using GTE.Domain.CatalogoGenerico;

namespace GTE.Application.Interfaces;

/// <summary>Contrato de LECTURA del motor de catalogos genericos (metadatos, no datos).</summary>
public interface ICatalogoGenericoQueryService
{
    Task<IReadOnlyList<CatalogoResumen>> ObtenerCatalogosAsync(CancellationToken cancellationToken = default);

    Task<CatalogoResumen?> ObtenerPorClaveAsync(string clave, CancellationToken cancellationToken = default);

    /// <summary>Esquema real (INFORMATION_SCHEMA) reconciliado con la config guardada (tblCatalogoGenericoColumna).</summary>
    Task<ConfiguracionCatalogo?> ObtenerConfigCatalogoAsync(string clave, CancellationToken cancellationToken = default);
}
