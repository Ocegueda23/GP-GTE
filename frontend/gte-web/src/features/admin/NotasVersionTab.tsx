import { useState } from "react";
import {
  Alert, Box, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, IconButton, LinearProgress, MenuItem, Paper, Snackbar, Stack, Table,
  TableBody, TableCell, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  actualizarNotaVersion, crearNotaVersion, eliminarNotaVersion, obtenerNotaVersion,
  obtenerNotasVersionAdministracion, obtenerTiposCambioVersion,
  type RenglonNotaVersionRequest,
} from "../../shared/api/notasVersion";

/** Renglon en edicion. Los nuevos viven solo con uiId hasta que el back les da su id real. */
interface RenglonEditable extends RenglonNotaVersionRequest {
  uiId: string;
}

function nuevoUiId(): string {
  return `ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * P20 - Notas de version: lo que trae cada liberacion, redactado para el usuario final.
 * Es lo que abre el sello de version de la barra superior. Mientras se redacta vive como
 * borrador; solo al marcar "Publicada" la ve el resto de la gente.
 */
export function NotasVersionTab() {
  const [modal, setModal] = useState(false);
  const [idEditando, setIdEditando] = useState<number | null>(null);
  const [version, setVersion] = useState("");
  const [fechaLiberacion, setFechaLiberacion] = useState(hoyIso());
  const [resumen, setResumen] = useState("");
  const [publicada, setPublicada] = useState(false);
  const [renglones, setRenglones] = useState<RenglonEditable[]>([]);
  const [guardando, setGuardando] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const notas = useQuery({ queryKey: ["notas-version-admin"], queryFn: obtenerNotasVersionAdministracion });
  const tiposCambio = useQuery({
    queryKey: ["tipos-cambio-version"], queryFn: obtenerTiposCambioVersion, staleTime: 5 * 60_000,
  });

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const idTipoPorDefecto = tiposCambio.data?.[0]?.idTipoCambioVersion ?? 2;

  const limpiar = () => {
    setIdEditando(null); setVersion(""); setFechaLiberacion(hoyIso());
    setResumen(""); setPublicada(false); setRenglones([]);
  };

  const abrirNueva = () => { limpiar(); setModal(true); };

  const abrirEdicion = async (idNotaVersion: number) => {
    try {
      const nota = await obtenerNotaVersion(idNotaVersion);
      setIdEditando(nota.idNotaVersion);
      setVersion(nota.version);
      setFechaLiberacion(nota.fechaLiberacion);
      setResumen(nota.resumen ?? "");
      setPublicada(nota.publicada);
      setRenglones(nota.renglones.map((r) => ({
        idNotaVersionDetalle: r.idNotaVersionDetalle,
        uiId: r.uiId ?? nuevoUiId(),
        idTipoCambioVersion: r.idTipoCambioVersion,
        modulo: r.modulo,
        descripcion: r.descripcion,
        orden: r.orden,
      })));
      setModal(true);
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo abrir la nota.", true);
    }
  };

  const agregarRenglon = () => setRenglones((previos) => [...previos, {
    idNotaVersionDetalle: null,
    uiId: nuevoUiId(),
    idTipoCambioVersion: idTipoPorDefecto,
    modulo: null,
    descripcion: "",
    orden: previos.length,
  }]);

  const cambiarRenglon = (uiId: string, cambios: Partial<RenglonEditable>) =>
    setRenglones((previos) => previos.map((r) => (r.uiId === uiId ? { ...r, ...cambios } : r)));

  const quitarRenglon = (uiId: string) =>
    setRenglones((previos) => previos.filter((r) => r.uiId !== uiId));

  /** El orden que vale es la posicion del arreglo: el back lo renumera al guardar. */
  const moverRenglon = (indice: number, direccion: -1 | 1) => setRenglones((previos) => {
    const destino = indice + direccion;
    if (destino < 0 || destino >= previos.length) return previos;
    const copia = [...previos];
    [copia[indice], copia[destino]] = [copia[destino], copia[indice]];
    return copia;
  });

  const guardar = async () => {
    const sinDescripcion = renglones.some((r) => r.descripcion.trim() === "");
    if (sinDescripcion) {
      avisar("Hay renglones sin descripcion.", true);
      return;
    }

    setGuardando(true);
    try {
      const datos = {
        version: version.trim(),
        fechaLiberacion,
        resumen: resumen.trim() || null,
        publicada,
        renglones: renglones.map((r, indice) => ({
          idNotaVersionDetalle: r.idNotaVersionDetalle,
          uiId: r.uiId,
          idTipoCambioVersion: r.idTipoCambioVersion,
          modulo: r.modulo?.trim() || null,
          descripcion: r.descripcion.trim(),
          orden: indice,
        })),
      };

      const { mensaje } = idEditando === null
        ? await crearNotaVersion(datos)
        : await actualizarNotaVersion(idEditando, datos);

      avisar(mensaje);
      setModal(false);
      limpiar();
      await clienteQuery.invalidateQueries({ queryKey: ["notas-version-admin"] });
      // El panel de novedades cachea 5 min: se refresca para que el cambio se vea de inmediato.
      await clienteQuery.invalidateQueries({ queryKey: ["notas-version-publicadas"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo guardar la nota.", true);
    } finally {
      setGuardando(false);
    }
  };

  const eliminar = async (idNotaVersion: number) => {
    try {
      const { mensaje } = await eliminarNotaVersion(idNotaVersion);
      avisar(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["notas-version-admin"] });
      await clienteQuery.invalidateQueries({ queryKey: ["notas-version-publicadas"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo eliminar la nota.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Notas de version</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={abrirNueva}>Nueva nota</Button>
      </Stack>

      <Alert severity="info" sx={{ mb: 2 }}>
        Redactalas pensando en el usuario final: lo que ya puede hacer, no la peticion que
        origino el cambio. El numero debe ser el mismo que quedo en Directory.Build.props al
        liberar. Mientras "Publicada" este apagada, solo la ves tu.
      </Alert>

      <Paper variant="outlined">
        {notas.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Version</TableCell>
              <TableCell>Fecha</TableCell>
              <TableCell>Resumen</TableCell>
              <TableCell align="right">Renglones</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(notas.data ?? []).length === 0 && !notas.isLoading && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay notas de version capturadas.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {(notas.data ?? []).map((nota) => (
              <TableRow key={nota.idNotaVersion} hover>
                <TableCell sx={{ fontWeight: 600 }}>{nota.version}</TableCell>
                <TableCell>{new Date(`${nota.fechaLiberacion}T00:00:00`).toLocaleDateString()}</TableCell>
                <TableCell sx={{ maxWidth: 360 }}>{nota.resumen ?? "-"}</TableCell>
                <TableCell align="right">{nota.totalRenglones}</TableCell>
                <TableCell>{nota.publicada ? "Publicada" : "Borrador"}</TableCell>
                <TableCell align="right">
                  <Tooltip title="Editar">
                    <IconButton size="small" onClick={() => abrirEdicion(nota.idNotaVersion)}>
                      <EditOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Eliminar">
                    <IconButton size="small" onClick={() => eliminar(nota.idNotaVersion)}>
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} maxWidth="md" fullWidth>
        <DialogTitle>{idEditando === null ? "Nueva nota de version" : `Nota de la version ${version}`}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", gap: 1 }}>
              <TextField size="small" label="Version" value={version} sx={{ width: 160 }}
                onChange={(e) => setVersion(e.target.value)} placeholder="1.21.0.0" />
              <TextField size="small" type="date" label="Fecha de liberacion" value={fechaLiberacion}
                onChange={(e) => setFechaLiberacion(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }} sx={{ width: 190 }} />
              <FormControlLabel
                control={<Checkbox checked={publicada} onChange={(e) => setPublicada(e.target.checked)} />}
                label="Publicada" />
            </Stack>

            <TextField size="small" label="Resumen (opcional)" value={resumen} fullWidth
              onChange={(e) => setResumen(e.target.value)}
              placeholder="Frase corta que encabeza la version" />

            <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center" }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Detalle</Typography>
              <Button size="small" startIcon={<AddIcon />} onClick={agregarRenglon}>Agregar renglon</Button>
            </Stack>

            {renglones.length === 0 && (
              <Typography variant="body2" color="text.secondary">
                Sin renglones todavia. Agrega uno por cada cambio que el usuario va a notar.
              </Typography>
            )}

            <Stack spacing={1}>
              {renglones.map((renglon, indice) => (
                <Stack key={renglon.uiId} direction="row" spacing={1}
                  sx={{ alignItems: "flex-start", flexWrap: "wrap", gap: 1 }}>
                  <TextField select size="small" label="Tipo" value={renglon.idTipoCambioVersion}
                    sx={{ width: 130 }}
                    onChange={(e) => cambiarRenglon(renglon.uiId, { idTipoCambioVersion: Number(e.target.value) })}>
                    {(tiposCambio.data ?? []).map((t) => (
                      <MenuItem key={t.idTipoCambioVersion} value={t.idTipoCambioVersion}>{t.nombre}</MenuItem>
                    ))}
                  </TextField>
                  <TextField size="small" label="Modulo" value={renglon.modulo ?? ""} sx={{ width: 160 }}
                    onChange={(e) => cambiarRenglon(renglon.uiId, { modulo: e.target.value })} />
                  <TextField size="small" label="Descripcion" value={renglon.descripcion}
                    sx={{ flex: 1, minWidth: 260 }}
                    onChange={(e) => cambiarRenglon(renglon.uiId, { descripcion: e.target.value })} />
                  <Stack direction="row">
                    <Tooltip title="Subir">
                      <span>
                        <IconButton size="small" disabled={indice === 0} onClick={() => moverRenglon(indice, -1)}>
                          <ArrowUpwardIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title="Bajar">
                      <span>
                        <IconButton size="small" disabled={indice === renglones.length - 1}
                          onClick={() => moverRenglon(indice, 1)}>
                          <ArrowDownwardIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title="Quitar">
                      <IconButton size="small" onClick={() => quitarRenglon(renglon.uiId)}>
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </Stack>
              ))}
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" loading={guardando}
            disabled={version.trim() === "" || fechaLiberacion === ""} onClick={guardar}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={4000} onClose={() => setAviso(null)}>
        <Alert severity={aviso?.tipo ?? "success"} onClose={() => setAviso(null)}>{aviso?.mensaje}</Alert>
      </Snackbar>
    </Box>
  );
}
