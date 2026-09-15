import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, LinearProgress, MenuItem, Paper, Select,
  Snackbar, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import { obtenerProyectos } from "../../shared/api/administracion";
import {
  TIPOS_PRUEBA, actualizarCaso, obtenerCatalogoCasos, retirarCaso,
  type CasoAdmin, type PasoCaso,
} from "../../shared/api/calidad";

/** Administracion del catalogo de casos de prueba reutilizables de un proyecto:
 * buscador/orden, editar (titulo, precondiciones, resultado esperado, pasos) y
 * retirar (baja logica), mostrando cuantos WorkItems tienen el caso asignado. */
export function CasosPruebaTab() {
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [busqueda, setBusqueda] = useState("");
  const [casoEditar, setCasoEditar] = useState<CasoAdmin | null>(null);
  const [tituloEditar, setTituloEditar] = useState("");
  const [precondicionesEditar, setPrecondicionesEditar] = useState("");
  const [resultadoEsperadoEditar, setResultadoEsperadoEditar] = useState("");
  const [idTipoPruebaEditar, setIdTipoPruebaEditar] = useState(1);
  const [pasosEditar, setPasosEditar] = useState<PasoCaso[]>([]);
  const [casoRetirar, setCasoRetirar] = useState<CasoAdmin | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });
  const catalogo = useQuery({
    queryKey: ["catalogo-casos", idProyecto],
    queryFn: () => obtenerCatalogoCasos(idProyecto as number),
    enabled: idProyecto !== "",
  });

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const casosFiltrados = (catalogo.data ?? []).filter((c) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return c.titulo.toLowerCase().includes(texto) || (c.folio ?? "").toLowerCase().includes(texto);
  });
  const { datosOrdenados: casosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(casosFiltrados);

  const abrirEditar = (caso: CasoAdmin) => {
    setCasoEditar(caso);
    setTituloEditar(caso.titulo);
    setPrecondicionesEditar(caso.precondiciones ?? "");
    setResultadoEsperadoEditar(caso.resultadoEsperado ?? "");
    setIdTipoPruebaEditar(caso.idTipoPrueba);
    setPasosEditar(caso.pasos.length > 0 ? caso.pasos : [{ numeroPaso: 1, accion: "", resultadoEsperado: null }]);
  };

  const guardarEdicion = async () => {
    if (!casoEditar) return;
    setEnviando(true);
    try {
      const { mensaje } = await actualizarCaso(casoEditar.idCasoPrueba, {
        titulo: tituloEditar.trim(),
        precondiciones: precondicionesEditar.trim() || null,
        resultadoEsperado: resultadoEsperadoEditar.trim() || null,
        idTipoPrueba: idTipoPruebaEditar,
        pasos: pasosEditar
          .filter((p) => p.accion.trim().length > 0)
          .map((p, i) => ({ ...p, numeroPaso: i + 1 })),
      });
      avisar(mensaje);
      setCasoEditar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["catalogo-casos", idProyecto] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo actualizar el caso.", true);
    } finally {
      setEnviando(false);
    }
  };

  const confirmarRetiro = async () => {
    if (!casoRetirar) return;
    setEnviando(true);
    try {
      const { mensaje } = await retirarCaso(casoRetirar.idCasoPrueba);
      avisar(mensaje);
      setCasoRetirar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["catalogo-casos", idProyecto] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el caso.", true);
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2 }}>Casos de prueba</Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: "wrap" }}>
        <ComboBuscable
          label="Proyecto"
          required
          value={idProyecto}
          onChange={(v) => setIdProyecto(v as number | "")}
          opciones={(proyectos.data ?? []).map((p) => ({ valor: p.idProyecto, etiqueta: p.clave }))}
          sx={{ minWidth: 280 }}
        />
        <TextField size="small" placeholder="Buscar folio o titulo..." value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)} disabled={idProyecto === ""} sx={{ minWidth: 280 }} />
      </Stack>

      {idProyecto === "" ? (
        <Alert severity="info">Elige un proyecto para ver su catalogo de casos de prueba.</Alert>
      ) : (
        <Paper variant="outlined">
          {catalogo.isLoading && <LinearProgress />}
          <Table size="small">
            <TableHead>
              <TableRow>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="titulo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Titulo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="tipoPrueba" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Tipo</EncabezadoOrdenable>
                <TableCell>Reutilizable</TableCell>
                <TableCell>Estado</TableCell>
                <EncabezadoOrdenable clave="totalAsignaciones" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">WorkItems</EncabezadoOrdenable>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {casosOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                      No hay casos de prueba registrados en este proyecto.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {casosOrdenados.map((c) => (
                <TableRow key={c.idCasoPrueba} hover>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{c.folio ?? "-"}</TableCell>
                  <TableCell>{c.titulo}</TableCell>
                  <TableCell>{c.tipoPrueba}</TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" label={c.reutilizable ? "Si" : "No"} />
                  </TableCell>
                  <TableCell>
                    <Chip size="small" color={c.activo ? "success" : "default"} label={c.activo ? "Activo" : "Retirado"} />
                  </TableCell>
                  <TableCell align="right">{c.totalAsignaciones}</TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={0.5} sx={{ justifyContent: "flex-end" }}>
                      <Button size="small" onClick={() => abrirEditar(c)}>Editar</Button>
                      {c.activo && (
                        <Button size="small" color="error" onClick={() => setCasoRetirar(c)}>Retirar</Button>
                      )}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      <Dialog open={casoEditar !== null} onClose={() => setCasoEditar(null)} fullWidth maxWidth="sm">
        <DialogTitle>Editar caso de prueba {casoEditar?.folio ? `- ${casoEditar.folio}` : ""}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Titulo" value={tituloEditar}
            onChange={(e) => setTituloEditar(e.target.value)} />
          <TextField size="small" label="Precondiciones" multiline minRows={2} value={precondicionesEditar}
            onChange={(e) => setPrecondicionesEditar(e.target.value)} />
          <TextField size="small" label="Resultado esperado" multiline minRows={2} value={resultadoEsperadoEditar}
            onChange={(e) => setResultadoEsperadoEditar(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Tipo de prueba</InputLabel>
            <Select label="Tipo de prueba" value={idTipoPruebaEditar}
              onChange={(e) => setIdTipoPruebaEditar(Number(e.target.value))}>
              {TIPOS_PRUEBA.map((t) => (
                <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Stack spacing={1}>
            <Typography variant="caption" color="text.secondary">Pasos</Typography>
            {pasosEditar.map((paso, indice) => (
              <Stack key={indice} direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <TextField size="small" fullWidth label={`Paso ${indice + 1}`} value={paso.accion}
                  onChange={(e) => setPasosEditar((prev) => prev.map((p, i) =>
                    i === indice ? { ...p, accion: e.target.value } : p))} />
                <IconButton size="small" disabled={pasosEditar.length === 1}
                  onClick={() => setPasosEditar((prev) => prev
                    .filter((_, i) => i !== indice)
                    .map((p, i) => ({ ...p, numeroPaso: i + 1 })))}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button size="small" startIcon={<AddIcon />}
              onClick={() => setPasosEditar((prev) => [...prev, { numeroPaso: prev.length + 1, accion: "", resultadoEsperado: null }])}>
              Agregar paso
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setCasoEditar(null)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !tituloEditar.trim()} onClick={() => void guardarEdicion()}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={casoRetirar !== null} onClose={() => setCasoRetirar(null)} fullWidth maxWidth="xs">
        <DialogTitle>Retirar caso de prueba</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            {`"${casoRetirar?.titulo}" `}
            {casoRetirar && casoRetirar.totalAsignaciones > 0
              ? `esta asignado a ${casoRetirar.totalAsignaciones} WorkItem(s) activo(s). `
              : "no tiene asignaciones activas. "}
            ¿Retirarlo del catalogo? Ya no podra asignarse a nuevos WorkItems.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setCasoRetirar(null)}>Cancelar</Button>
          <Button variant="contained" color="error" disabled={enviando} onClick={() => void confirmarRetiro()}>
            Retirar
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
