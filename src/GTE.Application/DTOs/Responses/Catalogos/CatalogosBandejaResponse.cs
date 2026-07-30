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
}
