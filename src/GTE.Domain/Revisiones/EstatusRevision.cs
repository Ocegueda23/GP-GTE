namespace GTE.Domain.Revisiones;

/// <summary>IDs de dbo.tblEstatusRevision (contrato de seeds del script 01).</summary>
public static class EstatusRevision
{
    public const int Pendiente = 1;
    public const int EnProceso = 2;
    public const int Terminada = 3;
}

public static class AccionesRevision
{
    public const string Iniciar = "INICIAR";
    public const string Terminar = "TERMINAR";
    public const string Reabrir = "REABRIR";
}

public static class PermisosRevision
{
    /// <summary>Reabrir un hallazgo ya corregido: solo lider (regla heredada del GT).</summary>
    public const string Reabrir = "REV.Reabrir";

    /// <summary>Cierre masivo de revisiones de un elemento.</summary>
    public const string Activar = "REV.Activar";
}
