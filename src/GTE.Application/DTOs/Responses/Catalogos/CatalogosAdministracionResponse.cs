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

/// <summary>
/// Catalogos de la pantalla de Releases. Existe porque el combo de tipo de artefacto
/// estaba escrito a mano en el front y no reflejaba lo que se editara en el catalogo
/// dbo.tblTipoArtefacto.
/// </summary>
public class CatalogosEntregasResponse
{
    public List<CatalogoItemResponse> TiposArtefacto { get; set; } = [];

    /// <summary>
    /// Id del tipo "Script SQL" (RN-GTE-032): el front lo necesita para saber cuando pedir
    /// la justificacion de irreversibilidad, y asi no clava el 2 a mano.
    /// </summary>
    public int IdTipoArtefactoScriptSql { get; set; }

    /// <summary>Tipos de respaldo previos al despliegue (base de datos, servicio, sitio...).</summary>
    public List<CatalogoItemResponse> TiposRespaldo { get; set; } = [];

    /// <summary>
    /// Estatus de release para el filtro del listado. Viene del catalogo real y no de una
    /// lista escrita en el front, que se desincronizaria del orden de dbo.tblEstatusRelease.
    /// </summary>
    public List<CatalogoItemResponse> EstatusRelease { get; set; } = [];
}
