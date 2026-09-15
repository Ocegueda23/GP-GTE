namespace GTE.Application.DTOs.Responses.Catalogos;

public class CatalogoItemResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ProyectoItemResponse
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int IdCategoriaProyecto { get; set; }
    public string CategoriaProyecto { get; set; } = string.Empty;

    /// <summary>Equipo responsable del proyecto; el tablero de un equipo solo muestra los suyos.</summary>
    public int? IdEquipo { get; set; }
}

/// <summary>
/// Complejidad del catalogo junto con la categoria de proyecto a la que pertenece.
/// IdCategoriaProyecto nulo significa que aplica a cualquier categoria.
/// </summary>
public class ComplejidadItemResponse : CatalogoItemResponse
{
    public int? IdCategoriaProyecto { get; set; }
}

/// <summary>
/// Categoria de incidente junto con el nivel que la atiende ('Soporte N1-N2' o
/// 'Desarrollo'). El nivel viaja para agrupar el combo: son 41 categorias y sin
/// encabezados la lista se vuelve ilegible.
/// </summary>
public class CategoriaIncidenteItemResponse : CatalogoItemResponse
{
    public string Nivel { get; set; } = string.Empty;
}

/// <summary>Catalogos que alimentan la barra de filtros de la bandeja.</summary>
public class CatalogosBandejaResponse
{
    public IReadOnlyList<CatalogoItemResponse> Estatus { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Tipos { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Prioridades { get; set; } = [];
    public IReadOnlyList<ProyectoItemResponse> Proyectos { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Usuarios { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> TiposSolicitud { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Equipos { get; set; } = [];
    public IReadOnlyList<ComplejidadItemResponse> Complejidades { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> CategoriasTicket { get; set; } = [];
    public IReadOnlyList<CategoriaIncidenteItemResponse> CategoriasIncidente { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> EstatusTicket { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Severidades { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> UsuariosSolicitantes { get; set; } = [];
    public IReadOnlyList<CatalogoItemResponse> Locaciones { get; set; } = [];

    /// <summary>Sprints vigentes para el filtro de la bandeja (cerrados fuera).</summary>
    public IReadOnlyList<CatalogoItemResponse> Sprints { get; set; } = [];
}
