import type { ReactNode } from "react";
import { TableCell, TableSortLabel } from "@mui/material";

interface Props<T extends string> {
  clave: T;
  ordenActual: T | null;
  descendente: boolean;
  onOrdenar: (clave: T) => void;
  children: ReactNode;
  align?: "left" | "right" | "center";
}

/** TableCell con TableSortLabel para tablas con orden client-side (ver useOrdenTabla). */
export function EncabezadoOrdenable<T extends string>({
  clave, ordenActual, descendente, onOrdenar, children, align,
}: Props<T>) {
  const activo = ordenActual === clave;
  return (
    <TableCell align={align} sortDirection={activo ? (descendente ? "desc" : "asc") : false}>
      <TableSortLabel
        active={activo}
        direction={activo ? (descendente ? "desc" : "asc") : "asc"}
        onClick={() => onOrdenar(clave)}
      >
        {children}
      </TableSortLabel>
    </TableCell>
  );
}
