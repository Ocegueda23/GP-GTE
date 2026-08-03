namespace GTE.Domain.Operacion;

/// <summary>IDs de dbo.tblEstatusIncidente (contrato de seeds del script 01).</summary>
public static class EstatusIncidente
{
    public const int Detectado = 1;
    public const int EnAtencion = 2;
    public const int Mitigado = 3;
    public const int Resuelto = 4;
    public const int Cerrado = 5;

    /// <summary>tblTipoWorkItem.Id del WorkItem correctivo que se crea al vincular (script 01).</summary>
    public const int IdTipoWorkItemCorreccion = 9;
}

/// <summary>
/// Acciones del grafo del proceso Incidente (dbo.tblTransicion). Sin reapertura ni
/// cancelacion: un incidente siempre concluye en Cerrado.
/// </summary>
public static class AccionesIncidente
{
    public const string Atender = "ATENDER";
    public const string Mitigar = "MITIGAR";
    public const string Resolver = "RESOLVER";
    public const string Cerrar = "CERRAR";
}

/// <summary>IDs de dbo.tblSeveridad (S1-S4, contrato de seeds del script 01).</summary>
public static class Severidad
{
    public const int S1Critica = 1;
    public const int S2Alta = 2;
    public const int S3Media = 3;
    public const int S4Baja = 4;
}

public static class PermisosIncidente
{
    public const string Gestionar = "INC.Gestionar";
}
