import { useRef, useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, FormControlLabel, Checkbox, IconButton, InputLabel, MenuItem, Select,
  Stack, Table, TableBody, TableCell, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import PlaylistAddCheckIcon from "@mui/icons-material/PlaylistAddCheck";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import { subirArchivoRevision } from "../../shared/api/archivos";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import {
  RESULTADOS, asignarCasoExistente, crearCasoYAsignar, obtenerCasosAsignados,
  obtenerCasosDisponibles, registrarEjecucion, retirarAsignacion,
  type CasoAsignado, type PasoCaso,
} from "../../shared/api/calidad";

interface Props {
  idWorkItem: number;
  idProyecto: number;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Pruebas: casos asignados a este WorkItem, del catalogo del proyecto o creados aqui mismo.
 * Una falla crea un hallazgo (ver pestana Revisiones) sobre este mismo item, no un ticket nuevo. */
export function PanelPruebas({ idWorkItem, idProyecto, alExito, alError }: Props) {
  const [modalAsignar, setModalAsignar] = useState(false);
  const [idCasoAsignar, setIdCasoAsignar] = useState<number | "">("");
  const [modalNuevo, setModalNuevo] = useState(false);
  const [tituloNuevo, setTituloNuevo] = useState("");
  const [reutilizableNuevo, setReutilizableNuevo] = useState(true);
  const [pasosNuevo, setPasosNuevo] = useState<PasoCaso[]>([{ numeroPaso: 1, accion: "", resultadoEsperado: null }]);
  const [modalResultado, setModalResultado] = useState<CasoAsignado | null>(null);
  const [idResultado, setIdResultado] = useState(1);
  const [idSeveridad, setIdSeveridad] = useState<number | "">("");
  const [observaciones, setObservaciones] = useState("");
  const [archivosPendientes, setArchivosPendientes] = useState<File[]>([]);
  const [enviando, setEnviando] = useState(false);
  const inputArchivoRef = useRef<HTMLInputElement>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

  const casos = useQuery({
    queryKey: ["casos-asignados", idWorkItem],
    queryFn: () => obtenerCasosAsignados(idWorkItem),
  });

  const disponibles = useQuery({
    queryKey: ["casos-disponibles", idProyecto],
    queryFn: () => obtenerCasosDisponibles(idProyecto),
    enabled: modalAsignar,
  });

  const yaAsignados = new Set((casos.data ?? []).map((c) => c.idCasoPrueba));
  const opcionesDisponibles = (disponibles.data ?? []).filter((c) => !yaAsignados.has(c.idCasoPrueba));

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["casos-asignados", idWorkItem] }),
    clienteQuery.invalidateQueries({ queryKey: ["casos-disponibles", idProyecto] }),
    clienteQuery.invalidateQueries({ queryKey: ["revisiones", idWorkItem] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejarError = (error: unknown, respaldo: string) => {
    alError(error instanceof ErrorApi ? error.message : respaldo);
  };

  const asignarExistente = async () => {
    if (idCasoAsignar === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await asignarCasoExistente(idWorkItem, idCasoAsignar as number);
      alExito(mensaje);
      setModalAsignar(false);
      setIdCasoAsignar("");
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo asignar el caso.");
    } finally {
      setEnviando(false);
    }
  };

  const crearNuevo = async () => {
    setEnviando(true);
    try {
      const { mensaje } = await crearCasoYAsignar(idWorkItem, {
        titulo: tituloNuevo.trim(),
        precondiciones: null,
        resultadoEsperado: null,
        idTipoPrueba: 1,
        reutilizable: reutilizableNuevo,
        pasos: pasosNuevo.filter((p) => p.accion.trim().length > 0).map((p, i) => ({ ...p, numeroPaso: i + 1 })),
      });
      alExito(mensaje);
      setModalNuevo(false);
      setTituloNuevo("");
      setReutilizableNuevo(true);
      setPasosNuevo([{ numeroPaso: 1, accion: "", resultadoEsperado: null }]);
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo crear el caso.");
    } finally {
      setEnviando(false);
    }
  };

  const retirar = async (idWorkItemCasoPrueba: number) => {
    try {
      const mensaje = (await retirarAsignacion(idWorkItemCasoPrueba)).mensaje;
      alExito(mensaje);
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo retirar el caso.");
    }
  };

  const abrirRegistrarResultado = (caso: CasoAsignado) => {
    setModalResultado(caso);
    setIdResultado(1);
    setIdSeveridad("");
    setObservaciones("");
    setArchivosPendientes([]);
  };

  const guardarResultado = async () => {
    if (!modalResultado) return;
    const caso = modalResultado;
    setEnviando(true);
    try {
      const { dato, mensaje } = await registrarEjecucion(idWorkItem, {
        idCasoPrueba: caso.idCasoPrueba,
        idResultadoPrueba: idResultado,
        observaciones: observaciones.trim() || null,
        idSeveridad: idResultado === 2 ? (idSeveridad as number) : null,
      });
      if (dato?.idRevision && archivosPendientes.length > 0) {
        for (const archivo of archivosPendientes) {
          await subirArchivoRevision(dato.idRevision, archivo);
        }
        await clienteQuery.invalidateQueries({ queryKey: ["archivos-revision", dato.idRevision] });
      }
      alExito(dato?.idRevision
        ? `${mensaje} Se registro un hallazgo para dar seguimiento.`
        : mensaje);
      setModalResultado(null);
      await refrescar();
    } catch (error) {
      manejarError(error, "No se pudo registrar el resultado.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Pruebas</Typography>
        <Stack direction="row" spacing={1}>
          <Button size="small" onClick={() => setModalAsignar(true)}>Usar caso existente</Button>
          <Button size="small" variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
            Nuevo caso
          </Button>
        </Stack>
      </Stack>

      {casos.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
          Sin casos de prueba asignados todavia.
        </Typography>
      )}

      <Table size="small">
        <TableBody>
          {casos.data?.map((caso) => {
            const resultado = RESULTADOS.find((r) => r.id === caso.idUltimoResultado);
            return (
              <TableRow key={caso.idWorkItemCasoPrueba} hover>
                <TableCell sx={{ whiteSpace: "nowrap", fontWeight: 600 }}>{caso.folio ?? "-"}</TableCell>
                <TableCell sx={{ maxWidth: 320 }}>
                  <Tooltip title={caso.pasos.map((p) => `${p.numeroPaso}. ${p.accion}`).join("\n")}>
                    <Typography variant="body2" noWrap>{caso.titulo}</Typography>
                  </Tooltip>
                </TableCell>
                <TableCell>
                  {resultado
                    ? <Chip size="small" color={resultado.color} label={resultado.nombre} />
                    : <Typography variant="caption" color="text.secondary">sin ejecutar</Typography>}
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={0.5} sx={{ justifyContent: "flex-end" }}>
                    <Button size="small" startIcon={<PlaylistAddCheckIcon fontSize="small" />}
                      onClick={() => abrirRegistrarResultado(caso)}>
                      Registrar
                    </Button>
                    <Tooltip title="Quitar de este elemento">
                      <IconButton size="small" onClick={() => void retirar(caso.idWorkItemCasoPrueba)}>
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>

      <Dialog open={modalAsignar} onClose={() => setModalAsignar(false)} fullWidth maxWidth="sm">
        <DialogTitle>Usar caso existente</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <ComboBuscable
            label="Caso de prueba"
            required
            value={idCasoAsignar}
            onChange={(v) => setIdCasoAsignar(v as number | "")}
            opciones={opcionesDisponibles.map((c) => ({ valor: c.idCasoPrueba, etiqueta: `${c.folio ?? ""} ${c.titulo}`.trim() }))}
          />
          {opcionesDisponibles.length === 0 && (
            <Alert severity="info" sx={{ mt: 2 }}>
              No hay casos reutilizables disponibles en este proyecto todavia.
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalAsignar(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idCasoAsignar === ""} onClick={() => void asignarExistente()}>
            Asignar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo caso de prueba</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo del caso" value={tituloNuevo}
            onChange={(e) => setTituloNuevo(e.target.value)} />
          <Stack spacing={1}>
            <Typography variant="caption" color="text.secondary">Pasos</Typography>
            {pasosNuevo.map((paso, indice) => (
              <Stack key={indice} direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <TextField size="small" fullWidth label={`Paso ${indice + 1}`} value={paso.accion}
                  onChange={(e) => setPasosNuevo((prev) => prev.map((p, i) =>
                    i === indice ? { ...p, accion: e.target.value } : p))} />
                <IconButton size="small" disabled={pasosNuevo.length === 1}
                  onClick={() => setPasosNuevo((prev) => prev
                    .filter((_, i) => i !== indice)
                    .map((p, i) => ({ ...p, numeroPaso: i + 1 })))}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button size="small" startIcon={<AddIcon />}
              onClick={() => setPasosNuevo((prev) => [...prev, { numeroPaso: prev.length + 1, accion: "", resultadoEsperado: null }])}>
              Agregar paso
            </Button>
          </Stack>
          <FormControlLabel
            control={<Checkbox checked={reutilizableNuevo} onChange={(e) => setReutilizableNuevo(e.target.checked)} />}
            label="Guardar en el catalogo del proyecto para reutilizarlo en otros elementos"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !tituloNuevo.trim()} onClick={() => void crearNuevo()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalResultado !== null} onClose={() => setModalResultado(null)} fullWidth maxWidth="sm">
        <DialogTitle>Registrar resultado - {modalResultado?.folio ?? modalResultado?.titulo}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <Typography variant="body2">{modalResultado?.titulo}</Typography>
          <FormControl size="small">
            <InputLabel>Resultado</InputLabel>
            <Select label="Resultado" value={idResultado}
              onChange={(e) => setIdResultado(Number(e.target.value))}>
              {RESULTADOS.map((r) => (
                <MenuItem key={r.id} value={r.id}>{r.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          {idResultado === 2 && (
            <FormControl size="small" required>
              <InputLabel>Severidad</InputLabel>
              <Select label="Severidad" value={idSeveridad}
                onChange={(e) => setIdSeveridad(Number(e.target.value))}>
                {(catalogos.data?.severidades ?? []).map((s) => (
                  <MenuItem key={s.id} value={s.id}>{s.nombre}</MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          <EditorEnriquecido
            label="Observaciones"
            placeholder={idResultado === 2 || idResultado === 3
              ? "Obligatorio: describe que fallo o que bloqueo la prueba..."
              : "Observaciones (opcional)..."}
            value={observaciones}
            onChange={setObservaciones}
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
            {idResultado !== 2 && archivosPendientes.length > 0 && (
              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 0.5 }}>
                Los adjuntos solo se guardan si el resultado es Falla (quedan en el hallazgo).
              </Typography>
            )}
            <Stack direction="row" spacing={0.5} sx={{ mt: 1, flexWrap: "wrap", gap: 0.5 }}>
              {archivosPendientes.map((archivo, indice) => (
                <Chip key={indice} size="small" label={archivo.name}
                  onDelete={() => setArchivosPendientes((prev) => prev.filter((_, i) => i !== indice))} />
              ))}
            </Stack>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalResultado(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || (idResultado === 2 && idSeveridad === "")}
            onClick={() => void guardarResultado()}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
