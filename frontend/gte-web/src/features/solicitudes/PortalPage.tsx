import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, MenuItem, Paper, Select, Snackbar, Stack, Table,
  TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import {
  colorEstatusSolicitud, crearSolicitud, obtenerMisSolicitudes,
} from "../../shared/api/solicitudes";

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
  const [justificacion, setJustificacion] = useState("");
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [fechaDeseada, setFechaDeseada] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });
  const mias = useQuery({ queryKey: ["mis-solicitudes"], queryFn: obtenerMisSolicitudes });

  const valido = titulo.trim().length > 0 && idTipo !== "" && idPrioridad !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await crearSolicitud({
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        idTipoSolicitud: idTipo as number,
        idPrioridad: idPrioridad as number,
        fechaDeseada: fechaDeseada || null,
        justificacionNegocio: justificacion.trim() || null,
      });
      setAviso({ tipo: "success", mensaje });
      setModal(false);
      setTitulo("");
      setDescripcion("");
      setJustificacion("");
      setFechaDeseada("");
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

      <Paper variant="outlined">
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <TableCell>Folio</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Tipo</TableCell>
                <TableCell>Prioridad</TableCell>
                <TableCell>Estatus</TableCell>
                <TableCell>Proyecto</TableCell>
                <TableCell>Enviada</TableCell>
                <TableCell>Items generados</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {mias.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                      Aun no tienes solicitudes. Crea la primera con el boton Nueva solicitud.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {mias.data?.map((s) => (
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
          <FormControl size="small" required>
            <InputLabel>Tipo de solicitud</InputLabel>
            <Select label="Tipo de solicitud" value={idTipo}
              onChange={(e) => setIdTipo(e.target.value as number | "")}>
              {catalogos.data?.tiposSolicitud.map((t) => (
                <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" required>
            <InputLabel>Prioridad</InputLabel>
            <Select label="Prioridad" value={idPrioridad}
              onChange={(e) => setIdPrioridad(e.target.value as number | "")}>
              {catalogos.data?.prioridades.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" label="Descripcion de lo que necesitas" multiline minRows={3}
            value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
          <TextField size="small" label="Justificacion de negocio" multiline minRows={2}
            value={justificacion} onChange={(e) => setJustificacion(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }} />
          <TextField size="small" type="date" label="Fecha deseada" value={fechaDeseada}
            onChange={(e) => setFechaDeseada(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
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
