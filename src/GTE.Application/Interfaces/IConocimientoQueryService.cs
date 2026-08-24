using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Domain.Conocimiento;

namespace GTE.Application.Interfaces;

/// <summary>Contrato de LECTURA del modulo Base de conocimiento (P23).</summary>
public interface IConocimientoQueryService
{
    Task<PagedResult<ArticuloListaResponse>> ObtenerListaAsync(
        FiltroArticulos filtro, CancellationToken cancellationToken = default);

    Task<ArticuloResponse?> ObtenerPorIdAsync(int idArticulo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticuloVersionResponse>> ObtenerVersionesAsync(
        int idArticulo, CancellationToken cancellationToken = default);

    Task<ArticuloVersionContenidoResponse?> ObtenerVersionAsync(
        int idArticulo, int version, CancellationToken cancellationToken = default);

    // --- Consumo anonimo -------------------------------------------------
    // Metodos SEPARADOS de los internos a proposito: cada uno filtra EsPublico en
    // su propia consulta, para que ningun cambio futuro en los internos pueda
    // abrir contenido privado por descuido.

    Task<PagedResult<ArticuloPublicoListaResponse>> ObtenerListaPublicaAsync(
        FiltroArticulos filtro, CancellationToken cancellationToken = default);

    /// <summary>Null si el articulo no existe, esta inactivo o NO esta marcado publico.</summary>
    Task<ArticuloPublicoResponse?> ObtenerPublicoPorIdAsync(
        int idArticulo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Autorizacion de la descarga anonima de una imagen. Solo es true si el GUID
    /// (a) esta vinculado y activo sobre un articulo publico y activo, Y
    /// (b) aparece incrustado dentro del HTML de ese articulo.
    /// La condicion (b) es la que impide que un adjunto suelto (un PDF, un archivo
    /// interno) se sirva sin sesion aunque alguien adivine su GUID: el consumo
    /// publico solo alcanza a las imagenes que son parte del texto.
    /// </summary>
    Task<bool> EsImagenDeArticuloPublicoAsync(Guid guidArchivo, CancellationToken cancellationToken = default);
}
