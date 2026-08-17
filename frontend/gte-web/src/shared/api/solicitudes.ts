import { enviar, obtener, type ResultadoPaginado } from "./http";
import type { AccionDisponible } from "./workitems";

export interface ItemGenerado {
  folio: string;
  titulo: string;
  estatus: string;
}

export interface Solicitud {
  idSolicitud: number;
  folio: string | null;
  titulo: string;
  descripcion: string | null;
  idTipoSolicitud: number;
  tipo: string;
  idPrioridad: number;
  prioridad: string;
  idEstatus: number;
  estatus: string;
  solicitante: string;
  idUsuarioSolicitante: number | null;
  usuarioSolicitante: string | null;
  proyecto: string | null;
  idProyecto: number | null;
  fechaDeseada: string | null;
  justificacionNegocio: string | null;
  fechaRegistro: string;
  diasEspera: number;
  itemsGenerados: ItemGenerado[];
}

export interface NuevaSolicitud {
  titulo: string;
  descripcion: string | null;
  idTipoSolicitud: number;
  idPrioridad: number;
  fechaDeseada: string | null;
  justificacionNegocio: string | null;
  idUsuarioSolicitante?: number | null;
}

export interface ItemConversion {
  uiId: string;
  idTipoWorkItem: number;
  titulo: string;
  descripcion: string | null;
  idPrioridad: number;
  idAsignado: number | null;
  fechaCompromiso: string | null;
}

export async function crearSolicitud(datos: NuevaSolicitud) {
  return enviar<Solicitud>("post", "/api/v1/solicitudes", datos);
}

export async function actualizarSolicitud(idSolicitud: number, datos: NuevaSolicitud) {
  return enviar<Solicitud>("put", `/api/v1/solicitudes/${idSolicitud}`, datos);
}

/** Estatus en los que una solicitud todavia admite edicion (Enviada, En analisis, Aprobada). */
export const ESTATUS_SOLICITUD_EDITABLE = [2, 3, 4];

export async function obtenerMisSolicitudes() {
  return obtener<Solicitud[]>("/api/v1/solicitudes/mias");
}

export async function obtenerTriage(
  page: number, pageSize: number, texto: string,
  ordenarPor: string | null = null, ordenDescendente = false,
) {
  const params = new URLSearchParams();
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  if (texto.trim()) params.set("texto", texto.trim());
  if (ordenarPor) {
    params.set("ordenarPor", ordenarPor);
    params.set("ordenDescendente", String(ordenDescendente));
  }
  return obtener<ResultadoPaginado<Solicitud>>("/api/v1/solicitudes", params);
}

export async function obtenerAccionesSolicitud(id: number) {
  return obtener<AccionDisponible[]>(`/api/v1/solicitudes/${id}/acciones`);
}

export async function cambiarEstatusSolicitud(
  id: number,
  datos: { accion: string; motivo?: string; idProyecto?: number },
) {
  return enviar<Solicitud>("put", `/api/v1/solicitudes/${id}/estatus`, {
    accion: datos.accion,
    motivo: datos.motivo || null,
    idProyecto: datos.idProyecto ?? null,
  });
}

export async function convertirSolicitud(id: number, items: ItemConversion[]) {
  return enviar<{ items: { uiId: string; idWorkItem: number; folio: string }[] }>(
    "post",
    `/api/v1/solicitudes/${id}/convertir`,
    { items },
  );
}

/** Colores de chip por estatus de solicitud (contrato de IDs). */
export function colorEstatusSolicitud(
  idEstatus: number,
): "default" | "info" | "warning" | "success" | "error" {
  switch (idEstatus) {
    case 2: return "info";      // Enviada
    case 3: return "warning";   // En Analisis
    case 4: return "success";   // Aprobada
    case 5: return "error";     // Rechazada
    case 6: return "success";   // Convertida
    default: return "default";
  }
}
