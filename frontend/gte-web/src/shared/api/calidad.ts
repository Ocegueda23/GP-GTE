import { enviar, obtener } from "./http";

export interface PasoCaso {
  numeroPaso: number;
  accion: string;
  resultadoEsperado: string | null;
}

/** Caso del catalogo reutilizable de un proyecto. */
export interface CasoPrueba {
  idCasoPrueba: number;
  folio: string | null;
  titulo: string;
  precondiciones: string | null;
  resultadoEsperado: string | null;
  tipoPrueba: string;
  pasos: PasoCaso[];
}

/** Caso asignado a un WorkItem, con el resultado de su ultima ejecucion contra el. */
export interface CasoAsignado {
  idWorkItemCasoPrueba: number;
  idCasoPrueba: number;
  folio: string | null;
  titulo: string;
  tipoPrueba: string;
  pasos: PasoCaso[];
  idEjecucion: number | null;
  idUltimoResultado: number | null;
  ultimoResultado: string | null;
  fechaUltimaEjecucion: string | null;
}

export interface EjecucionRegistrada {
  idEjecucionPrueba: number;
  idRevision: number | null;
}

export const RESULTADOS = [
  { id: 1, nombre: "Pasa", color: "success" as const },
  { id: 2, nombre: "Falla", color: "error" as const },
  { id: 3, nombre: "Bloqueado", color: "warning" as const },
  { id: 4, nombre: "No aplica", color: "default" as const },
];

/** Catalogo de casos reutilizables del proyecto, para el selector de "usar caso existente". */
export async function obtenerCasosDisponibles(idProyecto: number) {
  return obtener<CasoPrueba[]>(`/api/v1/proyectos/${idProyecto}/casosprueba`);
}

export async function obtenerCasosAsignados(idWorkItem: number) {
  return obtener<CasoAsignado[]>(`/api/v1/workitems/${idWorkItem}/casosprueba`);
}

export async function crearCasoYAsignar(idWorkItem: number, datos: {
  titulo: string;
  precondiciones: string | null;
  resultadoEsperado: string | null;
  idTipoPrueba: number;
  reutilizable: boolean;
  pasos: PasoCaso[];
}) {
  return enviar<number>("post", `/api/v1/workitems/${idWorkItem}/casosprueba`, datos);
}

export async function asignarCasoExistente(idWorkItem: number, idCasoPrueba: number) {
  return enviar<object>("post", `/api/v1/workitems/${idWorkItem}/casosprueba/asignar`, { idCasoPrueba });
}

export async function retirarAsignacion(idWorkItemCasoPrueba: number) {
  return enviar<object>("put", `/api/v1/workitemcasoprueba/${idWorkItemCasoPrueba}/retirar`, {});
}

export async function actualizarCaso(idCasoPrueba: number, datos: {
  titulo: string;
  precondiciones: string | null;
  resultadoEsperado: string | null;
  idTipoPrueba: number;
  pasos: PasoCaso[];
}) {
  return enviar<object>("put", `/api/v1/casosprueba/${idCasoPrueba}`, datos);
}

export async function retirarCaso(idCasoPrueba: number) {
  return enviar<object>("put", `/api/v1/casosprueba/${idCasoPrueba}/retirar`, {});
}

/** Si el resultado es Falla, la respuesta trae idRevision con el hallazgo creado en automatico. */
export async function registrarEjecucion(idWorkItem: number, datos: {
  idCasoPrueba: number;
  idResultadoPrueba: number;
  observaciones: string | null;
  idSeveridad: number | null;
}) {
  return enviar<EjecucionRegistrada>("post", `/api/v1/workitems/${idWorkItem}/ejecuciones`, datos);
}
