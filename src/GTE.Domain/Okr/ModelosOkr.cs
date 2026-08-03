namespace GTE.Domain.Okr;

public record ObjetivoOkrNuevo(
    int? IdProyecto, int? IdEquipo, string Nombre, string? Descripcion, int Anio, byte Trimestre);

public record ObjetivoOkrEdicion(int IdObjetivoOkr, string Nombre, string? Descripcion);

public record ResultadoClaveNuevo(int IdObjetivoOkr, string Nombre, decimal ValorMeta, string? ClaveKpi);

public record ResultadoClaveEdicion(int IdResultadoClave, string Nombre, decimal ValorMeta, decimal ValorActual, string? ClaveKpi);

public static class PermisosOkr
{
    public const string Gestionar = "POR.GestionarOkr";
}
