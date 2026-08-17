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
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import {
  colorEstatusTicket, crearTicket, obtenerMisTickets, registrarEncuestaTicket, type Ticket,
} from "../../shared/api/tickets";

const ESTATUS_RESUELTO = 5;
const ESTATUS_CERRADO = 6;

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
  const clienteQuery = useQueryClient();
  const puede = useSesion((estado) => estado.puede);
  const esIngeniero = puede("TKT.Atender");

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const mios = useQuery({ queryKey: ["mis-tickets"], queryFn: obtenerMisTickets });

  const valido = titulo.trim().length > 0 && idPrioridad !== "";

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
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Mis tickets</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo ticket
        </Button>
      </Stack>

      {mios.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(mios.error as Error).message}</Alert>
      )}

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
              {mios.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      Aun no tienes tickets. Crea el primero con el boton Nuevo ticket.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {mios.data?.map((t) => (
                <FilaTicket key={t.idTicket} ticket={t}
                  alExito={(mensaje) => {
                    setAviso({ tipo: "success", mensaje });
                    void clienteQuery.invalidateQueries({ queryKey: ["mis-tickets"] });
                  }}
                  alError={(mensaje) => setAviso({ tipo: "error", mensaje })} />
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm">
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
          <Button onClick={() => setModal(false)}>Cancelar</Button>
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

function FilaTicket({ ticket, alExito, alError }: {
  ticket: Ticket; alExito: (mensaje: string) => void; alError: (mensaje: string) => void;
}) {
  const [calificando, setCalificando] = useState(false);
  const [calificacion, setCalificacion] = useState<number | null>(null);
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);

  const puedeCalificar = (ticket.idEstatus === ESTATUS_RESUELTO || ticket.idEstatus === ESTATUS_CERRADO)
    && ticket.calificacion === null;

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

  return (
    <TableRow hover>
      <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
        <Typography component={RouterLink} to={`/tickets/${ticket.folio}`} variant="body2"
          sx={{ fontWeight: 600, color: "inherit" }}>
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
          <Button size="small" onClick={() => { setCalificando(true); setCalificacion(null); setComentario(""); }}>
            Calificar
          </Button>
        ) : "-"}
      </TableCell>

      <Dialog open={calificando} onClose={() => setCalificando(false)} fullWidth maxWidth="xs">
        <DialogTitle>Califica {ticket.folio}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <Rating value={calificacion} onChange={(_, valor) => setCalificacion(valor)} />
          <TextField size="small" label="Comentario (opcional)" multiline minRows={2}
            value={comentario} onChange={(e) => setComentario(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCalificando(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !calificacion} onClick={() => void calificar()}>
            Enviar
          </Button>
        </DialogActions>
      </Dialog>
    </TableRow>
  );
}
