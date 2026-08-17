import { enviar, http, lanzarErrorApi, obtener, ErrorApi, type ApiResponse } from "./http";

export interface Archivo {
  idArchivoVinculo: number;
  guidArchivo: string;
  nombreArchivo: string;
  extension: string | null;
  tamanoBytes: number;
  autor: string;
  usuarioRegistro: string;
  fechaRegistro: string;
}

export async function obtenerArchivos(idWorkItem: number) {
  return obtener<Archivo[]>(`/api/v1/workitems/${idWorkItem}/archivos`);
}

export async function obtenerArchivosSolicitud(idSolicitud: number) {
  return obtener<Archivo[]>(`/api/v1/solicitudes/${idSolicitud}/archivos`);
}

/**
 * Content-Type se deja "undefined" a proposito: el default de la instancia es
 * application/json y pisaria el boundary multipart que el navegador calcula solo.
 */
async function subirArchivoA(ruta: string, archivo: File) {
  const formulario = new FormData();
  formulario.append("archivo", archivo);
  try {
    const { data } = await http.post<ApiResponse<Archivo>>(
      ruta,
      formulario,
      { headers: { "Content-Type": undefined } },
    );
    if (!data.success || !data.response) {
      throw new ErrorApi(data.userMessage, data.code);
    }
    return { dato: data.response, mensaje: data.userMessage };
  } catch (error) {
    if (error instanceof ErrorApi) throw error;
    lanzarErrorApi(error);
  }
}

export async function subirArchivo(idWorkItem: number, archivo: File) {
  return subirArchivoA(`/api/v1/workitems/${idWorkItem}/archivos`, archivo);
}

export async function subirArchivoSolicitud(idSolicitud: number, archivo: File) {
  return subirArchivoA(`/api/v1/solicitudes/${idSolicitud}/archivos`, archivo);
}

export async function eliminarArchivoVinculo(idArchivoVinculo: number) {
  const { mensaje } = await enviar<object>("delete", `/api/v1/archivos-vinculo/${idArchivoVinculo}`);
  return mensaje;
}

export function urlDescargaArchivo(guidArchivo: string) {
  return `/api/v1/archivos/${guidArchivo}`;
}

/** Descarga autenticada: nunca <img src> o <a href> directo al endpoint (expondria el JWT en la URL). */
export async function descargarArchivoBlob(guidArchivo: string): Promise<Blob> {
  const { data } = await http.get<Blob>(urlDescargaArchivo(guidArchivo), { responseType: "blob" });
  return data;
}

/** Formato "12.4 KB" / "3.1 MB" para el tamano de un adjunto. */
export function formatearTamano(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
