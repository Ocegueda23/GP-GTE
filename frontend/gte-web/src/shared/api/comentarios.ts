import { enviar, obtener } from "./http";

export interface Comentario {
  idComentario: number;
  idWorkItem: number;
  contenido: string;
  idComentarioPadre: number | null;
  autor: string;
  usuarioRegistro: string;
  fechaRegistro: string;
}

export async function obtenerComentarios(idWorkItem: number) {
  return obtener<Comentario[]>(`/api/v1/workitems/${idWorkItem}/comentarios`);
}

export async function crearComentario(
  idWorkItem: number,
  contenido: string,
  idComentarioPadre?: number,
) {
  return enviar<Comentario>("post", `/api/v1/workitems/${idWorkItem}/comentarios`, {
    contenido,
    idComentarioPadre: idComentarioPadre ?? null,
  });
}

export async function eliminarComentario(idComentario: number) {
  const { mensaje } = await enviar<object>("delete", `/api/v1/comentarios/${idComentario}`);
  return mensaje;
}
