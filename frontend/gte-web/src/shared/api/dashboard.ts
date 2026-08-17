import { obtener } from "./http";

export interface KpiEstado {
  estado: string;
  total: number;
  totalMesAnterior: number;
  variacionPorcentaje: number | null;
}

export interface KpiGrupo {
  grupo: string;
  estados: KpiEstado[];
}

export interface EmpleadoResumen {
  idUsuario: number;
  nombre: string;
  area: string | null;
  puesto: string | null;
  urlFoto: string | null;
}

export interface EmpleadoDelMes {
  empleado: EmpleadoResumen | null;
  anio: number;
  mes: number;
  puntaje: number | null;
  motivo: string;
}

export interface CargaTrabajoEmpleado {
  idUsuario: number;
  nombre: string;
  area: string | null;
  proyectoPrincipal: string | null;
  horasAsignadas: number;
  horasConsumidas: number;
  horasDisponibles: number | null;
  porcentajeUtilizacion: number | null;
}

export interface CargaTrabajoDetalle {
  idUsuario: number;
  nombre: string;
  pendiente: number;
  enProceso: number;
  terminado: number;
  retrasos: number;
  promedioDuracionDias: number | null;
  total: number;
  eficienciaEntrega: number | null;
}

export interface PuntajeDimension {
  dimension: string;
  valor: number | null;
}

export interface PuntoPuntajeMensual {
  mes: number;
  puntaje: number | null;
}

export interface IndicadoresEmpleado {
  empleado: EmpleadoResumen;
  anio: number;
  mes: number;
  horasEstimadas: number;
  horasReales: number;
  indiceEficiencia: number | null;
  itemsTerminados: number;
  promedioTerminadosEquipo: number;
  entregasATiempo: number;
  entregasRetrasadas: number;
  porcentajeCumplimientoEntregas: number | null;
  evaluacion: PuntajeDimension[];
  puntajeMensual: number | null;
  evolucionPuntajeAnio: PuntoPuntajeMensual[];
}

export interface RankingItem {
  idUsuario: number;
  nombre: string;
  area: string | null;
  valor: number;
}

export interface Ranking {
  metrica: string;
  top10: RankingItem[];
  bottom10: RankingItem[];
}

export interface ComparativoBarra {
  etiqueta: string;
  valor: number;
}

export interface Comparativos {
  empleadoVsPromedioEquipo: ComparativoBarra[];
  porArea: ComparativoBarra[];
  porProyecto: ComparativoBarra[];
  anioActualVsAnterior: ComparativoBarra[];
}

export interface PuntoTendencia {
  anio: number;
  mes: number;
  valor: number;
}

export interface Tendencia {
  metrica: string;
  puntos: PuntoTendencia[];
}

export interface ProyectoItem {
  id: number;
  clave: string;
  nombre: string;
  categoriaProyecto: string;
}

export interface CatalogoItem {
  id: number;
  nombre: string;
}

export interface FiltroCatalogosDashboard {
  proyectos: ProyectoItem[];
  areas: CatalogoItem[];
  empleados: CatalogoItem[];
  alcanceVisibilidad: string;
}

export interface DashboardData {
  anio: number;
  mes: number;
  alcanceVisibilidad: string;
  empleadoDelMes: EmpleadoDelMes | null;
  historicoEmpleadoDelMes: EmpleadoDelMes[];
  resumenEjecutivo: KpiGrupo[];
  cargaTrabajo: CargaTrabajoEmpleado[];
  desgloseCargaTrabajo: CargaTrabajoDetalle[];
  rankings: Ranking[];
  comparativos: Comparativos;
}

export interface FiltroDashboard {
  [clave: string]: string | number | null | undefined;
  anio: number;
  mes: number;
  idProyecto?: number | null;
  idArea?: number | null;
  idUsuario?: number | null;
}

function armarParams(filtro: Record<string, string | number | null | undefined>) {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtro)) {
    if (valor !== null && valor !== undefined) {
      params.set(clave, String(valor));
    }
  }
  return params;
}

export async function obtenerDashboard(filtro: FiltroDashboard) {
  return obtener<DashboardData>("/api/v1/dashboard", armarParams(filtro));
}

export async function obtenerIndicadoresEmpleado(idUsuario: number, anio: number, mes: number) {
  return obtener<IndicadoresEmpleado>(`/api/v1/dashboard/empleados/${idUsuario}`, armarParams({ anio, mes }));
}

export async function obtenerTendenciasDashboard(anio: number, idProyecto?: number | null, idArea?: number | null) {
  return obtener<Tendencia[]>("/api/v1/dashboard/tendencias", armarParams({ anio, idProyecto, idArea }));
}

export async function obtenerFiltrosDashboard() {
  return obtener<FiltroCatalogosDashboard>("/api/v1/dashboard/filtros");
}
