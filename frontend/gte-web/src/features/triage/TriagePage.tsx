import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControl, IconButton, InputLabel, Menu, MenuItem, Paper, Select,
  Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja, type AccionDisponible, type CatalogosBandeja } from "../../shared/api/workitems";
import {
  cambiarEstatusSolicitud, colorEstatusSolicitud, convertirSolicitud,
  obtenerAccionesSolicitud, obtenerTriage, type ItemConversion, type Solicitud,
} from "../../shared/api/solicitudes";

interface FilaConversion extends ItemConversion {}

/** crypto.randomUUID() exige contexto seguro (HTTPS o localhost); produccion
 * corre en HTTP plano, asi que se usa un id simple -- el uiId es solo
 * correlacion cliente-servidor de esta pantalla, no necesita ser criptografico. */
function generarUiId(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

function nuevaFila(titulo = ""): FilaConversion {
  return {
    uiId: generarUiId(),
    idTipoWorkItem: 3,   // Historia
    titulo,
    descripcion: null,
    idPrioridad: 3,      // Media
    idAsignado: null,
    fechaCompromiso: null,
  };
}

/** P08 - Triage: bandeja del lider para canalizar las solicitudes que llegan. */
export function TriagePage() {
  const [texto, setTexto] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const triage = useQuery({
    queryKey: ["triage", texto],
    queryFn: () => obtenerTriage(1, 50, texto),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["triage"] });

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Revision de solicitudes</Typography>

      <TextField size="small" label="Buscar folio, titulo o solicitante" value={texto}
        onChange={(e) => setTexto(e.target.value)} sx={{ mb: 2, minWidth: 300 }} />

      {triage.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(triage.error as Error).message}</Alert>
      )}

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <TableCell>Folio</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Solicitante</TableCell>
                <TableCell>Tipo</TableCell>
                <TableCell>Prioridad</TableCell>
                <TableCell align="center">Dias esperando</TableCell>
                <TableCell>Estatus</TableCell>
                <TableCell>Proyecto</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {triage.data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      No hay solicitudes pendientes de revision.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {triage.data?.items.map((s) => (
                <TableRow key={s.idSolicitud} hover
                  sx={{ backgroundColor: s.diasEspera >= 3 && s.idEstatus === 2 ? "#fdecea" : undefined }}>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{s.folio}</TableCell>
                  <TableCell sx={{ maxWidth: 300 }}>
                    <Tooltip title={`${s.descripcion ?? ""}\n${s.justificacionNegocio ?? ""}`.trim()}>
                      <Typography noWrap variant="body2">{s.titulo}</Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {s.usuarioSolicitante ? (
                      <Tooltip title={`A nombre de: ${s.usuarioSolicitante}`}>
                        <span>{s.solicitante}*</span>
                      </Tooltip>
                    ) : s.solicitante}
                  </TableCell>
                  <TableCell>{s.tipo}</TableCell>
                  <TableCell>{s.prioridad}</TableCell>
                  <TableCell align="center">{s.diasEspera}</TableCell>
                  <TableCell>
                    <Chip size="small" label={s.estatus} color={colorEstatusSolicitud(s.idEstatus)} />
                  </TableCell>
                  <TableCell>{s.proyecto ?? "-"}</TableCell>
                  <TableCell align="center">
                    <MenuAccionesSolicitud
                      solicitud={s}
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
  solicitud: Solicitud;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function MenuAccionesSolicitud({ solicitud, catalogos, alExito, alError }: PropsAcciones) {
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  const [acciones, setAcciones] = useState<AccionDisponible[] | null>(null);
  const [cargando, setCargando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [dialogoAprobar, setDialogoAprobar] = useState(false);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [dialogoConvertir, setDialogoConvertir] = useState(false);
  const [filas, setFilas] = useState<FilaConversion[]>([]);

  const abrirMenu = async (evento: React.MouseEvent<HTMLElement>) => {
    setAncla(evento.currentTarget);
    setCargando(true);
    try {
      setAcciones(await obtenerAccionesSolicitud(solicitud.idSolicitud));
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

  const ejecutar = async (accion: string, motivoCapturado?: string, proyecto?: number) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatusSolicitud(solicitud.idSolicitud, {
        accion,
        motivo: motivoCapturado,
        idProyecto: proyecto,
      });
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      setDialogoAprobar(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al ejecutar la accion.");
    } finally {
      setEnviando(false);
    }
  };

  const convertir = async () => {
    setEnviando(true);
    try {
      const { mensaje } = await convertirSolicitud(solicitud.idSolicitud, filas);
      alExito(mensaje);
      setDialogoConvertir(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al convertir la solicitud.");
    } finally {
      setEnviando(false);
    }
  };

  const seleccionar = (accion: AccionDisponible) => {
    cerrarMenu();
    if (accion.accion === "APROBAR") {
      setDialogoAprobar(true);
    } else if (accion.accion === "CONVERTIR") {
      setFilas([nuevaFila(solicitud.titulo)]);
      setDialogoConvertir(true);
    } else if (accion.requiereMotivo) {
      setAccionConMotivo(accion);
    } else {
      void ejecutar(accion.accion);
    }
  };

  const actualizarFila = (uiId: string, cambios: Partial<FilaConversion>) => {
    setFilas((previas) => previas.map((f) => (f.uiId === uiId ? { ...f, ...cambios } : f)));
  };

  const conversionValida = filas.length > 0 && filas.every((f) => f.titulo.trim().length > 0);

  return (
    <>
      <IconButton size="small" onClick={abrirMenu} aria-label={`Acciones de ${solicitud.folio}`}>
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
        <DialogTitle>{accionConMotivo?.etiqueta} - {solicitud.folio}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo para el solicitante (obligatorio)" value={motivo}
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

      <Dialog open={dialogoAprobar} onClose={() => setDialogoAprobar(false)} fullWidth maxWidth="xs">
        <DialogTitle>Aprobar {solicitud.folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <FormControl size="small" fullWidth required>
            <InputLabel>Proyecto destino</InputLabel>
            <Select label="Proyecto destino" value={idProyecto}
              onChange={(e) => setIdProyecto(e.target.value as number | "")}>
              {catalogos?.proyectos.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoAprobar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyecto === ""}
            onClick={() => void ejecutar("APROBAR", undefined, idProyecto as number)}>
            Aprobar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoConvertir} onClose={() => setDialogoConvertir(false)} fullWidth maxWidth="md">
        <DialogTitle>Convertir {solicitud.folio} en elementos de trabajo</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <Stack spacing={1.5}>
            {filas.map((fila) => (
              <Stack key={fila.uiId} direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <FormControl size="small" sx={{ minWidth: 130 }}>
                  <InputLabel>Tipo</InputLabel>
                  <Select label="Tipo" value={fila.idTipoWorkItem}
                    onChange={(e) => actualizarFila(fila.uiId, { idTipoWorkItem: Number(e.target.value) })}>
                    {catalogos?.tipos.map((t) => (
                      <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <TextField size="small" label="Titulo" value={fila.titulo} sx={{ flex: 1 }}
                  onChange={(e) => actualizarFila(fila.uiId, { titulo: e.target.value })} />
                <FormControl size="small" sx={{ minWidth: 120 }}>
                  <InputLabel>Prioridad</InputLabel>
                  <Select label="Prioridad" value={fila.idPrioridad}
                    onChange={(e) => actualizarFila(fila.uiId, { idPrioridad: Number(e.target.value) })}>
                    {catalogos?.prioridades.map((p) => (
                      <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 150 }}>
                  <InputLabel>Asignado</InputLabel>
                  <Select label="Asignado" value={fila.idAsignado ?? ""}
                    onChange={(e) => actualizarFila(fila.uiId, {
                      idAsignado: (e.target.value as number | "") === "" ? null : Number(e.target.value),
                    })}>
                    <MenuItem value="">Sin asignar</MenuItem>
                    {catalogos?.usuarios.map((u) => (
                      <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <TextField size="small" type="date" label="Compromiso"
                  value={fila.fechaCompromiso ?? ""}
                  onChange={(e) => actualizarFila(fila.uiId, { fechaCompromiso: e.target.value || null })}
                  slotProps={{ inputLabel: { shrink: true } }} sx={{ minWidth: 150 }} />
                <IconButton size="small" disabled={filas.length === 1}
                  onClick={() => setFilas((previas) => previas.filter((f) => f.uiId !== fila.uiId))}
                  aria-label="Quitar fila">
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button size="small" startIcon={<AddIcon />} sx={{ alignSelf: "flex-start" }}
              onClick={() => setFilas((previas) => [...previas, nuevaFila()])}>
              Agregar item
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoConvertir(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !conversionValida}
            onClick={() => void convertir()}>
            Convertir
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
