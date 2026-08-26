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

/// <summary>
/// IDs de dbo.tblSeveridad. Decide si un hallazgo (QA o code review, tblRevision) bloquea:
/// S1/S2 impiden Terminar el WorkItem y bloquean la aprobacion de un release que lo incluya;
/// S3/S4 quedan registrados pero no bloquean.
/// </summary>
public static class Severidad
{
    public const int S1Critica = 1;
    public const int S2Alta = 2;
    public const int S3Media = 3;
    public const int S4Baja = 4;
}

public static class PermisosCalidad
{
    /// <summary>Crear/editar/retirar casos de prueba del catalogo de un proyecto.</summary>
    public const string GestionarPlanes = "QA.GestionarPlanes";

    /// <summary>Asignar un caso a un WorkItem y registrar el resultado de su ejecucion.</summary>
    public const string Ejecutar = "QA.Ejecutar";
}
