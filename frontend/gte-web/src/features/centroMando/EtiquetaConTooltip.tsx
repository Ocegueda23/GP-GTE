import { Stack, Tooltip as MuiTooltip } from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";

/** Etiqueta con la explicacion del indicador al pasar el mouse (mismo patron que el resto de dashboards). */
export function EtiquetaConTooltip({ texto, explicacion }: { texto: string; explicacion?: string | null }) {
  if (!explicacion) return <>{texto}</>;
  return (
    <MuiTooltip title={explicacion} arrow>
      <Stack direction="row" spacing={0.4} sx={{ alignItems: "center", cursor: "help" }} component="span">
        <span>{texto}</span>
        <InfoOutlinedIcon sx={{ fontSize: 13, color: "text.disabled" }} />
      </Stack>
    </MuiTooltip>
  );
}
