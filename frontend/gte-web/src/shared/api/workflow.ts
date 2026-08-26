import { enviar, obtener } from "./http";

export interface ProcesoWorkflow {
  idProceso: number;
  proceso: string;
}

export interface EstatusWorkflow {
  id: number;
  descripcion: string;
}

export interface TransicionWorkflow {
  idEstatusOrigen: number;
  estatusOrigen: string;
  accion: string;
  idEstatusDestino: number;
  estatusDestino: string;
  etiquetaBoton: string;
  requierePermiso: string | null;
  requiereMotivo: boolean;
  esAccionPrincipal: boolean;
  orden: number;
}

export interface DefinicionWorkflow {
  proceso: string;
  estatus: EstatusWorkflow[];
  transiciones: TransicionWorkflow[];
}

export interface PermisoWorkflow {
  clave: string;
  modulo: string;
  descripcion: string | null;
}

export interface TransicionConfigGuardar {
  idEstatusOrigen: number;
  accion: string;
  etiquetaBoton: string;
  requierePermiso: string | null;
  requiereMotivo: boolean;
  esAccionPrincipal: boolean;
  orden: number;
}

export async function obtenerProcesosWorkflow() {
  return obtener<ProcesoWorkflow[]>("/api/v1/workflow/procesos");
}

export async function obtenerPermisosWorkflow() {
  return obtener<PermisoWorkflow[]>("/api/v1/workflow/permisos");
}

export async function obtenerDefinicionWorkflow(proceso: string) {
  return obtener<DefinicionWorkflow>(`/api/v1/workflow/${proceso}/definicion`);
}

export async function guardarTransicionesWorkflow(proceso: string, transiciones: TransicionConfigGuardar[]) {
  return enviar<object>("put", `/api/v1/workflow/${proceso}/transiciones`, transiciones);
}

/** Vacia = el proyecto usa el default fijo (QA, Lider, Negocio). */
export async function obtenerCadenaAprobacion(idProyecto: number) {
  return obtener<string[]>(`/api/v1/proyectos/${idProyecto}/cadena-aprobacion`);
}

/** Lista vacia para volver a dejar al proyecto en el default fijo. */
export async function guardarCadenaAprobacion(idProyecto: number, roles: string[]) {
  return enviar<object>("put", `/api/v1/proyectos/${idProyecto}/cadena-aprobacion`, { roles });
}
