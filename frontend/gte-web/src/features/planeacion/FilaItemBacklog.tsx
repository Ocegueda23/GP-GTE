import { Link as RouterLink } from "react-router-dom";
import { Box, Chip, Link, Paper, Stack, Tooltip, Typography } from "@mui/material";
import { colorEstatus, formatearMinutos, type BandejaItem } from "../../shared/api/workitems";

/**
 * Renglon de un elemento de backlog o de sprint. Compartido por la pestana Backlog
 * (priorizacion) y el detalle del sprint (asignacion), para que un elemento se vea
 * igual en las dos pantallas.
 */
export function FilaItemBacklog({ item, acciones }: {
  item: BandejaItem;
  acciones: React.ReactNode;
}) {
  return (
    <Paper variant="outlined" sx={{ p: 1, mb: 0.5, display: "flex", alignItems: "center", gap: 1 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover"
            sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
            {item.folio}
          </Link>
          <Chip size="small" label={item.tipo} variant="outlined" sx={{ height: 20 }} />
          <Chip size="small" label={item.estatus} color={colorEstatus(item.idEstatus)}
            variant={item.idEstatus === 6 ? "outlined" : "filled"} sx={{ height: 20 }} />
          {item.puntosHistoria !== null && (
            <Chip size="small" label={`${item.puntosHistoria} pts`} sx={{ height: 20 }} />
          )}
          {item.minutosPresupuesto !== null && (
            <Tooltip title="Presupuesto de tiempo">
              <Chip size="small" variant="outlined" sx={{ height: 20 }}
                label={formatearMinutos(item.minutosPresupuesto)} />
            </Tooltip>
          )}
        </Stack>
        <Typography variant="body2" noWrap>{item.titulo}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.claveProyecto} - {item.asignado ?? "sin asignar"}
        </Typography>
      </Box>
      {acciones}
    </Paper>
  );
}
