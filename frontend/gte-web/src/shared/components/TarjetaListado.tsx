import type { ReactNode } from "react";
import { Box, Paper, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";

export interface CampoTarjeta {
  etiqueta: string;
  valor: ReactNode;
  /** Ocupa el renglon completo en vez de media tarjeta (textos largos). */
  completo?: boolean;
  /** Pinta el valor en rojo (vencidos, fuera de SLA). */
  resaltar?: boolean;
}

interface Props {
  /** Renglon superior: folio y chips de estatus/severidad. */
  encabezado: ReactNode;
  titulo: ReactNode;
  campos: CampoTarjeta[];
  /** Botones o menu de acciones; se dibujan al pie, alineados a la derecha. */
  acciones?: ReactNode;
  /** Tine la tarjeta completa con la misma semantica que el fondo de la fila en la tabla. */
  tinte?: "error" | "success";
}

/**
 * Sustituto de una fila de tabla en pantallas angostas: las bandejas de tickets e
 * incidentes tienen nueve o diez columnas y en un celular obligan a barrer en horizontal.
 * De sm en adelante las bandejas siguen dibujando la tabla completa (ver useEsMovil).
 */
export function TarjetaListado({ encabezado, titulo, campos, acciones, tinte }: Props) {
  return (
    <Paper
      variant="outlined"
      sx={(tema) => ({
        p: 1.5,
        backgroundColor: tinte
          ? alpha(tema.palette[tinte].main, tema.palette.mode === "dark" ? 0.18 : 0.08)
          : undefined,
      })}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap", mb: 0.5 }}>
        {encabezado}
      </Stack>

      <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>{titulo}</Typography>

      <Box sx={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", columnGap: 1.5, rowGap: 1 }}>
        {campos.map((campo) => (
          <Box key={campo.etiqueta} sx={{ minWidth: 0, gridColumn: campo.completo ? "1 / -1" : undefined }}>
            <Typography variant="caption" color="text.secondary" sx={{ display: "block", lineHeight: 1.3 }}>
              {campo.etiqueta}
            </Typography>
            <Typography variant="body2" component="div"
              sx={{ wordBreak: "break-word", color: campo.resaltar ? "error.main" : undefined }}>
              {campo.valor}
            </Typography>
          </Box>
        ))}
      </Box>

      {acciones && (
        <Stack direction="row" spacing={0.5}
          sx={{ alignItems: "center", justifyContent: "flex-end", flexWrap: "wrap", mt: 1 }}>
          {acciones}
        </Stack>
      )}
    </Paper>
  );
}
