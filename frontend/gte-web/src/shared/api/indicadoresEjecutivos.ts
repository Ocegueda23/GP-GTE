import { enviar, obtener } from "./http";

export interface LeadCycleTime {
  leadTimeHorasP50: number;
  leadTimeHorasP85: number;
  cycleTimeHorasPromedio: number;
  itemsConsiderados: number;
}

export interface Dora {
  deploymentsPorSemana: number;
  leadTimeCambiosHoras: number | null;
  leadTimeCambiosSinDatos: boolean;
  changeFailureRatePorcentaje: number;
  mttrHoras: number | null;
  despliguesConsiderados: number;
  incidentesConsiderados: number;
}

export interface EntregaATiempo {
  porcentaje: number;
  semaforo: string;
  totalConCompromiso: number;
}

export interface Retrabajo {
  porcentaje: number;
  reaperturasSinDatos: boolean;
}

export interface Productividad {
  puntosPromedioPorPersona: number;
  personasConsideradas: number;
}

export interface SlaEjecutivo {
  cumplimientoPorcentaje: number;
  ticketsConsiderados: number;
  csat: number | null;
  encuestasConsideradas: number;
}

export interface SemaforoProyecto {
  idProyecto: number;
  clave: string;
  proyecto: string;
  semaforo: string;
  entregaATiempoPorcentaje: number;
  montoAutorizado: number | null;
  costoReal: number | null;
}

export interface PuntoSerie {
  fecha: string;
  valor: number;
}

export interface KpiPersonalizadoSerie {
  clave: string;
  nombre: string;
  meta: number | null;
  direccion: string;
  serie: PuntoSerie[];
}

export interface RiesgoEjecutivo {
  idRiesgo: number;
  idProyecto: number;
  proyecto: string;
  descripcion: string;
  probabilidad: number;
  impacto: number;
  exposicion: number;
  estatus: string;
}

export interface PuntoBurndown {
  fecha: string;
  restanteIdeal: number;
  restanteReal: number;
}

export interface BurndownSprint {
  idSprint: number;
  nombre: string;
  idEquipo: number;
  equipo: string;
  fechaInicio: string;
  fechaFin: string;
  puntosTotales: number;
  puntos: PuntoBurndown[];
}

export interface ObjetivoOkr {
  idObjetivoOkr: number;
  idProyecto: number | null;
  proyecto: string | null;
  idEquipo: number | null;
  equipo: string | null;
  nombre: string;
  descripcion: string | null;
  anio: number;
  trimestre: number;
  resultadosClave: { idResultadoClave: number; nombre: string; valorMeta: number; valorActual: number; claveKpi: string | null }[];
}

export interface IndicadoresEjecutivos {
  alcance: string;
  anio: number;
  mes: number;
  leadCycleTime: LeadCycleTime;
  dora: Dora;
  entregaATiempo: EntregaATiempo;
  eficienciaPorcentaje: number;
  retrabajo: Retrabajo;
  productividad: Productividad;
  sla: SlaEjecutivo;
  proyectos: SemaforoProyecto[];
  okr: ObjetivoOkr[];
  kpisPersonalizados: KpiPersonalizadoSerie[];
  topRiesgos: RiesgoEjecutivo[];
  burndownSprintActivo: BurndownSprint | null;
}

export interface FiltroIndicadoresEjecutivos {
  [clave: string]: string | number | null | undefined;
  anio: number;
  mes: number;
  idEquipo?: number | null;
  idProyecto?: number | null;
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

export async function obtenerIndicadoresEjecutivos(filtro: FiltroIndicadoresEjecutivos) {
  return obtener<IndicadoresEjecutivos>("/api/v1/indicadores-ejecutivos", armarParams(filtro));
}

export async function obtenerLayoutDashboardEjecutivo() {
  const layout = await obtener<{ layoutJson: string | null }>("/api/v1/indicadores-ejecutivos/layout");
  return layout.layoutJson;
}

export async function guardarLayoutDashboardEjecutivo(layoutJson: string) {
  return enviar<object>("put", "/api/v1/indicadores-ejecutivos/layout", { layoutJson });
}
