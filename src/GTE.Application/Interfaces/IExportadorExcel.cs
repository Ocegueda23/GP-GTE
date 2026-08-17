namespace GTE.Application.Interfaces;

/// <summary>
/// Exportacion generica de una tabla a un libro de Excel (.xlsx), usada por el catalogo de
/// reportes R01-R14 (Doctos/GTE-DocumentoMaestro.md seccion 13: "exportacion Excel via back").
/// </summary>
public interface IExportadorExcel
{
    /// <summary>Genera un libro de una sola hoja con encabezados en negrita y columnas autoajustadas.</summary>
    byte[] GenerarLibro(string tituloHoja, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas);
}
