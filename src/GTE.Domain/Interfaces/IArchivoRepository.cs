using GTE.Domain.Archivos;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Archivos (metadatos; el binario lo maneja IAlmacenArchivos).</summary>
public interface IArchivoRepository
{
    /// <summary>Crea tblArchivo y su tblArchivoVinculo en una sola operacion.</summary>
    Task<EstadoArchivoVinculo> VincularAsync(ArchivoNuevo datos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea SOLO tblArchivo, sin vinculo: imagen pegada en un formulario de alta, cuando la
    /// entidad destino todavia no tiene Id.
    /// </summary>
    Task CrearBorradorAsync(ArchivoBorrador datos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vincula a la entidad recien creada los archivos en borrador cuyos GUID vienen en el
    /// contenido. Solo toma archivos sin vinculo y subidos por el usuario actual, para que un
    /// GUID ajeno colado en el HTML no pueda robar el adjunto de otro. Devuelve cuantos vinculo.
    /// </summary>
    Task<int> VincularBorradoresAsync(
        string entidad, int idEntidad, IReadOnlyCollection<Guid> guids,
        CancellationToken cancellationToken = default);

    Task<EstadoArchivoVinculo?> ObtenerVinculoAsync(int idArchivoVinculo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Metadatos para descarga; null si el GUID no existe o quedo sin vinculos activos. Un
    /// archivo en borrador (sin vinculo todavia) solo lo ve quien lo subio: es lo que permite
    /// que la imagen se muestre en el formulario de alta antes de guardar.
    /// </summary>
    Task<ArchivoDescarga?> ObtenerDescargaAsync(Guid guidArchivo, CancellationToken cancellationToken = default);

    /// <summary>Baja logica del vinculo. El registro fisico de tblArchivo se conserva.</summary>
    Task DesvincularAsync(int idArchivoVinculo, CancellationToken cancellationToken = default);
}
