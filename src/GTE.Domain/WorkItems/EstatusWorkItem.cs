namespace GTE.Domain.WorkItems;

/// <summary>
/// IDs de dbo.tblEstatusWorkItem. CONTRATO del motor de workflow
/// (seeds del script 01): no cambiar sin coordinar BD + transiciones.
/// </summary>
public static class EstatusWorkItem
{
    public const int Pendiente = 1;
    public const int EnProceso = 2;
    public const int EnPruebas = 3;
    public const int Correccion = 4;
    public const int Suspendido = 5;
    public const int Terminado = 6;
    public const int Cancelado = 7;
}

/// <summary>Acciones del grafo del proceso WorkItem (dbo.tblTransicion).</summary>
public static class AccionesWorkItem
{
    public const string Iniciar = "INICIAR";
    public const string Reanudar = "REANUDAR";
    public const string Suspender = "SUSPENDER";
    public const string EnviarPruebas = "ENVIAR_PRUEBAS";
    public const string RechazarQa = "RECHAZAR_QA";
    public const string Terminar = "TERMINAR";
    public const string Revertir = "REVERTIR";
    public const string Cancelar = "CANCELAR";
}

/// <summary>Claves de permisos del modulo (dbo.tblPermiso).</summary>
public static class PermisosWorkItem
{
    public const string Crear = "WI.Crear";
    public const string Editar = "WI.Editar";
    public const string Eliminar = "WI.Eliminar";
    public const string ModificarCompromiso = "WI.ModificarCompromiso";
    public const string ModificarTerminado = "WI.ModificarTerminado";
    public const string ModificarAjeno = "WI.ModificarAjeno";
    public const string TerminarMantenimiento = "WI.TerminarMantenimiento";
    public const string ModificarComplejidad = "WI.ModificarComplejidad";
    public const string ModificarTiempo = "WI.ModificarTiempo";
}
