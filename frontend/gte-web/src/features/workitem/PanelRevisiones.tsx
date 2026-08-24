import { useRef, useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, List, ListItem, ListItemText, MenuItem, Select,
  Stack, TextField, Typography,
} from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import DownloadIcon from "@mui/icons-material/Download";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import {
  descargarArchivoBlob, formatearTamano, obtenerArchivosRevision, subirArchivoRevision,
} from "../../shared/api/archivos";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
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

/** Adjuntos de un hallazgo puntual: lista compacta + boton para agregar mas despues. */
function AdjuntosRevision({ idRevision, alError }: { idRevision: number; alError: (mensaje: string) => void }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const clienteQuery = useQueryClient();
  const archivos = useQuery({
    queryKey: ["archivos-revision", idRevision],
    queryFn: () => obtenerArchivosRevision(idRevision),
  });

  const subir = async (archivo: File) => {
    try {
      await subirArchivoRevision(idRevision, archivo);
      await clienteQuery.invalidateQueries({ queryKey: ["archivos-revision", idRevision] });
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo subir el archivo.");
    }
  };

  const descargar = async (guidArchivo: string, nombreArchivo: string) => {
    try {
      const blob = await descargarArchivoBlob(guidArchivo);
      const url = URL.createObjectURL(blob);
      const enlace = document.createElement("a");
      enlace.href = url;
      enlace.download = nombreArchivo;
      enlace.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo descargar el archivo.");
    }
  };

  return (
    <Stack direction="row" spacing={0.5} sx={{ alignItems: "center", flexWrap: "wrap", mt: 0.5 }}>
      {archivos.data?.map((archivo) => (
        <Chip key={archivo.idArchivoVinculo} size="small" variant="outlined"
          icon={<AttachFileIcon fontSize="small" />}
          label={`${archivo.nombreArchivo} (${formatearTamano(archivo.tamanoBytes)})`}
          onClick={() => void descargar(archivo.guidArchivo, archivo.nombreArchivo)}
          deleteIcon={<DownloadIcon fontSize="small" />}
          onDelete={() => void descargar(archivo.guidArchivo, archivo.nombreArchivo)}
        />
      ))}
      <input ref={inputRef} type="file" hidden
        onChange={(evento) => {
          const archivo = evento.target.files?.[0];
          evento.target.value = "";
          if (archivo) void subir(archivo);
        }} />
      <Button size="small" onClick={() => inputRef.current?.click()}>+ Adjuntar</Button>
    </Stack>
  );
}

/** Hallazgos de QA y code review: la severidad decide si bloquean el cierre (S1/S2) o solo quedan registrados. */
export function PanelRevisiones({ idWorkItem, folio, alExito, alError }: Props) {
  const [modalNuevo, setModalNuevo] = useState(false);
  const [comentarios, setComentarios] = useState("");
  const [comentariosVacio, setComentariosVacio] = useState(true);
  const [idSeveridad, setIdSeveridad] = useState<number | "">("");
  const [archivosPendientes, setArchivosPendientes] = useState<File[]>([]);
  const inputArchivoRef = useRef<HTMLInputElement>(null);
  const [reabrir, setReabrir] = useState<Revision | null>(null);
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

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
    if (idSeveridad === "") return;
    setEnviando(true);
    try {
      const { dato, mensaje } = await crearRevision(idWorkItem, {
        comentarios, idSeveridad: idSeveridad as number,
      });
      if (dato && archivosPendientes.length > 0) {
        for (const archivo of archivosPendientes) {
          await subirArchivoRevision(dato.idRevision, archivo);
        }
      }
      alExito(mensaje);
      setModalNuevo(false);
      setComentarios("");
      setIdSeveridad("");
      setArchivosPendientes([]);
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

  const pendientesBloqueantes = revisiones.data?.filter((r) => !r.corregido && r.bloqueante).length ?? 0;

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Hallazgos de revision</Typography>
        <Button size="small" variant="contained" onClick={() => { setModalNuevo(true); setIdSeveridad(""); setArchivosPendientes([]); }}>
          Reportar hallazgo
        </Button>
      </Stack>

      {pendientesBloqueantes > 0 && (
        <Alert severity="warning" sx={{ mb: 1 }}>
          {pendientesBloqueantes === 1
            ? "1 hallazgo critico/alto sin corregir impide cerrar este elemento."
            : `${pendientesBloqueantes} hallazgos criticos/altos sin corregir impiden cerrar este elemento.`}
        </Alert>
      )}

      {revisiones.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
          Sin hallazgos registrados.
        </Typography>
      )}

      <List dense disablePadding>
        {revisiones.data?.map((revision) => (
          <ListItem key={revision.idRevision} disableGutters divider sx={{ gap: 1, alignItems: "flex-start" }}>
            <ListItemText
              sx={{ flex: 1, minWidth: 0 }}
              primary={
                <Stack spacing={0.5} sx={{ alignItems: "flex-start" }}>
                  <Stack direction="row" spacing={0.5} sx={{ flexWrap: "wrap" }}>
                    <Chip size="small" color={revision.corregido ? "success" : "warning"}
                      label={revision.corregido ? "Corregido" : "Pendiente"} />
                    {revision.severidad && (
                      <Chip size="small" color={revision.bloqueante ? "error" : "default"}
                        variant={revision.bloqueante ? "filled" : "outlined"}
                        label={revision.severidad} />
                    )}
                    {revision.casoPrueba && (
                      <Chip size="small" variant="outlined" label={`Prueba: ${revision.casoPrueba}`} />
                    )}
                  </Stack>
                  <ContenidoEnriquecido html={revision.comentarios ?? ""} />
                  <AdjuntosRevision idRevision={revision.idRevision} alError={alError} />
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
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" required>
            <InputLabel>Severidad</InputLabel>
            <Select label="Severidad" value={idSeveridad}
              onChange={(e) => setIdSeveridad(Number(e.target.value))}>
              {(catalogos.data?.severidades ?? []).map((s) => (
                <MenuItem key={s.id} value={s.id}>{s.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <EditorEnriquecido
            label="Que se encontro y que hay que ajustar"
            placeholder="Describe el hallazgo..."
            value={comentarios}
            onChange={setComentarios}
            onVacioChange={setComentariosVacio}
            idWorkItemParaAdjuntos={idWorkItem}
            onError={alError}
          />
          <Box>
            <input ref={inputArchivoRef} type="file" hidden multiple
              onChange={(evento) => {
                const nuevos = Array.from(evento.target.files ?? []);
                evento.target.value = "";
                setArchivosPendientes((prev) => [...prev, ...nuevos]);
              }} />
            <Button size="small" startIcon={<AttachFileIcon fontSize="small" />}
              onClick={() => inputArchivoRef.current?.click()}>
              Adjuntar archivo
            </Button>
            <Stack direction="row" spacing={0.5} sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}>
              {archivosPendientes.map((archivo, indice) => (
                <Chip key={indice} size="small" label={archivo.name}
                  onDelete={() => setArchivosPendientes((prev) => prev.filter((_, i) => i !== indice))} />
              ))}
            </Stack>
          </Box>
          <Typography variant="caption" color="text.secondary">
            Si el elemento ya estaba terminado, un hallazgo critico o alto lo regresa a Correccion.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || comentariosVacio || idSeveridad === ""}
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
          <Button color="error" onClick={() => setReabrir(null)}>Cancelar</Button>
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
