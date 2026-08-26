import {
  enviar, http, lanzarErrorApi, obtener, ErrorApi, URL_BASE_API,
  type ApiResponse, type ResultadoPaginado,
} from "./http";
import type { Archivo } from "./archivos";

export interface ArticuloLista {
  idArticuloConocimiento: number;
  titulo: string;
  fragmento: string;
  esGlosario: boolean;
  esPublico: boolean;
  tieneImagen: boolean;
  fechaRegistro: string;
  fechaMovto: string | null;
  ultimoAutor: string;
}

export interface Articulo {
  idArticuloConocimiento: number;
  titulo: string;
  contenido: string;
  versionActual: number;
  esGlosario: boolean;
  esPublico: boolean;
  fechaRegistro: string;
  fechaMovto: string | null;
  ultimoAutor: string;
}

export interface ArticuloVersion {
  idArticuloVersion: number;
  version: number;
  esVersionActual: boolean;
  fechaRegistro: string;
  autor: string;
}

export interface ArticuloVersionContenido {
  version: number;
  contenido: string;
  fechaRegistro: string;
  autor: string;
}

export interface FiltroArticulos {
  page?: number;
  pageSize?: number;
  texto?: string;
  /** Sin valor = articulos y terminos del glosario juntos. */
  esGlosario?: boolean;
}

export interface ArticuloGuardar {
  titulo: string;
  contenido: string;
  esGlosario: boolean;
  esPublico: boolean;
}

function armarParametros(filtro: FiltroArticulos): URLSearchParams {
  const params = new URLSearchParams();
  params.set("page", String(filtro.page ?? 1));
  params.set("pageSize", String(filtro.pageSize ?? 25));
  if (filtro.texto?.trim()) params.set("texto", filtro.texto.trim());
  if (filtro.esGlosario !== undefined) params.set("esGlosario", String(filtro.esGlosario));
  return params;
}

// --- Consumo interno (exige sesion) ---------------------------------------

export async function obtenerArticulos(filtro: FiltroArticulos) {
  return obtener<ResultadoPaginado<ArticuloLista>>("/api/v1/conocimiento", armarParametros(filtro));
}

export async function obtenerArticulo(idArticulo: number) {
  return obtener<Articulo>(`/api/v1/conocimiento/${idArticulo}`);
}

export async function obtenerVersionesArticulo(idArticulo: number) {
  return obtener<ArticuloVersion[]>(`/api/v1/conocimiento/${idArticulo}/versiones`);
}

export async function obtenerVersionArticulo(idArticulo: number, version: number) {
  return obtener<ArticuloVersionContenido>(`/api/v1/conocimiento/${idArticulo}/versiones/${version}`);
}

export async function crearArticulo(datos: ArticuloGuardar) {
  return enviar<Articulo>("post", "/api/v1/conocimiento", datos);
}

export async function actualizarArticulo(idArticulo: number, datos: ArticuloGuardar) {
  return enviar<Articulo>("put", `/api/v1/conocimiento/${idArticulo}`, datos);
}

export async function eliminarArticulo(idArticulo: number) {
  const { mensaje } = await enviar<object>("delete", `/api/v1/conocimiento/${idArticulo}`);
  return mensaje;
}

export async function obtenerArchivosArticulo(idArticulo: number) {
  return obtener<Archivo[]>(`/api/v1/conocimiento/${idArticulo}/archivos`);
}

/**
 * Content-Type se deja "undefined" a proposito: el default de la instancia es
 * application/json y pisaria el boundary multipart que el navegador calcula solo
 * (mismo patron que archivos.ts).
 */
export async function subirArchivoArticulo(idArticulo: number, archivo: File) {
  const formulario = new FormData();
  formulario.append("archivo", archivo);
  try {
    const { data } = await http.post<ApiResponse<Archivo>>(
      `/api/v1/conocimiento/${idArticulo}/archivos`,
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

// --- Consumo publico (sin sesion) ----------------------------------------
// Rutas y tipos SEPARADOS de los internos: el backend expone solo lo marcado
// publico y con menos campos (sin autores ni versiones).

export interface ArticuloPublicoLista {
  idArticuloConocimiento: number;
  titulo: string;
  fragmento: string;
  esGlosario: boolean;
  tieneImagen: boolean;
}

export interface ArticuloPublico {
  idArticuloConocimiento: number;
  titulo: string;
  contenido: string;
  esGlosario: boolean;
  fechaActualizacion: string;
}

export async function obtenerArticulosPublicos(filtro: FiltroArticulos) {
  return obtener<ResultadoPaginado<ArticuloPublicoLista>>(
    "/api/v1/publico/conocimiento", armarParametros(filtro));
}

export async function obtenerArticuloPublico(idArticulo: number) {
  return obtener<ArticuloPublico>(`/api/v1/publico/conocimiento/${idArticulo}`);
}

/**
 * URL directa de una imagen incrustada en un articulo publico. A diferencia del
 * endpoint autenticado de archivos, aqui SI se puede usar como <img src> porque la
 * ruta es anonima: no hay token que exponer en la URL.
 */
export function urlImagenPublica(guidArchivo: string) {
  return `${URL_BASE_API}/api/v1/publico/conocimiento/imagenes/${guidArchivo}`;
}
