using GTE.Application.Common;
using GTE.Domain.CatalogoGenerico;

namespace GTE.Application.Interfaces;

/// <summary>
/// CRUD generico ("como se edita") contra la tabla de negocio de un catalogo, con SQL
/// dinamico blindado: los identificadores de tabla/columna SOLO salen de
/// ConfiguracionCatalogo.Columnas (ya validados contra INFORMATION_SCHEMA cuando se
/// guardo la configuracion) -- nunca de las claves del diccionario de valores, que son
/// datos de request y siempre se filtran contra esa lista antes de usarse.
/// </summary>
public interface IMotorCrudGenerico
{
    Task<PagedResult<Dictionary<string, object?>>> ListarAsync(
        ConfiguracionCatalogo catalogo,
        FiltrosListadoCatalogo filtros,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default);

    /// <summary>Valores distintos de una columna visible, para el filtro tipo "filtro de Excel".</summary>
    Task<IReadOnlyList<string>> ObtenerValoresDistintosAsync(
        ConfiguracionCatalogo catalogo, string nombreColumna, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opciones {valor, etiqueta} de la tabla referenciada por un combo FK. Los 3 nombres
    /// SOLO deben venir de una ColumnaReconciliada ya guardada (TablaFk/ColumnaClaveFk/
    /// ColumnaMostrarFk validados contra INFORMATION_SCHEMA al guardarse la config) --
    /// nunca de un request sin pasar antes por esa validacion.
    /// </summary>
    Task<IReadOnlyList<OpcionFk>> ObtenerOpcionesFkAsync(
        string tabla, string columnaClave, string columnaMostrar, CancellationToken cancellationToken = default);

    /// <summary>Una fila por PK, sin desproteger columnas cifradas (eso es DescifrarValorCommand).</summary>
    Task<Dictionary<string, object?>?> ObtenerPorPkAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, CancellationToken cancellationToken = default);

    /// <summary>Crea el registro y devuelve la fila resultante (incluye el valor autogenerado de la PK).</summary>
    Task<Dictionary<string, object?>> CrearAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> valores, string usuario,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, Dictionary<string, object?> valores,
        string usuario, CancellationToken cancellationToken = default);

    /// <summary>Baja logica (Activo = 0) si la tabla tiene columna Activo; si no, hard delete.</summary>
    Task EliminarAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, string usuario,
        CancellationToken cancellationToken = default);
}
