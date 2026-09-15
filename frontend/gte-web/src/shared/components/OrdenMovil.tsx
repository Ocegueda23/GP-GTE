import { IconButton, Stack, Tooltip } from "@mui/material";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import { ComboBuscable, type OpcionComboBuscable } from "./ComboBuscable";

interface Props {
  /** El valor de cada opcion es la misma clave de orden que usa EncabezadoOrdenable. */
  opciones: OpcionComboBuscable[];
  ordenarPor: string | null;
  descendente: boolean;
  /** Misma firma que en la tabla: repetir la clave actual alterna el sentido. */
  onOrdenar: (clave: string) => void;
}

/**
 * Reemplaza a los encabezados ordenables cuando la bandeja se dibuja como tarjetas
 * (ver useEsMovil): sin encabezados de tabla no habria manera de ordenar en un celular.
 */
export function OrdenMovil({ opciones, ordenarPor, descendente, onOrdenar }: Props) {
  return (
    <Stack direction="row" spacing={0.5} sx={{ alignItems: "center", width: "100%" }}>
      <ComboBuscable
        label="Ordenar por"
        value={ordenarPor ?? ""}
        onChange={(valor) => onOrdenar(String(valor))}
        opciones={opciones}
        sx={{ flexGrow: 1 }}
      />
      <Tooltip title={descendente ? "Descendente" : "Ascendente"}>
        <span>
          <IconButton
            aria-label="Cambiar sentido del orden"
            disabled={ordenarPor === null}
            onClick={() => ordenarPor !== null && onOrdenar(ordenarPor)}
          >
            {descendente ? <ArrowDownwardIcon /> : <ArrowUpwardIcon />}
          </IconButton>
        </span>
      </Tooltip>
    </Stack>
  );
}
