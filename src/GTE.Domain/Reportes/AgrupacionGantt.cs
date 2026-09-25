namespace GTE.Domain.Reportes;

/// <summary>
/// Nivel por el que se agrupan los renglones del Gantt de actividades (R16). No es un filtro:
/// los tres filtros del reporte (proyecto, usuario y periodo) se combinan entre si y son
/// independientes de esta agrupacion, que solo decide el orden y los encabezados de banda.
/// Se ordena por la llave de grupo ANTES de paginar para que una pagina nunca parta un grupo
/// a la mitad.
/// </summary>
public enum AgrupacionGantt
{
    Ninguno = 0,
    Proyecto = 1,
    Usuario = 2,
}
