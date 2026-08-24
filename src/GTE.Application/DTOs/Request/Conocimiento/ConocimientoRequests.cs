namespace GTE.Application.DTOs.Request.Conocimiento;

/// <summary>
/// Alta de articulo. El contenido viaja como HTML del editor enriquecido y SIEMPRE
/// pasa por ISanitizadorHtml en el handler: nunca se confia en el HTML del front.
/// </summary>
public class ArticuloCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Termino del Glosario (se lista en el indice alfabetico) en vez de articulo largo.</summary>
    public bool EsGlosario { get; set; }

    /// <summary>Visible SIN iniciar sesion en /publico/conocimiento. Privado por default.</summary>
    public bool EsPublico { get; set; }
}

/// <summary>Edicion de articulo. Si el contenido cambia, el backend genera una version nueva.</summary>
public class ArticuloActualizarRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public bool EsGlosario { get; set; }
    public bool EsPublico { get; set; }
}
