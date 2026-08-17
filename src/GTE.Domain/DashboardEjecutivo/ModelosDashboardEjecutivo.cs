namespace GTE.Domain.DashboardEjecutivo;

/// <summary>
/// Dashboard Ejecutivo P18 (Doctos/GTE-DocumentoMaestro.md 3.10/5.10): vista de
/// equipo/proyecto (DORA, costo, rentabilidad, avance de OKR). No sustituye el
/// Dashboard de colaborador individual (<see cref="GTE.Domain.Dashboard.CalculadoraPuntaje"/>,
/// modulo GTE.Domain.Dashboard) -- son dos vistas distintas del mismo concepto
/// "dashboard ejecutivo", documentadas por separado en Doctos/PENDIENTES.md.
/// Reusa los permisos ya sembrados <see cref="GTE.Domain.Dashboard.PermisosDashboard"/>
/// (DASH.Ejecutivo/DASH.VerDepartamento) en vez de crear permisos nuevos.
/// </summary>
public static class CalculadoraIndicadoresEjecutivos
{
    /// <summary>
    /// Percentil por interpolacion lineal (metodo "linear"/R-7, el mismo que usan
    /// Excel/NumPy por default) sobre una coleccion de minutos/dias ya convertida a
    /// double. Se aisla aqui para poder probarlo sin EF ni base de datos.
    /// </summary>
    public static double Percentil(IReadOnlyList<double> valores, double percentil)
    {
        if (valores.Count == 0) return 0;
        if (valores.Count == 1) return valores[0];

        var ordenados = valores.OrderBy(v => v).ToArray();
        var posicion = (percentil / 100.0) * (ordenados.Length - 1);
        var indiceInferior = (int)Math.Floor(posicion);
        var indiceSuperior = (int)Math.Ceiling(posicion);
        if (indiceInferior == indiceSuperior) return ordenados[indiceInferior];

        var fraccion = posicion - indiceInferior;
        return ordenados[indiceInferior] + (ordenados[indiceSuperior] - ordenados[indiceInferior]) * fraccion;
    }

    /// <summary>
    /// Semaforo heredado del GT para "Entrega a tiempo" (3.10 del Documento Maestro):
    /// >=90% verde, >=80% naranja, menor rojo. Mismo umbral reutilizable para cualquier
    /// otro indicador de cumplimiento porcentual del dashboard (SLA, DORA CFR invertido).
    /// </summary>
    public static string Semaforo(decimal porcentaje) => porcentaje switch
    {
        >= 90 => "Verde",
        >= 80 => "Naranja",
        _ => "Rojo",
    };
}
