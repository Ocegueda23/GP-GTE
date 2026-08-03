import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  List, ListItem, ListItemText, Stack, TextField, Typography,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import {
  corregirRevision, crearRevision, obtenerRevisiones, type Revision,
} from "../../shared/api/workitems";

interface Props {
  idWorkItem: number;
  folio: string;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** Hallazgos de QA y code review: mientras haya pendientes, el elemento no cierra. */
export function PanelRevisiones({ idWorkItem, folio, alExito, alError }: Props) {
  const [modalNuevo, setModalNuevo] = useState(false);
  const [comentarios, setComentarios] = useState("");
  const [comentariosVacio, setComentariosVacio] = useState(true);
  const [reabrir, setReabrir] = useState<Revision | null>(null);
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const revisiones = useQuery({
    queryKey: ["revisiones", idWorkItem],
    queryFn: () => obtenerRevisiones(idWorkItem),
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["revisiones", idWorkItem] }),
    clienteQuery.invalidateQueries({ queryKey: ["workitem", folio] }),
    clienteQuery.invalidateQueries({ queryKey: ["acciones", idWorkItem] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejarError = (error: unknown, respaldo: string) => {
    alError(error instanceof ErrorApi ? error.message : respaldo);
  };

  const reportar = async () => {
    setEnviando(true);
    try {
      const { mensaje } = await crearRevision(idWorkItem, comentarios);
      alExito(mensaje);
      setModalNuevo(false);
      setComentarios("");
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo registrar el hallazgo.");
    } finally {
      setEnviando(false);
    }
  };

  const marcarCorregido = async (revision: Revision) => {
    try {
      const { mensaje } = await corregirRevision(revision.idRevision, { corregido: true });
      alExito(mensaje);
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo marcar el hallazgo.");
    }
  };

  const confirmarReapertura = async () => {
    if (!reabrir) return;
    setEnviando(true);
    try {
      const { mensaje } = await corregirRevision(reabrir.idRevision, {
        corregido: false,
        motivo: motivo.trim(),
      });
      alExito(mensaje);
      setReabrir(null);
      setMotivo("");
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo reabrir el hallazgo.");
    } finally {
      setEnviando(false);
    }
  };

  const pendientes = revisiones.data?.filter((r) => !r.corregido).length ?? 0;

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Hallazgos de revision</Typography>
        <Button size="small" variant="contained" onClick={() => setModalNuevo(true)}>
          Reportar hallazgo
        </Button>
      </Stack>

      {pendientes > 0 && (
        <Alert severity="warning" sx={{ mb: 1 }}>
          {pendientes === 1
            ? "1 hallazgo sin corregir impide cerrar este elemento."
            : `${pendientes} hallazgos sin corregir impiden cerrar este elemento.`}
        </Alert>
      )}

      {revisiones.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
          Sin hallazgos registrados.
        </Typography>
      )}

      <List dense disablePadding>
        {revisiones.data?.map((revision) => (
          <ListItem key={revision.idRevision} disableGutters divider sx={{ gap: 1 }}>
            <ListItemText
              sx={{ flex: 1, minWidth: 0 }}
              primary={
                <Stack spacing={0.5} sx={{ alignItems: "flex-start" }}>
                  <Chip size="small" color={revision.corregido ? "success" : "warning"}
                    label={revision.corregido ? "Corregido" : "Pendiente"} />
                  <ContenidoEnriquecido html={revision.comentarios ?? ""} />
                </Stack>
              }
              secondary={`${revision.revisor} - reportado ${formatearFecha(revision.fechaRegistro)}`
                + (revision.corregido ? ` - corregido ${formatearFecha(revision.fechaCorreccion)}` : "")}
            />
            {revision.corregido ? (
              <Button size="small" color="warning" sx={{ flexShrink: 0 }}
                onClick={() => setReabrir(revision)}>
                Reabrir
              </Button>
            ) : (
              <Button size="small" sx={{ flexShrink: 0 }}
                onClick={() => void marcarCorregido(revision)}>
                Marcar corregido
              </Button>
            )}
          </ListItem>
        ))}
      </List>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="sm">
        <DialogTitle>Reportar hallazgo - {folio}</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <EditorEnriquecido
            label="Que se encontro y que hay que ajustar"
            placeholder="Describe el hallazgo..."
            value={comentarios}
            onChange={setComentarios}
            onVacioChange={setComentariosVacio}
            idWorkItemParaAdjuntos={idWorkItem}
            onError={alError}
          />
          <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 1 }}>
            Si el elemento ya estaba terminado, el hallazgo lo regresa a Correccion.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || comentariosVacio}
            onClick={() => void reportar()}>
            Reportar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={reabrir !== null} onClose={() => setReabrir(null)} fullWidth maxWidth="sm">
        <DialogTitle>Reabrir hallazgo</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Por que no quedo resuelto (obligatorio)"
            value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          <Typography variant="caption" color="text.secondary">
            Reabrir un hallazgo corregido es facultad del lider.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReabrir(null)}>Cancelar</Button>
          <Button variant="contained" color="warning"
            disabled={enviando || motivo.trim().length === 0}
            onClick={() => void confirmarReapertura()}>
            Reabrir
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
