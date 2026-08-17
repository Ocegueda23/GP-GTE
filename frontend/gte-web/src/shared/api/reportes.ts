import { http, obtener, type ResultadoPaginado } from "./http";

export interface ActividadDetalle {
  idRegistroTiempo: number;
  idWorkItem: number;
  folio: string;
  titulo: string;
  minutos: number;
  descripcion: string | null;
}

export interface ActividadDia {
  fecha: string;
  minutosDia: number;
  registros: ActividadDetalle[];
}

export interface ActividadUsuario {
  idUsuario: number;
  usuario: string;
  fechaInicio: string;
  fechaFin: string;
  minutosTotales: number;
  dias: ActividadDia[];
}

export async function obtenerActividadUsuario(idUsuario: number, fechaInicio: string, fechaFin: string) {
  const params = new URLSearchParams({ idUsuario: String(idUsuario), fechaInicio, fechaFin });
  return obtener<ActividadUsuario>("/api/v1/reportes/actividad-usuario", params);
}

function armarParams(filtro: Record<string, string | number | null | undefined>) {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtro)) {
    if (valor !== null && valor !== undefined && valor !== "") {
      params.set(clave, String(valor));
    }
  }
  return params;
}

/** Descarga el .xlsx generado por el back y dispara el guardado en el navegador. */
export async function descargarReporteExcel(
  ruta: string, filtro: Record<string, string | number | null | undefined>, nombreArchivo: string,
) {
  const { data } = await http.get<Blob>(`/api/v1/reportes/${ruta}/exportar`, {
    params: armarParams(filtro), responseType: "blob",
  });
  const url = URL.createObjectURL(data);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.download = nombreArchivo;
  enlace.click();
  URL.revokeObjectURL(url);
}

// ---------- R01 Productividad ----------
export interface ProductividadPersona {
  idUsuario: number;
  usuario: string;
  itemsTerminados: number;
  puntosTotales: number;
  porcentajeATiempo: number;
  eficienciaPorcentaje: number | null;
}

export interface ProductividadReporte {
  desde: string;
  hasta: string;
  personas: ProductividadPersona[];
}

export async function obtenerReporteProductividad(desde: string, hasta: string, idProyecto?: number | null, idEquipo?: number | null) {
  return obtener<ProductividadReporte>("/api/v1/reportes/productividad", armarParams({ desde, hasta, idProyecto, idEquipo }));
}

// ---------- R02 Horas registradas ----------
export interface HorasDia {
  fecha: string;
  minutos: number;
  esAusencia: boolean;
}

export interface HorasUsuario {
  idUsuario: number;
  usuario: string;
  minutosTotales: number;
  dias: HorasDia[];
}

export interface HorasRegistradasReporte {
  desde: string;
  hasta: string;
  usuarios: HorasUsuario[];
}

export async function obtenerReporteHorasRegistradas(desde: string, hasta: string, idEquipo?: number | null) {
  return obtener<HorasRegistradasReporte>("/api/v1/reportes/horas-registradas", armarParams({ desde, hasta, idEquipo }));
}

// ---------- R03 Retrabajo ----------
export interface RetrabajoDetalle {
  idUsuario: number;
  usuario: string;
  idProyecto: number;
  proyecto: string;
  minutosCorreccion: number;
  minutosTotales: number;
  porcentaje: number;
}

export interface RetrabajoReporte {
  desde: string;
  hasta: string;
  reaperturasSinDatos: boolean;
  detalle: RetrabajoDetalle[];
}

export async function obtenerReporteRetrabajo(desde: string, hasta: string, idProyecto?: number | null) {
  return obtener<RetrabajoReporte>("/api/v1/reportes/retrabajo", armarParams({ desde, hasta, idProyecto }));
}

// ---------- R04 Bugs y defectos ----------
export interface BugsProyecto {
  idProyecto: number;
  proyecto: string;
  totalBugs: number;
  totalItems: number;
  densidadPorcentaje: number;
  agingPromedioDias: number;
  escapados: number;
  tasaEscapePorcentaje: number;
}

export interface BugsDefectosReporte {
  desde: string;
  hasta: string;
  proyectos: BugsProyecto[];
}

export async function obtenerReporteBugsDefectos(desde: string, hasta: string, idProyecto?: number | null) {
  return obtener<BugsDefectosReporte>("/api/v1/reportes/bugs-defectos", armarParams({ desde, hasta, idProyecto }));
}

// ---------- R05 Releases ----------
export interface ReleaseReporteItem {
  idRelease: number;
  folio: string | null;
  version: string;
  idProyecto: number;
  proyecto: string;
  fechaLiberacion: string | null;
  diasAprobacion: number | null;
  itemsIncluidos: number;
}

export interface ReleasesReporte {
  desde: string;
  hasta: string;
  totalReleases: number;
  frecuenciaPorSemana: number;
  releases: ReleaseReporteItem[];
}

export async function obtenerReporteReleases(desde: string, hasta: string, idProyecto?: number | null) {
  return obtener<ReleasesReporte>("/api/v1/reportes/releases", armarParams({ desde, hasta, idProyecto }));
}

// ---------- R06 Riesgos ----------
export interface RiesgoReporteItem {
  idRiesgo: number;
  idProyecto: number;
  proyecto: string;
  descripcion: string;
  probabilidad: number;
  impacto: number;
  exposicion: number;
  estatus: string;
}

export interface RiesgosReporte {
  totalExpuestos: number;
  totalMitigados: number;
  totalMaterializados: number;
  totalCerrados: number;
  riesgos: RiesgoReporteItem[];
}

export async function obtenerReporteRiesgos(idProyecto?: number | null) {
  return obtener<RiesgosReporte>("/api/v1/reportes/riesgos", armarParams({ idProyecto }));
}

// ---------- R07 Solicitantes ----------
export interface SolicitudesArea {
  area: string;
  totalSolicitudes: number;
  tiempoTriagePromedioDias: number | null;
  tiempoEntregaPromedioDias: number | null;
}

export interface SolicitantesReporte {
  desde: string;
  hasta: string;
  satisfaccionSinDatos: boolean;
  porArea: SolicitudesArea[];
}

export async function obtenerReporteSolicitantes(desde: string, hasta: string) {
  return obtener<SolicitantesReporte>("/api/v1/reportes/solicitantes", armarParams({ desde, hasta }));
}

// ---------- R08 Costos ----------
export interface CostoProyectoMes {
  idProyecto: number;
  proyecto: string;
  mes: number;
  horasReales: number;
  costoReal: number;
}

export interface CostoDesarrollador {
  idUsuario: number;
  usuario: string;
  horasReales: number;
  costoReal: number;
}

export interface CostosReporte {
  anio: number;
  porProyectoMes: CostoProyectoMes[];
  porDesarrollador: CostoDesarrollador[];
}

export async function obtenerReporteCostos(anio: number, idProyecto?: number | null) {
  return obtener<CostosReporte>("/api/v1/reportes/costos", armarParams({ anio, idProyecto }));
}

// ---------- R09 Rentabilidad ----------
export interface RentabilidadProyecto {
  idProyecto: number;
  proyecto: string;
  montoAutorizado: number | null;
  costoReal: number;
  porcentajeConsumido: number | null;
  semaforo: string;
}

export interface RentabilidadReporte {
  anio: number;
  proyectos: RentabilidadProyecto[];
}

export async function obtenerReporteRentabilidad(anio: number) {
  return obtener<RentabilidadReporte>("/api/v1/reportes/rentabilidad", armarParams({ anio }));
}

// ---------- R10 SLA ----------
export interface SlaPrioridad {
  prioridad: string;
  totalTickets: number;
  dentroSla: number;
  cumplimientoPorcentaje: number;
}

export interface SlaAgente {
  idUsuario: number;
  usuario: string;
  totalTickets: number;
  dentroSla: number;
  cumplimientoPorcentaje: number;
  incumplimientos: number;
}

export interface SlaReporte {
  desde: string;
  hasta: string;
  csat: number | null;
  porPrioridad: SlaPrioridad[];
  porAgente: SlaAgente[];
}

export async function obtenerReporteSla(desde: string, hasta: string) {
  return obtener<SlaReporte>("/api/v1/reportes/sla", armarParams({ desde, hasta }));
}

// ---------- R11 KPIs / DORA ----------
export interface PuntoKpi {
  fecha: string;
  valor: number;
}

export interface KpiHistorico {
  clave: string;
  nombre: string;
  serieAnioActual: PuntoKpi[];
  serieAnioComparativo: PuntoKpi[];
}

export interface KpisHistoricosReporte {
  anio: number;
  anioComparativo: number | null;
  kpis: KpiHistorico[];
}

export async function obtenerReporteKpisHistoricos(anio: number, anioComparativo?: number | null) {
  return obtener<KpisHistoricosReporte>("/api/v1/reportes/kpis-historicos", armarParams({ anio, anioComparativo }));
}

// ---------- R12 Carga de trabajo ----------
export interface CargaTrabajoPersona {
  idUsuario: number;
  usuario: string;
  wip: number;
  porcentajeOcupacion: number | null;
}

export interface CargaTrabajoReporte {
  personas: CargaTrabajoPersona[];
}

export async function obtenerReporteCargaTrabajo(idEquipo?: number | null) {
  return obtener<CargaTrabajoReporte>("/api/v1/reportes/carga-trabajo", armarParams({ idEquipo }));
}

// ---------- R13 Flujo (CFD) ----------
export interface PuntoFlujo {
  fecha: string;
  conteoPorEstatus: Record<string, number>;
}

export interface FlujoReporte {
  idProyecto: number;
  proyecto: string;
  desde: string;
  hasta: string;
  estatus: string[];
  puntos: PuntoFlujo[];
}

export async function obtenerReporteFlujo(desde: string, hasta: string, idProyecto: number) {
  return obtener<FlujoReporte>("/api/v1/reportes/flujo", armarParams({ desde, hasta, idProyecto }));
}

// ---------- R14 Auditoria ----------
export interface AuditoriaItem {
  idBitacora: number;
  usuario: string;
  entidad: string | null;
  idEntidad: number | null;
  accion: string;
  detalle: string | null;
  fecha: string;
}

export async function obtenerReporteAuditoria(filtro: {
  desde?: string | null; hasta?: string | null; usuario?: string | null; entidad?: string | null;
  page: number; pageSize: number;
}) {
  return obtener<ResultadoPaginado<AuditoriaItem>>("/api/v1/reportes/auditoria", armarParams(filtro));
}
