import { useTheme } from "@mui/material";

/**
 * Los colores de serie de recharts estan fijos en el codigo y se eligieron para el tema
 * claro: varios son tonos oscuros (slate, teal, indigo...) que sobre el fondo del tema
 * oscuro quedan practicamente invisibles. Esta tabla da el equivalente claro del esquema
 * Dark+ para cada uno; los tonos que ya eran brillantes (ambar, rojo, verde lima...) no
 * aparecen aqui porque se leen bien en ambos modos y se devuelven tal cual.
 */
const EQUIVALENTE_OSCURO: Record<string, string> = {
  "#334155": "#4fc1ff", // slate 700 -> azul
  "#0f766e": "#4ec9b0", // teal 700  -> verde azulado
  "#b45309": "#ce9178", // ambar 700 -> naranja
  "#7c3aed": "#c586c0", // violeta   -> magenta
  "#0891b2": "#569cd6", // cyan 600  -> azul medio
  "#be123c": "#f48771", // rosa 700  -> rojo
  "#4d7c0f": "#89d185", // lima 700  -> verde
  "#a16207": "#dcdcaa", // ambar 800 -> amarillo
  "#0369a1": "#9cdcfe", // azul 700  -> azul claro
  "#9333ea": "#d7ba7d", // purpura   -> arena
};

/**
 * Devuelve un mapeador de color de serie que respeta el modo del tema: en claro deja el
 * color tal cual, en oscuro lo cambia por su equivalente legible.
 */
export function useColorSerie() {
  const modo = useTheme().palette.mode;
  return (color: string) => (
    modo === "dark" ? EQUIVALENTE_OSCURO[color.toLowerCase()] ?? color : color
  );
}
