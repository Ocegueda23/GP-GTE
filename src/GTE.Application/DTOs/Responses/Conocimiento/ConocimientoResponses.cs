namespace GTE.Application.DTOs.Responses.Conocimiento;

/// <summary>Fila del listado/buscador: sin el HTML completo, solo un fragmento en texto plano.</summary>
public class ArticuloListaResponse
{
    public int IdArticuloConocimiento { get; set; }
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Primeras lineas del contenido ya sin etiquetas HTML (lo arma el QueryService).</summary>
    public string Fragmento { get; set; } = string.Empty;

    public bool EsGlosario { get; set; }
    public bool EsPublico { get; set; }

    /// <summary>Hay al menos una imagen incrustada en el contenido (pinta la miniatura del indice).</summary>
    public bool TieneImagen { get; set; }

    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaMovto { get; set; }

    /// <summary>Nombre de quien registro o movio por ultima vez; vacio si el usuario ya no existe.</summary>
    public string UltimoAutor { get; set; } = string.Empty;
}

/// <summary>Detalle completo del articulo, con el HTML sanitizado listo para renderizar.</summary>
public class ArticuloResponse
{
    public int IdArticuloConocimiento { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public int VersionActual { get; set; }
    public bool EsGlosario { get; set; }
    public bool EsPublico { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaMovto { get; set; }
    public string UltimoAutor { get; set; } = string.Empty;
}

/// <summary>Una entrada del historial. El contenido va aparte (solo al abrir esa version).</summary>
public class ArticuloVersionResponse
{
    public int IdArticuloVersion { get; set; }
    public int Version { get; set; }
    public bool EsVersionActual { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string Autor { get; set; } = string.Empty;
}

/// <summary>Contenido historico de una version concreta (para verla sin restaurarla).</summary>
public class ArticuloVersionContenidoResponse
{
    public int Version { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string Autor { get; set; } = string.Empty;
}

/// <summary>
/// Detalle para el consumo ANONIMO. Deliberadamente mas pobre que ArticuloResponse:
/// no expone autores ni el numero de version (metadata interna que un visitante
/// externo no tiene por que ver).
/// </summary>
public class ArticuloPublicoResponse
{
    public int IdArticuloConocimiento { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public bool EsGlosario { get; set; }

    /// <summary>Fecha de la ultima actualizacion; lo unico de trazabilidad que se publica.</summary>
    public DateTime FechaActualizacion { get; set; }
}

/// <summary>Fila del listado publico. Sin autores ni marca de publico (todo lo listado lo es).</summary>
public class ArticuloPublicoListaResponse
{
    public int IdArticuloConocimiento { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Fragmento { get; set; } = string.Empty;
    public bool EsGlosario { get; set; }
    public bool TieneImagen { get; set; }
}
