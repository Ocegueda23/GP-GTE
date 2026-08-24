import { useEffect, useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, Menu, MenuItem, Paper,
  Snackbar, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import UpgradeIcon from "@mui/icons-material/UpgradeOutlined";
import { alpha } from "@mui/material/styles";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { obtenerCatalogosBandeja, type AccionDisponible, type CatalogosBandeja } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
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
  // Sin filtro = abiertos (todos menos Cerrado), igual que la Bandeja de trabajo;
  // "Todos" (-1) sigue disponible como opcion explicita en el combo de abajo.
  const [estatus, setEstatus] = useState<number[]>([]);
  const [idAsignado, setIdAsignado] = useState<number | null>(null);
  const [ordenarPor, setOrdenarPor] = useState<string | null>(null);
  const [ordenDescendente, setOrdenDescendente] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();
  const sesion = useSesion((estado) => estado.sesion);

  const manejarOrden = (clave: string) => {
    if (ordenarPor === clave) setOrdenDescendente((d) => !d);
    else { setOrdenarPor(clave); setOrdenDescendente(false); }
  };

  // Al entrar a la bandeja sin un Agente ya elegido, parte filtrando por el usuario firmado.
  useEffect(() => {
    if (idAsignado === null && sesion) {
      setIdAsignado(sesion.idUsuario);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo al montar la pantalla
  }, []);

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const bandeja = useQuery({
    queryKey: ["bandeja-tickets", texto, estatus, idAsignado, ordenarPor, ordenDescendente],
    queryFn: () => obtenerBandejaTickets({
      ...filtroBandejaTicketsInicial, texto, estatus, idAsignado, ordenarPor, ordenDescendente,
    }),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["bandeja-tickets"] });

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Mesa de ayuda</Typography>

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mb: 2 }}>
        <TextField size="small" label="Buscar folio, titulo o solicitante" value={texto}
          onChange={(e) => setTexto(e.target.value)} sx={{ minWidth: 300 }} />

        <ComboBuscableMultiple
          label="Estatus"
          value={estatus}
          onChange={(valores) => {
            const valor = valores as number[];
            // -1 (Todos) es excluyente: elegirlo limpia cualquier otro estatus marcado
            const eligioTodos = valor.includes(-1) && !estatus.includes(-1);
            setEstatus(eligioTodos ? [-1] : valor.filter((v) => v !== -1));
          }}
          opciones={[
            { valor: -1, etiqueta: "Todos" },
            ...(catalogos.data?.estatusTicket ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre })),
          ]}
          sx={{ minWidth: 200 }}
        />

        <ComboBuscable
          label="Agente"
          value={idAsignado ?? ""}
          onChange={(v) => setIdAsignado(v === "" ? null : Number(v))}
          opciones={[
            { valor: "", etiqueta: "Todos" },
            ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
          ]}
          sx={{ minWidth: 180 }}
        />
      </Box>

      {bandeja.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(bandeja.error as Error).message}</Alert>
      )}

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Titulo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="solicitante" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Solicitante</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="categoria" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Categoria</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="prioridad" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Prioridad</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="asignado" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Asignado</EncabezadoOrdenable>
                <TableCell>Limite resolucion</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {bandeja.data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No hay tickets con estos filtros.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {bandeja.data?.items.map((t) => {
                const vencido = t.fechaLimiteResolucion !== null
                  && new Date(t.fechaLimiteResolucion) < new Date()
                  && t.fechaResolucion === null;
                return (
                  <TableRow key={t.idTicket} hover
                    sx={(theme) => ({
                      backgroundColor: vencido
                        ? alpha(theme.palette.error.main, theme.palette.mode === "dark" ? 0.18 : 0.08)
                        : undefined,
                    })}>
                    <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                      <Typography component={RouterLink} to={`/tickets/${t.folio}`} variant="body2"
                        sx={{ fontWeight: 600, color: "info.main" }}>
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
  const [dialogoResolver, setDialogoResolver] = useState(false);
  const [solucion, setSolucion] = useState("");
  const [minutosSolucion, setMinutosSolucion] = useState<number | "">("");
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

  const ejecutar = async (
    accion: string, motivoCapturado?: string, asignado?: number,
    solucionCapturada?: string, minutosCapturados?: number,
  ) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusTicket(ticket.idTicket, {
        accion, motivo: motivoCapturado, idAsignado: asignado,
        solucion: solucionCapturada, minutosSolucion: minutosCapturados,
      });
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      setDialogoAsignar(false);
      setDialogoResolver(false);
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
    } else if (accion.accion === "RESOLVER") {
      setSolucion("");
      setMinutosSolucion("");
      setDialogoResolver(true);
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
          <Button color="error" onClick={() => setAccionConMotivo(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || motivo.trim().length === 0}
            onClick={() => accionConMotivo && void ejecutar(accionConMotivo.accion, motivo.trim())}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoAsignar} onClose={() => setDialogoAsignar(false)} fullWidth maxWidth="xs">
        <DialogTitle>Asignar {ticket.folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <ComboBuscable
            label="Agente"
            required
            value={idAsignado}
            onChange={(v) => setIdAsignado(v as number | "")}
            opciones={(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))}
          />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setDialogoAsignar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idAsignado === ""}
            onClick={() => void ejecutar("ASIGNAR", undefined, idAsignado as number)}>
            Asignar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoResolver} onClose={() => setDialogoResolver(false)} fullWidth maxWidth="sm">
        <DialogTitle>Resolver {ticket.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField autoFocus fullWidth multiline minRows={3} label="Solucion (obligatorio)"
            value={solucion} onChange={(e) => setSolucion(e.target.value)} />
          <TextField size="small" type="number" label="Minutos invertidos (obligatorio)"
            value={minutosSolucion}
            onChange={(e) => setMinutosSolucion(e.target.value === "" ? "" : Number(e.target.value))}
            slotProps={{ htmlInput: { min: 1 } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setDialogoResolver(false)}>Cancelar</Button>
          <Button variant="contained"
            disabled={enviando || solucion.trim().length === 0 || minutosSolucion === "" || minutosSolucion <= 0}
            onClick={() => void ejecutar("RESOLVER", undefined, undefined, solucion.trim(), minutosSolucion as number)}>
            Resolver
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoEscalar} onClose={() => setDialogoEscalar(false)} fullWidth maxWidth="xs">
        <DialogTitle>Escalar {ticket.folio} a elemento de trabajo</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Proyecto destino"
            required
            value={idProyecto}
            onChange={(v) => setIdProyecto(v as number | "")}
            opciones={(catalogos?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
          />
          <ComboBuscable
            label="Asignado (opcional)"
            value={idAsignadoEscalar}
            onChange={(v) => setIdAsignadoEscalar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin asignar" },
              ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setDialogoEscalar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyecto === ""} onClick={() => void escalar()}>
            Escalar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
