using GTE.Domain.CatalogoGenerico;

namespace GTE.Application.Interfaces;

/// <summary>
/// Introspeccion de esquema de bdsGTE via INFORMATION_SCHEMA/sys.* ("que hay"). Los
/// nombres de tabla/columna aqui son siempre VALORES parametrizados en la consulta al
/// catalogo del motor de BD, nunca identificadores interpolados.
/// </summary>
public interface IMotorMetadatosEsquema
{
    /// <summary>Tablas base de dbo en bdsGTE, para el combo de alta de catalogo nuevo.</summary>
    Task<IReadOnlyList<string>> ObtenerTablasDisponiblesAsync(CancellationToken cancellationToken = default);

    /// <summary>Columnas reales de la tabla (tipo, nulabilidad, longitud, PK, identity).</summary>
    Task<IReadOnlyList<ColumnaEsquema>> ObtenerColumnasAsync(
        string nombreTabla, CancellationToken cancellationToken = default);
}
