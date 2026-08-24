import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, Menu, MenuItem, Paper,
  Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import VisibilityIcon from "@mui/icons-material/Visibility";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { alpha } from "@mui/material/styles";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { htmlATextoPlano } from "../../shared/editor/textoPlano";
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

function nuevaFila(titulo = "", descripcion: string | null = null): FilaConversion {
  return {
    uiId: generarUiId(),
    idTipoWorkItem: 3,   // Historia
    titulo,
    descripcion,
    idPrioridad: 3,      // Media
    idAsignado: null,
    fechaCompromiso: null,
  };
}

/** P08 - Triage: bandeja del lider para canalizar las solicitudes que llegan. */
export function TriagePage() {
  const [texto, setTexto] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [verDetalle, setVerDetalle] = useState<Solicitud | null>(null);
  const [ordenarPor, setOrdenarPor] = useState<string | null>(null);
  const [ordenDescendente, setOrdenDescendente] = useState(false);
  const clienteQuery = useQueryClient();

  const manejarOrden = (clave: string) => {
    if (ordenarPor === clave) setOrdenDescendente((d) => !d);
    else { setOrdenarPor(clave); setOrdenDescendente(false); }
  };

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const triage = useQuery({
    queryKey: ["triage", texto, ordenarPor, ordenDescendente],
    queryFn: () => obtenerTriage(1, 50, texto, ordenarPor, ordenDescendente),
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
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Titulo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="solicitante" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Solicitante</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="tipo" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Tipo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="prioridad" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Prioridad</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="diasEspera" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden} align="center">Dias esperando</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="proyecto" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Proyecto</EncabezadoOrdenable>
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
                  sx={(theme) => ({
                    backgroundColor: s.diasEspera >= 3 && s.idEstatus === 2
                      ? alpha(theme.palette.error.main, theme.palette.mode === "dark" ? 0.18 : 0.08)
                      : undefined,
                  })}>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{s.folio}</TableCell>
                  <TableCell sx={{ maxWidth: 300 }}>
                    <Tooltip title={`${htmlATextoPlano(s.descripcion ?? "")}\n${s.justificacionNegocio ?? ""}`.trim()}>
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
                    <Tooltip title="Ver detalle">
                      <IconButton size="small" onClick={() => setVerDetalle(s)} aria-label={`Ver detalle de ${s.folio}`}>
                        <VisibilityIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
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

      <Dialog open={verDetalle !== null} onClose={() => setVerDetalle(null)} fullWidth maxWidth="sm">
        <DialogTitle>{verDetalle?.folio} - {verDetalle?.titulo}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <Stack direction="row" spacing={3}>
            <Box>
              <Typography variant="caption" color="text.secondary">Solicitante</Typography>
              <Typography variant="body2">
                {verDetalle?.usuarioSolicitante ?? verDetalle?.solicitante}
              </Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Tipo</Typography>
              <Typography variant="body2">{verDetalle?.tipo}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Prioridad</Typography>
              <Typography variant="body2">{verDetalle?.prioridad}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Fecha deseada</Typography>
              <Typography variant="body2">{verDetalle?.fechaDeseada ?? "-"}</Typography>
            </Box>
          </Stack>
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
              Descripcion
            </Typography>
            {verDetalle?.descripcion
              ? <ContenidoEnriquecido html={verDetalle.descripcion} />
              : <Typography variant="body2" color="text.secondary">Sin descripcion capturada.</Typography>}
          </Box>
          {verDetalle?.justificacionNegocio && (
            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
                Justificacion de negocio
              </Typography>
              <Typography variant="body2">{verDetalle.justificacionNegocio}</Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setVerDetalle(null)}>Cerrar</Button>
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
      // La descripcion de la solicitud (con sus imagenes ya adjuntas) se copia al unico
      // elemento inicial para no perderla en la conversion; si el usuario agrega mas
      // filas para partir el trabajo, esas nacen vacias (el se decide que va en cada una).
      setFilas([nuevaFila(solicitud.titulo, solicitud.descripcion)]);
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
          <ComboBuscable
            label="Proyecto destino"
            required
            value={idProyecto}
            onChange={(v) => setIdProyecto(v as number | "")}
            opciones={(catalogos?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
            sx={{ width: "100%" }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoAprobar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyecto === ""}
            onClick={() => void ejecutar("APROBAR", undefined, idProyecto as number)}>
            Aprobar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoConvertir} onClose={() => setDialogoConvertir(false)} fullWidth maxWidth="lg">
        <DialogTitle>Convertir {solicitud.folio} en elementos de trabajo</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <Stack spacing={2}>
            {filas.map((fila) => (
              <Paper key={fila.uiId} variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1.5}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <TextField size="small" label="Titulo" value={fila.titulo} fullWidth
                      onChange={(e) => actualizarFila(fila.uiId, { titulo: e.target.value })} />
                    <IconButton size="small" disabled={filas.length === 1}
                      onClick={() => setFilas((previas) => previas.filter((f) => f.uiId !== fila.uiId))}
                      aria-label="Quitar fila">
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                  {fila.descripcion && (
                    <Typography variant="caption" color="text.secondary">
                      Incluye la descripcion original de la solicitud (con sus imagenes, si tiene).
                    </Typography>
                  )}
                  <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", "& > *": { flex: "1 1 200px" } }}>
                    <ComboBuscable
                      label="Tipo"
                      value={fila.idTipoWorkItem}
                      onChange={(v) => actualizarFila(fila.uiId, { idTipoWorkItem: Number(v) })}
                      opciones={(catalogos?.tipos ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre }))}
                    />
                    <ComboBuscable
                      label="Prioridad"
                      value={fila.idPrioridad}
                      onChange={(v) => actualizarFila(fila.uiId, { idPrioridad: Number(v) })}
                      opciones={(catalogos?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
                    />
                    <ComboBuscable
                      label="Asignado"
                      value={fila.idAsignado ?? ""}
                      onChange={(v) => actualizarFila(fila.uiId, { idAsignado: v === "" ? null : Number(v) })}
                      opciones={[
                        { valor: "", etiqueta: "Sin asignar" },
                        ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
                      ]}
                    />
                    <TextField size="small" type="date" label="Compromiso"
                      value={fila.fechaCompromiso ?? ""}
                      onChange={(e) => actualizarFila(fila.uiId, { fechaCompromiso: e.target.value || null })}
                      slotProps={{ inputLabel: { shrink: true } }} />
                  </Stack>
                </Stack>
              </Paper>
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
