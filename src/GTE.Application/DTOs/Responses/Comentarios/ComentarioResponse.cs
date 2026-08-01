namespace GTE.Application.DTOs.Responses.Comentarios;

public class ComentarioResponse
{
    public int IdComentario { get; set; }
    public int IdWorkItem { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public int? IdComentarioPadre { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string UsuarioRegistro { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
