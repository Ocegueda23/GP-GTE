import { enviar, obtener } from "./http";

/* ---------- Tarifas por nivel ---------- */

export interface TarifaNivel {
  idTarifaNivel: number;
  idNivel: number;
  nivel: string;
  costoHora: number;
  vigenciaDesde: string;
}

export async function obtenerTarifas() {
  return obtener<TarifaNivel[]>("/api/v1/costeo/tarifas");
}

export async function crearTarifa(datos: { idNivel: number; costoHora: number; vigenciaDesde: string }) {
  return enviar<TarifaNivel>("post", "/api/v1/costeo/tarifas", datos);
}

export async function actualizarTarifa(
  idTarifaNivel: number, datos: { costoHora: number; vigenciaDesde: string },
) {
  return enviar<TarifaNivel>("put", `/api/v1/costeo/tarifas/${idTarifaNivel}`, datos);
}

export async function retirarTarifa(idTarifaNivel: number) {
  return enviar<object>("put", `/api/v1/costeo/tarifas/${idTarifaNivel}/retirar`, {});
}

/* ---------- Presupuesto por proyecto ---------- */

export interface PresupuestoProyecto {
  idPresupuestoProyecto: number;
  idProyecto: number;
  proyecto: string;
  anio: number;
  montoAutorizado: number;
  horasAutorizadas: number;
}

export async function obtenerPresupuestos(idProyecto: number) {
  const params = new URLSearchParams();
  params.set("idProyecto", String(idProyecto));
  return obtener<PresupuestoProyecto[]>("/api/v1/costeo/presupuestos", params);
}

export async function crearPresupuesto(datos: {
  idProyecto: number; anio: number; montoAutorizado: number; horasAutorizadas: number;
}) {
  return enviar<PresupuestoProyecto>("post", "/api/v1/costeo/presupuestos", datos);
}

export async function actualizarPresupuesto(
  idPresupuestoProyecto: number, datos: { montoAutorizado: number; horasAutorizadas: number },
) {
  return enviar<PresupuestoProyecto>("put", `/api/v1/costeo/presupuestos/${idPresupuestoProyecto}`, datos);
}

export async function retirarPresupuesto(idPresupuestoProyecto: number) {
  return enviar<object>("put", `/api/v1/costeo/presupuestos/${idPresupuestoProyecto}/retirar`, {});
}

/* ---------- Reporte de costo real ---------- */

export interface CostoUsuario {
  idUsuario: number;
  usuario: string;
  minutos: number;
  horas: number;
  costo: number;
}

export interface CostoProyecto {
  idProyecto: number;
  proyecto: string;
  anio: number;
  montoAutorizado: number;
  horasAutorizadas: number;
  horasReales: number;
  costoReal: number;
  detallePorUsuario: CostoUsuario[];
}

export async function obtenerCostoProyecto(idProyecto: number, anio: number) {
  const params = new URLSearchParams();
  params.set("anio", String(anio));
  return obtener<CostoProyecto>(`/api/v1/costeo/proyectos/${idProyecto}`, params);
}
