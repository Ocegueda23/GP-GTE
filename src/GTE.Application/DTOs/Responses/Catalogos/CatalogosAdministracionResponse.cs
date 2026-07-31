namespace GTE.Application.DTOs.Responses.Catalogos;

/// <summary>Catalogos para las pantallas de /admin (dropdowns de alta/edicion).</summary>
public class CatalogosAdministracionResponse
{
    public List<CatalogoItemResponse> CategoriasProyecto { get; set; } = [];
    public List<CatalogoItemResponse> EstatusProyecto { get; set; } = [];
    public List<CatalogoItemResponse> Niveles { get; set; } = [];
    public List<CatalogoItemResponse> Areas { get; set; } = [];
    public List<CatalogoItemResponse> Puestos { get; set; } = [];
    public List<CatalogoItemResponse> Usuarios { get; set; } = [];
    public List<CatalogoItemResponse> Equipos { get; set; } = [];
    public List<CatalogoItemResponse> Roles { get; set; } = [];
    public List<CatalogoItemResponse> Horarios { get; set; } = [];
}
