import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, LinearProgress, MenuItem, Paper, Select,
  Snackbar, Stack, TextField, Tooltip, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { useSesion } from "../../shared/api/sesion";
import {
  asignarLiderSprint, asignarSprint, cambiarEstatusSprint, editarSprint, obtenerBacklog,
  obtenerCapacidad, obtenerItemsSprint, obtenerSprint, reordenarBacklog,
} from "../../shared/api/planeacion";
import { obtenerCatalogosBandeja, type BandejaItem } from "../../shared/api/workitems";
import { obtenerCoberturaReleaseSprint, enviarSprintARelease } from "../../shared/api/entregas";
import { formatearFecha } from "../entregas/formato";
import { FilaItemBacklog } from "./FilaItemBacklog";

const PLANEADO = 1;
const ACTIVO = 2;
const CERRADO = 3;

/**
 * Detalle de un sprint: encabezado con folio, estatus y lider editable, avance del
 * compromiso, las transiciones de estatus y la asignacion de elementos del backlog al
 * sprint. Es la unica pantalla donde se compromete contenido (la pestana Backlog solo
 * prioriza). Ruta propia /sprints/:id, igual que /releases/:id y /wi/:folio.
 */
export function DetalleSprintPage() {
  const { id } = useParams<{ id: string }>();
  const idSprint = Number(id);
  const puede = useSesion((estado) => estado.puede);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [modalEditar, setModalEditar] = useState(false);
  const [nombreEditar, setNombreEditar] = useState("");
  const [objetivoEditar, setObjetivoEditar] = useState("");
  const [fechaInicioEditar, setFechaInicioEditar] = useState("");
  const [fechaFinEditar, setFechaFinEditar] = useState("");
  const [modalCierre, setModalCierre] = useState(false);
  const [destinoCierre, setDestinoCierre] = useState("Backlog");
  const [modalRelease, setModalRelease] = useState(false);
  const [versionesNuevas, setVersionesNuevas] = useState<Record<number, string>>({});
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });
  const detalle = useQuery({
    queryKey: ["sprint", idSprint],
    queryFn: () => obtenerSprint(idSprint),
    enabled: Number.isFinite(idSprint) && idSprint > 0,
  });

  const sprintValido = Number.isFinite(idSprint) && idSprint > 0;
  const proyectoActual = idProyecto === ""
    ? catalogos.data?.proyectos[0]?.id
    : (idProyecto as number);

  const backlog = useQuery({
    queryKey: ["backlog", proyectoActual],
    queryFn: () => obtenerBacklog(proyectoActual!),
    enabled: proyectoActual !== undefined,
  });

  const itemsSprint = useQuery({
    queryKey: ["items-sprint", idSprint],
    queryFn: () => obtenerItemsSprint(idSprint),
    enabled: sprintValido,
  });

  const capacidad = useQuery({
    queryKey: ["capacidad", idSprint],
    queryFn: () => obtenerCapacidad(idSprint),
    enabled: sprintValido,
  });

  // Paso aparte tras cerrar el sprint (RN-GTE-018 complementaria): que le falta a cada
  // proyecto para no dejar nada terminado fuera de un release.
  const cobertura = useQuery({
    queryKey: ["cobertura-release", idSprint],
    queryFn: () => obtenerCoberturaReleaseSprint(idSprint),
    enabled: modalRelease && sprintValido,
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["sprints"] }),
    clienteQuery.invalidateQueries({ queryKey: ["sprint", idSprint] }),
    clienteQuery.invalidateQueries({ queryKey: ["backlog"] }),
    clienteQuery.invalidateQueries({ queryKey: ["backlog-global"] }),
    clienteQuery.invalidateQueries({ queryKey: ["items-sprint"] }),
    clienteQuery.invalidateQueries({ queryKey: ["capacidad"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
    clienteQuery.invalidateQueries({ queryKey: ["tablero"] }),
    clienteQuery.invalidateQueries({ queryKey: ["cobertura-release"] }),
    clienteQuery.invalidateQueries({ queryKey: ["releases"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : respaldo });
    } finally {
      await refrescar();
      setEnviando(false);
    }
  };

  const s = detalle.data;
  const editable = s?.idEstatus !== CERRADO;

  const abrirModalEditar = () => {
    if (!s) return;
    setNombreEditar(s.nombre);
    setObjetivoEditar(s.objetivo ?? "");
    setFechaInicioEditar(s.fechaInicio);
    setFechaFinEditar(s.fechaFin);
    setModalEditar(true);
  };

  const guardarEdicion = () => manejar(() => editarSprint(idSprint, {
    nombre: nombreEditar.trim(),
    objetivo: objetivoEditar.trim() || null,
    fechaInicio: fechaInicioEditar,
    fechaFin: fechaFinEditar,
  }).then((r) => { setModalEditar(false); return r; }), "No se pudo modificar el sprint.");

  const mover = (item: BandejaItem, aSprint: boolean) =>
    manejar(() => asignarSprint(item.idWorkItem, aSprint ? idSprint : null),
      "No se pudo mover el elemento.");

  const reordenar = (indice: number, direccion: -1 | 1) => {
    const items = backlog.data?.items ?? [];
    const destino = indice + direccion;
    if (destino < 0 || destino >= items.length) return;
    const ids = items.map((i) => i.idWorkItem);
    [ids[indice], ids[destino]] = [ids[destino], ids[indice]];
    void manejar(() => reordenarBacklog(ids), "No se pudo reordenar el backlog.");
  };

  const enviarARelease = (idProyectoRelease: number, idReleaseExistente: number | null) =>
    manejar(() => enviarSprintARelease(idSprint, {
      idProyecto: idProyectoRelease,
      idReleaseExistente,
      versionNueva: idReleaseExistente
        ? null
        : (versionesNuevas[idProyectoRelease] ?? "").trim() || null,
    }).then((r) => {
      setVersionesNuevas((v) => ({ ...v, [idProyectoRelease]: "" }));
      return r;
    }), "No se pudo enviar el contenido al release.");

  const porcentajeItems = s && s.totalItems > 0 ? (s.itemsTerminados / s.totalItems) * 100 : 0;
  const porcentajePuntos = s && s.puntosComprometidos > 0
    ? (s.puntosTerminados / s.puntosComprometidos) * 100 : 0;

  const excedeCapacidad = capacidad.data !== undefined
    && capacidad.data.horasComprometidas > capacidad.data.horasCapacidad
    && capacidad.data.horasCapacidad > 0;

  if (detalle.isLoading) {
    return <Box sx={{ p: 2 }}><LinearProgress /></Box>;
  }

  if (!s) {
    return (
      <Box sx={{ p: 2 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navegar("/sprints")} sx={{ mb: 2 }}>
          Volver a sprints
        </Button>
        <Alert severity="error">No se encontro el sprint solicitado.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 2 }}>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navegar("/sprints")} sx={{ mb: 1.5 }}>
        Volver a sprints
      </Button>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}
          sx={{ justifyContent: "space-between" }}>
          <Box>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", flexWrap: "wrap" }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>{s.nombre}</Typography>
              <Chip size="small" label={s.estatus}
                color={s.idEstatus === ACTIVO ? "success" : "default"} />
              {s.folio && <Chip size="small" variant="outlined" label={s.folio} />}
            </Stack>
            <Typography variant="body2" color="text.secondary">
              creado por {s.creadoPor} el {formatearFecha(s.fechaCreacion)} -{" "}
              {formatearFecha(s.fechaInicio)} al {formatearFecha(s.fechaFin)}
              {s.fechaCierre && ` - terminado ${formatearFecha(s.fechaCierre)}`}
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", alignItems: "flex-start" }}>
            {s.idEstatus === PLANEADO && puede("PLA.CambiarEstatusSprint") && (
              <Button size="small" variant="contained"
                onClick={() => void manejar(
                  () => cambiarEstatusSprint(s.idSprint, { accion: "ACTIVAR" }),
                  "No se pudo activar el sprint.")}>
                Activar
              </Button>
            )}
            {s.idEstatus === ACTIVO && puede("PLA.CambiarEstatusSprint") && (
              <Button size="small" variant="outlined"
                onClick={() => void manejar(
                  () => cambiarEstatusSprint(s.idSprint, { accion: "VOLVER_PLANEADO" }),
                  "No se pudo volver el sprint a planeado.")}>
                Volver a planeado
              </Button>
            )}
            {s.idEstatus === ACTIVO && puede("PLA.CerrarSprint") && (
              <Button size="small" variant="outlined" onClick={() => setModalCierre(true)}>
                Cerrar sprint
              </Button>
            )}
            {s.idEstatus === CERRADO && (
              <Button size="small" variant="contained" onClick={() => setModalRelease(true)}>
                Enviar a release
              </Button>
            )}
            {editable && puede("PLA.ModificarSprint") && (
              <Button size="small" startIcon={<EditOutlinedIcon />} onClick={abrirModalEditar}>
                Modificar
              </Button>
            )}
          </Stack>
        </Stack>

        <Box sx={{ mt: 2, maxWidth: 360 }}>
          <ComboBuscable
            label="Lider asignado"
            disabled={enviando || !editable}
            value={s.idLider ?? ""}
            onChange={(v) => void manejar(
              () => asignarLiderSprint(s.idSprint, v === "" ? null : Number(v))
                .then((res) => ({ mensaje: res.mensaje })),
              "No se pudo asignar el lider.")}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({
              valor: u.id, etiqueta: u.nombre,
            }))}
          />
        </Box>

        {s.objetivo && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2">Objetivo</Typography>
            <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{s.objetivo}</Typography>
          </Box>
        )}

        <Box sx={{ mt: 2 }}>
          <Stack direction="row" sx={{ justifyContent: "space-between", mb: 0.5 }}>
            <Typography variant="caption" color="text.secondary">
              Elementos terminados: {s.itemsTerminados}/{s.totalItems}
            </Typography>
          </Stack>
          <LinearProgress variant="determinate" value={porcentajeItems}
            sx={{ mb: 1.5, height: 8, borderRadius: 4 }} />

          <Stack direction="row" sx={{ justifyContent: "space-between", mb: 0.5 }}>
            <Typography variant="caption" color="text.secondary">
              Puntos terminados: {s.puntosTerminados}/{s.puntosComprometidos}
            </Typography>
          </Stack>
          <LinearProgress variant="determinate" value={porcentajePuntos}
            sx={{ height: 8, borderRadius: 4 }} />
        </Box>
      </Paper>

      <Stack direction={{ xs: "column", lg: "row" }} spacing={2}>
        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>Backlog</Typography>
            <ComboBuscable
              label="Proyecto"
              value={proyectoActual ?? ""}
              onChange={(v) => setIdProyecto(v as number | "")}
              opciones={(catalogos.data?.proyectos ?? []).map((p) => ({
                valor: p.id, etiqueta: `${p.clave} - ${p.nombre}`,
              }))}
              sx={{ minWidth: 320 }}
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
                <IconButton size="small" disabled={indice === 0 || enviando}
                  onClick={() => reordenar(indice, -1)} aria-label="Subir prioridad">
                  <ArrowUpwardIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" disabled={enviando
                  || indice === (backlog.data?.items.length ?? 0) - 1}
                  onClick={() => reordenar(indice, 1)} aria-label="Bajar prioridad">
                  <ArrowDownwardIcon fontSize="small" />
                </IconButton>
                <Tooltip title={editable
                  ? "Mover al sprint"
                  : "El sprint esta cerrado: ya no admite elementos"}>
                  <span>
                    <IconButton size="small" disabled={!editable || enviando}
                      onClick={() => void mover(item, true)} aria-label="Mover al sprint">
                      <ArrowForwardIcon fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              </Stack>
            } />
          ))}
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
            Comprometido en este sprint
          </Typography>

          {capacidad.data && (
            <Alert severity={excedeCapacidad ? "warning" : "info"} sx={{ mb: 1 }}>
              Capacidad {capacidad.data.horasCapacidad} h - comprometido{" "}
              {capacidad.data.horasComprometidas} h
              {excedeCapacidad && " (el compromiso excede la capacidad del equipo)"}
              {capacidad.data.personas.length === 0
                && " - el equipo no tiene miembros con horario asignado"}
            </Alert>
          )}

          {itemsSprint.isLoading && <LinearProgress />}
          {itemsSprint.data?.items.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
              Sin elementos comprometidos. Muevelos desde el backlog con la flecha.
            </Typography>
          )}
          {itemsSprint.data?.items.map((item) => (
            <FilaItemBacklog key={item.idWorkItem} item={item} acciones={
              <Tooltip title={editable
                ? "Regresar al backlog"
                : "El sprint esta cerrado: su contenido ya no se mueve"}>
                <span>
                  <IconButton size="small" sx={{ flexShrink: 0 }} disabled={!editable || enviando}
                    onClick={() => void mover(item, false)} aria-label="Regresar al backlog">
                    <ArrowBackIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            } />
          ))}
        </Paper>
      </Stack>

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
          <Button variant="contained" onClick={() => void guardarEdicion()}
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
            void manejar(() => cambiarEstatusSprint(s.idSprint, {
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

      <Snackbar open={aviso !== null} autoHideDuration={8000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
