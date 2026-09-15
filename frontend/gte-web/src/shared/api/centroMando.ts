import { enviar, obtener } from "./http";

/** Permiso de lectura del Centro de Mando TI (dashboard ejecutivo y evaluacion por equipo). */
export const PERMISO_VER_CENTRO_MANDO = "GES.Ver";

/** Permiso de administracion: catalogo de indicadores, recalculo y atencion de alertas. */
export const PERMISO_ADMINISTRAR_CENTRO_MANDO = "GES.Administrar";

/** Una fila del catalogo de indicadores (pantalla de administracion). */
export interface IndicadorGestion {
  idIndicadorGestion: number;
  clave: string;
  nombre: string;
  descripcion: string | null;
  categoria: string;
  ambito: string;
  origen: string;
  formula: string | null;
  unidad: string;
  meta: number | null;
  umbralAlerta: number | null;
  direccion: string;
  peso: number;
  ponderaEnScore: boolean;
  periodicidad: string;
  interpretacionBuena: string | null;
  interpretacionMala: string | null;
  accionSugerida: string | null;
  activo: boolean;
}

/** Valor de un indicador dentro de la evaluacion de un responsable. */
export interface IndicadorEvaluado {
  idIndicadorGestion: number;
  clave: string;
  nombre: string;
  categoria: string;
  ambito: string;
  origen: string;
  unidad: string;
  valor: number | null;
  valorNormalizado: number | null;
  meta: number | null;
  umbralAlerta: number | null;
  direccion: string;
  peso: number;
  ponderaEnScore: boolean;
  semaforo: string | null;
  sinDatos: boolean;
  valorPeriodoAnterior: number | null;
  /** Mejora / Empeora / Estable / null cuando no hay periodo anterior con dato. */
  tendencia: string | null;
  accionSugerida: string | null;
  interpretacionMala: string | null;
}

/** Una causa detectada por el modelo de diagnostico. */
export interface CausaDiagnostico {
  /** Persona / Proceso / Recursos / Dependencia / Prioridad. */
  causa: string;
  indiceClave: string;
  valor: number | null;
  umbral: number | null;
  evidencia: string;
}

/** Analisis de carga del equipo en el periodo. */
export interface CargaEquipo {
  horasDisponibles: number;
  horasAsignadas: number;
  horasEjecutadas: number;
  indiceCarga: number | null;
  /** Subutilizado / Adecuado / SobrecargaModerada / SobrecargaCritica. */
  situacion: string | null;
  integrantes: number;
}

/** Evaluacion mensual completa de un responsable. */
export interface EvaluacionResponsable {
  idEquipo: number;
  equipo: string;
  ambito: string | null;
  idResponsable: number | null;
  responsable: string | null;
  urlFoto: string | null;
  anio: number;
  mes: number;
  scoreGeneral: number | null;
  scoreMesAnterior: number | null;
  nivel: string | null;
  semaforo: string | null;
  indicadoresConDato: number;
  indicadoresTotales: number;
  carga: CargaEquipo | null;
  indicadores: IndicadorEvaluado[];
  diagnostico: CausaDiagnostico[];
  /** Conclusion automatica en una frase, lista para mostrarse bajo el score. */
  conclusion: string | null;
}

/** Una dimension transversal del IT Health Score. */
export interface DimensionSalud {
  dimension: string;
  peso: number;
  valor: number | null;
  semaforo: string | null;
}

export interface AlertaGestion {
  idAlertaGestion: number;
  clave: string;
  severidad: string;
  idEquipo: number | null;
  equipo: string | null;
  indicador: string | null;
  anio: number;
  mes: number;
  titulo: string;
  mensaje: string | null;
  requiereGerencia: boolean;
  atendida: boolean;
  fechaRegistro: string;
}

/** Un punto de la tendencia mensual del score. */
export interface PuntoTendenciaCentroMando {
  anio: number;
  mes: number;
  valor: number | null;
}

export interface TendenciaCentroMando {
  serie: string;
  idEquipo: number | null;
  puntos: PuntoTendenciaCentroMando[];
}

/** Payload del dashboard ejecutivo: todo lo que se revisa en menos de dos minutos. */
export interface CentroMando {
  anio: number;
  mes: number;
  saludTi: number | null;
  saludSemaforo: string | null;
  saludNivel: string | null;
  /** True cuando la regla de piso topo el score porque algo critico esta en rojo. */
  saludTopada: boolean;
  razonTope: string | null;
  dimensiones: DimensionSalud[];
  responsables: EvaluacionResponsable[];
  alertas: AlertaGestion[];
  tendencias: TendenciaCentroMando[];
  fechaUltimoCalculo: string | null;
}

/**
 * Ajuste del catalogo. Clave, nombre, formula y ambito NO son editables desde la UI: los
 * gobierna el script de despliegue para que el modelo siga siendo comparable entre periodos.
 */
export interface ActualizarIndicadorGestionRequest {
  meta: number | null;
  umbralAlerta: number | null;
  peso: number;
  ponderaEnScore: boolean;
  accionSugerida: string | null;
  activo: boolean;
}

const BASE = "/api/v1/centro-mando";

export async function obtenerCentroMando(anio: number, mes: number) {
  const params = new URLSearchParams({ anio: String(anio), mes: String(mes) });
  return obtener<CentroMando>(BASE, params);
}

export async function obtenerEvaluacionResponsable(idEquipo: number, anio: number, mes: number) {
  const params = new URLSearchParams({ anio: String(anio), mes: String(mes) });
  return obtener<EvaluacionResponsable>(`${BASE}/equipos/${idEquipo}`, params);
}

export async function obtenerCatalogoIndicadores(ambito?: string | null) {
  const params = new URLSearchParams();
  if (ambito) params.set("ambito", ambito);
  return obtener<IndicadorGestion[]>(`${BASE}/catalogo`, params);
}

export async function obtenerAlertasGestion(anio: number, mes: number, soloVigentes: boolean) {
  const params = new URLSearchParams({
    anio: String(anio), mes: String(mes), soloVigentes: String(soloVigentes),
  });
  return obtener<AlertaGestion[]>(`${BASE}/alertas`, params);
}

export async function actualizarIndicadorGestion(id: number, cambios: ActualizarIndicadorGestionRequest) {
  return enviar<void>("put", `${BASE}/catalogo/${id}`, cambios);
}

/** Recalculo manual de un periodo (el mensual lo dispara Hangfire). Devuelve equipos recalculados. */
export async function recalcularPeriodoCentroMando(anio: number, mes: number) {
  const { dato, mensaje } = await enviar<number>("post", `${BASE}/recalcular`, { anio, mes });
  return { equipos: dato, mensaje };
}

export async function atenderAlertaGestion(idAlertaGestion: number) {
  return enviar<void>("post", `${BASE}/alertas/${idAlertaGestion}/atender`);
}
