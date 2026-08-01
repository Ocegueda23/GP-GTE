namespace GTE.Application.Interfaces;

/// <summary>
/// Limpia HTML de contenido enriquecido antes de persistirlo. Nunca se confia en el
/// HTML que manda el front: todo comentario pasa por aqui antes de tocar la BD.
/// </summary>
public interface ISanitizadorHtml
{
    string Sanitizar(string html);
}
