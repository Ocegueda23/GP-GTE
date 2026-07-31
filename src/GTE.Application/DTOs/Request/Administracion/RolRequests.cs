namespace GTE.Application.DTOs.Request.Administracion;

public class AsignarRolRequest
{
    public int IdRol { get; set; }
    public int? IdProyecto { get; set; }
}

public class GuardarMatrizPermisosRequest
{
    public List<int> IdsPermiso { get; set; } = [];
}
