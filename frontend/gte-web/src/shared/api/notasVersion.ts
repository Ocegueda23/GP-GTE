import { enviar, obtener } from "./http";

export interface RenglonNotaVersion {
  idNotaVersionDetalle: number;
  /** Eco del uiId que mando el front al dar de alta el renglon; null al releer. */
  uiId: string | null;
  idTipoCambioVersion: number;
  /** Nombre resuelto por el back: se pinta tal cual, no se rederiva. */
  tipoCambio: string;
  modulo: string | null;
  descripcion: string;
  orden: number;
}

export interface NotaVersion {
  idNotaVersion: number;
  version: string;
  fechaLiberacion: string;
  resumen: string | null;
  publicada: boolean;
  renglones: RenglonNotaVersion[];
}

export interface NotaVersionLista {
  idNotaVersion: number;
  version: string;
  fechaLiberacion: string;
  resumen: string | null;
  publicada: boolean;
  totalRenglones: number;
}

export interface TipoCambioVersion {
  idTipoCambioVersion: number;
  nombre: string;
  orden: number;
}

/** Renglon en edicion: sin id todavia = alta, y viaja con uiId para rehidratarse. */
export interface RenglonNotaVersionRequest {
  idNotaVersionDetalle: number | null;
  uiId: string | null;
  idTipoCambioVersion: number;
  modulo: string | null;
  descripcion: string;
  orden: number;
}

export interface NotaVersionUpsertRequest {
  version: string;
  fechaLiberacion: string;
  resumen: string | null;
  publicada: boolean;
  renglones: RenglonNotaVersionRequest[];
}

/** Historial que ve cualquier usuario autenticado: solo notas publicadas. */
export async function obtenerNotasVersionPublicadas() {
  return obtener<NotaVersion[]>("/api/v1/notas-version");
}

export async function obtenerTiposCambioVersion() {
  return obtener<TipoCambioVersion[]>("/api/v1/notas-version/tipos-cambio");
}

/** Listado de administracion: incluye borradores. */
export async function obtenerNotasVersionAdministracion() {
  return obtener<NotaVersionLista[]>("/api/v1/notas-version/administracion");
}

export async function obtenerNotaVersion(idNotaVersion: number) {
  return obtener<NotaVersion>(`/api/v1/notas-version/${idNotaVersion}`);
}

export async function crearNotaVersion(datos: NotaVersionUpsertRequest) {
  return enviar<NotaVersion>("post", "/api/v1/notas-version", datos);
}

export async function actualizarNotaVersion(idNotaVersion: number, datos: NotaVersionUpsertRequest) {
  return enviar<NotaVersion>("put", `/api/v1/notas-version/${idNotaVersion}`, datos);
}

export async function eliminarNotaVersion(idNotaVersion: number) {
  return enviar<null>("delete", `/api/v1/notas-version/${idNotaVersion}`);
}
