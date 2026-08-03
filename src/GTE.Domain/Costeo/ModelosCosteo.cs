namespace GTE.Domain.Costeo;

public record TarifaNivelNueva(int IdNivel, decimal CostoHora, DateOnly VigenciaDesde);

public record TarifaNivelEdicion(int IdTarifaNivel, decimal CostoHora, DateOnly VigenciaDesde);

public record PresupuestoProyectoNuevo(int IdProyecto, int Anio, decimal MontoAutorizado, decimal HorasAutorizadas);

public record PresupuestoProyectoEdicion(int IdPresupuestoProyecto, decimal MontoAutorizado, decimal HorasAutorizadas);

public static class PermisosCosteo
{
    /// <summary>Alta/edicion/baja de tarifas y presupuesto.</summary>
    public const string Gestionar = "POR.GestionarCosteo";

    /// <summary>
    /// Ver tarifas, presupuesto y el reporte de costo real -- datos sensibles (revelan
    /// costo/hora por nivel, cercano a banda salarial). Reutiliza RPT.Costos, ya sembrado
    /// en el script 02 (modulo Indicadores, "Ver reportes de costos y rentabilidad") y
    /// reservado para el Dashboard Ejecutivo de Fase 5 -- mismo permiso, no uno nuevo
    /// redundante. Quien tiene Gestionar puede ver tambien (ver PermisosCosteo en el
    /// handler de lectura).
    /// </summary>
    public const string VerCostos = "RPT.Costos";
}
