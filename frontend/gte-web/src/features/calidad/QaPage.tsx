import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, LinearProgress, Link, MenuItem, Paper, Select, Snackbar,
  Stack, Tab, Table, TableBody, TableCell, TableHead, TableRow, Tabs, TextField,
  Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import BugReportIcon from "@mui/icons-material/BugReport";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  RESULTADOS, crearBugDesdeEjecucion, crearCaso, crearCiclo, crearPlan,
  obtenerCasos, obtenerCiclos, obtenerMatriz, obtenerPlanes, registrarEjecucion,
  type CasoPrueba,
} from "../../shared/api/calidad";
import { filtroInicial, obtenerBandeja, obtenerCatalogosBandeja } from "../../shared/api/workitems";

/** P12 - QA: planes, ciclos, ejecucion de casos y matriz de trazabilidad. */
export function QaPage() {
  const [pestana, setPestana] = useState(0);
  const [idPlan, setIdPlan] = useState<number | "">("");
  const [idCiclo, setIdCiclo] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalPlan, setModalPlan] = useState(false);
  const [modalCaso, setModalCaso] = useState(false);
  const [modalResultado, setModalResultado] = useState<CasoPrueba | null>(null);
  const [idResultado, setIdResultado] = useState(1);
  const [observaciones, setObservaciones] = useState("");
  const [nombrePlan, setNombrePlan] = useState("");
  const [idProyectoPlan, setIdProyectoPlan] = useState<number | "">("");
  const [tituloCaso, setTituloCaso] = useState("");
  const [pasoCaso, setPasoCaso] = useState("");
  const [idRequisito, setIdRequisito] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const [busquedaCasos, setBusquedaCasos] = useState("");
  const [busquedaMatriz, setBusquedaMatriz] = useState("");
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });
  const planes = useQuery({ queryKey: ["planes-prueba"], queryFn: () => obtenerPlanes() });
  const planActual = idPlan === "" ? planes.data?.[0]?.idPlanPrueba : (idPlan as number);

  const ciclos = useQuery({
    queryKey: ["ciclos", planActual],
    queryFn: () => obtenerCiclos(planActual!),
    enabled: planActual !== undefined,
  });
  const cicloActual = idCiclo === "" ? ciclos.data?.[0]?.idCicloPrueba : (idCiclo as number);

  const casos = useQuery({
    queryKey: ["casos", planActual, cicloActual],
    queryFn: () => obtenerCasos(planActual!, cicloActual),
    enabled: planActual !== undefined,
  });

  const matriz = useQuery({
    queryKey: ["matriz", planActual],
    queryFn: () => obtenerMatriz(planActual!),
    enabled: planActual !== undefined && pestana === 1,
  });

  // Requisitos del proyecto del plan, para vincular el caso al abrir el modal
  const idProyectoDelPlan = planes.data?.find((p) => p.idPlanPrueba === planActual)?.idProyecto;
  const requisitos = useQuery({
    queryKey: ["requisitos", idProyectoDelPlan],
    queryFn: () => obtenerBandeja({
      ...filtroInicial, pageSize: 100, estatus: [-1], idProyecto: idProyectoDelPlan!,
    }),
    enabled: modalCaso && idProyectoDelPlan !== undefined,
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["planes-prueba"] }),
    clienteQuery.invalidateQueries({ queryKey: ["ciclos"] }),
    clienteQuery.invalidateQueries({ queryKey: ["casos"] }),
    clienteQuery.invalidateQueries({ queryKey: ["matriz"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
      await refrescar();
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : respaldo });
    } finally {
      setEnviando(false);
    }
  };

  const guardarResultado = () => {
    if (!modalResultado || cicloActual === undefined) return;
    const caso = modalResultado;
    setModalResultado(null);
    void manejar(() => registrarEjecucion(cicloActual, {
      idCasoPrueba: caso.idCasoPrueba,
      idResultadoPrueba: idResultado,
      observaciones: observaciones.trim() || null,
    }).then((r) => { setObservaciones(""); return r; }), "No se pudo registrar el resultado.");
  };

  const cicloSeleccionado = ciclos.data?.find((c) => c.idCicloPrueba === cicloActual);

  const casosFiltrados = (casos.data ?? []).filter((c) => {
    const texto = busquedaCasos.trim().toLowerCase();
    if (!texto) return true;
    return (c.folio ?? "").toLowerCase().includes(texto) || c.titulo.toLowerCase().includes(texto);
  });
  const { datosOrdenados: casosOrdenados, ordenarPor: ordenCasos, descendente: descCasos, ordenar: ordenarCasos }
    = useOrdenTabla(casosFiltrados);

  const matrizFiltrada = (matriz.data ?? []).filter((f) => {
    const texto = busquedaMatriz.trim().toLowerCase();
    if (!texto) return true;
    return f.folio.toLowerCase().includes(texto) || f.titulo.toLowerCase().includes(texto);
  });
  const { datosOrdenados: matrizOrdenada, ordenarPor: ordenMatriz, descendente: descMatriz, ordenar: ordenarMatriz }
    = useOrdenTabla(matrizFiltrada);

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Calidad</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setModalPlan(true)}>
            Nuevo plan
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} disabled={planActual === undefined}
            onClick={() => setModalCaso(true)}>
            Nuevo caso
          </Button>
        </Stack>
      </Stack>

      <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: "wrap" }}>
        <ComboBuscable
          label="Plan de pruebas"
          value={planActual ?? ""}
          onChange={(v) => { setIdPlan(v as number | ""); setIdCiclo(""); }}
          opciones={(planes.data ?? []).map((p) => ({ valor: p.idPlanPrueba, etiqueta: p.nombre }))}
          sx={{ minWidth: 260 }}
        />
        <ComboBuscable
          label="Ciclo"
          value={cicloActual ?? ""}
          onChange={(v) => setIdCiclo(v as number | "")}
          opciones={(ciclos.data ?? []).map((c) => ({ valor: c.idCicloPrueba, etiqueta: c.nombre }))}
          sx={{ minWidth: 200 }}
        />
        <Button size="small" disabled={planActual === undefined}
          onClick={() => void manejar(
            () => crearCiclo(planActual!, `Ciclo ${(ciclos.data?.length ?? 0) + 1}`),
            "No se pudo crear el ciclo.")}>
          Nuevo ciclo
        </Button>
      </Stack>

      {planes.data?.length === 0 && (
        <Alert severity="info">
          No hay planes de prueba. Crea uno para empezar a registrar casos.
        </Alert>
      )}

      {cicloSeleccionado && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {cicloSeleccionado.nombre}: {cicloSeleccionado.ejecutados}/{cicloSeleccionado.totalCasos} ejecutados
          {" - "}{cicloSeleccionado.pasa} pasa, {cicloSeleccionado.falla} falla,
          {" "}{cicloSeleccionado.bloqueado} bloqueado
        </Alert>
      )}

      <Tabs value={pestana} onChange={(_, v) => setPestana(v)} sx={{ mb: 2 }}>
        <Tab label="Casos y ejecucion" />
        <Tab label="Trazabilidad" />
      </Tabs>

      {(casos.isLoading || planes.isLoading) && <LinearProgress />}

      {pestana === 0 && (
        <Paper variant="outlined">
          <Box sx={{ p: 1.5 }}>
            <TextField size="small" placeholder="Buscar folio o caso..." value={busquedaCasos}
              onChange={(e) => setBusquedaCasos(e.target.value)} sx={{ minWidth: 280 }} />
          </Box>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Caso</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="tipoPrueba" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Tipo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="folioWorkItem" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Requisito</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="idUltimoResultado" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Ultimo resultado</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="folioBug" ordenActual={ordenCasos} descendente={descCasos} onOrdenar={ordenarCasos}>Bug</EncabezadoOrdenable>
                <TableCell align="center">Acciones</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {casosOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Typography color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
                      Este plan no tiene casos de prueba.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {casosOrdenados.map((caso) => {
                const resultado = RESULTADOS.find((r) => r.id === caso.idUltimoResultado);
                return (
                  <TableRow key={caso.idCasoPrueba} hover>
                    <TableCell sx={{ whiteSpace: "nowrap", fontWeight: 600 }}>{caso.folio}</TableCell>
                    <TableCell sx={{ maxWidth: 340 }}>
                      <Tooltip title={caso.pasos.map((p) => `${p.numeroPaso}. ${p.accion}`).join("\n")}>
                        <Typography variant="body2" noWrap>{caso.titulo}</Typography>
                      </Tooltip>
                      <Typography variant="caption" color="text.secondary">
                        {caso.pasos.length} paso(s)
                      </Typography>
                    </TableCell>
                    <TableCell>{caso.tipoPrueba}</TableCell>
                    <TableCell>
                      {caso.folioWorkItem ? (
                        <Link component={RouterLink} to={`/wi/${caso.folioWorkItem}`} underline="hover">
                          {caso.folioWorkItem}
                        </Link>
                      ) : "-"}
                    </TableCell>
                    <TableCell>
                      {resultado
                        ? <Chip size="small" color={resultado.color} label={resultado.nombre} />
                        : <Typography variant="caption" color="text.secondary">sin ejecutar</Typography>}
                    </TableCell>
                    <TableCell>
                      {caso.folioBug ? (
                        <Link component={RouterLink} to={`/wi/${caso.folioBug}`} underline="hover">
                          {caso.folioBug}
                        </Link>
                      ) : "-"}
                    </TableCell>
                    <TableCell align="center">
                      <Stack direction="row" spacing={0.5} sx={{ justifyContent: "center" }}>
                        <Button size="small" disabled={cicloActual === undefined}
                          onClick={() => { setModalResultado(caso); setIdResultado(1); }}>
                          Registrar
                        </Button>
                        {caso.idUltimoResultado === 2 && !caso.folioBug && (
                          <Tooltip title="Crear bug desde esta falla">
                            <Button size="small" color="error" startIcon={<BugReportIcon />}
                              onClick={() => void manejar(
                                () => crearBugDesdeEjecucion(caso.idEjecucion!, {
                                  idPrioridad: 2, idAsignado: null,
                                }), "No se pudo crear el bug.")}>
                              Bug
                            </Button>
                          </Tooltip>
                        )}
                      </Stack>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </Paper>
      )}

      {pestana === 1 && (
        <Paper variant="outlined">
          <Box sx={{ p: 1.5 }}>
            <TextField size="small" placeholder="Buscar folio o titulo..." value={busquedaMatriz}
              onChange={(e) => setBusquedaMatriz(e.target.value)} sx={{ minWidth: 280 }} />
          </Box>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Requisito</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Titulo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="totalCasos" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz} align="center">Casos</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="casosPasa" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz} align="center">Pasa</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="casosFalla" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz} align="center">Falla</EncabezadoOrdenable>
                <TableCell>Cobertura</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {matrizOrdenada.map((fila) => (
                <TableRow key={fila.idWorkItem} hover
                  sx={{ backgroundColor: fila.sinCobertura ? "#fff8e1" : undefined }}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    <Link component={RouterLink} to={`/wi/${fila.folio}`} underline="hover"
                      sx={{ fontWeight: 600 }}>
                      {fila.folio}
                    </Link>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 360 }}>
                    <Typography variant="body2" noWrap>{fila.titulo}</Typography>
                  </TableCell>
                  <TableCell align="center">{fila.totalCasos}</TableCell>
                  <TableCell align="center">{fila.casosPasa}</TableCell>
                  <TableCell align="center">{fila.casosFalla}</TableCell>
                  <TableCell>
                    {fila.sinCobertura
                      ? <Chip size="small" color="warning" label="Sin cobertura" />
                      : <Chip size="small" color="success" label="Cubierto" />}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      <Dialog open={modalResultado !== null} onClose={() => setModalResultado(null)} fullWidth maxWidth="sm">
        <DialogTitle>Registrar resultado - {modalResultado?.folio}</DialogTitle>
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
          <TextField size="small" multiline minRows={2} label="Observaciones"
            value={observaciones} onChange={(e) => setObservaciones(e.target.value)}
            helperText={idResultado === 2 || idResultado === 3
              ? "Obligatorio: describe que fallo o que bloqueo la prueba"
              : undefined} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalResultado(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando} onClick={guardarResultado}>Guardar</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalPlan} onClose={() => setModalPlan(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo plan de pruebas</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Proyecto"
            required
            value={idProyectoPlan}
            onChange={(v) => setIdProyectoPlan(v as number | "")}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
          />
          <TextField size="small" required label="Nombre del plan" value={nombrePlan}
            onChange={(e) => setNombrePlan(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalPlan(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyectoPlan === "" || !nombrePlan.trim()}
            onClick={() => { setModalPlan(false); void manejar(() => crearPlan({
              idProyecto: idProyectoPlan as number,
              nombre: nombrePlan.trim(),
              descripcion: null,
              idRelease: null,
            }).then((r) => { setNombrePlan(""); return r; }), "No se pudo crear el plan."); }}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalCaso} onClose={() => setModalCaso(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo caso de prueba</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo del caso" value={tituloCaso}
            onChange={(e) => setTituloCaso(e.target.value)} />
          <TextField size="small" label="Primer paso" value={pasoCaso}
            onChange={(e) => setPasoCaso(e.target.value)}
            helperText="Los pasos adicionales se agregan editando el caso" />
          <ComboBuscable
            label="Requisito que cubre"
            value={idRequisito}
            onChange={(v) => setIdRequisito(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin vincular" },
              ...(requisitos.data?.items ?? []).map((item) => ({
                valor: item.idWorkItem, etiqueta: `${item.folio} - ${item.titulo}`,
              })),
            ]}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalCaso(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !tituloCaso.trim()}
            onClick={() => { setModalCaso(false); void manejar(() => crearCaso(planActual!, {
              titulo: tituloCaso.trim(),
              precondiciones: null,
              resultadoEsperado: null,
              idTipoPrueba: 1,
              idWorkItem: idRequisito === "" ? null : (idRequisito as number),
              pasos: pasoCaso.trim()
                ? [{ numeroPaso: 1, accion: pasoCaso.trim(), resultadoEsperado: null }]
                : [],
            }).then((r) => { setTituloCaso(""); setPasoCaso(""); return r; }), "No se pudo crear el caso."); }}>
            Crear
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
