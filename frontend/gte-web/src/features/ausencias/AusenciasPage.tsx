import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, Menu, MenuItem, Paper, Snackbar, Stack, Tab, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Tabs, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useSesion } from "../../shared/api/sesion";
import { obtenerCatalogosBandeja, type AccionDisponible } from "../../shared/api/workitems";
import {
  actualizarAusencia, cambiarEstatusAusencia, colorEstatusAusencia, contarAusenciasPendientes,
  crearAusencia, ESTATUS_AUSENCIA, obtenerAccionesAusencia, obtenerBandejaAusencias,
  obtenerCatalogosAusencia, obtenerMisAusencias,
  type Ausencia, type TraslapeAusencia,
} from "../../shared/api/ausencias";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** El 409 de traslape trae la lista de periodos que chocan: se pinta sin re-consultar. */
function mensajeConflicto(error: ErrorApi): string {
  const detalle = error.detalle as TraslapeAusencia[] | undefined;
  if (!Array.isArray(detalle) || detalle.length === 0) return error.message;
  const periodos = detalle
    .map((t) => `${t.tipo} (${t.estatus}) del ${formatearFecha(t.fechaInicio)} al ${formatearFecha(t.fechaFin)}`)
    .join("; ");
  return `${error.message} ${periodos}`;
}

function textoError(error: unknown, respaldo: string): string {
  if (error instanceof ErrorApi) {
    return error.code === "CONFLICT" ? mensajeConflicto(error) : error.message;
  }
  return respaldo;
}

/** Registro de ausencias: cada quien captura las suyas y quien tiene ADM.Ausencias las resuelve. */
export function AusenciasPage() {
  const puede = useSesion((estado) => estado.puede);
  const puedeGestionar = puede("ADM.Ausencias");
  const [pestana, setPestana] = useState(0);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);

  const catalogos = useQuery({
    queryKey: ["catalogos-ausencia"],
    queryFn: obtenerCatalogosAusencia,
    staleTime: 5 * 60_000,
  });
  const pendientes = useQuery({
    queryKey: ["ausencias-pendientes"],
    queryFn: contarAusenciasPendientes,
    enabled: puedeGestionar,
  });

  const notificar = (tipo: "success" | "error", mensaje: string) => setAviso({ tipo, mensaje });

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Ausencias</Typography>

      {puedeGestionar && (
        <Tabs value={pestana} onChange={(_, valor) => setPestana(valor as number)} sx={{ mb: 2 }}>
          <Tab label="Mis ausencias" />
          <Tab label={`Por aprobar${pendientes.data ? ` (${pendientes.data})` : ""}`} />
        </Tabs>
      )}

      {pestana === 0 && (
        <MisAusencias
          tipos={catalogos.data?.tipos ?? []}
          estatusCatalogo={catalogos.data?.estatus ?? []}
          puedeGestionar={puedeGestionar}
          notificar={notificar}
        />
      )}

      {pestana === 1 && puedeGestionar && (
        <BandejaAusencias
          tipos={catalogos.data?.tipos ?? []}
          estatusCatalogo={catalogos.data?.estatus ?? []}
          notificar={notificar}
        />
      )}

      <Snackbar open={aviso !== null} autoHideDuration={8000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}

interface Opcion { id: number; nombre: string }

interface PropsMisAusencias {
  tipos: Opcion[];
  estatusCatalogo: Opcion[];
  puedeGestionar: boolean;
  notificar: (tipo: "success" | "error", mensaje: string) => void;
}

function MisAusencias({ tipos, estatusCatalogo, puedeGestionar, notificar }: PropsMisAusencias) {
  const clienteQuery = useQueryClient();
  // Sin filtro = vigentes (Solicitada, Aprobada); "Todas" (-1) sigue disponible.
  const [filtroEstatus, setFiltroEstatus] = useState<number[]>([-1]);
  const [modal, setModal] = useState(false);
  const [enEdicion, setEnEdicion] = useState<Ausencia | null>(null);

  const mias = useQuery({
    queryKey: ["mis-ausencias", filtroEstatus],
    queryFn: () => obtenerMisAusencias(filtroEstatus),
  });

  const refrescar = async () => {
    await clienteQuery.invalidateQueries({ queryKey: ["mis-ausencias"] });
    await clienteQuery.invalidateQueries({ queryKey: ["ausencias-pendientes"] });
  };

  return (
    <>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Registrar ausencia
        </Button>
        <ComboBuscableMultiple
          label="Estatus"
          opciones={[
            { valor: -1, etiqueta: "Todas" },
            ...estatusCatalogo.map((e) => ({ valor: e.id, etiqueta: e.nombre })),
          ]}
          value={filtroEstatus}
          onChange={(valores) => setFiltroEstatus(valores.map(Number))}
          resumenSimple
          sx={{ minWidth: 260 }}
        />
      </Stack>

      {mias.isError && <Alert severity="error" sx={{ mb: 2 }}>{(mias.error as Error).message}</Alert>}

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <TableCell>Tipo</TableCell>
                <TableCell>Inicio</TableCell>
                <TableCell>Fin</TableCell>
                <TableCell align="center">Dias</TableCell>
                <TableCell>Estatus</TableCell>
                <TableCell>Motivo</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {mias.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No tienes ausencias registradas con ese filtro.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {mias.data?.map((a) => (
                <TableRow key={a.idAusencia} hover>
                  <TableCell>{a.tipo}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(a.fechaInicio)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(a.fechaFin)}</TableCell>
                  <TableCell align="center">{a.diasNaturales}</TableCell>
                  <TableCell>
                    <Chip size="small" label={a.estatus} color={colorEstatusAusencia(a.idEstatus)} />
                  </TableCell>
                  <TableCell sx={{ maxWidth: 280 }}>
                    <Tooltip title={a.motivo ?? ""}>
                      <Typography noWrap variant="body2">{a.motivo ?? "-"}</Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell align="center">
                    {a.idEstatus === ESTATUS_AUSENCIA.solicitada && (
                      <Tooltip title="Editar">
                        <IconButton size="small" onClick={() => setEnEdicion(a)}
                          aria-label={`Editar ausencia del ${a.fechaInicio}`}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    <MenuAccionesAusencia
                      ausencia={a}
                      alExito={(mensaje) => { notificar("success", mensaje); void refrescar(); }}
                      alError={(mensaje) => notificar("error", mensaje)}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <FormularioAusencia
        abierto={modal || enEdicion !== null}
        ausencia={enEdicion}
        tipos={tipos}
        puedeCapturarOtro={puedeGestionar}
        alCerrar={() => { setModal(false); setEnEdicion(null); }}
        alGuardar={async (mensaje) => {
          notificar("success", mensaje);
          setModal(false);
          setEnEdicion(null);
          await refrescar();
        }}
        alError={(mensaje) => notificar("error", mensaje)}
      />
    </>
  );
}

interface PropsBandeja {
  tipos: Opcion[];
  estatusCatalogo: Opcion[];
  notificar: (tipo: "success" | "error", mensaje: string) => void;
}

function BandejaAusencias({ tipos, estatusCatalogo, notificar }: PropsBandeja) {
  const clienteQuery = useQueryClient();
  const [filtroEstatus, setFiltroEstatus] = useState<number[]>([-1]);
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [idUsuario, setIdUsuario] = useState<number | "">("");
  const [desde, setDesde] = useState("");
  const [hasta, setHasta] = useState("");
  const [ordenarPor, setOrdenarPor] = useState<string | null>(null);
  const [ordenDescendente, setOrdenDescendente] = useState(false);

  const manejarOrden = (clave: string) => {
    if (ordenarPor === clave) setOrdenDescendente((d) => !d);
    else { setOrdenarPor(clave); setOrdenDescendente(false); }
  };

  const catalogosBandeja = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });

  const bandeja = useQuery({
    queryKey: ["bandeja-ausencias", filtroEstatus, idTipo, idUsuario, desde, hasta, ordenarPor, ordenDescendente],
    queryFn: () => obtenerBandejaAusencias({
      estatus: filtroEstatus,
      idTipoAusencia: idTipo === "" ? null : idTipo,
      idUsuario: idUsuario === "" ? null : idUsuario,
      desde: desde || null,
      hasta: hasta || null,
      ordenarPor,
      ordenDescendente,
    }),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = async () => {
    await clienteQuery.invalidateQueries({ queryKey: ["bandeja-ausencias"] });
    await clienteQuery.invalidateQueries({ queryKey: ["ausencias-pendientes"] });
  };

  return (
    <>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ mb: 2 }}>
        <ComboBuscableMultiple
          label="Estatus"
          opciones={[
            { valor: -1, etiqueta: "Todas" },
            ...estatusCatalogo.map((e) => ({ valor: e.id, etiqueta: e.nombre })),
          ]}
          value={filtroEstatus}
          onChange={(valores) => setFiltroEstatus(valores.map(Number))}
          resumenSimple
          sx={{ minWidth: 220 }}
        />
        <ComboBuscable
          label="Tipo"
          opciones={[{ valor: "", etiqueta: "Todos" }, ...tipos.map((t) => ({ valor: t.id, etiqueta: t.nombre }))]}
          value={idTipo}
          onChange={(valor) => setIdTipo(valor === "" ? "" : Number(valor))}
          sx={{ minWidth: 200 }}
        />
        <ComboBuscable
          label="Persona"
          opciones={[
            { valor: "", etiqueta: "Todas" },
            ...(catalogosBandeja.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
          ]}
          value={idUsuario}
          onChange={(valor) => setIdUsuario(valor === "" ? "" : Number(valor))}
          sx={{ minWidth: 240 }}
        />
        <TextField size="small" type="date" label="Desde" slotProps={{ inputLabel: { shrink: true } }}
          value={desde} onChange={(e) => setDesde(e.target.value)} />
        <TextField size="small" type="date" label="Hasta" slotProps={{ inputLabel: { shrink: true } }}
          value={hasta} onChange={(e) => setHasta(e.target.value)} />
      </Stack>

      {bandeja.isError && <Alert severity="error" sx={{ mb: 2 }}>{(bandeja.error as Error).message}</Alert>}

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <EncabezadoOrdenable clave="usuario" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Persona</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="tipo" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Tipo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaInicio" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Inicio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaFin" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Fin</EncabezadoOrdenable>
                <TableCell align="center">Dias</TableCell>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Estatus</EncabezadoOrdenable>
                <TableCell>Motivo</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {bandeja.data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No hay ausencias pendientes de aprobacion.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {bandeja.data?.items.map((a) => (
                <TableRow key={a.idAusencia} hover>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{a.usuario}</TableCell>
                  <TableCell>{a.tipo}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(a.fechaInicio)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(a.fechaFin)}</TableCell>
                  <TableCell align="center">{a.diasNaturales}</TableCell>
                  <TableCell>
                    <Chip size="small" label={a.estatus} color={colorEstatusAusencia(a.idEstatus)} />
                  </TableCell>
                  <TableCell sx={{ maxWidth: 280 }}>
                    <Tooltip title={a.motivo ?? ""}>
                      <Typography noWrap variant="body2">{a.motivo ?? "-"}</Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell align="center">
                    <MenuAccionesAusencia
                      ausencia={a}
                      alExito={(mensaje) => { notificar("success", mensaje); void refrescar(); }}
                      alError={(mensaje) => notificar("error", mensaje)}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
    </>
  );
}

interface PropsFormulario {
  abierto: boolean;
  ausencia: Ausencia | null;
  tipos: Opcion[];
  puedeCapturarOtro: boolean;
  alCerrar: () => void;
  alGuardar: (mensaje: string) => Promise<void>;
  alError: (mensaje: string) => void;
}

function FormularioAusencia({
  abierto, ausencia, tipos, puedeCapturarOtro, alCerrar, alGuardar, alError,
}: PropsFormulario) {
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [inicio, setInicio] = useState("");
  const [fin, setFin] = useState("");
  const [motivo, setMotivo] = useState("");
  const [idUsuario, setIdUsuario] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  // Sincroniza el formulario con la ausencia que se abre a editar (o lo limpia al dar de alta).
  const [ultimaAbierta, setUltimaAbierta] = useState<number | null | undefined>(undefined);

  const catalogosBandeja = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
    enabled: puedeCapturarOtro && abierto,
  });

  const clave = ausencia?.idAusencia ?? null;
  if (abierto && ultimaAbierta !== clave) {
    setUltimaAbierta(clave);
    setIdTipo(ausencia?.idTipoAusencia ?? "");
    setInicio(ausencia?.fechaInicio.slice(0, 10) ?? "");
    setFin(ausencia?.fechaFin.slice(0, 10) ?? "");
    setMotivo(ausencia?.motivo ?? "");
    setIdUsuario("");
  }

  const valido = idTipo !== "" && inicio !== "" && fin !== "" && fin >= inicio;

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const datos = {
        idTipoAusencia: idTipo as number,
        fechaInicio: inicio,
        fechaFin: fin,
        motivo: motivo.trim() || null,
      };
      const { mensaje } = ausencia
        ? await actualizarAusencia(ausencia.idAusencia, datos)
        : await crearAusencia({
            ...datos,
            idUsuario: idUsuario === "" ? null : (idUsuario as number),
          });
      setUltimaAbierta(undefined);
      await alGuardar(mensaje);
    } catch (error) {
      alError(textoError(error, "Error al guardar la ausencia."));
    } finally {
      setEnviando(false);
    }
  };

  const cerrar = () => {
    setUltimaAbierta(undefined);
    alCerrar();
  };

  return (
    <Dialog open={abierto} onClose={cerrar} fullWidth maxWidth="sm">
      <DialogTitle>{ausencia ? "Editar ausencia" : "Registrar ausencia"}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
        <ComboBuscable
          label="Tipo de ausencia"
          opciones={tipos.map((t) => ({ valor: t.id, etiqueta: t.nombre }))}
          value={idTipo}
          onChange={(valor) => setIdTipo(valor === "" ? "" : Number(valor))}
          required
        />
        {!ausencia && puedeCapturarOtro && (
          <ComboBuscable
            label="Persona (vacio = yo)"
            opciones={[
              { valor: "", etiqueta: "Yo" },
              ...(catalogosBandeja.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
            value={idUsuario}
            onChange={(valor) => setIdUsuario(valor === "" ? "" : Number(valor))}
          />
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
          <TextField fullWidth size="small" type="date" label="Inicio" required
            slotProps={{ inputLabel: { shrink: true } }} value={inicio} onChange={(e) => setInicio(e.target.value)} />
          <TextField fullWidth size="small" type="date" label="Fin" required
            slotProps={{ inputLabel: { shrink: true } }} value={fin} onChange={(e) => setFin(e.target.value)}
            error={fin !== "" && inicio !== "" && fin < inicio}
            helperText={fin !== "" && inicio !== "" && fin < inicio ? "No puede ser anterior al inicio." : " "} />
        </Stack>
        <TextField fullWidth size="small" multiline minRows={2} label="Motivo (opcional)"
          value={motivo} onChange={(e) => setMotivo(e.target.value)} slotProps={{ htmlInput: { maxLength: 500 } }} />
      </DialogContent>
      <DialogActions>
        <Button onClick={cerrar}>Cancelar</Button>
        <Button variant="contained" disabled={!valido || enviando} onClick={() => void guardar()}>
          {ausencia ? "Guardar" : "Registrar"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

interface PropsAcciones {
  ausencia: Ausencia;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Las acciones salen del backend (GET acciones): el front nunca decide la transicion. */
function MenuAccionesAusencia({ ausencia, alExito, alError }: PropsAcciones) {
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  const [acciones, setAcciones] = useState<AccionDisponible[] | null>(null);
  const [cargando, setCargando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");

  const abrirMenu = async (evento: React.MouseEvent<HTMLElement>) => {
    setAncla(evento.currentTarget);
    setCargando(true);
    try {
      setAcciones(await obtenerAccionesAusencia(ausencia.idAusencia));
    } catch (error) {
      alError(textoError(error, "No se pudieron consultar las acciones."));
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
      const { mensaje } = await cambiarEstatusAusencia(ausencia.idAusencia, {
        accion, motivo: motivoCapturado,
      });
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
    } catch (error) {
      alError(textoError(error, "Error al ejecutar la accion."));
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
      <IconButton size="small" onClick={abrirMenu}
        aria-label={`Acciones de la ausencia de ${ausencia.usuario}`}>
        {cargando ? <CircularProgress size={18} /> : <MoreVertIcon fontSize="small" />}
      </IconButton>

      <Menu anchorEl={ancla} open={ancla !== null && acciones !== null} onClose={cerrarMenu}>
        {acciones?.length === 0 && <MenuItem disabled>Sin acciones disponibles</MenuItem>}
        {acciones?.map((accion) => (
          <MenuItem key={accion.accion} onClick={() => seleccionar(accion)}>
            {accion.etiqueta}
          </MenuItem>
        ))}
      </Menu>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth>
        <DialogTitle>{accionConMotivo?.etiqueta} - {ausencia.usuario}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo para la persona (obligatorio)" value={motivo}
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
    </>
  );
}
