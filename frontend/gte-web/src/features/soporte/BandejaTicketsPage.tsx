import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControl, IconButton, InputLabel, Menu, MenuItem, Paper, Select,
  Snackbar, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import UpgradeIcon from "@mui/icons-material/UpgradeOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja, type AccionDisponible, type CatalogosBandeja } from "../../shared/api/workitems";
import {
  cambiarEstatusTicket, colorEstatusTicket, escalarTicket, filtroBandejaTicketsInicial,
  obtenerAccionesTicket, obtenerBandejaTickets, type Ticket,
} from "../../shared/api/tickets";

const ESTATUS_CERRADO = 6;

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleString("es-MX", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
}

/** P15 - Mesa de ayuda: bandeja de agentes (permiso TKT.Atender). */
export function BandejaTicketsPage() {
  const [texto, setTexto] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const bandeja = useQuery({
    queryKey: ["bandeja-tickets", texto],
    queryFn: () => obtenerBandejaTickets({ ...filtroBandejaTicketsInicial, texto }),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["bandeja-tickets"] });

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Mesa de ayuda</Typography>

      <TextField size="small" label="Buscar folio, titulo o solicitante" value={texto}
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
                <TableCell>Solicitante</TableCell>
                <TableCell>Categoria</TableCell>
                <TableCell>Prioridad</TableCell>
                <TableCell>Estatus</TableCell>
                <TableCell>Asignado</TableCell>
                <TableCell>Limite resolucion</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {bandeja.data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No hay tickets abiertos.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {bandeja.data?.items.map((t) => {
                const vencido = t.fechaLimiteResolucion !== null
                  && new Date(t.fechaLimiteResolucion) < new Date()
                  && t.fechaResolucion === null;
                return (
                  <TableRow key={t.idTicket} hover sx={{ backgroundColor: vencido ? "#fdecea" : undefined }}>
                    <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                      <Typography component={RouterLink} to={`/tickets/${t.folio}`} variant="body2"
                        sx={{ fontWeight: 600, color: "inherit" }}>
                        {t.folio}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ maxWidth: 280 }}>
                      <Tooltip title={t.descripcion ?? ""}>
                        <Typography noWrap variant="body2">{t.titulo}</Typography>
                      </Tooltip>
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{t.solicitante}</TableCell>
                    <TableCell>{t.categoria ?? "-"}</TableCell>
                    <TableCell>{t.prioridad}</TableCell>
                    <TableCell>
                      <Chip size="small" label={t.estatus} color={colorEstatusTicket(t.idEstatus)} />
                    </TableCell>
                    <TableCell>{t.asignado ?? "-"}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap", color: vencido ? "error.main" : undefined }}>
                      {formatearFecha(t.fechaLimiteResolucion)}
                    </TableCell>
                    <TableCell align="center">
                      <MenuAccionesTicket
                        ticket={t}
                        catalogos={catalogos.data}
                        alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescar(); }}
                        alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
                      />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
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

interface PropsAcciones {
  ticket: Ticket;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function MenuAccionesTicket({ ticket, catalogos, alExito, alError }: PropsAcciones) {
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  const [acciones, setAcciones] = useState<AccionDisponible[] | null>(null);
  const [cargando, setCargando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [dialogoAsignar, setDialogoAsignar] = useState(false);
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [dialogoEscalar, setDialogoEscalar] = useState(false);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idAsignadoEscalar, setIdAsignadoEscalar] = useState<number | "">("");
  const [fechaCompromiso, setFechaCompromiso] = useState("");

  const abrirMenu = async (evento: React.MouseEvent<HTMLElement>) => {
    setAncla(evento.currentTarget);
    setCargando(true);
    try {
      setAcciones(await obtenerAccionesTicket(ticket.idTicket));
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

  const ejecutar = async (accion: string, motivoCapturado?: string, asignado?: number) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusTicket(ticket.idTicket, {
        accion, motivo: motivoCapturado, idAsignado: asignado,
      });
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      setDialogoAsignar(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al ejecutar la accion.");
    } finally {
      setEnviando(false);
    }
  };

  const escalar = async () => {
    if (idProyecto === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await escalarTicket(ticket.idTicket, {
        idProyecto: idProyecto as number,
        idAsignado: idAsignadoEscalar === "" ? undefined : (idAsignadoEscalar as number),
        fechaCompromiso: fechaCompromiso || undefined,
      });
      alExito(mensaje);
      setDialogoEscalar(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al escalar el ticket.");
    } finally {
      setEnviando(false);
    }
  };

  const seleccionar = (accion: AccionDisponible) => {
    cerrarMenu();
    if (accion.accion === "ASIGNAR") {
      setIdAsignado("");
      setDialogoAsignar(true);
    } else if (accion.requiereMotivo) {
      setAccionConMotivo(accion);
    } else {
      void ejecutar(accion.accion);
    }
  };

  const puedeEscalar = ticket.idWorkItemDerivado === null && ticket.idEstatus !== ESTATUS_CERRADO;

  return (
    <>
      <IconButton size="small" onClick={abrirMenu} aria-label={`Acciones de ${ticket.folio}`}>
        {cargando ? <CircularProgress size={18} /> : <MoreVertIcon fontSize="small" />}
      </IconButton>
      {puedeEscalar && (
        <Tooltip title="Escalar a elemento de trabajo">
          <IconButton size="small" onClick={() => { setIdProyecto(""); setIdAsignadoEscalar(""); setFechaCompromiso(""); setDialogoEscalar(true); }}>
            <UpgradeIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      )}

      <Menu anchorEl={ancla} open={ancla !== null && acciones !== null} onClose={cerrarMenu}>
        {acciones?.length === 0 && <MenuItem disabled>Sin acciones disponibles</MenuItem>}
        {acciones?.map((accion) => (
          <MenuItem key={accion.accion} onClick={() => seleccionar(accion)}>
            {accion.etiqueta}
          </MenuItem>
        ))}
      </Menu>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth>
        <DialogTitle>{accionConMotivo?.etiqueta} - {ticket.folio}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo (obligatorio)" value={motivo}
            onChange={(e) => setMotivo(e.target.value)} />
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
        <DialogTitle>Asignar {ticket.folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Agente</InputLabel>
            <Select label="Agente" value={idAsignado}
              onChange={(e) => setIdAsignado(e.target.value as number | "")}>
              {catalogos?.usuarios.map((u) => (
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

      <Dialog open={dialogoEscalar} onClose={() => setDialogoEscalar(false)} fullWidth maxWidth="xs">
        <DialogTitle>Escalar {ticket.folio} a elemento de trabajo</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Proyecto destino</InputLabel>
            <Select label="Proyecto destino" value={idProyecto}
              onChange={(e) => setIdProyecto(e.target.value as number | "")}>
              {catalogos?.proyectos.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" fullWidth>
            <InputLabel>Asignado (opcional)</InputLabel>
            <Select label="Asignado (opcional)" value={idAsignadoEscalar}
              onChange={(e) => setIdAsignadoEscalar(e.target.value as number | "")}>
              <MenuItem value="">Sin asignar</MenuItem>
              {catalogos?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoEscalar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyecto === ""} onClick={() => void escalar()}>
            Escalar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
