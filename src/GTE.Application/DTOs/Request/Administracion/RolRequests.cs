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

/// <summary>Alta de un acceso desde la pantalla del proyecto (el proyecto viaja en la ruta).</summary>
public class AsignarAccesoProyectoRequest
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
}
