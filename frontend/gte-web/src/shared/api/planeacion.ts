import { enviar, obtener } from "./http";
import type { BandejaItem } from "./workitems";

export interface Sprint {
  idSprint: number;
  idEquipo: number;
  equipo: string;
  nombre: string;
  objetivo: string | null;
  fechaInicio: string;
  fechaFin: string;
  idEstatus: number;
  estatus: string;
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
  idEquipo: number;
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

export async function obtenerSprints(idEquipo?: number, soloAbiertos = true) {
  const params = new URLSearchParams();
  if (idEquipo) params.set("idEquipo", String(idEquipo));
  params.set("soloAbiertos", String(soloAbiertos));
  return obtener<Sprint[]>("/api/v1/sprints", params);
}

export async function crearSprint(datos: {
  idEquipo: number;
  nombre: string;
  objetivo: string | null;
  fechaInicio: string;
  fechaFin: string;
}) {
  return enviar<Sprint>("post", "/api/v1/sprints", datos);
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

export async function reordenarBacklog(idsEnOrden: number[]) {
  return enviar<object>("put", "/api/v1/backlog/orden", { idsEnOrden });
}

export async function asignarSprint(idWorkItem: number, idSprint: number | null) {
  return enviar<object>("put", `/api/v1/workitems/${idWorkItem}/sprint`, { idSprint });
}

export async function obtenerTablero(idEquipo: number) {
  return obtener<Tablero>(`/api/v1/equipos/${idEquipo}/tablero`);
}

export async function moverTarjeta(idWorkItem: number, idEstatusDestino: number) {
  return enviar<{ idEstatusAnterior: number; idEstatusNuevo: number; estatus: string }>(
    "put",
    `/api/v1/workitems/${idWorkItem}/columna`,
    { idEstatusDestino },
  );
}
