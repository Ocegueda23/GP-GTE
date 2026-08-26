namespace GTE.WebApi.Seguridad;

/// <summary>Nombres de las politicas de limitacion de tasa registradas en Program.cs.</summary>
public static class LimitadoresTasa
{
    /// <summary>
    /// Rutas anonimas expuestas a internet (hoy: /api/v1/publico/conocimiento). El resto
    /// de la API no necesita limitador: exige token y vive en la red interna.
    /// </summary>
    public const string Publico = "publico";
}
