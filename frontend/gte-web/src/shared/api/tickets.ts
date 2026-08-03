import { enviar, obtener, type ResultadoPaginado } from "./http";
import type { AccionDisponible } from "./workitems";

export interface Ticket {
  idTicket: number;
  folio: string | null;
  titulo: string;
  descripcion: string | null;
  categoria: string | null;
  prioridad: string;
  idEstatus: number;
  estatus: string;
  idSolicitante: number;
  solicitante: string;
  asignado: string | null;
  sla: string | null;
  fechaLimiteRespuesta: string | null;
  fechaLimiteResolucion: string | null;
  fechaPrimeraRespuesta: string | null;
  fechaResolucion: string | null;
  solucion: string | null;
  minutosSolucion: number | null;
  usuarioSolicitante: string | null;
  locacion: string | null;
  idWorkItemDerivado: number | null;
  folioWorkItemDerivado: string | null;
  fechaRegistro: string;
  calificacion: number | null;
  comentarioEncuesta: string | null;
}

export interface NuevoTicket {
  titulo: string;
  descripcion: string | null;
  idCategoriaTicket: number | null;
  idPrioridad: number;
  idUsuarioSolicitante?: number | null;
  idLocacion?: number | null;
}

export interface FiltroBandejaTickets {
  page: number;
  pageSize: number;
  estatus: number[]; // vacio = abiertos (todos menos Cerrado); [-1] = todos
  texto: string;
  idAsignado: number | null;
}

export const filtroBandejaTicketsInicial: FiltroBandejaTickets = {
  page: 1,
  pageSize: 25,
  estatus: [],
  texto: "",
  idAsignado: null,
};

export async function crearTicket(datos: NuevoTicket) {
  return enviar<Ticket>("post", "/api/v1/tickets", datos);
}

export async function obtenerMisTickets() {
  return obtener<Ticket[]>("/api/v1/tickets/mios");
}

export async function obtenerBandejaTickets(filtro: FiltroBandejaTickets) {
  const params = new URLSearchParams();
  params.set("page", String(filtro.page));
  params.set("pageSize", String(filtro.pageSize));
  filtro.estatus.forEach((e) => params.append("estatus", String(e)));
  if (filtro.texto.trim()) params.set("texto", filtro.texto.trim());
  if (filtro.idAsignado) params.set("idAsignado", String(filtro.idAsignado));
  return obtener<ResultadoPaginado<Ticket>>("/api/v1/tickets", params);
}

export async function obtenerTicketPorFolio(folio: string) {
  return obtener<Ticket>(`/api/v1/tickets/${folio}`);
}

export async function obtenerAccionesTicket(id: number) {
  return obtener<AccionDisponible[]>(`/api/v1/tickets/${id}/acciones`);
}

export async function cambiarEstatusTicket(
  id: number,
  datos: {
    accion: string; motivo?: string; idAsignado?: number;
    solucion?: string; minutosSolucion?: number;
  },
) {
  return enviar<Ticket>("put", `/api/v1/tickets/${id}/estatus`, {
    accion: datos.accion,
    motivo: datos.motivo || null,
    idAsignado: datos.idAsignado ?? null,
    solucion: datos.solucion || null,
    minutosSolucion: datos.minutosSolucion ?? null,
  });
}

export async function escalarTicket(
  id: number,
  datos: { idProyecto: number; idAsignado?: number; fechaCompromiso?: string },
) {
  return enviar<{ idWorkItem: number; folio: string }>("post", `/api/v1/tickets/${id}/escalar`, {
    idProyecto: datos.idProyecto,
    idAsignado: datos.idAsignado ?? null,
    fechaCompromiso: datos.fechaCompromiso || null,
  });
}

export async function registrarEncuestaTicket(id: number, calificacion: number, comentario?: string) {
  return enviar<Ticket>("post", `/api/v1/tickets/${id}/encuesta`, {
    calificacion,
    comentario: comentario || null,
  });
}

/** Colores de chip por estatus de ticket (contrato de IDs, ver EstatusTicket.cs). */
export function colorEstatusTicket(
  idEstatus: number,
): "default" | "info" | "warning" | "success" | "error" {
  switch (idEstatus) {
    case 1: return "default";   // Nuevo
    case 2: return "info";      // Asignado
    case 3: return "warning";   // En Atencion
    case 4: return "warning";   // Esperando Usuario
    case 5: return "success";   // Resuelto
    case 6: return "success";   // Cerrado
    default: return "default";
  }
}
