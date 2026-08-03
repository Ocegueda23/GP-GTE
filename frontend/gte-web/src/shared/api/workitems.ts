import { enviar, obtener, type ResultadoPaginado } from "./http";

export interface BandejaItem {
  idWorkItem: number;
  folio: string;
  tipo: string;
  titulo: string;
  idProyecto: number;
  claveProyecto: string;
  proyecto: string;
  idEstatus: number;
  estatus: string;
  prioridad: string;
  idAsignado: number | null;
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
  equipos: CatalogoItem[];
  complejidades: CatalogoItem[];
  categoriasTicket: CatalogoItem[];
  estatusTicket: CatalogoItem[];
  severidades: CatalogoItem[];
  usuariosSolicitantes: CatalogoItem[];
  locaciones: CatalogoItem[];
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
  ordenarPor: string | null;
  ordenDescendente: boolean;
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
  ordenarPor: null,
  ordenDescendente: false,
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
  if (filtro.ordenarPor) params.set("ordenarPor", filtro.ordenarPor);
  if (filtro.ordenDescendente) params.set("ordenDescendente", "true");
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
  idProyecto: number;
  claveProyecto: string;
  proyecto: string;
  esMantenimiento: boolean;
  idEstatus: number;
  estatus: string;
  idPrioridad: number;
  prioridad: string;
  idComplejidad: number | null;
  idAsignado: number | null;
  asignado: string | null;
  solicitante: string | null;
  usuarioSolicitante: string | null;
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

export interface WorkItemEditar {
  titulo: string;
  descripcion: string | null;
  criteriosAceptacion: string | null;
  idPrioridad: number;
  idComplejidad: number | null;
  idAsignado: number | null;
  fechaCompromiso: string | null;
  puntosHistoria: number | null;
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
  idPadre?: number;
}

export async function obtenerWorkItem(folio: string) {
  return obtener<WorkItemDetalle>(`/api/v1/workitems/${folio}`);
}

export async function obtenerTiempos(idWorkItem: number) {
  return obtener<RegistroTiempo[]>(`/api/v1/workitems/${idWorkItem}/tiempo`);
}

/** MinutosRegistrados es la suma directa de tblRegistroTiempo (no el
 * "Invertido" del padre, que sale de tblHistorialEstatus y la migracion del
 * GT nunca llena para los hijos). */
export interface WorkItemHijo {
  idWorkItem: number;
  folio: string;
  titulo: string;
  idEstatus: number;
  estatus: string;
  asignado: string | null;
  minutosRegistrados: number;
}

export async function obtenerHijos(idWorkItem: number) {
  return obtener<WorkItemHijo[]>(`/api/v1/workitems/${idWorkItem}/hijos`);
}

export async function crearWorkItem(datos: NuevoWorkItem) {
  return enviar<WorkItemDetalle>("post", "/api/v1/workitems", datos);
}

export async function actualizarWorkItem(idWorkItem: number, datos: WorkItemEditar) {
  return enviar<WorkItemDetalle>("put", `/api/v1/workitems/${idWorkItem}`, datos);
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
