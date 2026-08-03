import { enviar, obtener, type ResultadoPaginado } from "./http";
import type { AccionDisponible } from "./workitems";
import type { Release } from "./entregas";

export interface Incidente {
  idIncidente: number;
  folio: string | null;
  titulo: string;
  descripcion: string | null;
  idProyecto: number;
  proyecto: string;
  idSeveridad: number;
  severidad: string;
  idEstatus: number;
  estatus: string;
  fechaOcurrencia: string;
  fechaDeteccion: string | null;
  fechaResolucion: string | null;
  minutosIndisponibilidad: number | null;
  causaRaiz: string | null;
  idWorkItemCorrectivo: number | null;
  folioWorkItemCorrectivo: string | null;
  idReleaseCausante: number | null;
  versionReleaseCausante: string | null;
  fechaRegistro: string;
}

export interface NuevoIncidente {
  idProyecto: number;
  idSeveridad: number;
  titulo: string;
  descripcion: string | null;
  fechaOcurrencia: string;
  fechaDeteccion: string | null;
}

export interface ActualizarIncidente {
  titulo: string;
  descripcion: string | null;
  causaRaiz: string | null;
  minutosIndisponibilidad: number | null;
  fechaDeteccion: string | null;
}

export interface FiltroBandejaIncidentes {
  page: number;
  pageSize: number;
  estatus: number[]; // vacio = abiertos (todos menos Cerrado); [-1] = todos
  idSeveridad: number | null;
  idProyecto: number | null;
  texto: string;
}

export const filtroBandejaIncidentesInicial: FiltroBandejaIncidentes = {
  page: 1,
  pageSize: 25,
  estatus: [],
  idSeveridad: null,
  idProyecto: null,
  texto: "",
};

export async function crearIncidente(datos: NuevoIncidente) {
  return enviar<Incidente>("post", "/api/v1/incidentes", datos);
}

export async function obtenerBandejaIncidentes(filtro: FiltroBandejaIncidentes) {
  const params = new URLSearchParams();
  params.set("page", String(filtro.page));
  params.set("pageSize", String(filtro.pageSize));
  filtro.estatus.forEach((e) => params.append("estatus", String(e)));
  if (filtro.idSeveridad) params.set("idSeveridad", String(filtro.idSeveridad));
  if (filtro.idProyecto) params.set("idProyecto", String(filtro.idProyecto));
  if (filtro.texto.trim()) params.set("texto", filtro.texto.trim());
  return obtener<ResultadoPaginado<Incidente>>("/api/v1/incidentes", params);
}

export async function obtenerIncidentePorFolio(folio: string) {
  return obtener<Incidente>(`/api/v1/incidentes/${folio}`);
}

export async function obtenerAccionesIncidente(id: number) {
  return obtener<AccionDisponible[]>(`/api/v1/incidentes/${id}/acciones`);
}

export async function actualizarIncidente(id: number, datos: ActualizarIncidente) {
  return enviar<Incidente>("put", `/api/v1/incidentes/${id}`, datos);
}

export async function cambiarEstatusIncidente(id: number, accion: string, motivo?: string) {
  return enviar<Incidente>("put", `/api/v1/incidentes/${id}/estatus`, {
    accion,
    motivo: motivo || null,
  });
}

export async function cambiarSeveridadIncidente(id: number, idSeveridad: number, motivo: string) {
  return enviar<Incidente>("put", `/api/v1/incidentes/${id}/severidad`, { idSeveridad, motivo });
}

export async function vincularCorrectivo(
  id: number,
  datos: { idPrioridad: number; idAsignado?: number; fechaCompromiso?: string },
) {
  return enviar<{ idWorkItem: number; folio: string }>("post", `/api/v1/incidentes/${id}/correctivo`, {
    idPrioridad: datos.idPrioridad,
    idAsignado: datos.idAsignado ?? null,
    fechaCompromiso: datos.fechaCompromiso || null,
  });
}

export async function vincularReleaseCausante(id: number, idRelease: number) {
  return enviar<Incidente>("post", `/api/v1/incidentes/${id}/release-causante`, { idRelease });
}

/** Releases del proyecto (incluye liberados/revertidos: un causante casi siempre ya se desplego). */
export async function obtenerReleasesParaVincular(idProyecto: number) {
  const params = new URLSearchParams();
  params.set("idProyecto", String(idProyecto));
  params.set("soloAbiertos", "false");
  return obtener<Release[]>("/api/v1/releases", params);
}

/** Colores de chip por estatus de incidente (contrato de IDs, ver EstatusIncidente.cs). */
export function colorEstatusIncidente(
  idEstatus: number,
): "default" | "info" | "warning" | "success" | "error" {
  switch (idEstatus) {
    case 1: return "error";     // Detectado
    case 2: return "warning";   // En Atencion
    case 3: return "warning";   // Mitigado
    case 4: return "success";   // Resuelto
    case 5: return "success";   // Cerrado
    default: return "default";
  }
}

/** Colores de chip por severidad (1 S1-Critica .. 4 S4-Baja). */
export function colorSeveridad(idSeveridad: number): "error" | "warning" | "info" | "default" {
  switch (idSeveridad) {
    case 1: return "error";
    case 2: return "warning";
    case 3: return "info";
    default: return "default";
  }
}
