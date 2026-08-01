namespace GTE.Application.DTOs.Request.Comentarios;

public class ComentarioCrearRequest
{
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Si se manda, el comentario nuevo cuelga de este como respuesta del hilo.</summary>
    public int? IdComentarioPadre { get; set; }
}
