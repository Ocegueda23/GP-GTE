import { enviar, obtener, type ResultadoPaginado } from "./http";
import type { Ticket } from "./tickets";
import type { Incidente } from "./incidentes";
import type { Solicitud } from "./solicitudes";
import type { Release } from "./entregas";

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
  complejidad: string | null;
  idAsignado: number | null;
  asignado: string | null;
  idSprint: number | null;
  sprint: string | null;
  folioSprint: string | null;
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

/** IdCategoriaProyecto nulo = la complejidad aplica a cualquier categoria de proyecto. */
export interface ComplejidadItem extends CatalogoItem {
  idCategoriaProyecto: number | null;
}

export interface CatalogosBandeja {
  estatus: CatalogoItem[];
  tipos: CatalogoItem[];
  prioridades: CatalogoItem[];
  proyectos: {
    id: number; clave: string; nombre: string; idCategoriaProyecto: number;
    categoriaProyecto: string; idEquipo: number | null;
  }[];
  usuarios: CatalogoItem[];
  tiposSolicitud: CatalogoItem[];
  equipos: CatalogoItem[];
  complejidades: ComplejidadItem[];
  categoriasTicket: CatalogoItem[];
  /** Categorias de incidente con el nivel que las atiende; ya vienen ordenadas por nivel. */
  categoriasIncidente: { id: number; nombre: string; nivel: string }[];
  estatusTicket: CatalogoItem[];
  severidades: CatalogoItem[];
  usuariosSolicitantes: CatalogoItem[];
  locaciones: CatalogoItem[];
  sprints: CatalogoItem[];
}

export interface FiltroBandeja {
  page: number;
  pageSize: number;
  estatus: number[]; // vacio = abiertos; [-1] = todos (default de la UI)
  idProyecto: number | null;
  idAsignado: number | null;
  idTipo: number | null;
  idSprint: number | null; // -1 = Backlog (sin sprint)
  texto: string;
  soloVencidas: boolean;
  ordenarPor: string | null;
  ordenDescendente: boolean;
}

export const filtroInicial: FiltroBandeja = {
  page: 1,
  pageSize: 25,
  estatus: [-1],
  idProyecto: null,
  idAsignado: null,
  idTipo: null,
  idSprint: null,
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
  if (filtro.idSprint) params.set("idSprint", String(filtro.idSprint));
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

/** Colores de chip por estatus de work item (contrato de IDs del motor). */
export function colorEstatus(idEstatus: number): "default" | "success" | "info" | "warning" | "error" {
  switch (idEstatus) {
    case 2: return "success";   // En Proceso
    case 3: return "info";      // En Pruebas
    case 4: return "warning";   // Correccion
    case 7: return "error";     // Cancelado
    default: return "default";
  }
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
  idCategoriaProyecto: number;
  esMantenimiento: boolean;
  idEstatus: number;
  estatus: string;
  idPrioridad: number;
  prioridad: string;
  idComplejidad: number | null;
  complejidad: string | null;
  idAsignado: number | null;
  asignado: string | null;
  solicitante: string | null;
  usuarioSolicitante: string | null;
  idSprint: number | null;
  sprint: string | null;
  folioSprint: string | null;
  puntosHistoria: number | null;
  minutosPresupuesto: number | null;
  minutosInvertidos: number | null;
  fechaCompromiso: string | null;
  fechaInicio: string | null;
  fechaFin: string | null;
  fechaRegistro: string;
  esVencida: boolean;
  revisionesPendientes: number;
  /** Tarea padre si este elemento es una subtarea; nulo si es de primer nivel. */
  idPadre: number | null;
  folioPadre: string | null;
  tituloPadre: string | null;
}

export interface WorkItemEditar {
  titulo: string;
  descripcion: string | null;
  criteriosAceptacion: string | null;
  idPrioridad: number;
  idComplejidad: number | null;
  idAsignado: number | null;
  fechaCompromiso: string | null;
  idSprint: number | null;
}

export interface RegistroTiempo {
  idRegistroTiempo: number;
  fecha: string;
  minutos: number;
  descripcion: string | null;
  usuario: string;
  fechaRegistro: string;
  /** Folio de la subtarea de origen; null cuando el registro es del propio elemento consultado. */
  folioOrigen: string | null;
}

export interface NuevoWorkItem {
  idProyecto: number;
  idTipoWorkItem: number;
  titulo: string;
  descripcion: string | null;
  idPrioridad: number;
  idComplejidad: number;
  idAsignado: number | null;
  fechaCompromiso: string | null;
  idSprint: number | null;
  idPadre?: number;
}

/** Vista previa del presupuesto por complejidad (RN-GTE-015); el valor real lo congela el backend. */
export interface PresupuestoEstimado {
  idComplejidad: number;
  complejidad: string;
  idAsignado: number | null;
  idNivel: number | null;
  nivel: string | null;
  minutos: number | null;
  puntos: number | null;
  niveles: { idNivel: number; nivel: string; minutos: number; puntos: number | null }[];
}

export async function obtenerPresupuestoEstimado(idComplejidad: number, idAsignado: number | null) {
  const params = new URLSearchParams({ idComplejidad: String(idComplejidad) });
  if (idAsignado !== null) params.set("idAsignado", String(idAsignado));
  return obtener<PresupuestoEstimado>("/api/v1/workitems/presupuesto", params);
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
  ticketsAsignados: Ticket[];
  incidentesRelevantes: Incidente[];
  solicitudesPendientes: Solicitud[];
  triagePendientes: number;
  releasesRelevantes: Release[];
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
  /** Cerrado como "No es un error" en vez de arreglado; la razon va en motivoDescarte. */
  esFalsoPositivo: boolean;
  motivoDescarte: string | null;
  fechaCorreccion: string | null;
  fechaRegistro: string;
  idSeveridad: number | null;
  severidad: string | null;
  bloqueante: boolean;
  idEjecucionPrueba: number | null;
  casoPrueba: string | null;
}

export async function obtenerRevisiones(idWorkItem: number) {
  return obtener<Revision[]>(`/api/v1/workitems/${idWorkItem}/revisiones`);
}

export async function crearRevision(idWorkItem: number, datos: { comentarios: string; idSeveridad: number }) {
  return enviar<Revision>("post", `/api/v1/workitems/${idWorkItem}/revisiones`, datos);
}

export async function corregirRevision(
  idRevision: number,
  datos: { corregido: boolean; motivo?: string; esFalsoPositivo?: boolean },
) {
  return enviar<Revision>("put", `/api/v1/revisiones/${idRevision}/correccion`, {
    corregido: datos.corregido,
    motivo: datos.motivo || null,
    esFalsoPositivo: datos.esFalsoPositivo ?? false,
  });
}

/**
 * Las complejidades del catalogo pertenecen a una categoria de proyecto (tblComplejidad
 * .IdCategoriaProyecto); las de categoria nula aplican a cualquiera. Se filtra por la
 * categoria del proyecto del elemento para no ofrecer complejidades de otra categoria.
 * Sin categoria conocida se devuelve el catalogo completo.
 */
export function filtrarComplejidades(
  complejidades: ComplejidadItem[] | undefined,
  idCategoriaProyecto: number | null | undefined,
): ComplejidadItem[] {
  const lista = complejidades ?? [];
  if (idCategoriaProyecto == null) return lista;
  return lista.filter(
    (c) => c.idCategoriaProyecto == null || c.idCategoriaProyecto === idCategoriaProyecto,
  );
}
