import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  IconButton, Paper, Snackbar, Stack, Table,
  TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import { subirArchivoSolicitud } from "../../shared/api/archivos";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import {
  actualizarSolicitud, colorEstatusSolicitud, crearSolicitud, ESTATUS_SOLICITUD_EDITABLE,
  obtenerMisSolicitudes, type Solicitud,
} from "../../shared/api/solicitudes";

/** Contrato de IDs de dbo.tblEstatusSolicitud (GTE.Domain.Solicitudes.EstatusSolicitud). */
const ESTATUS_SOLICITUD = [
  { id: 1, nombre: "Borrador" },
  { id: 2, nombre: "Enviada" },
  { id: 3, nombre: "En Analisis" },
  { id: 4, nombre: "Aprobada" },
  { id: 5, nombre: "Rechazada" },
  { id: 6, nombre: "Convertida" },
  { id: 7, nombre: "Cancelada" },
];

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** P07 - Portal del solicitante: captura y seguimiento de sus peticiones. */
export function PortalPage() {
  const [modal, setModal] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [titulo, setTitulo] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [descripcionVacia, setDescripcionVacia] = useState(true);
  const [justificacion, setJustificacion] = useState("");
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [fechaDeseada, setFechaDeseada] = useState("");
  const [idUsuarioSolicitante, setIdUsuarioSolicitante] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  // Sin filtro = pendientes (Enviada, En Analisis, Aprobada); "Todas" (-1) sigue disponible.
  const [filtroEstatus, setFiltroEstatus] = useState<number[]>([]);
  const clienteQuery = useQueryClient();
  const puede = useSesion((estado) => estado.puede);
  const puedeCapturarSolicitante = puede("SOL.Triage");

  const [solicitudEditar, setSolicitudEditar] = useState<Solicitud | null>(null);
  const [tituloEditar, setTituloEditar] = useState("");
  const [descripcionEditar, setDescripcionEditar] = useState("");
  const [descripcionEditarVacia, setDescripcionEditarVacia] = useState(true);
  const [justificacionEditar, setJustificacionEditar] = useState("");
  const [idTipoEditar, setIdTipoEditar] = useState<number | "">("");
  const [idPrioridadEditar, setIdPrioridadEditar] = useState<number | "">("");
  const [fechaDeseadaEditar, setFechaDeseadaEditar] = useState("");
  const [idUsuarioSolicitanteEditar, setIdUsuarioSolicitanteEditar] = useState<number | "">("");
  const [guardandoEdicion, setGuardandoEdicion] = useState(false);

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const mias = useQuery({
    queryKey: ["mis-solicitudes", filtroEstatus],
    queryFn: () => obtenerMisSolicitudes(filtroEstatus),
  });

  const miasFiltradas = (mias.data ?? []).filter((s) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return s.folio?.toLowerCase().includes(texto) || s.titulo.toLowerCase().includes(texto);
  });
  const { datosOrdenados: miasOrdenadas, ordenarPor, descendente, ordenar } = useOrdenTabla(miasFiltradas);

  const valido = titulo.trim().length > 0 && idTipo !== "" && idPrioridad !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await crearSolicitud({
        titulo: titulo.trim(),
        descripcion: descripcionVacia ? null : descripcion,
        idTipoSolicitud: idTipo as number,
        idPrioridad: idPrioridad as number,
        fechaDeseada: fechaDeseada || null,
        justificacionNegocio: justificacion.trim() || null,
        idUsuarioSolicitante: idUsuarioSolicitante === "" ? null : (idUsuarioSolicitante as number),
      });
      setAviso({ tipo: "success", mensaje });
      setModal(false);
      setTitulo("");
      setDescripcion("");
      setDescripcionVacia(true);
      setJustificacion("");
      setFechaDeseada("");
      setIdUsuarioSolicitante("");
      await clienteQuery.invalidateQueries({ queryKey: ["mis-solicitudes"] });
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "Error al enviar la solicitud.",
      });
    } finally {
      setEnviando(false);
    }
  };

  const abrirEditar = (s: Solicitud) => {
    setSolicitudEditar(s);
    setTituloEditar(s.titulo);
    setDescripcionEditar(s.descripcion ?? "");
    setDescripcionEditarVacia(!s.descripcion);
    setJustificacionEditar(s.justificacionNegocio ?? "");
    setIdTipoEditar(s.idTipoSolicitud);
    setIdPrioridadEditar(s.idPrioridad);
    setFechaDeseadaEditar(s.fechaDeseada?.slice(0, 10) ?? "");
    setIdUsuarioSolicitanteEditar(s.idUsuarioSolicitante ?? "");
  };

  const validoEditar = tituloEditar.trim().length > 0 && idTipoEditar !== "" && idPrioridadEditar !== "";

  const guardarEdicion = async () => {
    if (!solicitudEditar || !validoEditar) return;
    setGuardandoEdicion(true);
    try {
      const { mensaje } = await actualizarSolicitud(solicitudEditar.idSolicitud, {
        titulo: tituloEditar.trim(),
        descripcion: descripcionEditarVacia ? null : descripcionEditar,
        idTipoSolicitud: idTipoEditar as number,
        idPrioridad: idPrioridadEditar as number,
        fechaDeseada: fechaDeseadaEditar || null,
        justificacionNegocio: justificacionEditar.trim() || null,
        idUsuarioSolicitante: idUsuarioSolicitanteEditar === "" ? null : (idUsuarioSolicitanteEditar as number),
      });
      setAviso({ tipo: "success", mensaje });
      setSolicitudEditar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["mis-solicitudes"] });
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "Error al actualizar la solicitud.",
      });
    } finally {
      setGuardandoEdicion(false);
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Mis solicitudes</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nueva solicitud
        </Button>
      </Stack>

      {mias.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(mias.error as Error).message}</Alert>
      )}

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mb: 1.5 }}>
        <TextField size="small" placeholder="Buscar folio o titulo..." value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)} sx={{ minWidth: 280 }} />
        <ComboBuscableMultiple
          label="Estatus"
          value={filtroEstatus}
          onChange={(valores) => {
            const valor = valores as number[];
            const eligioTodos = valor.includes(-1) && !filtroEstatus.includes(-1);
            setFiltroEstatus(eligioTodos ? [-1] : valor.filter((v) => v !== -1));
          }}
          opciones={[
            { valor: -1, etiqueta: "Todas" },
            ...ESTATUS_SOLICITUD.map((e) => ({ valor: e.id, etiqueta: e.nombre })),
          ]}
          sx={{ minWidth: 220 }}
        />
      </Box>

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Titulo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="tipo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Tipo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="prioridad" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Prioridad</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="proyecto" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Proyecto</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaRegistro" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Enviada</EncabezadoOrdenable>
                <TableCell>Items generados</TableCell>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {miasOrdenadas.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      {(mias.data?.length ?? 0) === 0 && filtroEstatus.length === 0 && !busqueda.trim()
                        ? "Aun no tienes solicitudes. Crea la primera con el boton Nueva solicitud."
                        : "No hay solicitudes con estos filtros."}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {miasOrdenadas.map((s) => (
                <TableRow key={s.idSolicitud} hover>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{s.folio}</TableCell>
                  <TableCell sx={{ maxWidth: 320 }}>
                    <Tooltip title={s.justificacionNegocio ?? ""}>
                      <Typography noWrap variant="body2">{s.titulo}</Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell>{s.tipo}</TableCell>
                  <TableCell>{s.prioridad}</TableCell>
                  <TableCell>
                    <Chip size="small" label={s.estatus} color={colorEstatusSolicitud(s.idEstatus)} />
                  </TableCell>
                  <TableCell>{s.proyecto ?? "-"}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(s.fechaRegistro)}</TableCell>
                  <TableCell>
                    {s.itemsGenerados.length > 0
                      ? s.itemsGenerados.map((i) => i.folio).join(", ")
                      : "-"}
                  </TableCell>
                  <TableCell align="center">
                    {ESTATUS_SOLICITUD_EDITABLE.includes(s.idEstatus) && (
                      <Tooltip title="Editar">
                        <IconButton size="small" onClick={() => abrirEditar(s)} aria-label={`Editar ${s.folio}`}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nueva solicitud</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo" value={titulo}
            onChange={(e) => setTitulo(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }} />
          <ComboBuscable
            label="Tipo de solicitud"
            required
            value={idTipo}
            onChange={(v) => setIdTipo(v as number | "")}
            opciones={(catalogos.data?.tiposSolicitud ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre }))}
          />
          <ComboBuscable
            label="Prioridad"
            required
            value={idPrioridad}
            onChange={(v) => setIdPrioridad(v as number | "")}
            opciones={(catalogos.data?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
          />
          <EditorEnriquecido
            label="Descripcion de lo que necesitas"
            value={descripcion}
            onChange={setDescripcion}
            onVacioChange={setDescripcionVacia}
            onError={(mensaje) => setAviso({ tipo: "error", mensaje })}
          />
          <TextField size="small" label="Justificacion de negocio" multiline minRows={2}
            value={justificacion} onChange={(e) => setJustificacion(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }} />
          <TextField size="small" type="date" label="Fecha deseada" value={fechaDeseada}
            onChange={(e) => setFechaDeseada(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          {puedeCapturarSolicitante && (
            <ComboBuscable
              label="Usuario solicitante"
              value={idUsuarioSolicitante}
              onChange={(v) => setIdUsuarioSolicitante(v as number | "")}
              opciones={[
                { valor: "", etiqueta: "Sin especificar" },
                ...(catalogos.data?.usuariosSolicitantes ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
              ]}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
            Enviar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={solicitudEditar !== null} onClose={() => setSolicitudEditar(null)} fullWidth maxWidth="sm">
        <DialogTitle>Editar {solicitudEditar?.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo" value={tituloEditar}
            onChange={(e) => setTituloEditar(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }} />
          <ComboBuscable
            label="Tipo de solicitud"
            required
            value={idTipoEditar}
            onChange={(v) => setIdTipoEditar(v as number | "")}
            opciones={(catalogos.data?.tiposSolicitud ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre }))}
          />
          <ComboBuscable
            label="Prioridad"
            required
            value={idPrioridadEditar}
            onChange={(v) => setIdPrioridadEditar(v as number | "")}
            opciones={(catalogos.data?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
          />
          <EditorEnriquecido
            label="Descripcion de lo que necesitas"
            value={descripcionEditar}
            onChange={setDescripcionEditar}
            onVacioChange={setDescripcionEditarVacia}
            onError={(mensaje) => setAviso({ tipo: "error", mensaje })}
            onSubirImagen={solicitudEditar
              ? (archivo) => subirArchivoSolicitud(solicitudEditar.idSolicitud, archivo)
              : undefined}
          />
          <TextField size="small" label="Justificacion de negocio" multiline minRows={2}
            value={justificacionEditar} onChange={(e) => setJustificacionEditar(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }} />
          <TextField size="small" type="date" label="Fecha deseada" value={fechaDeseadaEditar}
            onChange={(e) => setFechaDeseadaEditar(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          {puedeCapturarSolicitante && (
            <ComboBuscable
              label="Usuario solicitante"
              value={idUsuarioSolicitanteEditar}
              onChange={(v) => setIdUsuarioSolicitanteEditar(v as number | "")}
              opciones={[
                { valor: "", etiqueta: "Sin especificar" },
                ...(catalogos.data?.usuariosSolicitantes ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
              ]}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setSolicitudEditar(null)}>Cancelar</Button>
          <Button variant="contained" disabled={guardandoEdicion || !validoEditar} onClick={() => void guardarEdicion()}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={5000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
