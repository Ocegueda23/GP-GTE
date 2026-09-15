import { enviar, obtener } from "./http";

export interface ItemRelease {
  idWorkItem: number;
  folio: string;
  titulo: string;
  tipo: string;
  estatus: string;
}

export interface Artefacto {
  idArtefacto: number;
  nombre: string;
  tipo: string;
  idTipoArtefacto: number;
  hashSha256: string | null;
  ordenEjecucion: number | null;
  idArtefactoRollback: number | null;
  nombreRollback: string | null;
  justificacionIrreversible: string | null;
  /** HTML enriquecido (formato, tablas e imagenes) con el instructivo propio del artefacto. */
  instruccionesImplementacion: string | null;
  requiereRollback: boolean;
  cumpleRollback: boolean;
}

/** Respaldo previo al despliegue: la base, el servicio, el sitio o la ubicacion exacta. */
export interface Respaldo {
  idReleaseRespaldo: number;
  idTipoRespaldo: number;
  tipo: string;
  descripcion: string;
}

export interface Aprobacion {
  idAprobacion: number;
  rolAprobacion: string;
  idEstatus: number;
  estatus: string;
  aprobador: string | null;
  comentario: string | null;
  fechaResolucion: string | null;
  firmaHash: string | null;
}

export interface Despliegue {
  idDespliegue: number;
  ambiente: string;
  estatus: string;
  esRollback: boolean;
  ejecutor: string | null;
  fechaInicio: string;
  fechaFin: string | null;
  bitacora: string | null;
}

export interface Release {
  idRelease: number;
  idProyecto: number;
  proyecto: string;
  claveProyecto: string;
  version: string;
  folio: string | null;
  notasVersion: string | null;
  /** HTML enriquecido con el instructivo de despliegue; es lo que se imprime en la Solicitud de despliegue. */
  instruccionesImplementacion: string | null;
  idEstatus: number;
  estatus: string;
  fechaPlan: string | null;
  fechaLiberacion: string | null;
  totalItems: number;
  totalArtefactos: number;
  aprobacionesPendientes: number;
}

export interface ReleaseDetalle extends Release {
  items: ItemRelease[];
  artefactos: Artefacto[];
  respaldos: Respaldo[];
  aprobaciones: Aprobacion[];
  despliegues: Despliegue[];
}

export interface MatrizAmbiente {
  idAmbiente: number;
  ambiente: string;
  claveProyecto: string | null;
  versionDesplegada: string | null;
  fechaDespliegue: string | null;
}

export interface ItemCobertura {
  idWorkItem: number;
  folio: string;
  titulo: string;
}

export interface ProyectoCobertura {
  idProyecto: number;
  claveProyecto: string;
  proyecto: string;
  disponibles: ItemCobertura[];
  bloqueados: ItemCobertura[];
  idReleaseEnPreparacion: number | null;
  versionEnPreparacion: string | null;
  folioReleaseEnPreparacion: string | null;
}

export interface CoberturaReleaseSprint {
  idSprint: number;
  sprint: string;
  proyectos: ProyectoCobertura[];
}

export function colorEstatusRelease(
  idEstatus: number,
): "default" | "info" | "warning" | "success" | "error" {
  switch (idEstatus) {
    case 2: return "warning";   // En Aprobacion
    case 3: return "info";      // Aprobado
    case 4: return "success";   // Liberado
    case 5: return "error";     // Revertido
    default: return "default";
  }
}

export async function obtenerReleases(idProyecto?: number) {
  const params = new URLSearchParams();
  if (idProyecto) params.set("idProyecto", String(idProyecto));
  return obtener<Release[]>("/api/v1/releases", params);
}

export async function obtenerRelease(idRelease: number) {
  return obtener<ReleaseDetalle>(`/api/v1/releases/${idRelease}`);
}

export async function crearRelease(datos: {
  idProyecto: number;
  version: string;
  fechaPlan: string | null;
}) {
  return enviar<ReleaseDetalle>("post", "/api/v1/releases", datos);
}

export async function cambiarEstatusRelease(idRelease: number, accion: string, motivo?: string) {
  return enviar<ReleaseDetalle>("put", `/api/v1/releases/${idRelease}/estatus`, {
    accion,
    motivo: motivo ?? null,
  });
}

export async function agregarContenido(idRelease: number, idsWorkItem: number[]) {
  return enviar<ReleaseDetalle>("post", `/api/v1/releases/${idRelease}/items`, { idsWorkItem });
}

export async function quitarContenido(idRelease: number, idWorkItem: number) {
  const { mensaje } = await enviar<object>(
    "delete", `/api/v1/releases/${idRelease}/items/${idWorkItem}`,
  );
  return { mensaje };
}

export async function agregarArtefacto(idRelease: number, datos: {
  nombre: string;
  idTipoArtefacto: number;
  ordenEjecucion: number | null;
  idArtefactoRollback: number | null;
  justificacionIrreversible: string | null;
  instruccionesImplementacion: string | null;
}) {
  return enviar<number>("post", `/api/v1/releases/${idRelease}/artefactos`, datos);
}

export async function agregarRespaldo(idRelease: number, datos: {
  idTipoRespaldo: number;
  descripcion: string;
}) {
  return enviar<number>("post", `/api/v1/releases/${idRelease}/respaldos`, datos);
}

export async function quitarRespaldo(idRelease: number, idRespaldo: number) {
  const { mensaje } = await enviar<object>(
    "delete", `/api/v1/releases/${idRelease}/respaldos/${idRespaldo}`,
  );
  return { mensaje };
}

export async function actualizarInstrucciones(idRelease: number, html: string | null) {
  return enviar<ReleaseDetalle>("put", `/api/v1/releases/${idRelease}/instrucciones`, {
    instruccionesImplementacion: html,
  });
}

export async function quitarArtefacto(idRelease: number, idArtefacto: number) {
  const { mensaje } = await enviar<object>(
    "delete", `/api/v1/releases/${idRelease}/artefactos/${idArtefacto}`,
  );
  return { mensaje };
}

export async function resolverAprobacion(idAprobacion: number, aprobada: boolean, comentario?: string) {
  return enviar<ReleaseDetalle>("post", `/api/v1/aprobaciones/${idAprobacion}/resolver`, {
    aprobada,
    comentario: comentario ?? null,
  });
}

export async function registrarDespliegue(idRelease: number, datos: {
  idAmbiente: number;
  esRollback: boolean;
  bitacora: string | null;
}) {
  return enviar<ReleaseDetalle>("post", `/api/v1/releases/${idRelease}/despliegues`, {
    ...datos,
    exitoso: true,
  });
}

export async function generarNotas(idRelease: number) {
  return enviar<string>("post", `/api/v1/releases/${idRelease}/notas`);
}

export async function obtenerMatrizAmbientes() {
  return obtener<MatrizAmbiente[]>("/api/v1/ambientes/matriz");
}

export async function obtenerCoberturaReleaseSprint(idSprint: number) {
  return obtener<CoberturaReleaseSprint>(`/api/v1/sprints/${idSprint}/cobertura-release`);
}

export async function enviarSprintARelease(idSprint: number, datos: {
  idProyecto: number;
  idReleaseExistente: number | null;
  versionNueva: string | null;
}) {
  return enviar<ReleaseDetalle>("post", `/api/v1/sprints/${idSprint}/enviar-a-release`, datos);
}

export interface CatalogosEntregas {
  tiposArtefacto: { id: number; nombre: string }[];
  /** Id de "Script SQL": el unico tipo que pide justificacion si no hay reversa (RN-GTE-032). */
  idTipoArtefactoScriptSql: number;
  /** Tipos de respaldo previos al despliegue (base de datos, servicio, sitio, ubicacion). */
  tiposRespaldo: { id: number; nombre: string }[];
}

/**
 * Tipos de artefacto vivos del catalogo dbo.tblTipoArtefacto. Antes el combo los tenia
 * escritos a mano y la pantalla seguia mostrando los cuatro originales aunque el catalogo
 * se editara en Administracion.
 */
export async function obtenerCatalogosEntregas() {
  return obtener<CatalogosEntregas>("/api/v1/catalogos/entregas");
}

export interface CandidatoContenido {
  idWorkItem: number;
  folio: string;
  titulo: string;
  tipo: string;
  /** Hallazgos de revision sin corregir: mayor que cero lo bloquea (RN-GTE-031). */
  hallazgosPendientes: number;
  /** Nulo si el elemento se termino fuera de un sprint. */
  idSprint: number | null;
  sprint: string | null;
}

/**
 * Lo que puede entrar al release, ya ordenado por folio y sin lo que pertenece a otro
 * release. Reemplaza el uso de la bandeja general, que no sabia nada de releases y por eso
 * ofrecia elementos ya entregados en otra version.
 */
export async function obtenerCandidatosContenido(idRelease: number) {
  return obtener<CandidatoContenido[]>(`/api/v1/releases/${idRelease}/candidatos`);
}
