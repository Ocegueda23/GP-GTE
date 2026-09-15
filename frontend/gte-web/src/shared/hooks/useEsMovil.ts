import { useMediaQuery, useTheme } from "@mui/material";

/**
 * Punto de corte unico de las vistas moviles. Coincide a proposito con el que ya usa el
 * menu lateral de App.tsx (Drawer permanente desde sm, temporal por debajo): asi el
 * contenido cambia de forma en el mismo ancho en que cambia el armazon de la aplicacion.
 */
export function useEsMovil(): boolean {
  const tema = useTheme();
  return useMediaQuery(tema.breakpoints.down("sm"));
}
