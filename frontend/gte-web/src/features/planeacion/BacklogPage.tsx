import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, IconButton, LinearProgress, Paper, Snackbar, Stack, TextField,
  Tooltip, Typography,
} from "@mui/material";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { obtenerBacklog, obtenerBacklogGlobal, reordenarBacklog } from "../../shared/api/planeacion";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { FilaItemBacklog } from "./FilaItemBacklog";

/**
 * P06 - Backlog: prioriza el pendiente por proyecto y permite buscarlo en todos los
 * proyectos. El compromiso de contenido a un sprint (mover elementos, capacidad,
 * cierre y envio a release) vive en el detalle del sprint (/sprints/:id): esta pantalla
 * no lo duplica para que no existan dos formas de hacer lo mismo.
 */
export function BacklogPage() {
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [busquedaGlobal, setBusquedaGlobal] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const proyectoActual = idProyecto === ""
    ? catalogos.data?.proyectos[0]?.id
    : (idProyecto as number);

  const backlog = useQuery({
    queryKey: ["backlog", proyectoActual],
    queryFn: () => obtenerBacklog(proyectoActual!),
    enabled: proyectoActual !== undefined,
  });

  const backlogGlobal = useQuery({
    queryKey: ["backlog-global", busquedaGlobal],
    queryFn: () => obtenerBacklogGlobal(busquedaGlobal),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["backlog"] }),
    clienteQuery.invalidateQueries({ queryKey: ["backlog-global"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : respaldo });
    } finally {
      await refrescar();
    }
  };

  const reordenar = (indice: number, direccion: -1 | 1) => {
    const items = backlog.data?.items ?? [];
    const destino = indice + direccion;
    if (destino < 0 || destino >= items.length) return;
    const ids = items.map((i) => i.idWorkItem);
    [ids[indice], ids[destino]] = [ids[destino], ids[indice]];
    void manejar(() => reordenarBacklog(ids), "No se pudo reordenar el backlog.");
  };

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Backlog</Typography>
        <Button variant="outlined" onClick={() => navegar("/sprints")}>
          Ir a sprints
        </Button>
      </Stack>

      <Alert severity="info" sx={{ mb: 2 }}>
        Aqui se prioriza el pendiente. Para comprometer elementos a un sprint, abre el
        sprint desde Sprints: en su detalle estan el backlog, la capacidad y el contenido
        comprometido.
      </Alert>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>
            Backlog por proyecto
          </Typography>
          <ComboBuscable
            label="Proyecto"
            value={proyectoActual ?? ""}
            onChange={(v) => setIdProyecto(v as number | "")}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({
              valor: p.id, etiqueta: `${p.clave} - ${p.nombre}`,
            }))}
            sx={{ minWidth: 420 }}
          />
        </Stack>
        {backlog.isLoading && <LinearProgress />}
        {backlog.data?.items.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
            El backlog de este proyecto esta vacio.
          </Typography>
        )}
        {backlog.data && backlog.data.items.length > 0 && (
          <Typography variant="caption" color="text.secondary">
            {backlog.data.items.length} elemento(s) - {backlog.data.puntosTotales} pts
          </Typography>
        )}
        {backlog.data?.items.map((item, indice) => (
          <FilaItemBacklog key={item.idWorkItem} item={item} acciones={
            <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
              <Tooltip title="Subir prioridad">
                <span>
                  <IconButton size="small" disabled={indice === 0}
                    onClick={() => reordenar(indice, -1)} aria-label="Subir prioridad">
                    <ArrowUpwardIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
              <Tooltip title="Bajar prioridad">
                <span>
                  <IconButton size="small"
                    disabled={indice === (backlog.data?.items.length ?? 0) - 1}
                    onClick={() => reordenar(indice, 1)} aria-label="Bajar prioridad">
                    <ArrowDownwardIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            </Stack>
          } />
        ))}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2, mt: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Buscar en el backlog de todos los proyectos
        </Typography>
        <TextField size="small" placeholder="Buscar folio, titulo o proyecto..." value={busquedaGlobal}
          onChange={(e) => setBusquedaGlobal(e.target.value)} sx={{ mb: 1.5, minWidth: 320 }} />
        {backlogGlobal.data && (
          <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
            {backlogGlobal.data.items.length} elemento(s) - {backlogGlobal.data.puntosTotales} pts.
            Solo lectura: para reordenar, elige el proyecto arriba.
          </Typography>
        )}
        <Box sx={{ maxHeight: 360, overflowY: "auto" }}>
          {backlogGlobal.data?.items.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              Sin elementos con esa busqueda.
            </Typography>
          )}
          {backlogGlobal.data?.items.map((item) => (
            <FilaItemBacklog key={item.idWorkItem} item={item} acciones={null} />
          ))}
        </Box>
      </Paper>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
