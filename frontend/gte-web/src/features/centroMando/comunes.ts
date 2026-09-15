export const NOMBRES_MES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

/** Semaforo del modelo de gestion: Verde/Amarillo/Rojo. Sin semaforo se trata como neutro. */
export function colorSemaforo(semaforo: string | null | undefined): "success" | "warning" | "error" | "default" {
  if (semaforo === "Verde") return "success";
  if (semaforo === "Amarillo") return "warning";
  if (semaforo === "Rojo") return "error";
  return "default";
}

/** Igual que colorSemaforo pero para propiedades sx (color/bgcolor). */
export function tonoSemaforo(semaforo: string | null | undefined): string {
  const color = colorSemaforo(semaforo);
  return color === "default" ? "text.disabled" : `${color}.main`;
}

/**
 * Un valor nulo NUNCA se pinta como cero: el modelo distingue "no hay dato" de "el dato
 * es cero", y confundirlos cambia por completo la lectura del indicador.
 */
export function formatearValor(valor: number | null | undefined, unidad?: string | null): string {
  if (valor === null || valor === undefined) return "Sin datos";
  const texto = Number.isInteger(valor) ? String(valor) : valor.toFixed(1);
  if (!unidad) return texto;
  if (unidad === "Porcentaje" || unidad === "%") return `${texto}%`;
  if (unidad === "Horas") return `${texto} h`;
  if (unidad === "Dias") return `${texto} d`;
  return `${texto} ${unidad}`;
}
