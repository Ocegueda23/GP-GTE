import { enviar, obtener } from "./http";

export interface ResultadoClave {
  idResultadoClave: number;
  nombre: string;
  valorMeta: number;
  valorActual: number;
  claveKpi: string | null;
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
  resultadosClave: ResultadoClave[];
}

export async function obtenerObjetivos(filtro: { idProyecto?: number; idEquipo?: number; anio?: number }) {
  const params = new URLSearchParams();
  if (filtro.idProyecto) params.set("idProyecto", String(filtro.idProyecto));
  if (filtro.idEquipo) params.set("idEquipo", String(filtro.idEquipo));
  if (filtro.anio) params.set("anio", String(filtro.anio));
  return obtener<ObjetivoOkr[]>("/api/v1/okr/objetivos", params);
}

export async function crearObjetivo(datos: {
  idProyecto: number | null; idEquipo: number | null; nombre: string; descripcion: string | null;
  anio: number; trimestre: number;
}) {
  return enviar<ObjetivoOkr>("post", "/api/v1/okr/objetivos", datos);
}

export async function actualizarObjetivo(idObjetivoOkr: number, datos: { nombre: string; descripcion: string | null }) {
  return enviar<ObjetivoOkr>("put", `/api/v1/okr/objetivos/${idObjetivoOkr}`, datos);
}

export async function retirarObjetivo(idObjetivoOkr: number) {
  return enviar<object>("put", `/api/v1/okr/objetivos/${idObjetivoOkr}/retirar`, {});
}

export async function crearResultadoClave(
  idObjetivoOkr: number, datos: { nombre: string; valorMeta: number; claveKpi: string | null },
) {
  return enviar<ObjetivoOkr>("post", `/api/v1/okr/objetivos/${idObjetivoOkr}/resultados`, datos);
}

export async function actualizarResultadoClave(
  idObjetivoOkr: number, idResultadoClave: number,
  datos: { nombre: string; valorMeta: number; valorActual: number; claveKpi: string | null },
) {
  return enviar<ObjetivoOkr>(
    "put", `/api/v1/okr/objetivos/${idObjetivoOkr}/resultados/${idResultadoClave}`, datos,
  );
}

export async function retirarResultadoClave(idObjetivoOkr: number, idResultadoClave: number) {
  return enviar<ObjetivoOkr>(
    "put", `/api/v1/okr/objetivos/${idObjetivoOkr}/resultados/${idResultadoClave}/retirar`, {},
  );
}
