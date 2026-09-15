import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Paper, Rating, Snackbar, Stack, Table,
  TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { TarjetaListado } from "../../shared/components/TarjetaListado";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import {
  colorEstatusTicket, crearTicket, obtenerMisTickets, registrarEncuestaTicket, type Ticket,
} from "../../shared/api/tickets";

const ESTATUS_RESUELTO = 5;
const ESTATUS_CERRADO = 6;

/** Contrato de IDs de dbo.tblEstatusTicket (GTE.Domain.Soporte.EstatusTicket). */
const ESTATUS_TICKET = [
  { id: 1, nombre: "Nuevo" },
  { id: 2, nombre: "Asignado" },
  { id: 3, nombre: "En Atencion" },
  { id: 4, nombre: "Esperando Usuario" },
  { id: 5, nombre: "Resuelto" },
  { id: 6, nombre: "Cerrado" },
];

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** P07/P16 - Portal de tickets: captura y seguimiento de las peticiones propias. */
export function PortalTicketsPage() {
  const [modal, setModal] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [titulo, setTitulo] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [idCategoria, setIdCategoria] = useState<number | "">("");
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [idUsuarioSolicitante, setIdUsuarioSolicitante] = useState<number | "">("");
  const [idLocacion, setIdLocacion] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  // Sin filtro = abiertos (todos menos Cerrado); "Todos" (-1) sigue disponible como opcion.
  const [filtroEstatus, setFiltroEstatus] = useState<number[]>([-1]);
  const clienteQuery = useQueryClient();
  const puede = useSesion((estado) => estado.puede);
  const esIngeniero = puede("TKT.Atender");
  const esMovil = useEsMovil();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const mios = useQuery({
    queryKey: ["mis-tickets", filtroEstatus],
    queryFn: () => obtenerMisTickets(filtroEstatus),
  });
  const miosFiltrados = (mios.data ?? []).filter((t) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return t.folio?.toLowerCase().includes(texto) || t.titulo.toLowerCase().includes(texto);
  });

  const valido = titulo.trim().length > 0 && idPrioridad !== "";

  const mensajeVacio = (mios.data?.length ?? 0) === 0 && filtroEstatus.length === 0 && !busqueda.trim()
    ? "Aun no tienes tickets. Crea el primero con el boton Nuevo ticket."
    : "No hay tickets con estos filtros.";

  const alExito = (mensaje: string) => {
    setAviso({ tipo: "success", mensaje });
    void clienteQuery.invalidateQueries({ queryKey: ["mis-tickets"] });
  };
  const alError = (mensaje: string) => setAviso({ tipo: "error", mensaje });

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await crearTicket({
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        idCategoriaTicket: idCategoria === "" ? null : (idCategoria as number),
        idPrioridad: idPrioridad as number,
        idUsuarioSolicitante: idUsuarioSolicitante === "" ? null : (idUsuarioSolicitante as number),
        idLocacion: idLocacion === "" ? null : (idLocacion as number),
      });
      setAviso({ tipo: "success", mensaje });
      setModal(false);
      setTitulo("");
      setDescripcion("");
      setIdCategoria("");
      setIdPrioridad("");
      setIdUsuarioSolicitante("");
      setIdLocacion("");
      await clienteQuery.invalidateQueries({ queryKey: ["mis-tickets"] });
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "Error al registrar el ticket.",
      });
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box sx={{ p: { xs: 1.5, sm: 2 } }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { xs: "stretch", sm: "center" }, mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Mis tickets</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo ticket
        </Button>
      </Stack>

      {mios.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(mios.error as Error).message}</Alert>
      )}

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mb: 1.5 }}>
        <TextField size="small" placeholder="Buscar folio o titulo..." value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)} sx={{ minWidth: { xs: "100%", sm: 260 } }} />
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
            ...ESTATUS_TICKET.map((e) => ({ valor: e.id, etiqueta: e.nombre })),
          ]}
          sx={{ minWidth: { xs: "100%", sm: 220 } }}
        />
      </Box>

      {esMovil ? (
        <Stack spacing={1}>
          {miosFiltrados.length === 0 && (
            <Paper variant="outlined" sx={{ p: 3 }}>
              <Typography color="text.secondary" sx={{ textAlign: "center" }}>{mensajeVacio}</Typography>
            </Paper>
          )}
          {miosFiltrados.map((t) => (
            <FilaTicket key={t.idTicket} ticket={t} esMovil alExito={alExito} alError={alError} />
          ))}
        </Stack>
      ) : (
        <Paper variant="outlined">
          <TableContainer sx={{ overflowX: "auto" }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                  <TableCell>Folio</TableCell>
                  <TableCell>Titulo</TableCell>
                  <TableCell>Categoria</TableCell>
                  <TableCell>Prioridad</TableCell>
                  <TableCell>Estatus</TableCell>
                  <TableCell>Asignado</TableCell>
                  <TableCell>Registrado</TableCell>
                  <TableCell>Calificacion</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {miosFiltrados.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={8}>
                      <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                        {mensajeVacio}
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
                {miosFiltrados.map((t) => (
                  <FilaTicket key={t.idTicket} ticket={t} esMovil={false} alExito={alExito} alError={alError} />
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm" fullScreen={esMovil}>
        <DialogTitle>Nuevo ticket</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo" value={titulo}
            onChange={(e) => setTitulo(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }} />
          <ComboBuscable
            label="Categoria"
            value={idCategoria}
            onChange={(v) => setIdCategoria(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin categoria" },
              ...(catalogos.data?.categoriasTicket ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre })),
            ]}
          />
          <ComboBuscable
            label="Prioridad"
            required
            value={idPrioridad}
            onChange={(v) => setIdPrioridad(v as number | "")}
            opciones={(catalogos.data?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
          />
          <TextField size="small" label="Descripcion" multiline minRows={3}
            value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
          {esIngeniero && (
            <>
              <ComboBuscable
                label="Usuario solicitante"
                value={idUsuarioSolicitante}
                onChange={(v) => setIdUsuarioSolicitante(v as number | "")}
                opciones={[
                  { valor: "", etiqueta: "Sin especificar" },
                  ...(catalogos.data?.usuariosSolicitantes ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
                ]}
              />
              <ComboBuscable
                label="Locacion"
                value={idLocacion}
                onChange={(v) => setIdLocacion(v as number | "")}
                opciones={[
                  { valor: "", etiqueta: "Sin especificar" },
                  ...(catalogos.data?.locaciones ?? []).map((l) => ({ valor: l.id, etiqueta: l.nombre })),
                ]}
              />
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
            Enviar
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

/** Fila de la tabla o tarjeta, segun el ancho: la encuesta de satisfaccion es la misma en ambas. */
function FilaTicket({ ticket, esMovil, alExito, alError }: {
  ticket: Ticket; esMovil: boolean;
  alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [calificando, setCalificando] = useState(false);
  const [calificacion, setCalificacion] = useState<number | null>(null);
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);

  const puedeCalificar = (ticket.idEstatus === ESTATUS_RESUELTO || ticket.idEstatus === ESTATUS_CERRADO)
    && ticket.calificacion === null;

  const abrirEncuesta = () => {
    setCalificando(true);
    setCalificacion(null);
    setComentario("");
  };

  const calificar = async () => {
    if (!calificacion) return;
    setEnviando(true);
    try {
      const { mensaje } = await registrarEncuestaTicket(ticket.idTicket, calificacion, comentario.trim() || undefined);
      alExito(mensaje);
      setCalificando(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al registrar la calificacion.");
    } finally {
      setEnviando(false);
    }
  };

  const dialogoEncuesta = (
    <Dialog open={calificando} onClose={() => setCalificando(false)} fullWidth maxWidth="xs" fullScreen={esMovil}>
      <DialogTitle>Califica {ticket.folio}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <Rating value={calificacion} onChange={(_, valor) => setCalificacion(valor)} size={esMovil ? "large" : "medium"} />
        <TextField size="small" label="Comentario (opcional)" multiline minRows={2}
          value={comentario} onChange={(e) => setComentario(e.target.value)} />
      </DialogContent>
      <DialogActions>
        <Button color="error" onClick={() => setCalificando(false)}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !calificacion} onClick={() => void calificar()}>
          Enviar
        </Button>
      </DialogActions>
    </Dialog>
  );

  if (esMovil) {
    return (
      <>
        <TarjetaListado
          encabezado={(
            <>
              <Typography component={RouterLink} to={`/tickets/${ticket.folio}`} variant="body2"
                sx={{ fontWeight: 700, color: "info.main" }}>
                {ticket.folio}
              </Typography>
              <Chip size="small" label={ticket.estatus} color={colorEstatusTicket(ticket.idEstatus)} />
            </>
          )}
          titulo={(
            <Typography component={RouterLink} to={`/tickets/${ticket.folio}`} variant="body2"
              sx={{ fontWeight: 600, color: "text.primary", textDecoration: "none" }}>
              {ticket.titulo}
            </Typography>
          )}
          campos={[
            { etiqueta: "Categoria", valor: ticket.categoria ?? "-" },
            { etiqueta: "Prioridad", valor: ticket.prioridad },
            { etiqueta: "Asignado", valor: ticket.asignado ?? "-" },
            { etiqueta: "Registrado", valor: formatearFecha(ticket.fechaRegistro) },
            ...(ticket.calificacion !== null
              ? [{
                  etiqueta: "Calificacion",
                  valor: <Rating value={ticket.calificacion} readOnly size="small" />,
                  completo: true,
                }]
              : []),
          ]}
          acciones={puedeCalificar
            ? <Button size="small" variant="outlined" onClick={abrirEncuesta}>Calificar</Button>
            : undefined}
        />
        {dialogoEncuesta}
      </>
    );
  }

  return (
    <TableRow hover>
      <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
        <Typography component={RouterLink} to={`/tickets/${ticket.folio}`} variant="body2"
          sx={{ fontWeight: 600, color: "info.main" }}>
          {ticket.folio}
        </Typography>
      </TableCell>
      <TableCell sx={{ maxWidth: 320 }}>
        <Tooltip title={ticket.descripcion ?? ""}>
          <Typography noWrap variant="body2">{ticket.titulo}</Typography>
        </Tooltip>
      </TableCell>
      <TableCell>{ticket.categoria ?? "-"}</TableCell>
      <TableCell>{ticket.prioridad}</TableCell>
      <TableCell>
        <Chip size="small" label={ticket.estatus} color={colorEstatusTicket(ticket.idEstatus)} />
      </TableCell>
      <TableCell>{ticket.asignado ?? "-"}</TableCell>
      <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(ticket.fechaRegistro)}</TableCell>
      <TableCell>
        {ticket.calificacion !== null ? (
          <Rating value={ticket.calificacion} readOnly size="small" />
        ) : puedeCalificar ? (
          <Button size="small" onClick={abrirEncuesta}>Calificar</Button>
        ) : "-"}
      </TableCell>

      {dialogoEncuesta}
    </TableRow>
  );
}
