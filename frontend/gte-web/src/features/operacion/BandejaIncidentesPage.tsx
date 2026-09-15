import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, Menu, MenuItem, Paper,
  Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import BuildIcon from "@mui/icons-material/Build";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { OrdenMovil } from "../../shared/components/OrdenMovil";
import { TarjetaListado } from "../../shared/components/TarjetaListado";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import { formatearMinutos, obtenerCatalogosBandeja, type AccionDisponible, type CatalogosBandeja } from "../../shared/api/workitems";
import {
  cambiarEstatusIncidente, cambiarSeveridadIncidente, colorEstatusIncidente, colorSeveridad,
  crearIncidente, filtroBandejaIncidentesInicial, obtenerAccionesIncidente,
  obtenerBandejaIncidentes, opcionesCategoriaIncidente, vincularCorrectivo, type Incidente,
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

/** Reloj corrido desde que el incidente entro En Atencion; sigue corriendo mientras no
    se mitigue o resuelva. */
function tiempoAtencion(incidente: Incidente): string {
  if (incidente.minutosAtencion === null) return "-";
  return incidente.atencionEnCurso
    ? `${formatearMinutos(incidente.minutosAtencion)} (en curso)`
    : formatearMinutos(incidente.minutosAtencion);
}

/** Contrato de IDs de dbo.tblEstatusIncidente (GTE.Domain.Operacion.EstatusIncidente). */
const ESTATUS_INCIDENTE = [
  { id: 1, nombre: "Detectado" },
  { id: 2, nombre: "En Atencion" },
  { id: 3, nombre: "Mitigado" },
  { id: 4, nombre: "Resuelto" },
  { id: 5, nombre: "Cerrado" },
];

/** Mismas claves que los encabezados ordenables de la tabla, para el selector de movil. */
const COLUMNAS_ORDEN = [
  { valor: "folio", etiqueta: "Folio" },
  { valor: "titulo", etiqueta: "Titulo" },
  { valor: "proyecto", etiqueta: "Proyecto" },
  { valor: "categoria", etiqueta: "Categoria" },
  { valor: "severidad", etiqueta: "Severidad" },
  { valor: "estatus", etiqueta: "Estatus" },
  { valor: "fechaOcurrencia", etiqueta: "Ocurrencia" },
  { valor: "fechaResolucion", etiqueta: "Resolucion" },
];

/** P17 - Incidentes: bandeja de operacion (permiso INC.Gestionar). */
export function BandejaIncidentesPage() {
  const [texto, setTexto] = useState("");
  const [modal, setModal] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idSeveridad, setIdSeveridad] = useState<number | "">("");
  const [idCategoria, setIdCategoria] = useState<number | "">("");
  // Sin filtro = abiertos (todos menos Cerrado); "Todos" (-1) sigue disponible como opcion
  // explicita en el combo de abajo.
  const [filtroEstatus, setFiltroEstatus] = useState<number[]>([-1]);
  const [filtroSeveridad, setFiltroSeveridad] = useState<number | "">("");
  const [filtroProyecto, setFiltroProyecto] = useState<number | "">("");
  const [titulo, setTitulo] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [fechaOcurrencia, setFechaOcurrencia] = useState(fechaLocalAhora());
  const [fechaDeteccion, setFechaDeteccion] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [ordenarPor, setOrdenarPor] = useState<string | null>(null);
  const [ordenDescendente, setOrdenDescendente] = useState(false);
  const clienteQuery = useQueryClient();
  const esMovil = useEsMovil();

  const manejarOrden = (clave: string) => {
    // Cadena vacia = el selector de movil se quedo sin columna (boton de limpiar del combo).
    if (clave === "") { setOrdenarPor(null); setOrdenDescendente(false); return; }
    if (ordenarPor === clave) setOrdenDescendente((d) => !d);
    else { setOrdenarPor(clave); setOrdenDescendente(false); }
  };

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const bandeja = useQuery({
    queryKey: ["bandeja-incidentes", texto, filtroEstatus, filtroSeveridad, filtroProyecto, ordenarPor, ordenDescendente],
    queryFn: () => obtenerBandejaIncidentes({
      ...filtroBandejaIncidentesInicial,
      texto,
      estatus: filtroEstatus,
      idSeveridad: filtroSeveridad === "" ? null : filtroSeveridad,
      idProyecto: filtroProyecto === "" ? null : filtroProyecto,
      ordenarPor,
      ordenDescendente,
    }),
    placeholderData: (anterior) => anterior,
  });

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["bandeja-incidentes"] });
  const items = bandeja.data?.items ?? [];
  const sinResultados = bandeja.data !== undefined && items.length === 0;

  const menuAcciones = (i: Incidente) => (
    <MenuAccionesIncidente
      incidente={i}
      catalogos={catalogos.data}
      alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescar(); }}
      alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
    />
  );

  const valido = titulo.trim().length > 0 && idProyecto !== "" && idSeveridad !== ""
    && idCategoria !== "" && fechaOcurrencia !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await crearIncidente({
        idProyecto: idProyecto as number,
        idSeveridad: idSeveridad as number,
        idCategoriaIncidente: idCategoria as number,
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
      setIdCategoria("");
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
    <Box sx={{ p: { xs: 1.5, sm: 2 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { xs: "stretch", sm: "center" }, mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Incidentes</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo incidente
        </Button>
      </Stack>

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mb: 2 }}>
        <TextField size="small" label="Buscar folio o titulo" value={texto}
          onChange={(e) => setTexto(e.target.value)} sx={{ minWidth: { xs: "100%", sm: 260 } }} />
        <ComboBuscableMultiple
          label="Estatus"
          value={filtroEstatus}
          onChange={(valores) => {
            const valor = valores as number[];
            const eligioTodos = valor.includes(-1) && !filtroEstatus.includes(-1);
            setFiltroEstatus(eligioTodos ? [-1] : valor.filter((v) => v !== -1));
          }}
          opciones={[
            { valor: -1, etiqueta: "Todos" },
            ...ESTATUS_INCIDENTE.map((e) => ({ valor: e.id, etiqueta: e.nombre })),
          ]}
          sx={{ minWidth: { xs: "100%", sm: 220 } }}
        />
        <ComboBuscable
          label="Severidad"
          value={filtroSeveridad}
          onChange={(v) => setFiltroSeveridad(v as number | "")}
          opciones={[
            { valor: "", etiqueta: "Todas" },
            ...(catalogos.data?.severidades ?? []).map((s) => ({ valor: s.id, etiqueta: s.nombre })),
          ]}
          sx={{ minWidth: { xs: "100%", sm: 160 } }}
        />
        <ComboBuscable
          label="Proyecto"
          value={filtroProyecto}
          onChange={(v) => setFiltroProyecto(v as number | "")}
          opciones={[
            { valor: "", etiqueta: "Todos" },
            ...(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` })),
          ]}
          sx={{ minWidth: { xs: "100%", sm: 220 } }}
        />

        {/* En movil la tabla se cambia por tarjetas y no quedan encabezados donde ordenar. */}
        {esMovil && (
          <OrdenMovil opciones={COLUMNAS_ORDEN} ordenarPor={ordenarPor}
            descendente={ordenDescendente} onOrdenar={manejarOrden} />
        )}
      </Box>

      {bandeja.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(bandeja.error as Error).message}</Alert>
      )}

      {esMovil ? (
        <Stack spacing={1}>
          {sinResultados && (
            <Paper variant="outlined" sx={{ p: 3 }}>
              <Typography color="text.secondary" sx={{ textAlign: "center" }}>
                No hay incidentes con estos filtros.
              </Typography>
            </Paper>
          )}
          {items.map((i) => (
            <TarjetaListado
              key={i.idIncidente}
              encabezado={(
                <>
                  <Typography component={RouterLink} to={`/operacion/incidentes/${i.folio}`} variant="body2"
                    sx={{ fontWeight: 700, color: "info.main" }}>
                    {i.folio}
                  </Typography>
                  <Chip size="small" label={i.estatus} color={colorEstatusIncidente(i.idEstatus)} />
                  <Chip size="small" label={i.severidad} color={colorSeveridad(i.idSeveridad)} />
                </>
              )}
              titulo={(
                <Typography component={RouterLink} to={`/operacion/incidentes/${i.folio}`} variant="body2"
                  sx={{ fontWeight: 600, color: "text.primary", textDecoration: "none" }}>
                  {i.titulo}
                </Typography>
              )}
              campos={[
                { etiqueta: "Proyecto", valor: i.proyecto, completo: true },
                { etiqueta: "Categoria", valor: i.categoriaIncidente ?? "-", completo: true },
                { etiqueta: "Ocurrencia", valor: formatearFecha(i.fechaOcurrencia) },
                { etiqueta: "Resolucion", valor: formatearFecha(i.fechaResolucion) },
                { etiqueta: "Tiempo atencion", valor: tiempoAtencion(i) },
              ]}
              acciones={menuAcciones(i)}
            />
          ))}
        </Stack>
      ) : (
        <Paper variant="outlined">
          <TableContainer sx={{ overflowX: "auto" }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                  <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Folio</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="titulo" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Titulo</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="proyecto" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Proyecto</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="categoria" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Categoria</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="severidad" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Severidad</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Estatus</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="fechaOcurrencia" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Ocurrencia</EncabezadoOrdenable>
                  <EncabezadoOrdenable clave="fechaResolucion" ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={manejarOrden}>Resolucion</EncabezadoOrdenable>
                  <TableCell align="right">Tiempo atencion</TableCell>
                  <TableCell align="center">Acciones</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sinResultados && (
                  <TableRow>
                    <TableCell colSpan={10}>
                      <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                        No hay incidentes con estos filtros.
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
                {items.map((i) => (
                  <TableRow key={i.idIncidente} hover>
                    <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                      <Typography component={RouterLink} to={`/operacion/incidentes/${i.folio}`} variant="body2"
                        sx={{ fontWeight: 600, color: "info.main" }}>
                        {i.folio}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ maxWidth: 280 }}>
                      <Tooltip title={i.descripcion ?? ""}>
                        <Typography noWrap variant="body2">{i.titulo}</Typography>
                      </Tooltip>
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{i.proyecto}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{i.categoriaIncidente ?? "-"}</TableCell>
                    <TableCell>
                      <Chip size="small" label={i.severidad} color={colorSeveridad(i.idSeveridad)} />
                    </TableCell>
                    <TableCell>
                      <Chip size="small" label={i.estatus} color={colorEstatusIncidente(i.idEstatus)} />
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(i.fechaOcurrencia)}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(i.fechaResolucion)}</TableCell>
                    <TableCell align="right" sx={{ whiteSpace: "nowrap" }}>
                      {tiempoAtencion(i)}
                    </TableCell>
                    <TableCell align="center">
                      {menuAcciones(i)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm" fullScreen={esMovil}>
        <DialogTitle>Nuevo incidente</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Proyecto"
            required
            value={idProyecto}
            onChange={(v) => setIdProyecto(v as number | "")}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
          />
          <ComboBuscable
            label="Severidad"
            required
            value={idSeveridad}
            onChange={(v) => setIdSeveridad(v as number | "")}
            opciones={(catalogos.data?.severidades ?? []).map((s) => ({ valor: s.id, etiqueta: s.nombre }))}
          />
          <ComboBuscable
            label="Categoria"
            required
            value={idCategoria}
            onChange={(v) => setIdCategoria(v as number | "")}
            opciones={opcionesCategoriaIncidente(catalogos.data?.categoriasIncidente ?? [])}
          />
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
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
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
  const esMovil = useEsMovil();
  // En la tarjeta de movil los iconos de accion se llevan a 44 px, el area tocable
  // minima: "small" los deja en 30 y ni "medium" con un icono chico pasa de 35.
  const tamanoIcono = esMovil ? "medium" : "small";
  const areaTactil = esMovil ? { width: 44, height: 44 } : undefined;

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
      <IconButton size={tamanoIcono} sx={areaTactil} onClick={abrirMenu} aria-label={`Acciones de ${incidente.folio}`}>
        {cargando ? <CircularProgress size={18} /> : <MoreVertIcon fontSize="small" />}
      </IconButton>
      <Tooltip title="Cambiar severidad">
        <IconButton size={tamanoIcono} sx={areaTactil} onClick={() => { setNuevaSeveridad(incidente.idSeveridad); setMotivoSeveridad(""); setDialogoSeveridad(true); }}>
          <AddIcon fontSize="small" sx={{ transform: "rotate(45deg)" }} />
        </IconButton>
      </Tooltip>
      {!incidente.idWorkItemCorrectivo && (
        <Tooltip title="Vincular correctivo">
          <IconButton size={tamanoIcono} sx={areaTactil} onClick={() => { setIdPrioridad(""); setIdAsignado(""); setFechaCompromiso(""); setDialogoCorrectivo(true); }}>
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

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth fullScreen={esMovil}>
        <DialogTitle>{accionConMotivo?.etiqueta} - {incidente.folio}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo (obligatorio)" value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setAccionConMotivo(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || motivo.trim().length === 0}
            onClick={() => accionConMotivo && void ejecutar(accionConMotivo.accion, motivo.trim())}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoSeveridad} onClose={() => setDialogoSeveridad(false)} fullWidth maxWidth="xs" fullScreen={esMovil}>
        <DialogTitle>Cambiar severidad de {incidente.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Severidad"
            required
            value={nuevaSeveridad}
            onChange={(v) => setNuevaSeveridad(v as number | "")}
            opciones={(catalogos?.severidades ?? []).map((s) => ({ valor: s.id, etiqueta: s.nombre }))}
          />
          <TextField size="small" fullWidth multiline minRows={2} label="Motivo (obligatorio)"
            value={motivoSeveridad} onChange={(e) => setMotivoSeveridad(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setDialogoSeveridad(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || nuevaSeveridad === "" || motivoSeveridad.trim().length === 0}
            onClick={() => void cambiarSeveridad()}>
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialogoCorrectivo} onClose={() => setDialogoCorrectivo(false)} fullWidth maxWidth="xs" fullScreen={esMovil}>
        <DialogTitle>Vincular correctivo a {incidente.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Prioridad"
            required
            value={idPrioridad}
            onChange={(v) => setIdPrioridad(v as number | "")}
            opciones={(catalogos?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
          />
          <ComboBuscable
            label="Asignado (opcional)"
            value={idAsignado}
            onChange={(v) => setIdAsignado(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin asignar" },
              ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
          <TextField size="small" type="date" label="Compromiso (opcional)" value={fechaCompromiso}
            onChange={(e) => setFechaCompromiso(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setDialogoCorrectivo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idPrioridad === ""} onClick={() => void vincular()}>
            Vincular
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
