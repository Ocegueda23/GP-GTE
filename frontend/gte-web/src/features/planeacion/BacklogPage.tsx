import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, LinearProgress, Link, MenuItem, Paper,
  Select, Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import { useSesion } from "../../shared/api/sesion";
import {
  asignarSprint, cambiarEstatusSprint, crearSprint, editarSprint, obtenerBacklog, obtenerBacklogGlobal,
  obtenerCapacidad, obtenerItemsSprint, obtenerSprints, reordenarBacklog,
} from "../../shared/api/planeacion";
import { colorEstatus, formatearMinutos, obtenerCatalogosBandeja, type BandejaItem } from "../../shared/api/workitems";
import { obtenerCoberturaReleaseSprint, enviarSprintARelease } from "../../shared/api/entregas";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

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

/** P06 - Backlog y planeacion de sprint: prioriza, compromete y compara contra capacidad. */
export function BacklogPage() {
  const puede = useSesion((estado) => estado.puede);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idSprint, setIdSprint] = useState<number | "">("");
  const [modalSprint, setModalSprint] = useState(false);
  const [modalEditar, setModalEditar] = useState(false);
  const [nombreEditar, setNombreEditar] = useState("");
  const [objetivoEditar, setObjetivoEditar] = useState("");
  const [fechaInicioEditar, setFechaInicioEditar] = useState("");
  const [fechaFinEditar, setFechaFinEditar] = useState("");
  const [modalCierre, setModalCierre] = useState(false);
  const [destinoCierre, setDestinoCierre] = useState("Backlog");
  const [modalRelease, setModalRelease] = useState(false);
  const [versionesNuevas, setVersionesNuevas] = useState<Record<number, string>>({});
  const [nombre, setNombre] = useState("");
  const [objetivo, setObjetivo] = useState("");
  const [fechaInicio, setFechaInicio] = useState("");
  const [fechaFin, setFechaFin] = useState("");
  const [idEquipoNuevo, setIdEquipoNuevo] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [busquedaGlobal, setBusquedaGlobal] = useState("");
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const proyectoActual = idProyecto === "" ? catalogos.data?.proyectos[0]?.id : (idProyecto as number);

  const sprints = useQuery({ queryKey: ["sprints"], queryFn: () => obtenerSprints(undefined, false) });
  const sprintActual = idSprint === "" ? sprints.data?.[0]?.idSprint : (idSprint as number);
  const { datosOrdenados: sprintsOrdenados, ordenarPor: ordenSprints, descendente: descSprints, ordenar: ordenarSprints }
    = useOrdenTabla(sprints.data);

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

  const backlogGlobal = useQuery({
    queryKey: ["backlog-global", busquedaGlobal],
    queryFn: () => obtenerBacklogGlobal(busquedaGlobal),
    placeholderData: (anterior) => anterior,
  });

  // Paso aparte tras cerrar el sprint (RN-PLA-02 complementaria): que le falta a cada
  // proyecto para no dejar nada terminado fuera de un release.
  const cobertura = useQuery({
    queryKey: ["cobertura-release", sprintActual],
    queryFn: () => obtenerCoberturaReleaseSprint(sprintActual!),
    enabled: modalRelease && sprintActual !== undefined,
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["backlog"] }),
    clienteQuery.invalidateQueries({ queryKey: ["items-sprint"] }),
    clienteQuery.invalidateQueries({ queryKey: ["capacidad"] }),
    clienteQuery.invalidateQueries({ queryKey: ["sprints"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
    clienteQuery.invalidateQueries({ queryKey: ["tablero"] }),
    clienteQuery.invalidateQueries({ queryKey: ["cobertura-release"] }),
    clienteQuery.invalidateQueries({ queryKey: ["releases"] }),
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

  const enviarARelease = (idProyecto: number, idReleaseExistente: number | null) =>
    manejar(() => enviarSprintARelease(sprintActual!, {
      idProyecto,
      idReleaseExistente,
      versionNueva: idReleaseExistente ? null : (versionesNuevas[idProyecto] ?? "").trim() || null,
    }).then((r) => {
      setVersionesNuevas((v) => ({ ...v, [idProyecto]: "" }));
      return r;
    }), "No se pudo enviar el contenido al release.");

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

  const abrirModalEditar = () => {
    if (!sprintSeleccionado) return;
    setNombreEditar(sprintSeleccionado.nombre);
    setObjetivoEditar(sprintSeleccionado.objetivo ?? "");
    setFechaInicioEditar(sprintSeleccionado.fechaInicio);
    setFechaFinEditar(sprintSeleccionado.fechaFin);
    setModalEditar(true);
  };

  const guardarEdicionSprint = () =>
    manejar(() => editarSprint(sprintActual!, {
      nombre: nombreEditar.trim(),
      objetivo: objetivoEditar.trim() || null,
      fechaInicio: fechaInicioEditar,
      fechaFin: fechaFinEditar,
    }).then((r) => { setModalEditar(false); return r; }),
      "No se pudo modificar el sprint.");

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

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Todos los sprints ({sprintsOrdenados.length})
        </Typography>
        <TableContainer sx={{ maxHeight: 360, overflowY: "auto" }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                <EncabezadoOrdenable clave="nombre" ordenActual={ordenSprints} descendente={descSprints} onOrdenar={ordenarSprints}>Nombre</EncabezadoOrdenable>
                <TableCell>Objetivo</TableCell>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenSprints} descendente={descSprints} onOrdenar={ordenarSprints}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="equipo" ordenActual={ordenSprints} descendente={descSprints} onOrdenar={ordenarSprints}>Equipo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaInicio" ordenActual={ordenSprints} descendente={descSprints} onOrdenar={ordenarSprints}>Fecha inicio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaFin" ordenActual={ordenSprints} descendente={descSprints} onOrdenar={ordenarSprints}>Fecha fin</EncabezadoOrdenable>
              </TableRow>
            </TableHead>
            <TableBody>
              {sprintsOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                      No hay sprints creados.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {sprintsOrdenados.map((s) => (
                <TableRow key={s.idSprint} hover selected={s.idSprint === sprintActual}
                  sx={{ cursor: "pointer" }} onClick={() => setIdSprint(s.idSprint)}>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{s.nombre}</TableCell>
                  <TableCell sx={{ maxWidth: 320 }}>
                    <Typography noWrap variant="body2">{s.objetivo ?? "-"}</Typography>
                  </TableCell>
                  <TableCell>
                    <Chip size="small" label={s.estatus} color={s.idEstatus === 2 ? "success" : "default"} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{s.equipo}</TableCell>
                  <TableCell>{formatearFecha(s.fechaInicio)}</TableCell>
                  <TableCell>{formatearFecha(s.fechaFin)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Stack direction={{ xs: "column", lg: "row" }} spacing={2}>
        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>Backlog</Typography>
            <ComboBuscable
              label="Proyecto"
              value={proyectoActual ?? ""}
              onChange={(v) => setIdProyecto(v as number | "")}
              opciones={(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
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
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
            Sprint{sprintSeleccionado ? `: ${sprintSeleccionado.nombre}` : ""}
          </Typography>

          {sprints.data?.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              No hay sprints creados. Crea uno para empezar a planear.
            </Typography>
          )}
          {sprints.data && sprints.data.length > 0 && !sprintSeleccionado && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              Elige un sprint de la tabla de arriba para ver su backlog.
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
                {sprintSeleccionado.idEstatus === 1 && puede("PLA.CambiarEstatusSprint") && (
                  <Button size="small" variant="contained"
                    onClick={() => void manejar(
                      () => cambiarEstatusSprint(sprintSeleccionado.idSprint, { accion: "ACTIVAR" }),
                      "No se pudo activar el sprint.")}>
                    Activar
                  </Button>
                )}
                {sprintSeleccionado.idEstatus === 2 && puede("PLA.CambiarEstatusSprint") && (
                  <Button size="small" variant="outlined"
                    onClick={() => void manejar(
                      () => cambiarEstatusSprint(sprintSeleccionado.idSprint, { accion: "VOLVER_PLANEADO" }),
                      "No se pudo volver el sprint a planeado.")}>
                    Volver a planeado
                  </Button>
                )}
                {sprintSeleccionado.idEstatus === 2 && puede("PLA.CerrarSprint") && (
                  <Button size="small" variant="outlined" onClick={() => setModalCierre(true)}>
                    Cerrar sprint
                  </Button>
                )}
                {sprintSeleccionado.idEstatus === 3 && (
                  <Button size="small" variant="contained" onClick={() => setModalRelease(true)}>
                    Enviar a release
                  </Button>
                )}
                {sprintSeleccionado.idEstatus !== 3 && puede("PLA.ModificarSprint") && (
                  <Button size="small" onClick={abrirModalEditar}>
                    Modificar sprint
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

      <Paper variant="outlined" sx={{ p: 2, mt: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Buscar en el backlog de todos los proyectos
        </Typography>
        <TextField size="small" placeholder="Buscar folio, titulo o proyecto..." value={busquedaGlobal}
          onChange={(e) => setBusquedaGlobal(e.target.value)} sx={{ mb: 1.5, minWidth: 320 }} />
        {backlogGlobal.data && (
          <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
            {backlogGlobal.data.items.length} elemento(s) - {backlogGlobal.data.puntosTotales} pts.
            Solo lectura: para reordenar o mover a sprint, elige el proyecto arriba.
          </Typography>
        )}
        <Box sx={{ maxHeight: 360, overflowY: "auto" }}>
          {backlogGlobal.data?.items.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              Sin elementos con esa busqueda.
            </Typography>
          )}
          {backlogGlobal.data?.items.map((item) => <FilaItem key={item.idWorkItem} item={item} acciones={null} />)}
        </Box>
      </Paper>

      <Dialog open={modalSprint} onClose={() => setModalSprint(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo sprint</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Equipo"
            required
            value={idEquipoNuevo}
            onChange={(v) => setIdEquipoNuevo(v as number | "")}
            opciones={(catalogos.data?.equipos ?? []).map((eq) => ({ valor: eq.id, etiqueta: eq.nombre }))}
          />
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
          <Button color="error" onClick={() => setModalSprint(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => void guardarSprint()}
            disabled={idEquipoNuevo === "" || nombre.trim().length === 0 || !fechaInicio || !fechaFin}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalEditar} onClose={() => setModalEditar(false)} fullWidth maxWidth="sm">
        <DialogTitle>Modificar sprint</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre" value={nombreEditar}
            onChange={(e) => setNombreEditar(e.target.value)} />
          <TextField size="small" label="Objetivo del sprint" multiline minRows={2}
            value={objetivoEditar} onChange={(e) => setObjetivoEditar(e.target.value)} />
          <TextField size="small" type="date" required label="Inicio" value={fechaInicioEditar}
            onChange={(e) => setFechaInicioEditar(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" required label="Fin" value={fechaFinEditar}
            onChange={(e) => setFechaFinEditar(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalEditar(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => void guardarEdicionSprint()}
            disabled={nombreEditar.trim().length === 0 || !fechaInicioEditar || !fechaFinEditar}>
            Guardar
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
          <Button color="error" onClick={() => setModalCierre(false)}>Cancelar</Button>
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

      <Dialog open={modalRelease} onClose={() => setModalRelease(false)} fullWidth maxWidth="sm">
        <DialogTitle>Enviar a release</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          {cobertura.isLoading && <LinearProgress />}
          {cobertura.data && cobertura.data.proyectos.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              No hay elementos de este sprint pendientes de enviar a un release: o no hubo
              terminados, o ya todos tienen release.
            </Typography>
          )}
          {cobertura.data?.proyectos.map((p) => (
            <Paper key={p.idProyecto} variant="outlined" sx={{ p: 1.5 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {p.claveProyecto} - {p.proyecto}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
                {p.disponibles.length} elemento(s) listo(s) para enviar
                {p.bloqueados.length > 0
                  && ` - ${p.bloqueados.length} con hallazgos de revision pendientes (no entran todavia)`}
              </Typography>
              {p.disponibles.map((it) => (
                <Typography key={it.idWorkItem} variant="caption" sx={{ display: "block" }}>
                  {it.folio} - {it.titulo}
                </Typography>
              ))}
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: "center", flexWrap: "wrap" }}>
                {p.idReleaseEnPreparacion ? (
                  <Button size="small" variant="contained"
                    onClick={() => void enviarARelease(p.idProyecto, p.idReleaseEnPreparacion)}>
                    Agregar al release {p.versionEnPreparacion} ({p.folioReleaseEnPreparacion})
                  </Button>
                ) : (
                  <>
                    <TextField size="small" label="Version nueva" placeholder="1.0.0"
                      value={versionesNuevas[p.idProyecto] ?? ""}
                      onChange={(e) => setVersionesNuevas((v) => ({ ...v, [p.idProyecto]: e.target.value }))}
                      sx={{ minWidth: 140 }} />
                    <Button size="small" variant="contained"
                      disabled={!(versionesNuevas[p.idProyecto] ?? "").trim()}
                      onClick={() => void enviarARelease(p.idProyecto, null)}>
                      Crear release y agregar
                    </Button>
                  </>
                )}
              </Stack>
            </Paper>
          ))}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalRelease(false)}>Cerrar</Button>
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
