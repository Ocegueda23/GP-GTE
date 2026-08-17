namespace GTE.Domain.Reportes;

public static class PermisosReportes
{
    /// <summary>Ver la actividad diaria (tblRegistroTiempo) de cualquier usuario en un rango de fechas.</summary>
    public const string VerActividad = "RPT.Actividad";

    /// <summary>Catalogo general R01-R14 (Documento Maestro seccion 13), salvo Costos/Rentabilidad y Auditoria.</summary>
    public const string Ver = "RPT.Ver";

    /// <summary>R08 Costos y R09 Rentabilidad -- ya sembrado (script 02) sin consumidor hasta ahora.</summary>
    public const string VerCostos = "RPT.Costos";

    /// <summary>R14 Auditoria (bitacora tblBitacora).</summary>
    public const string VerAuditoria = "RPT.Auditoria";
}
