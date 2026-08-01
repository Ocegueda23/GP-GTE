namespace GTE.Domain.Comentarios;

/// <summary>Comentario nuevo sobre una entidad (hoy solo WorkItem consume este modulo).</summary>
public record ComentarioNuevo(string Entidad, int IdEntidad, string Contenido, int? IdComentarioPadre);

/// <summary>Estado minimo para validar autoria antes de una baja.</summary>
public record EstadoComentario(int IdComentario, string UsuarioRegistro, bool Activo);
