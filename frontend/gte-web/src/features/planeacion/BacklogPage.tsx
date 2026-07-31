import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, LinearProgress, Link, MenuItem, Paper,
  Select, Snackbar, Stack, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  asignarSprint, cambiarEstatusSprint, crearSprint, obtenerBacklog,
  obtenerCapacidad, obtenerItemsSprint, obtenerSprints, reordenarBacklog,
} from "../../shared/api/planeacion";
import { formatearMinutos, obtenerCatalogosBandeja, type BandejaItem } from "../../shared/api/workitems";

function FilaItem({ item, acciones }: { item: BandejaItem; acciones: React.ReactNode }) {
  return (
    <Paper variant="outlined" sx={{ p: 1, mb: 0.5, display: "flex", alignItems: "center", gap: 1 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover"
            sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
            {item.folio}
          </Link>
          <Chip size="small" label={item.tipo} variant="outlined" sx={{ height: 20 }} />
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
          {item.claveProyecto} - {item.estatus} - {item.asignado ?? "sin asignar"}
        </Typography>
      </Box>
      {acciones}
    </Paper>
  );
}

/** P06 - Backlog y planeacion de sprint: prioriza, compromete y compara contra capacidad. */
export function BacklogPage() {
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idSprint, setIdSprint] = useState<number | "">("");
  const [modalSprint, setModalSprint] = useState(false);
  const [modalCierre, setModalCierre] = useState(false);
  const [destinoCierre, setDestinoCierre] = useState("Backlog");
  const [nombre, setNombre] = useState("");
  const [objetivo, setObjetivo] = useState("");
  const [fechaInicio, setFechaInicio] = useState("");
  const [fechaFin, setFechaFin] = useState("");
  const [idEquipoNuevo, setIdEquipoNuevo] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const proyectoActual = idProyecto === "" ? catalogos.data?.proyectos[0]?.id : (idProyecto as number);

  const sprints = useQuery({ queryKey: ["sprints"], queryFn: () => obtenerSprints() });
  const sprintActual = idSprint === "" ? sprints.data?.[0]?.idSprint : (idSprint as number);

  const backlog = useQuery({
    queryKey: ["backlog", proyectoActual],
    queryFn: () => obtenerBacklog(proyectoActual!),
    enabled: proyectoActual !== undefined,
  });

  const itemsSprint = useQuery({
    queryKey: ["items-sprint", sprintActual],
    queryFn: () => obtenerItemsSprint(sprintActual!),
    enabled: sprintActual !== undefined,
  });

  const capacidad = useQuery({
    queryKey: ["capacidad", sprintActual],
    queryFn: () => obtenerCapacidad(sprintActual!),
    enabled: sprintActual !== undefined,
  });

  const sprintSeleccionado = sprints.data?.find((s) => s.idSprint === sprintActual);

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["backlog"] }),
    clienteQuery.invalidateQueries({ queryKey: ["items-sprint"] }),
    clienteQuery.invalidateQueries({ queryKey: ["capacidad"] }),
    clienteQuery.invalidateQueries({ queryKey: ["sprints"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
    clienteQuery.invalidateQueries({ queryKey: ["tablero"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
      await refrescar();
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : respaldo });
    }
  };

  const mover = (item: BandejaItem, aSprint: boolean) =>
    manejar(() => asignarSprint(item.idWorkItem, aSprint ? sprintActual! : null),
      "No se pudo mover el elemento.");

  const reordenar = (indice: number, direccion: -1 | 1) => {
    const items = backlog.data?.items ?? [];
    const destino = indice + direccion;
    if (destino < 0 || destino >= items.length) return;
    const ids = items.map((i) => i.idWorkItem);
    [ids[indice], ids[destino]] = [ids[destino], ids[indice]];
    void manejar(() => reordenarBacklog(ids), "No se pudo reordenar el backlog.");
  };

  const guardarSprint = () =>
    manejar(() => crearSprint({
      idEquipo: idEquipoNuevo as number,
      nombre: nombre.trim(),
      objetivo: objetivo.trim() || null,
      fechaInicio,
      fechaFin,
    }).then((r) => { setModalSprint(false); setNombre(""); setObjetivo(""); return r; }),
      "No se pudo crear el sprint.");

  const excedeCapacidad = capacidad.data !== undefined
    && capacidad.data.horasComprometidas > capacidad.data.horasCapacidad
    && capacidad.data.horasCapacidad > 0;

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Backlog y sprints</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalSprint(true)}>
          Nuevo sprint
        </Button>
      </Stack>

      <Stack direction={{ xs: "column", lg: "row" }} spacing={2}>
        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>Backlog</Typography>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Proyecto</InputLabel>
              <Select label="Proyecto" value={proyectoActual ?? ""}
                onChange={(e) => setIdProyecto(e.target.value as number)}>
                {catalogos.data?.proyectos.map((p) => (
                  <MenuItem key={p.id} value={p.id}>{p.clave}</MenuItem>
                ))}
              </Select>
            </FormControl>
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
            <FilaItem key={item.idWorkItem} item={item} acciones={
              <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
                <IconButton size="small" disabled={indice === 0}
                  onClick={() => reordenar(indice, -1)} aria-label="Subir prioridad">
                  <ArrowUpwardIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" disabled={indice === backlog.data.items.length - 1}
                  onClick={() => reordenar(indice, 1)} aria-label="Bajar prioridad">
                  <ArrowDownwardIcon fontSize="small" />
                </IconButton>
                <Tooltip title="Mover al sprint">
                  <IconButton size="small" disabled={sprintActual === undefined}
                    onClick={() => void mover(item, true)} aria-label="Mover al sprint">
                    <ArrowForwardIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Stack>
            } />
          ))}
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>Sprint</Typography>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel>Sprint</InputLabel>
              <Select label="Sprint" value={sprintActual ?? ""}
                onChange={(e) => setIdSprint(e.target.value as number)}>
                {sprints.data?.map((s) => (
                  <MenuItem key={s.idSprint} value={s.idSprint}>
                    {s.nombre} ({s.estatus})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>

          {sprints.data?.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              No hay sprints abiertos. Crea uno para empezar a planear.
            </Typography>
          )}

          {sprintSeleccionado && (
            <>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1, flexWrap: "wrap" }}>
                <Chip size="small" label={sprintSeleccionado.estatus}
                  color={sprintSeleccionado.idEstatus === 2 ? "success" : "default"} />
                <Typography variant="caption" color="text.secondary">
                  {sprintSeleccionado.fechaInicio} al {sprintSeleccionado.fechaFin}
                </Typography>
                {sprintSeleccionado.idEstatus === 1 && (
                  <Button size="small" variant="contained"
                    onClick={() => void manejar(
                      () => cambiarEstatusSprint(sprintSeleccionado.idSprint, { accion: "ACTIVAR" }),
                      "No se pudo activar el sprint.")}>
                    Activar
                  </Button>
                )}
                {sprintSeleccionado.idEstatus === 2 && (
                  <Button size="small" variant="outlined" onClick={() => setModalCierre(true)}>
                    Cerrar sprint
                  </Button>
                )}
              </Stack>

              {capacidad.data && (
                <Alert severity={excedeCapacidad ? "warning" : "info"} sx={{ mb: 1 }}>
                  Capacidad {capacidad.data.horasCapacidad} h - comprometido{" "}
                  {capacidad.data.horasComprometidas} h
                  {excedeCapacidad && " (el compromiso excede la capacidad del equipo)"}
                  {capacidad.data.personas.length === 0
                    && " - el equipo no tiene miembros con horario asignado"}
                </Alert>
              )}

              <Typography variant="caption" color="text.secondary">
                {sprintSeleccionado.itemsTerminados}/{sprintSeleccionado.totalItems} terminados -{" "}
                {sprintSeleccionado.puntosTerminados}/{sprintSeleccionado.puntosComprometidos} pts
              </Typography>

              {itemsSprint.data?.items.length === 0 && (
                <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                  Sin elementos comprometidos. Muevelos desde el backlog con la flecha.
                </Typography>
              )}
              {itemsSprint.data?.items.map((item) => (
                <FilaItem key={item.idWorkItem} item={item} acciones={
                  <Tooltip title="Regresar al backlog">
                    <IconButton size="small" sx={{ flexShrink: 0 }}
                      onClick={() => void mover(item, false)} aria-label="Regresar al backlog">
                      <ArrowBackIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                } />
              ))}
            </>
          )}
        </Paper>
      </Stack>

      <Dialog open={modalSprint} onClose={() => setModalSprint(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo sprint</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" required>
            <InputLabel>Equipo</InputLabel>
            <Select label="Equipo" value={idEquipoNuevo}
              onChange={(e) => setIdEquipoNuevo(e.target.value as number)}>
              {catalogos.data?.equipos.map((eq) => (
                <MenuItem key={eq.id} value={eq.id}>{eq.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" required label="Nombre" value={nombre}
            onChange={(e) => setNombre(e.target.value)} />
          <TextField size="small" label="Objetivo del sprint" multiline minRows={2}
            value={objetivo} onChange={(e) => setObjetivo(e.target.value)} />
          <TextField size="small" type="date" required label="Inicio" value={fechaInicio}
            onChange={(e) => setFechaInicio(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" required label="Fin" value={fechaFin}
            onChange={(e) => setFechaFin(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalSprint(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => void guardarSprint()}
            disabled={idEquipoNuevo === "" || nombre.trim().length === 0 || !fechaInicio || !fechaFin}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalCierre} onClose={() => setModalCierre(false)} fullWidth maxWidth="xs">
        <DialogTitle>Cerrar sprint</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <Typography variant="body2" sx={{ mb: 2 }}>
            Los elementos que no quedaron terminados se reubican. Elige a donde:
          </Typography>
          <FormControl size="small" fullWidth>
            <InputLabel>Destino</InputLabel>
            <Select label="Destino" value={destinoCierre}
              onChange={(e) => setDestinoCierre(e.target.value)}>
              <MenuItem value="Backlog">Regresar al backlog</MenuItem>
              <MenuItem value="SiguienteSprint">Pasar al siguiente sprint planeado</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalCierre(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => {
            setModalCierre(false);
            void manejar(() => cambiarEstatusSprint(sprintActual!, {
              accion: "CERRAR", destinoItemsAbiertos: destinoCierre,
            }), "No se pudo cerrar el sprint.");
          }}>
            Cerrar sprint
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
