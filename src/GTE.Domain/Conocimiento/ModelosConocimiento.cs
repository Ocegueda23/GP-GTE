namespace GTE.Domain.Conocimiento;

/// <summary>Datos para crear un articulo (la version inicial la fija el backend).</summary>
public record ArticuloNuevo(
    string Titulo,
    string Contenido,
    bool EsGlosario,
    bool EsPublico);

/// <summary>Datos editables de un articulo. Si el contenido cambia, el backend genera version nueva.</summary>
public record ArticuloActualizacion(
    string Titulo,
    string Contenido,
    bool EsGlosario,
    bool EsPublico);

/// <summary>Estado minimo de un articulo para evaluar reglas.</summary>
public record EstadoArticulo(
    int IdArticuloConocimiento,
    string Titulo,
    string Contenido,
    int VersionActual,
    bool EsGlosario,
    bool EsPublico,
    bool Activo);

/// <summary>Filtro del listado de articulos (interno y publico comparten forma).</summary>
public record FiltroArticulos(
    int Page = 1,
    int PageSize = 25,
    string? Texto = null,
    bool? EsGlosario = null);
