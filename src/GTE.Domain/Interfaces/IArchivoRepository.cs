using GTE.Domain.Archivos;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Archivos (metadatos; el binario lo maneja IAlmacenArchivos).</summary>
public interface IArchivoRepository
{
    /// <summary>Crea tblArchivo y su tblArchivoVinculo en una sola operacion.</summary>
    Task<EstadoArchivoVinculo> VincularAsync(ArchivoNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoArchivoVinculo?> ObtenerVinculoAsync(int idArchivoVinculo, CancellationToken cancellationToken = default);

    /// <summary>Metadatos para descarga; null si el GUID no existe o quedo sin vinculos activos.</summary>
    Task<ArchivoDescarga?> ObtenerDescargaAsync(Guid guidArchivo, CancellationToken cancellationToken = default);

    /// <summary>Baja logica del vinculo. El registro fisico de tblArchivo se conserva.</summary>
    Task DesvincularAsync(int idArchivoVinculo, CancellationToken cancellationToken = default);
}
