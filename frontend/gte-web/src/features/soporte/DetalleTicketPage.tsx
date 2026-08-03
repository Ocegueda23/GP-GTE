import { useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, Link, MenuItem, Paper, Rating, Select, Snackbar, Stack,
  TextField, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja, type AccionDisponible } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import {
  cambiarEstatusTicket, colorEstatusTicket, escalarTicket, obtenerAccionesTicket,
  obtenerTicketPorFolio, registrarEncuestaTicket,
} from "../../shared/api/tickets";

const ESTATUS_RESUELTO = 5;
const ESTATUS_CERRADO = 6;

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString("es-MX", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function Campo({ etiqueta, valor, resaltar }: { etiqueta: string; valor: string; resaltar?: boolean }) {
  return (
    <Box sx={{ display: "flex", justifyContent: "space-between", gap: 2, py: 0.5 }}>
      <Typography variant="body2" color="text.secondary">{etiqueta}</Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, color: resaltar ? "error.main" : undefined }}>
        {valor}
      </Typography>
    </Box>
  );
}

/** P16 - Detalle de ticket (Soporte, Solicitante). */
export function DetalleTicketPage() {
  const { folio = "" } = useParams();
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const sesion = useSesion((estado) => estado.sesion);
  const puede = useSesion((estado) => estado.puede);
  const clienteQuery = useQueryClient();

  const detalle = useQuery({
    queryKey: ["ticket", folio],
    queryFn: () => obtenerTicketPorFolio(folio),
    enabled: folio.length > 0,
  });

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

  const acciones = useQuery({
    queryKey: ["acciones-ticket", detalle.data?.idTicket],
    queryFn: () => obtenerAccionesTicket(detalle.data!.idTicket),
    enabled: detalle.data !== undefined,
  });

  const refrescarTodo = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["ticket", folio] }),
    clienteQuery.invalidateQueries({ queryKey: ["acciones-ticket"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja-tickets"] }),
    clienteQuery.invalidateQueries({ queryKey: ["mis-tickets"] }),
  ]);

  if (detalle.isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">{(detalle.error as Error).message}</Alert>
      </Box>
    );
  }
  const ticket = detalle.data;
  if (!ticket) {
    return <Box sx={{ p: 3 }}><Typography color="text.secondary">Cargando...</Typography></Box>;
  }

  const esSolicitante = sesion?.idUsuario === ticket.idSolicitante;
  const puedeCalificar = esSolicitante
    && (ticket.idEstatus === ESTATUS_RESUELTO || ticket.idEstatus === ESTATUS_CERRADO)
    && ticket.calificacion === null;
  const puedeEscalar = puede("TKT.Atender")
    && ticket.idWorkItemDerivado === null && ticket.idEstatus !== ESTATUS_CERRADO;
  const rutaOrigen = puede("TKT.Atender") ? "/soporte" : "/tickets";

  return (
    <Box sx={{ p: 2, maxWidth: 800 }}>
      <Link component={RouterLink} to={rutaOrigen} underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mb: 1 }}>
        <ArrowBackIcon fontSize="small" /> Volver
      </Link>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1} sx={{ justifyContent: "space-between", mb: 1 }}>
          <Box>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>{ticket.folio}</Typography>
              <Chip size="small" label={ticket.estatus} color={colorEstatusTicket(ticket.idEstatus)} />
            </Stack>
            <Typography variant="body1">{ticket.titulo}</Typography>
          </Box>
        </Stack>

        {ticket.descripcion && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2, whiteSpace: "pre-wrap" }}>
            {ticket.descripcion}
          </Typography>
        )}

        <BotonesAccionesTicket
          idTicket={ticket.idTicket}
          folio={ticket.folio ?? ""}
          acciones={acciones.data ?? []}
          alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
          alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
        />
        {puedeEscalar && (
          <BotonEscalar
            idTicket={ticket.idTicket}
            folio={ticket.folio ?? ""}
            proyectos={catalogos.data?.proyectos ?? []}
            usuarios={catalogos.data?.usuarios ?? []}
            alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
            alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
          />
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Campo etiqueta="Categoria" valor={ticket.categoria ?? "-"} />
        <Campo etiqueta="Prioridad" valor={ticket.prioridad} />
        <Campo etiqueta="Solicitante" valor={ticket.solicitante} />
        {ticket.usuarioSolicitante && (
          <Campo etiqueta="Usuario solicitante" valor={ticket.usuarioSolicitante} />
        )}
        {ticket.locacion && (
          <Campo etiqueta="Locacion" valor={ticket.locacion} />
        )}
        <Campo etiqueta="Asignado" valor={ticket.asignado ?? "-"} />
        <Campo etiqueta="SLA" valor={ticket.sla ?? "-"} />
        <Campo etiqueta="Limite de primera respuesta" valor={formatearFecha(ticket.fechaLimiteRespuesta)}
          resaltar={ticket.fechaPrimeraRespuesta === null && ticket.fechaLimiteRespuesta !== null
            && new Date(ticket.fechaLimiteRespuesta) < new Date()} />
        <Campo etiqueta="Primera respuesta" valor={formatearFecha(ticket.fechaPrimeraRespuesta)} />
        <Campo etiqueta="Limite de resolucion" valor={formatearFecha(ticket.fechaLimiteResolucion)}
          resaltar={ticket.fechaResolucion === null && ticket.fechaLimiteResolucion !== null
            && new Date(ticket.fechaLimiteResolucion) < new Date()} />
        <Campo etiqueta="Resolucion" valor={formatearFecha(ticket.fechaResolucion)} />
        <Campo etiqueta="Registrado" valor={formatearFecha(ticket.fechaRegistro)} />
        {ticket.minutosSolucion !== null && (
          <Campo etiqueta="Tiempo de solucion" valor={`${ticket.minutosSolucion} min`} />
        )}
        {ticket.solucion && (
          <Box sx={{ pt: 1 }}>
            <Typography variant="body2" color="text.secondary">Solucion</Typography>
            <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{ticket.solucion}</Typography>
          </Box>
        )}
        {ticket.folioWorkItemDerivado && (
          <Box sx={{ display: "flex", justifyContent: "space-between", gap: 2, py: 0.5 }}>
            <Typography variant="body2" color="text.secondary">Escalado a</Typography>
            <Link component={RouterLink} to={`/wi/${ticket.folioWorkItemDerivado}`} variant="body2" sx={{ fontWeight: 600 }}>
              {ticket.folioWorkItemDerivado}
            </Link>
          </Box>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Satisfaccion</Typography>
        {ticket.calificacion !== null ? (
          <Stack spacing={0.5}>
            <Rating value={ticket.calificacion} readOnly />
            {ticket.comentarioEncuesta && (
              <Typography variant="body2" color="text.secondary">{ticket.comentarioEncuesta}</Typography>
            )}
          </Stack>
        ) : puedeCalificar ? (
          <FormularioEncuesta idTicket={ticket.idTicket}
            alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescarTodo(); }}
            alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
        ) : (
          <Typography variant="body2" color="text.secondary">Sin calificacion todavia.</Typography>
        )}
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

function BotonesAccionesTicket({ idTicket, folio, acciones, alExito, alError }: {
  idTicket: number; folio: string; acciones: AccionDisponible[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [dialogoAsignar, setDialogoAsignar] = useState(false);
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [dialogoResolver, setDialogoResolver] = useState(false);
  const [solucion, setSolucion] = useState("");
  const [minutosSolucion, setMinutosSolucion] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const catalogos = useQuery({ queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000 });

  const ejecutar = async (
    accion: string, motivoCapturado?: string, asignado?: number,
    solucionCapturada?: string, minutosCapturados?: number,
  ) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusTicket(idTicket, {
        accion, motivo: motivoCapturado, idAsignado: asignado,
        solucion: solucionCapturada, minutosSolucion: minutosCapturados,
      });
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      setDialogoAsignar(false);
      setDialogoResolver(false);
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
          <Button key={accion.accion} size="small"
            variant={accion.esAccionPrincipal ? "contained" : "outlined"}
            disabled={enviando}
            onClick={() => {
              if (accion.accion === "ASIGNAR") { setIdAsignado(""); setDialogoAsignar(true); }
              else if (accion.accion === "RESOLVER") { setSolucion(""); setMinutosSolucion(""); setDialogoResolver(true); }
              else if (accion.requiereMotivo) setAccionConMotivo(accion);
              else void ejecutar(accion.accion);
            }}>
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

      <Dialog open={dialogoAsignar} onClose={() => setDialogoAsignar(false)} fullWidth maxWidth="xs">
        <DialogTitle>Asignar {folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Agente</InputLabel>
            <Select label="Agente" value={idAsignado} onChange={(e) => setIdAsignado(e.target.value as number | "")}>
              {catalogos.data?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoAsignar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idAsignado === ""}
            onClick={() => void ejecutar("ASIGNAR", undefined, idAsignado as number)}>
            Asignar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoResolver} onClose={() => setDialogoResolver(false)} fullWidth maxWidth="sm">
        <DialogTitle>Resolver {folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField autoFocus fullWidth multiline minRows={3} label="Solucion (obligatorio)"
            value={solucion} onChange={(e) => setSolucion(e.target.value)} />
          <TextField size="small" type="number" label="Minutos invertidos (obligatorio)"
            value={minutosSolucion}
            onChange={(e) => setMinutosSolucion(e.target.value === "" ? "" : Number(e.target.value))}
            slotProps={{ htmlInput: { min: 1 } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoResolver(false)}>Cancelar</Button>
          <Button variant="contained"
            disabled={enviando || solucion.trim().length === 0 || minutosSolucion === "" || minutosSolucion <= 0}
            onClick={() => void ejecutar("RESOLVER", undefined, undefined, solucion.trim(), minutosSolucion as number)}>
            Resolver
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function BotonEscalar({ idTicket, folio, proyectos, usuarios, alExito, alError }: {
  idTicket: number; folio: string;
  proyectos: { id: number; clave: string; nombre: string }[];
  usuarios: { id: number; nombre: string }[];
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [fechaCompromiso, setFechaCompromiso] = useState("");
  const [enviando, setEnviando] = useState(false);

  const escalar = async () => {
    if (idProyecto === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await escalarTicket(idTicket, {
        idProyecto: idProyecto as number,
        idAsignado: idAsignado === "" ? undefined : (idAsignado as number),
        fechaCompromiso: fechaCompromiso || undefined,
      });
      alExito(mensaje);
      setAbierto(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al escalar el ticket.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Button size="small" sx={{ mt: 1 }} onClick={() => { setIdProyecto(""); setIdAsignado(""); setFechaCompromiso(""); setAbierto(true); }}>
        Escalar a elemento de trabajo
      </Button>

      <Dialog open={abierto} onClose={() => setAbierto(false)} fullWidth maxWidth="xs">
        <DialogTitle>Escalar {folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Proyecto destino</InputLabel>
            <Select label="Proyecto destino" value={idProyecto} onChange={(e) => setIdProyecto(e.target.value as number | "")}>
              {proyectos.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" fullWidth>
            <InputLabel>Asignado (opcional)</InputLabel>
            <Select label="Asignado (opcional)" value={idAsignado} onChange={(e) => setIdAsignado(e.target.value as number | "")}>
              <MenuItem value="">Sin asignar</MenuItem>
              {usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAbierto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyecto === ""} onClick={() => void escalar()}>
            Escalar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function FormularioEncuesta({ idTicket, alExito, alError }: {
  idTicket: number; alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [calificacion, setCalificacion] = useState<number | null>(null);
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);

  const calificar = async () => {
    if (!calificacion) return;
    setEnviando(true);
    try {
      const { mensaje } = await registrarEncuestaTicket(idTicket, calificacion, comentario.trim() || undefined);
      alExito(mensaje);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al registrar la calificacion.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Stack spacing={1} sx={{ maxWidth: 400 }}>
      <Rating value={calificacion} onChange={(_, valor) => setCalificacion(valor)} />
      <TextField size="small" label="Comentario (opcional)" multiline minRows={2}
        value={comentario} onChange={(e) => setComentario(e.target.value)} />
      <Button variant="contained" size="small" sx={{ alignSelf: "flex-start" }}
        disabled={enviando || !calificacion} onClick={() => void calificar()}>
        Enviar calificacion
      </Button>
    </Stack>
  );
}
