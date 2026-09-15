import { enviar, obtener } from "./http";
import type { BandejaItem } from "./workitems";

export interface Sprint {
  idSprint: number;
  folio: string | null;
  idLider: number | null;
  lider: string | null;
  nombre: string;
  objetivo: string | null;
  fechaInicio: string;
  fechaFin: string;
  idEstatus: number;
  estatus: string;
  creadoPor: string;
  fechaCreacion: string;
  fechaCierre: string | null;
  totalItems: number;
  itemsTerminados: number;
  puntosComprometidos: number;
  puntosTerminados: number;
}

export interface CapacidadPersona {
  idUsuario: number;
  nombre: string;
  diasLaborables: number;
  diasAusente: number;
  horasPorDia: number;
  horasCapacidad: number;
}

export interface CapacidadSprint {
  idSprint: number;
  horasCapacidad: number;
  horasComprometidas: number;
  personas: CapacidadPersona[];
}

export interface Backlog {
  items: BandejaItem[];
  puntosTotales: number;
}

export interface ColumnaTablero {
  idTableroColumna: number;
  nombre: string;
  idEstatusWorkItem: number;
  orden: number;
  limiteWip: number | null;
  items: BandejaItem[];
}

export interface Tablero {
  idEquipo: number | null;
  equipo: string;
  idSprintActivo: number | null;
  sprintActivo: string | null;
  columnas: ColumnaTablero[];
}

export interface PuntoBurndown {
  fecha: string;
  puntosRestantes: number;
  puntosIdeales: number;
}

export async function obtenerSprints(filtros: {
  idSprint?: number;
  idEstatus?: number;
  idLider?: number;
  soloAbiertos?: boolean;
} = {}) {
  const params = new URLSearchParams();
  if (filtros.idSprint) params.set("idSprint", String(filtros.idSprint));
  if (filtros.idEstatus) params.set("idEstatus", String(filtros.idEstatus));
  if (filtros.idLider) params.set("idLider", String(filtros.idLider));
  params.set("soloAbiertos", String(filtros.soloAbiertos ?? true));
  return obtener<Sprint[]>("/api/v1/sprints", params);
}

export async function obtenerSprint(idSprint: number) {
  return obtener<Sprint>(`/api/v1/sprints/${idSprint}`);
}

export async function crearSprint(datos: {
  nombre: string;
  objetivo: string | null;
  fechaInicio: string;
  fechaFin: string;
  idLider: number | null;
}) {
  return enviar<Sprint>("post", "/api/v1/sprints", datos);
}

export async function editarSprint(idSprint: number, datos: {
  nombre: string;
  objetivo: string | null;
  fechaInicio: string;
  fechaFin: string;
}) {
  return enviar<Sprint>("put", `/api/v1/sprints/${idSprint}`, datos);
}

/** Nulo desasigna al lider. */
export async function asignarLiderSprint(idSprint: number, idLider: number | null) {
  return enviar<Sprint>("put", `/api/v1/sprints/${idSprint}/lider`, { idLider });
}

export async function cambiarEstatusSprint(
  idSprint: number,
  datos: { accion: string; destinoItemsAbiertos?: string },
) {
  return enviar<Sprint>("put", `/api/v1/sprints/${idSprint}/estatus`, {
    accion: datos.accion,
    destinoItemsAbiertos: datos.destinoItemsAbiertos ?? null,
  });
}

export async function obtenerItemsSprint(idSprint: number) {
  return obtener<Backlog>(`/api/v1/sprints/${idSprint}/items`);
}

export async function obtenerCapacidad(idSprint: number) {
  return obtener<CapacidadSprint>(`/api/v1/sprints/${idSprint}/capacidad`);
}

export async function obtenerBurndown(idSprint: number) {
  return obtener<PuntoBurndown[]>(`/api/v1/sprints/${idSprint}/burndown`);
}

export async function obtenerBacklog(idProyecto: number) {
  return obtener<Backlog>(`/api/v1/proyectos/${idProyecto}/backlog`);
}

/** Backlog de todos los proyectos a la vez, para consulta/busqueda (solo lectura). */
export async function obtenerBacklogGlobal(texto?: string) {
  const params = new URLSearchParams();
  if (texto?.trim()) params.set("texto", texto.trim());
  return obtener<Backlog>("/api/v1/backlog", params);
}

export async function reordenarBacklog(idsEnOrden: number[]) {
  return enviar<object>("put", "/api/v1/backlog/orden", { idsEnOrden });
}

export async function asignarSprint(idWorkItem: number, idSprint: number | null) {
  return enviar<object>("put", `/api/v1/workitems/${idWorkItem}/sprint`, { idSprint });
}

/** Sin idEquipo: vista consolidada de todos los equipos y usuarios a la vez. */
export async function obtenerTablero(idEquipo?: number, idAsignado?: number) {
  const params = new URLSearchParams();
  if (idEquipo !== undefined) params.set("idEquipo", String(idEquipo));
  if (idAsignado !== undefined) params.set("idAsignado", String(idAsignado));
  return obtener<Tablero>("/api/v1/tablero", params);
}

export async function moverTarjeta(idWorkItem: number, idEstatusDestino: number) {
  return enviar<{ idEstatusAnterior: number; idEstatusNuevo: number; estatus: string }>(
    "put",
    `/api/v1/workitems/${idWorkItem}/columna`,
    { idEstatusDestino },
  );
}
