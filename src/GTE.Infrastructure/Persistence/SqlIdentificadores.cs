namespace GTE.Infrastructure.Persistence;

/// <summary>
/// Citado de identificadores para SQL dinamico blindado: SOLO se usa con nombres de
/// tabla/columna que ya vienen de un catalogo interno de confianza (nunca de input de
/// usuario sin validar contra el esquema real primero). Ver MotorWorkflow y
/// MotorCrudGenerico/MotorMetadatosEsquema.
/// </summary>
public static class SqlIdentificadores
{
    public static string Citar(string identificador)
    {
        return "[" + identificador.Replace("]", "]]") + "]";
    }

    public static string CitarTabla(string tabla)
    {
        var partes = tabla.Split('.');
        return partes.Length switch
        {
            1 => "[dbo]." + Citar(partes[0]),
            2 => Citar(partes[0]) + "." + Citar(partes[1]),
            _ => throw new InvalidOperationException($"Nombre de tabla invalido: {tabla}")
        };
    }
}
