namespace GTE.Domain.Administracion;

/// <summary>Claves de permisos del modulo (dbo.tblPermiso, sembradas en el script 02).</summary>
public static class PermisosAdministracion
{
    public const string Usuarios = "ADM.Usuarios";
    public const string Roles = "ADM.Roles";
}

/// <summary>
/// IDs de dbo.tblEstatusProyecto. CONTRATO del motor de workflow
/// (seeds del script 01): no cambiar sin coordinar BD + transiciones.
/// </summary>
public static class EstatusProyecto
{
    public const int Propuesto = 1;
    public const int Autorizado = 2;
    public const int EnEjecucion = 3;
    public const int EnPausa = 4;
    public const int Cerrado = 5;
    public const int Cancelado = 6;
}

/// <summary>Acciones del grafo del proceso Proyecto (dbo.tblTransicion, script 09).</summary>
public static class AccionesProyecto
{
    public const string Autorizar = "AUTORIZAR";
    public const string Iniciar = "INICIAR";
    public const string Pausar = "PAUSAR";
    public const string Reanudar = "REANUDAR";
    public const string Cerrar = "CERRAR";
    public const string Cancelar = "CANCELAR";
}
