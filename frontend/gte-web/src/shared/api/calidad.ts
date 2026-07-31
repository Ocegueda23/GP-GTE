import { enviar, obtener } from "./http";

export interface PlanPrueba {
  idPlanPrueba: number;
  idProyecto: number;
  proyecto: string;
  idRelease: number | null;
  release: string | null;
  nombre: string;
  descripcion: string | null;
  totalCasos: number;
  casosEjecutados: number;
  casosPasa: number;
  casosFalla: number;
}

export interface PasoCaso {
  numeroPaso: number;
  accion: string;
  resultadoEsperado: string | null;
}

export interface CasoPrueba {
  idCasoPrueba: number;
  folio: string | null;
  titulo: string;
  precondiciones: string | null;
  resultadoEsperado: string | null;
  tipoPrueba: string;
  idWorkItem: number | null;
  folioWorkItem: string | null;
  pasos: PasoCaso[];
  idEjecucion: number | null;
  idUltimoResultado: number | null;
  ultimoResultado: string | null;
  folioBug: string | null;
}

export interface CicloPrueba {
  idCicloPrueba: number;
  idPlanPrueba: number;
  nombre: string;
  fechaInicio: string | null;
  fechaFin: string | null;
  totalCasos: number;
  ejecutados: number;
  pasa: number;
  falla: number;
  bloqueado: number;
}

export interface Trazabilidad {
  idWorkItem: number;
  folio: string;
  titulo: string;
  totalCasos: number;
  casosPasa: number;
  casosFalla: number;
  sinCobertura: boolean;
}

export const RESULTADOS = [
  { id: 1, nombre: "Pasa", color: "success" as const },
  { id: 2, nombre: "Falla", color: "error" as const },
  { id: 3, nombre: "Bloqueado", color: "warning" as const },
  { id: 4, nombre: "No aplica", color: "default" as const },
];

export async function obtenerPlanes(idProyecto?: number) {
  const params = new URLSearchParams();
  if (idProyecto) params.set("idProyecto", String(idProyecto));
  return obtener<PlanPrueba[]>("/api/v1/planesprueba", params);
}

export async function crearPlan(datos: {
  idProyecto: number;
  nombre: string;
  descripcion: string | null;
  idRelease: number | null;
}) {
  return enviar<PlanPrueba>("post", "/api/v1/planesprueba", datos);
}

export async function obtenerCiclos(idPlan: number) {
  return obtener<CicloPrueba[]>(`/api/v1/planesprueba/${idPlan}/ciclos`);
}

export async function crearCiclo(idPlan: number, nombre: string) {
  return enviar<number>("post", `/api/v1/planesprueba/${idPlan}/ciclos`, { nombre });
}

export async function obtenerCasos(idPlan: number, idCiclo?: number) {
  const params = new URLSearchParams();
  if (idCiclo) params.set("idCiclo", String(idCiclo));
  return obtener<CasoPrueba[]>(`/api/v1/planesprueba/${idPlan}/casos`, params);
}

export async function crearCaso(idPlan: number, datos: {
  titulo: string;
  precondiciones: string | null;
  resultadoEsperado: string | null;
  idTipoPrueba: number;
  idWorkItem: number | null;
  pasos: PasoCaso[];
}) {
  return enviar<number>("post", `/api/v1/planesprueba/${idPlan}/casos`, datos);
}

export async function registrarEjecucion(idCiclo: number, datos: {
  idCasoPrueba: number;
  idResultadoPrueba: number;
  observaciones: string | null;
}) {
  return enviar<number>("post", `/api/v1/ciclos/${idCiclo}/ejecuciones`, datos);
}

export async function crearBugDesdeEjecucion(idEjecucion: number, datos: {
  idPrioridad: number;
  idAsignado: number | null;
}) {
  return enviar<{ idWorkItem: number; folio: string }>(
    "post", `/api/v1/ejecuciones/${idEjecucion}/bug`, datos);
}

export async function obtenerMatriz(idPlan: number) {
  return obtener<Trazabilidad[]>(`/api/v1/planesprueba/${idPlan}/matriz`);
}
