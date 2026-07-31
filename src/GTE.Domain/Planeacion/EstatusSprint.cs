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
}

public static class PermisosPlaneacion
{
    public const string GestionarSprints = "PLA.GestionarSprints";

    /// <summary>Permite exceder el limite WIP de una columna, dejando rastro en bitacora.</summary>
    public const string SaltarWip = "PLA.SaltarWip";
}

/// <summary>Que hacer con los elementos abiertos al cerrar un sprint (RN-PLA-02).</summary>
public enum DestinoItemsAbiertos
{
    Backlog = 0,
    SiguienteSprint = 1
}
