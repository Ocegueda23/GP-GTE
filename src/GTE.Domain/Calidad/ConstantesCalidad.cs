namespace GTE.Domain.Calidad;

/// <summary>IDs de dbo.tblResultadoPrueba (contrato de seeds del script 01).</summary>
public static class ResultadoPrueba
{
    public const int Pasa = 1;
    public const int Falla = 2;
    public const int Bloqueado = 3;
    public const int NoAplica = 4;
}

/// <summary>IDs de dbo.tblTipoPrueba.</summary>
public static class TipoPrueba
{
    public const int Manual = 1;
    public const int Automatizada = 2;
    public const int Regresion = 3;
}

/// <summary>IDs de dbo.tblSeveridad (S1 y S2 bloquean la aprobacion de un release).</summary>
public static class Severidad
{
    public const int S1Critica = 1;
    public const int S2Alta = 2;
    public const int S3Media = 3;
    public const int S4Baja = 4;
}

public static class PermisosCalidad
{
    public const string GestionarPlanes = "QA.GestionarPlanes";
    public const string Ejecutar = "QA.Ejecutar";
}
