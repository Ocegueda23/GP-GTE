import { enviar, obtener, type ResultadoPaginado } from "./http";
import type { AccionDisponible } from "./workitems";

export interface Ausencia {
  idAusencia: number;
  idUsuario: number;
  usuario: string;
  idTipoAusencia: number;
  tipo: string;
  idEstatus: number;
  estatus: string;
  /** Fecha sin hora (yyyy-MM-dd). */
  fechaInicio: string;
  fechaFin: string;
  diasNaturales: number;
  motivo: string | null;
  fechaRegistro: string;
}

export interface NuevaAusencia {
  idTipoAusencia: number;
  fechaInicio: string;
  fechaFin: string;
  motivo: string | null;
  /** Registrar a nombre de otra persona; exige ADM.Ausencias. */
  idUsuario?: number | null;
}

export interface OpcionAusencia {
  id: number;
  nombre: string;
}

export interface CatalogosAusencia {
  tipos: OpcionAusencia[];
  estatus: OpcionAusencia[];
}

/** Contrato de IDs de dbo.tblEstatusAusencia (GTE.Domain.Ausencias.EstatusAusencia). */
export const ESTATUS_AUSENCIA = {
  solicitada: 1,
  aprobada: 2,
  rechazada: 3,
  cancelada: 4,
} as const;

export async function crearAusencia(datos: NuevaAusencia) {
  return enviar<Ausencia>("post", "/api/v1/ausencias", datos);
}

export async function actualizarAusencia(
  idAusencia: number,
  datos: Omit<NuevaAusencia, "idUsuario">,
) {
  return enviar<Ausencia>("put", `/api/v1/ausencias/${idAusencia}`, datos);
}

/** Sin estatus = vigentes (Solicitada, Aprobada); [-1] = todas. */
export async function obtenerMisAusencias(estatus: number[] = []) {
  const params = new URLSearchParams();
  estatus.forEach((e) => params.append("estatus", String(e)));
  return obtener<Ausencia[]>("/api/v1/ausencias/mias", params);
}

export interface FiltroBandejaAusencias {
  page?: number;
  pageSize?: number;
  estatus?: number[];
  idUsuario?: number | null;
  idTipoAusencia?: number | null;
  desde?: string | null;
  hasta?: string | null;
  ordenarPor?: string | null;
  ordenDescendente?: boolean;
}

/** Bandeja de aprobacion (ADM.Ausencias). Sin estatus = pendientes; [-1] = todas. */
export async function obtenerBandejaAusencias(filtro: FiltroBandejaAusencias = {}) {
  const params = new URLSearchParams();
  params.set("page", String(filtro.page ?? 1));
  params.set("pageSize", String(filtro.pageSize ?? 50));
  (filtro.estatus ?? []).forEach((e) => params.append("estatus", String(e)));
  if (filtro.idUsuario) params.set("idUsuario", String(filtro.idUsuario));
  if (filtro.idTipoAusencia) params.set("idTipoAusencia", String(filtro.idTipoAusencia));
  if (filtro.desde) params.set("desde", filtro.desde);
  if (filtro.hasta) params.set("hasta", filtro.hasta);
  if (filtro.ordenarPor) {
    params.set("ordenarPor", filtro.ordenarPor);
    params.set("ordenDescendente", String(filtro.ordenDescendente ?? false));
  }
  return obtener<ResultadoPaginado<Ausencia>>("/api/v1/ausencias", params);
}

export async function obtenerCatalogosAusencia() {
  return obtener<CatalogosAusencia>("/api/v1/ausencias/catalogos");
}

export async function contarAusenciasPendientes() {
  return obtener<number>("/api/v1/ausencias/pendientes/conteo");
}

export async function obtenerAccionesAusencia(idAusencia: number) {
  return obtener<AccionDisponible[]>(`/api/v1/ausencias/${idAusencia}/acciones`);
}

export async function cambiarEstatusAusencia(
  idAusencia: number,
  datos: { accion: string; motivo?: string },
) {
  return enviar<Ausencia>("put", `/api/v1/ausencias/${idAusencia}/estatus`, {
    accion: datos.accion,
    motivo: datos.motivo || null,
  });
}

/** Colores de chip por estatus de ausencia (contrato de IDs). */
export function colorEstatusAusencia(
  idEstatus: number,
): "default" | "info" | "warning" | "success" | "error" {
  switch (idEstatus) {
    case ESTATUS_AUSENCIA.solicitada: return "warning";
    case ESTATUS_AUSENCIA.aprobada: return "success";
    case ESTATUS_AUSENCIA.rechazada: return "error";
    default: return "default";
  }
}

/** Detalle estructurado del 409 de traslape (GTE.Domain.Ausencias.TraslapeAusencia). */
export interface TraslapeAusencia {
  idAusencia: number;
  tipo: string;
  estatus: string;
  fechaInicio: string;
  fechaFin: string;
}
