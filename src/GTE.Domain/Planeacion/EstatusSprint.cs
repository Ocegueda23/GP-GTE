namespace GTE.Domain.Planeacion;

/// <summary>IDs de dbo.tblEstatusSprint (contrato de seeds del script 01).</summary>
public static class EstatusSprint
{
    public const int Planeado = 1;
    public const int Activo = 2;
    public const int Cerrado = 3;
}

public static class AccionesSprint
{
    public const string Activar = "ACTIVAR";
    public const string Cerrar = "CERRAR";

    /// <summary>Reversa: Activo -> Planeado (ej. se activo por error o hay que replanear).</summary>
    public const string VolverAPlaneado = "VOLVER_PLANEADO";
}

public static class PermisosPlaneacion
{
    /// <summary>Crear sprints. Activar/cerrar/modificar tienen su propio permiso (ver abajo).</summary>
    public const string GestionarSprints = "PLA.GestionarSprints";

    /// <summary>Cerrar un sprint Activo.</summary>
    public const string CerrarSprint = "PLA.CerrarSprint";

    /// <summary>Activar un sprint Planeado, o revertirlo de Activo a Planeado.</summary>
    public const string CambiarEstatusSprint = "PLA.CambiarEstatusSprint";

    /// <summary>Editar nombre/objetivo/fechas de un sprint no cerrado.</summary>
    public const string ModificarSprint = "PLA.ModificarSprint";

    /// <summary>Permite exceder el limite WIP de una columna, dejando rastro en bitacora.</summary>
    public const string SaltarWip = "PLA.SaltarWip";
}

/// <summary>Que hacer con los elementos abiertos al cerrar un sprint (RN-GTE-018).</summary>
public enum DestinoItemsAbiertos
{
    Backlog = 0,
    SiguienteSprint = 1
}
