import { enviar, obtener, type ResultadoPaginado } from "./http";

export interface BandejaItem {
  idWorkItem: number;
  folio: string;
  tipo: string;
  titulo: string;
  claveProyecto: string;
  proyecto: string;
  idEstatus: number;
  estatus: string;
  prioridad: string;
  asignado: string | null;
  fechaCompromiso: string | null;
  esVencida: boolean;
  puntosHistoria: number | null;
  minutosPresupuesto: number | null;
  minutosInvertidos: number | null;
  revisionesPendientes: number;
}

export interface AccionDisponible {
  accion: string;
  etiqueta: string;
  requiereMotivo: boolean;
  esAccionPrincipal: boolean;
}

export interface EstatusCambiado {
  idEstatusAnterior: number;
  idEstatusNuevo: number;
  estatus: string;
}

export interface CatalogoItem {
  id: number;
  nombre: string;
}

export interface CatalogosBandeja {
  estatus: CatalogoItem[];
  tipos: CatalogoItem[];
  prioridades: CatalogoItem[];
  proyectos: { id: number; clave: string; nombre: string }[];
  usuarios: CatalogoItem[];
  tiposSolicitud: CatalogoItem[];
}

export interface FiltroBandeja {
  page: number;
  pageSize: number;
  estatus: number[]; // vacio = abiertos; [-1] = todos
  idProyecto: number | null;
  idAsignado: number | null;
  idTipo: number | null;
  texto: string;
  soloVencidas: boolean;
}

export const filtroInicial: FiltroBandeja = {
  page: 1,
  pageSize: 25,
  estatus: [],
  idProyecto: null,
  idAsignado: null,
  idTipo: null,
  texto: "",
  soloVencidas: false,
};

export async function obtenerBandeja(filtro: FiltroBandeja) {
  const params = new URLSearchParams();
  params.set("page", String(filtro.page));
  params.set("pageSize", String(filtro.pageSize));
  filtro.estatus.forEach((e) => params.append("estatus", String(e)));
  if (filtro.idProyecto) params.set("idProyecto", String(filtro.idProyecto));
  if (filtro.idAsignado) params.set("idAsignado", String(filtro.idAsignado));
  if (filtro.idTipo) params.set("idTipo", String(filtro.idTipo));
  if (filtro.texto.trim()) params.set("texto", filtro.texto.trim());
  if (filtro.soloVencidas) params.set("soloVencidas", "true");
  return obtener<ResultadoPaginado<BandejaItem>>("/api/v1/workitems", params);
}

export async function obtenerAcciones(idWorkItem: number) {
  return obtener<AccionDisponible[]>(`/api/v1/workitems/${idWorkItem}/acciones`);
}

export async function cambiarEstatus(idWorkItem: number, accion: string, motivo?: string) {
  return enviar<EstatusCambiado>("put", `/api/v1/workitems/${idWorkItem}/estatus`, {
    accion,
    motivo: motivo || null,
  });
}

export async function registrarTiempo(
  idWorkItem: number,
  datos: { fecha: string; minutos: number; descripcion: string },
) {
  return enviar<number>("post", `/api/v1/workitems/${idWorkItem}/tiempo`, datos);
}

export async function obtenerCatalogosBandeja() {
  return obtener<CatalogosBandeja>("/api/v1/catalogos/bandeja");
}

/** Formato "6h 30m" para presupuesto/invertido. */
export function formatearMinutos(minutos: number | null): string {
  if (minutos === null || minutos === undefined) return "-";
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) return `${resto}m`;
  return resto === 0 ? `${horas}h` : `${horas}h ${resto}m`;
}

export interface WorkItemDetalle {
  idWorkItem: number;
  folio: string;
  tipo: string;
  titulo: string;
  descripcion: string | null;
  criteriosAceptacion: string | null;
  claveProyecto: string;
  proyecto: string;
  esMantenimiento: boolean;
  idEstatus: number;
  estatus: string;
  prioridad: string;
  asignado: string | null;
  solicitante: string | null;
  sprint: string | null;
  puntosHistoria: number | null;
  minutosPresupuesto: number | null;
  minutosInvertidos: number | null;
  fechaCompromiso: string | null;
  fechaInicio: string | null;
  fechaFin: string | null;
  fechaRegistro: string;
  esVencida: boolean;
  revisionesPendientes: number;
}

export interface RegistroTiempo {
  idRegistroTiempo: number;
  fecha: string;
  minutos: number;
  descripcion: string | null;
  usuario: string;
  fechaRegistro: string;
}

export interface NuevoWorkItem {
  idProyecto: number;
  idTipoWorkItem: number;
  titulo: string;
  descripcion: string | null;
  idPrioridad: number;
  idAsignado: number | null;
  fechaCompromiso: string | null;
}

export async function obtenerWorkItem(folio: string) {
  return obtener<WorkItemDetalle>(`/api/v1/workitems/${folio}`);
}

export async function obtenerTiempos(idWorkItem: number) {
  return obtener<RegistroTiempo[]>(`/api/v1/workitems/${idWorkItem}/tiempo`);
}

export async function crearWorkItem(datos: NuevoWorkItem) {
  return enviar<WorkItemDetalle>("post", "/api/v1/workitems", datos);
}

/** Item de Mi Dia: el backend indica la accion que lo pone En Proceso. */
export interface MiDiaItem extends BandejaItem {
  accionInicio: string | null;
  etiquetaAccionInicio: string | null;
}

export interface MiDia {
  usuario: string;
  fecha: string;
  enProceso: MiDiaItem | null;
  vencidas: MiDiaItem[];
  paraHoy: MiDiaItem[];
  proximas: MiDiaItem[];
  minutosHoy: number;
  totalAbiertos: number;
}

export async function obtenerMiDia() {
  return obtener<MiDia>("/api/v1/mi-dia");
}

export interface Revision {
  idRevision: number;
  idWorkItem: number;
  folioWorkItem: string;
  revisor: string;
  comentarios: string | null;
  idEstatus: number;
  estatus: string;
  corregido: boolean;
  fechaCorreccion: string | null;
  fechaRegistro: string;
}

export async function obtenerRevisiones(idWorkItem: number) {
  return obtener<Revision[]>(`/api/v1/workitems/${idWorkItem}/revisiones`);
}

export async function crearRevision(idWorkItem: number, comentarios: string) {
  return enviar<Revision>("post", `/api/v1/workitems/${idWorkItem}/revisiones`, { comentarios });
}

export async function corregirRevision(
  idRevision: number,
  datos: { corregido: boolean; motivo?: string },
) {
  return enviar<Revision>("put", `/api/v1/revisiones/${idRevision}/correccion`, {
    corregido: datos.corregido,
    motivo: datos.motivo || null,
  });
}
