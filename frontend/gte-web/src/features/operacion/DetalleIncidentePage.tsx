import { useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, Link, MenuItem, Paper, Select, Snackbar, Stack,
  TextField, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja, type AccionDisponible } from "../../shared/api/workitems";
import {
  actualizarIncidente, cambiarEstatusIncidente, cambiarSeveridadIncidente, colorEstatusIncidente,
  colorSeveridad, obtenerAccionesIncidente, obtenerIncidentePorFolio, obtenerReleasesParaVincular,
  vincularCorrectivo, vincularReleaseCausante,
} from "../../shared/api/incidentes";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString("es-MX", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function Campo({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <Box sx={{ display: "flex", justifyContent: "space-between", gap: 2, py: 0.5 }}>
      <Typography variant="body2" color="text.secondary">{etiqueta}</Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, maxWidth: "60%", textAlign: "right" }}>{valor}</Typography>
    </Box>
  );
}

/** P17 - Detalle de incidente (Ops, Lider). */
export function DetalleIncidentePage() {
  const { folio = "" } = useParams();
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const detalle = useQuery({
    queryKey: ["incidente", folio],
    queryFn: () => obtenerIncidentePorFolio(folio),
    enabled: folio.length > 0,
  });

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

  const acciones = useQuery({
    queryKey: ["acciones-incidente", detalle.data?.idIncidente],
    queryFn: () => obtenerAccionesIncidente(detalle.data!.idIncidente),
    enabled: detalle.data !== undefined,
  });

  const releases = useQuery({
    queryKey: ["releases-vincular", detalle.data?.idProyecto],
    queryFn: () => obtenerReleasesParaVincular(detalle.data!.idProyecto),
    enabled: detalle.data !== undefined,
  });

  const refrescarTodo = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["incidente", folio] }),
    clienteQuery.invalidateQueries({ queryKey: ["acciones-incidente"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja-incidentes"] }),
  ]);

  if (detalle.isError) {
    return <Box sx={{ p: 3 }}><Alert severity="error">{(detalle.error as Error).message}</Alert></Box>;
  }
  const incidente = detalle.data;
  if (!incidente) {
    return <Box sx={{ p: 3 }}><Typography color="text.secondary">Cargando...</Typography></Box>;
  }

  return (
    <Box sx={{ p: 2, maxWidth: 800 }}>
      <Link component={RouterLink} to="/operacion/incidentes" underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mb: 1 }}>
        <ArrowBackIcon fontSize="small" /> Bandeja de incidentes
      </Link>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", mb: 1 }}>
          <Typography variant="h6" sx={{ fontWeight: 700 }}>{incidente.folio}</Typography>
          <Chip size="small" label={incidente.estatus} color={colorEstatusIncidente(incidente.idEstatus)} />
          <Chip size="small" label={incidente.severidad} color={colorSeveridad(incidente.idSeveridad)} />
        </Stack>
        <Typography variant="body1">{incidente.titulo}</Typography>
        {incidente.descripcion && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1, mb: 2, whiteSpace: "pre-wrap" }}>
            {incidente.descripcion}
          </Typography>
        )}

        <BotonesAccionesIncidente
          idIncidente={incidente.idIncidente}
          folio={incidente.folio ?? ""}
          acciones={acciones.data ?? []}
          alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
          alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
        />

        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", mt: 1 }}>
          <BotonEditar incidente={incidente}
            alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
            alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
          <BotonSeveridad idIncidente={incidente.idIncidente} folio={incidente.folio ?? ""}
            idSeveridadActual={incidente.idSeveridad} severidades={catalogos.data?.severidades ?? []}
            alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
            alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
          {!incidente.idWorkItemCorrectivo && (
            <BotonCorrectivo idIncidente={incidente.idIncidente} folio={incidente.folio ?? ""}
              prioridades={catalogos.data?.prioridades ?? []} usuarios={catalogos.data?.usuarios ?? []}
              alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
              alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
          )}
          <BotonReleaseCausante idIncidente={incidente.idIncidente} folio={incidente.folio ?? ""}
            releases={releases.data ?? []}
            alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
            alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Campo etiqueta="Proyecto" valor={incidente.proyecto} />
        <Campo etiqueta="Fecha de ocurrencia" valor={formatearFecha(incidente.fechaOcurrencia)} />
        <Campo etiqueta="Fecha de deteccion" valor={formatearFecha(incidente.fechaDeteccion)} />
        <Campo etiqueta="Fecha de resolucion" valor={formatearFecha(incidente.fechaResolucion)} />
        <Campo etiqueta="Minutos de indisponibilidad" valor={incidente.minutosIndisponibilidad?.toString() ?? "-"} />
        <Campo etiqueta="Causa raiz" valor={incidente.causaRaiz ?? "-"} />
        {incidente.folioWorkItemCorrectivo ? (
          <Box sx={{ display: "flex", justifyContent: "space-between", gap: 2, py: 0.5 }}>
            <Typography variant="body2" color="text.secondary">Correctivo</Typography>
            <Link component={RouterLink} to={`/wi/${incidente.folioWorkItemCorrectivo}`} variant="body2" sx={{ fontWeight: 600 }}>
              {incidente.folioWorkItemCorrectivo}
            </Link>
          </Box>
        ) : <Campo etiqueta="Correctivo" valor="-" />}
        <Campo etiqueta="Release causante" valor={incidente.versionReleaseCausante ?? "-"} />
        <Campo etiqueta="Registrado" valor={formatearFecha(incidente.fechaRegistro)} />
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

function BotonesAccionesIncidente({ idIncidente, folio, acciones, alExito, alError }: {
  idIncidente: number; folio: string; acciones: AccionDisponible[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);

  const ejecutar = async (accion: string, motivoCapturado?: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusIncidente(idIncidente, accion, motivoCapturado);
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al cambiar el estatus.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
        {acciones.map((accion) => (
          <Button key={accion.accion} size="small" variant={accion.esAccionPrincipal ? "contained" : "outlined"}
            disabled={enviando}
            onClick={() => accion.requiereMotivo ? setAccionConMotivo(accion) : void ejecutar(accion.accion)}>
            {accion.etiqueta}
          </Button>
        ))}
      </Stack>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth>
        <DialogTitle>{accionConMotivo?.etiqueta} - {folio}</DialogTitle>
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
    </>
  );
}

function BotonEditar({ incidente, alExito, alError }: {
  incidente: { idIncidente: number; titulo: string; descripcion: string | null; causaRaiz: string | null; minutosIndisponibilidad: number | null };
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [titulo, setTitulo] = useState(incidente.titulo);
  const [descripcion, setDescripcion] = useState(incidente.descripcion ?? "");
  const [causaRaiz, setCausaRaiz] = useState(incidente.causaRaiz ?? "");
  const [minutos, setMinutos] = useState(incidente.minutosIndisponibilidad?.toString() ?? "");
  const [enviando, setEnviando] = useState(false);

  const abrir = () => {
    setTitulo(incidente.titulo);
    setDescripcion(incidente.descripcion ?? "");
    setCausaRaiz(incidente.causaRaiz ?? "");
    setMinutos(incidente.minutosIndisponibilidad?.toString() ?? "");
    setAbierto(true);
  };

  const guardar = async () => {
    if (titulo.trim().length === 0) return;
    setEnviando(true);
    try {
      const { mensaje } = await actualizarIncidente(incidente.idIncidente, {
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        causaRaiz: causaRaiz.trim() || null,
        minutosIndisponibilidad: minutos.trim() ? Number(minutos) : null,
        fechaDeteccion: null,
      });
      alExito(mensaje);
      setAbierto(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al actualizar el incidente.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Button size="small" startIcon={<EditIcon fontSize="small" />} onClick={abrir}>Editar</Button>
      <Dialog open={abierto} onClose={() => setAbierto(false)} fullWidth maxWidth="sm">
        <DialogTitle>Editar incidente</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo" value={titulo}
            onChange={(e) => setTitulo(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
          <TextField size="small" label="Descripcion" multiline minRows={2} value={descripcion}
            onChange={(e) => setDescripcion(e.target.value)} />
          <TextField size="small" label="Causa raiz" multiline minRows={2} value={causaRaiz}
            onChange={(e) => setCausaRaiz(e.target.value)} />
          <TextField size="small" type="number" label="Minutos de indisponibilidad" value={minutos}
            onChange={(e) => setMinutos(e.target.value)} slotProps={{ htmlInput: { min: 0 } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAbierto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || titulo.trim().length === 0} onClick={() => void guardar()}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function BotonSeveridad({ idIncidente, folio, idSeveridadActual, severidades, alExito, alError }: {
  idIncidente: number; folio: string; idSeveridadActual: number;
  severidades: { id: number; nombre: string }[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [idSeveridad, setIdSeveridad] = useState<number | "">(idSeveridadActual);
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);

  const cambiar = async () => {
    if (idSeveridad === "" || motivo.trim().length === 0) return;
    setEnviando(true);
    try {
      const { mensaje } = await cambiarSeveridadIncidente(idIncidente, idSeveridad as number, motivo.trim());
      alExito(mensaje);
      setAbierto(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al cambiar la severidad.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Button size="small" onClick={() => { setIdSeveridad(idSeveridadActual); setMotivo(""); setAbierto(true); }}>
        Cambiar severidad
      </Button>
      <Dialog open={abierto} onClose={() => setAbierto(false)} fullWidth maxWidth="xs">
        <DialogTitle>Cambiar severidad de {folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Severidad</InputLabel>
            <Select label="Severidad" value={idSeveridad} onChange={(e) => setIdSeveridad(e.target.value as number | "")}>
              {severidades.map((s) => (<MenuItem key={s.id} value={s.id}>{s.nombre}</MenuItem>))}
            </Select>
          </FormControl>
          <TextField size="small" fullWidth multiline minRows={2} label="Motivo (obligatorio)"
            value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAbierto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idSeveridad === "" || motivo.trim().length === 0}
            onClick={() => void cambiar()}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function BotonCorrectivo({ idIncidente, folio, prioridades, usuarios, alExito, alError }: {
  idIncidente: number; folio: string;
  prioridades: { id: number; nombre: string }[]; usuarios: { id: number; nombre: string }[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [fechaCompromiso, setFechaCompromiso] = useState("");
  const [enviando, setEnviando] = useState(false);

  const vincular = async () => {
    if (idPrioridad === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await vincularCorrectivo(idIncidente, {
        idPrioridad: idPrioridad as number,
        idAsignado: idAsignado === "" ? undefined : (idAsignado as number),
        fechaCompromiso: fechaCompromiso || undefined,
      });
      alExito(mensaje);
      setAbierto(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al vincular el correctivo.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Button size="small" onClick={() => { setIdPrioridad(""); setIdAsignado(""); setFechaCompromiso(""); setAbierto(true); }}>
        Vincular correctivo
      </Button>
      <Dialog open={abierto} onClose={() => setAbierto(false)} fullWidth maxWidth="xs">
        <DialogTitle>Vincular correctivo a {folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Prioridad</InputLabel>
            <Select label="Prioridad" value={idPrioridad} onChange={(e) => setIdPrioridad(e.target.value as number | "")}>
              {prioridades.map((p) => (<MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>))}
            </Select>
          </FormControl>
          <FormControl size="small" fullWidth>
            <InputLabel>Asignado (opcional)</InputLabel>
            <Select label="Asignado (opcional)" value={idAsignado} onChange={(e) => setIdAsignado(e.target.value as number | "")}>
              <MenuItem value="">Sin asignar</MenuItem>
              {usuarios.map((u) => (<MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>))}
            </Select>
          </FormControl>
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAbierto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idPrioridad === ""} onClick={() => void vincular()}>
            Vincular
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function BotonReleaseCausante({ idIncidente, folio, releases, alExito, alError }: {
  idIncidente: number; folio: string;
  releases: { idRelease: number; version: string; folio: string | null }[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [idRelease, setIdRelease] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);

  const vincular = async () => {
    if (idRelease === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await vincularReleaseCausante(idIncidente, idRelease as number);
      alExito(mensaje);
      setAbierto(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al vincular el release.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Button size="small" onClick={() => { setIdRelease(""); setAbierto(true); }}>Vincular release causante</Button>
      <Dialog open={abierto} onClose={() => setAbierto(false)} fullWidth maxWidth="xs">
        <DialogTitle>Vincular release causante a {folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Release</InputLabel>
            <Select label="Release" value={idRelease} onChange={(e) => setIdRelease(e.target.value as number | "")}>
              {releases.map((r) => (
                <MenuItem key={r.idRelease} value={r.idRelease}>{r.version}{r.folio ? ` (${r.folio})` : ""}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAbierto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idRelease === ""} onClick={() => void vincular()}>
            Vincular
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
