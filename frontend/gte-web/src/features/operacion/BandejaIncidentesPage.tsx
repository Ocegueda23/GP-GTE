import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControl, IconButton, InputLabel, Menu, MenuItem, Paper, Select,
  Snackbar, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import BuildIcon from "@mui/icons-material/Build";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja, type AccionDisponible, type CatalogosBandeja } from "../../shared/api/workitems";
import {
  cambiarEstatusIncidente, cambiarSeveridadIncidente, colorEstatusIncidente, colorSeveridad,
  crearIncidente, filtroBandejaIncidentesInicial, obtenerAccionesIncidente,
  obtenerBandejaIncidentes, vincularCorrectivo, type Incidente,
} from "../../shared/api/incidentes";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString("es-MX", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
}

function fechaLocalAhora(): string {
  const ahora = new Date();
  ahora.setMinutes(ahora.getMinutes() - ahora.getTimezoneOffset());
  return ahora.toISOString().slice(0, 16);
}

/** P17 - Incidentes: bandeja de operacion (permiso INC.Gestionar). */
export function BandejaIncidentesPage() {
  const [texto, setTexto] = useState("");
  const [modal, setModal] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idSeveridad, setIdSeveridad] = useState<number | "">("");
  const [titulo, setTitulo] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [fechaOcurrencia, setFechaOcurrencia] = useState(fechaLocalAhora());
  const [fechaDeteccion, setFechaDeteccion] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const bandeja = useQuery({
    queryKey: ["bandeja-incidentes", texto],
    queryFn: () => obtenerBandejaIncidentes({ ...filtroBandejaIncidentesInicial, texto }),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["bandeja-incidentes"] });

  const valido = titulo.trim().length > 0 && idProyecto !== "" && idSeveridad !== "" && fechaOcurrencia !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await crearIncidente({
        idProyecto: idProyecto as number,
        idSeveridad: idSeveridad as number,
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        fechaOcurrencia: new Date(fechaOcurrencia).toISOString(),
        fechaDeteccion: fechaDeteccion ? new Date(fechaDeteccion).toISOString() : null,
      });
      setAviso({ tipo: "success", mensaje });
      setModal(false);
      setTitulo("");
      setDescripcion("");
      setIdProyecto("");
      setIdSeveridad("");
      setFechaOcurrencia(fechaLocalAhora());
      setFechaDeteccion("");
      await refrescar();
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : "Error al registrar el incidente." });
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Incidentes</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo incidente
        </Button>
      </Box>

      <TextField size="small" label="Buscar folio o titulo" value={texto}
        onChange={(e) => setTexto(e.target.value)} sx={{ mb: 2, minWidth: 300 }} />

      {bandeja.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(bandeja.error as Error).message}</Alert>
      )}

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <TableCell>Folio</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Proyecto</TableCell>
                <TableCell>Severidad</TableCell>
                <TableCell>Estatus</TableCell>
                <TableCell>Ocurrencia</TableCell>
                <TableCell>Resolucion</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {bandeja.data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No hay incidentes abiertos.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {bandeja.data?.items.map((i) => (
                <TableRow key={i.idIncidente} hover>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                    <Typography component={RouterLink} to={`/operacion/incidentes/${i.folio}`} variant="body2"
                      sx={{ fontWeight: 600, color: "inherit" }}>
                      {i.folio}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 280 }}>
                    <Tooltip title={i.descripcion ?? ""}>
                      <Typography noWrap variant="body2">{i.titulo}</Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{i.proyecto}</TableCell>
                  <TableCell>
                    <Chip size="small" label={i.severidad} color={colorSeveridad(i.idSeveridad)} />
                  </TableCell>
                  <TableCell>
                    <Chip size="small" label={i.estatus} color={colorEstatusIncidente(i.idEstatus)} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(i.fechaOcurrencia)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(i.fechaResolucion)}</TableCell>
                  <TableCell align="center">
                    <MenuAccionesIncidente
                      incidente={i}
                      catalogos={catalogos.data}
                      alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescar(); }}
                      alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo incidente</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" required>
            <InputLabel>Proyecto</InputLabel>
            <Select label="Proyecto" value={idProyecto} onChange={(e) => setIdProyecto(e.target.value as number | "")}>
              {catalogos.data?.proyectos.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" required>
            <InputLabel>Severidad</InputLabel>
            <Select label="Severidad" value={idSeveridad} onChange={(e) => setIdSeveridad(e.target.value as number | "")}>
              {catalogos.data?.severidades.map((s) => (
                <MenuItem key={s.id} value={s.id}>{s.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" required label="Titulo" value={titulo}
            onChange={(e) => setTitulo(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
          <TextField size="small" type="datetime-local" required label="Fecha de ocurrencia" value={fechaOcurrencia}
            onChange={(e) => setFechaOcurrencia(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="datetime-local" label="Fecha de deteccion (opcional)" value={fechaDeteccion}
            onChange={(e) => setFechaDeteccion(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" label="Descripcion" multiline minRows={3}
            value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
            Registrar
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

interface PropsAcciones {
  incidente: Incidente;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function MenuAccionesIncidente({ incidente, catalogos, alExito, alError }: PropsAcciones) {
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  const [acciones, setAcciones] = useState<AccionDisponible[] | null>(null);
  const [cargando, setCargando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [dialogoSeveridad, setDialogoSeveridad] = useState(false);
  const [nuevaSeveridad, setNuevaSeveridad] = useState<number | "">("");
  const [motivoSeveridad, setMotivoSeveridad] = useState("");
  const [dialogoCorrectivo, setDialogoCorrectivo] = useState(false);
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [fechaCompromiso, setFechaCompromiso] = useState("");

  const abrirMenu = async (evento: React.MouseEvent<HTMLElement>) => {
    setAncla(evento.currentTarget);
    setCargando(true);
    try {
      setAcciones(await obtenerAccionesIncidente(incidente.idIncidente));
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudieron consultar las acciones.");
      setAncla(null);
    } finally {
      setCargando(false);
    }
  };

  const cerrarMenu = () => {
    setAncla(null);
    setAcciones(null);
  };

  const ejecutar = async (accion: string, motivoCapturado?: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusIncidente(incidente.idIncidente, accion, motivoCapturado);
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al ejecutar la accion.");
    } finally {
      setEnviando(false);
    }
  };

  const cambiarSeveridad = async () => {
    if (nuevaSeveridad === "" || motivoSeveridad.trim().length === 0) return;
    setEnviando(true);
    try {
      const { mensaje } = await cambiarSeveridadIncidente(incidente.idIncidente, nuevaSeveridad as number, motivoSeveridad.trim());
      alExito(mensaje);
      setDialogoSeveridad(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al cambiar la severidad.");
    } finally {
      setEnviando(false);
    }
  };

  const vincular = async () => {
    if (idPrioridad === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await vincularCorrectivo(incidente.idIncidente, {
        idPrioridad: idPrioridad as number,
        idAsignado: idAsignado === "" ? undefined : (idAsignado as number),
        fechaCompromiso: fechaCompromiso || undefined,
      });
      alExito(mensaje);
      setDialogoCorrectivo(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al vincular el correctivo.");
    } finally {
      setEnviando(false);
    }
  };

  const seleccionar = (accion: AccionDisponible) => {
    cerrarMenu();
    if (accion.requiereMotivo) setAccionConMotivo(accion);
    else void ejecutar(accion.accion);
  };

  return (
    <>
      <IconButton size="small" onClick={abrirMenu} aria-label={`Acciones de ${incidente.folio}`}>
        {cargando ? <CircularProgress size={18} /> : <MoreVertIcon fontSize="small" />}
      </IconButton>
      <Tooltip title="Cambiar severidad">
        <IconButton size="small" onClick={() => { setNuevaSeveridad(incidente.idSeveridad); setMotivoSeveridad(""); setDialogoSeveridad(true); }}>
          <AddIcon fontSize="small" sx={{ transform: "rotate(45deg)" }} />
        </IconButton>
      </Tooltip>
      {!incidente.idWorkItemCorrectivo && (
        <Tooltip title="Vincular correctivo">
          <IconButton size="small" onClick={() => { setIdPrioridad(""); setIdAsignado(""); setFechaCompromiso(""); setDialogoCorrectivo(true); }}>
            <BuildIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      )}

      <Menu anchorEl={ancla} open={ancla !== null && acciones !== null} onClose={cerrarMenu}>
        {acciones?.length === 0 && <MenuItem disabled>Sin acciones disponibles</MenuItem>}
        {acciones?.map((accion) => (
          <MenuItem key={accion.accion} onClick={() => seleccionar(accion)}>{accion.etiqueta}</MenuItem>
        ))}
      </Menu>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth>
        <DialogTitle>{accionConMotivo?.etiqueta} - {incidente.folio}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo (obligatorio)" value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAccionConMotivo(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || motivo.trim().length === 0}
            onClick={() => accionConMotivo && void ejecutar(accionConMotivo.accion, motivo.trim())}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoSeveridad} onClose={() => setDialogoSeveridad(false)} fullWidth maxWidth="xs">
        <DialogTitle>Cambiar severidad de {incidente.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Severidad</InputLabel>
            <Select label="Severidad" value={nuevaSeveridad} onChange={(e) => setNuevaSeveridad(e.target.value as number | "")}>
              {catalogos?.severidades.map((s) => (
                <MenuItem key={s.id} value={s.id}>{s.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" fullWidth multiline minRows={2} label="Motivo (obligatorio)"
            value={motivoSeveridad} onChange={(e) => setMotivoSeveridad(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoSeveridad(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || nuevaSeveridad === "" || motivoSeveridad.trim().length === 0}
            onClick={() => void cambiarSeveridad()}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoCorrectivo} onClose={() => setDialogoCorrectivo(false)} fullWidth maxWidth="xs">
        <DialogTitle>Vincular correctivo a {incidente.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Prioridad</InputLabel>
            <Select label="Prioridad" value={idPrioridad} onChange={(e) => setIdPrioridad(e.target.value as number | "")}>
              {catalogos?.prioridades.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" fullWidth>
            <InputLabel>Asignado (opcional)</InputLabel>
            <Select label="Asignado (opcional)" value={idAsignado} onChange={(e) => setIdAsignado(e.target.value as number | "")}>
              <MenuItem value="">Sin asignar</MenuItem>
              {catalogos?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoCorrectivo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idPrioridad === ""} onClick={() => void vincular()}>
            Vincular
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
