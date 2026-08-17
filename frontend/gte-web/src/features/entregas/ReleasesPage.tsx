import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, FormControl, InputLabel, LinearProgress, Link, MenuItem, Paper, Select,
  Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField,
  Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  agregarArtefacto, agregarContenido, cambiarEstatusRelease, colorEstatusRelease,
  crearRelease, generarNotas, obtenerMatrizAmbientes, obtenerRelease, obtenerReleases,
  registrarDespliegue, resolverAprobacion,
} from "../../shared/api/entregas";
import { filtroInicial, obtenerBandeja, obtenerCatalogosBandeja } from "../../shared/api/workitems";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** P13 y P14 - Releases: contenido, artefactos, cadena de firmas y despliegues. */
export function ReleasesPage() {
  const [idRelease, setIdRelease] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [modalContenido, setModalContenido] = useState(false);
  const [modalArtefacto, setModalArtefacto] = useState(false);
  const [modalDespliegue, setModalDespliegue] = useState(false);
  const [modalRechazo, setModalRechazo] = useState<number | null>(null);
  const [modalReabrir, setModalReabrir] = useState(false);
  const [motivoReabrir, setMotivoReabrir] = useState("");
  const [idProyectoNuevo, setIdProyectoNuevo] = useState<number | "">("");
  const [version, setVersion] = useState("");
  const [seleccionados, setSeleccionados] = useState<number[]>([]);
  const [nombreArtefacto, setNombreArtefacto] = useState("");
  const [idTipoArtefacto, setIdTipoArtefacto] = useState(1);
  const [justificacion, setJustificacion] = useState("");
  const [idAmbiente, setIdAmbiente] = useState<number | "">("");
  const [esRollback, setEsRollback] = useState(false);
  const [bitacora, setBitacora] = useState("");
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [busquedaMatriz, setBusquedaMatriz] = useState("");
  const [busquedaReleases, setBusquedaReleases] = useState("");
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });
  const releases = useQuery({ queryKey: ["releases"], queryFn: () => obtenerReleases() });
  const actual = idRelease === "" ? releases.data?.[0]?.idRelease : (idRelease as number);

  const detalle = useQuery({
    queryKey: ["release", actual],
    queryFn: () => obtenerRelease(actual!),
    enabled: actual !== undefined,
  });

  const candidatos = useQuery({
    queryKey: ["candidatos", detalle.data?.idProyecto],
    queryFn: () => obtenerBandeja({
      ...filtroInicial, pageSize: 100, estatus: [6], idProyecto: detalle.data!.idProyecto,
    }),
    enabled: modalContenido && detalle.data !== undefined,
  });

  const releasesFiltrados = (releases.data ?? []).filter((rel) => {
    const texto = busquedaReleases.trim().toLowerCase();
    if (!texto) return true;
    return rel.claveProyecto.toLowerCase().includes(texto)
      || rel.proyecto.toLowerCase().includes(texto)
      || rel.version.toLowerCase().includes(texto)
      || (rel.folio ?? "").toLowerCase().includes(texto);
  });
  const { datosOrdenados: releasesOrdenados, ordenarPor: ordenReleases, descendente: descReleases, ordenar: ordenarReleases }
    = useOrdenTabla(releasesFiltrados);

  const matriz = useQuery({ queryKey: ["matriz-ambientes"], queryFn: obtenerMatrizAmbientes });
  const matrizFiltrada = (matriz.data ?? []).filter((f) => {
    const texto = busquedaMatriz.trim().toLowerCase();
    if (!texto) return true;
    return f.ambiente.toLowerCase().includes(texto) || (f.claveProyecto ?? "").toLowerCase().includes(texto);
  });
  const { datosOrdenados: matrizOrdenada, ordenarPor: ordenMatriz, descendente: descMatriz, ordenar: ordenarMatriz }
    = useOrdenTabla(matrizFiltrada);

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["releases"] }),
    clienteQuery.invalidateQueries({ queryKey: ["release"] }),
    clienteQuery.invalidateQueries({ queryKey: ["matriz-ambientes"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
      await refrescar();
    } catch (error) {
      if (error instanceof ErrorApi) {
        const d = error.detalle as Record<string, string[]> | undefined;
        const extra = d
          ? " " + Object.entries(d)
              .filter(([, v]) => Array.isArray(v) && v.length > 0)
              .map(([k, v]) => `${k}: ${v.join("; ")}`)
              .join(" | ")
          : "";
        setAviso({ tipo: "error", mensaje: error.message + extra });
      } else {
        setAviso({ tipo: "error", mensaje: respaldo });
      }
    } finally {
      setEnviando(false);
    }
  };

  const r = detalle.data;
  const artefactosIncompletos = r?.artefactos.filter((a) => !a.cumpleRollback) ?? [];

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Releases</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
          Nuevo release
        </Button>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Todos los releases ({releasesFiltrados.length})
        </Typography>
        <TextField size="small" placeholder="Buscar proyecto, version o folio..." value={busquedaReleases}
          onChange={(e) => setBusquedaReleases(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />
        <TableContainer sx={{ maxHeight: 360, overflowY: "auto" }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="claveProyecto" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Proyecto</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="version" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Version</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaPlan" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Fecha plan</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaLiberacion" ordenActual={ordenReleases} descendente={descReleases} onOrdenar={ordenarReleases}>Liberado</EncabezadoOrdenable>
              </TableRow>
            </TableHead>
            <TableBody>
              {releasesOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                      Sin releases con esa busqueda.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {releasesOrdenados.map((rel) => (
                <TableRow key={rel.idRelease} hover selected={rel.idRelease === actual}
                  sx={{ cursor: "pointer" }} onClick={() => setIdRelease(rel.idRelease)}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{rel.folio ?? "-"}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{rel.claveProyecto} - {rel.proyecto}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{rel.version}</TableCell>
                  <TableCell>
                    <Chip size="small" label={rel.estatus} color={colorEstatusRelease(rel.idEstatus)} />
                  </TableCell>
                  <TableCell>{formatearFecha(rel.fechaPlan)}</TableCell>
                  <TableCell>{formatearFecha(rel.fechaLiberacion)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      {releases.data?.length === 0 && (
        <Alert severity="info">No hay releases. Crea uno para empezar a preparar una entrega.</Alert>
      )}
      {detalle.isLoading && <LinearProgress />}

      {r && (
        <>
          <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}
              sx={{ justifyContent: "space-between" }}>
              <Box>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>
                    {r.claveProyecto} {r.version}
                  </Typography>
                  <Chip size="small" label={r.estatus} color={colorEstatusRelease(r.idEstatus)} />
                  {r.folio && <Chip size="small" variant="outlined" label={r.folio} />}
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  {r.proyecto} - planeado {formatearFecha(r.fechaPlan)}
                  {r.fechaLiberacion && ` - liberado ${formatearFecha(r.fechaLiberacion)}`}
                </Typography>
              </Box>
              <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", alignItems: "flex-start" }}>
                {r.idEstatus === 1 && (
                  <>
                    <Button size="small" variant="outlined" onClick={() => setModalContenido(true)}>
                      Agregar contenido
                    </Button>
                    <Button size="small" variant="outlined" onClick={() => setModalArtefacto(true)}>
                      Agregar artefacto
                    </Button>
                    <Button size="small" variant="contained"
                      onClick={() => void manejar(
                        () => cambiarEstatusRelease(r.idRelease, "SOLICITAR_APROBACION"),
                        "No se pudo solicitar la aprobacion.")}>
                      Solicitar aprobacion
                    </Button>
                  </>
                )}
                {(r.idEstatus === 3 || r.idEstatus === 4) && (
                  <Button size="small" variant="contained" onClick={() => setModalDespliegue(true)}>
                    Registrar despliegue
                  </Button>
                )}
                {r.idEstatus === 3 && (
                  <Button size="small" color="warning" variant="outlined"
                    onClick={() => setModalReabrir(true)}>
                    Reabrir
                  </Button>
                )}
                <Button size="small" onClick={() => void manejar(
                  () => generarNotas(r.idRelease).then((res) => ({ mensaje: res.mensaje })),
                  "No se pudieron generar las notas.")}>
                  Generar notas
                </Button>
              </Stack>
            </Stack>

            {artefactosIncompletos.length > 0 && (
              <Alert severity="warning" sx={{ mt: 2 }}>
                {artefactosIncompletos.length === 1
                  ? "Un script SQL no tiene rollback ni justificacion: "
                  : `${artefactosIncompletos.length} scripts SQL sin rollback ni justificacion: `}
                {artefactosIncompletos.map((a) => a.nombre).join(", ")}
              </Alert>
            )}

            {r.notasVersion && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2">Notas de version</Typography>
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{r.notasVersion}</Typography>
              </Box>
            )}
          </Paper>

          <Stack direction={{ xs: "column", lg: "row" }} spacing={2}>
            <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                Contenido ({r.items.length})
              </Typography>
              {r.items.length === 0 && (
                <Typography variant="body2" color="text.secondary">
                  Sin elementos. Solo entran los terminados y sin hallazgos pendientes.
                </Typography>
              )}
              {r.items.map((item) => (
                <Stack key={item.idWorkItem} direction="row" spacing={1}
                  sx={{ alignItems: "center", py: 0.5 }}>
                  <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover"
                    sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                    {item.folio}
                  </Link>
                  <Chip size="small" variant="outlined" label={item.tipo} sx={{ height: 18 }} />
                  <Typography variant="body2" noWrap>{item.titulo}</Typography>
                </Stack>
              ))}

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                Artefactos ({r.artefactos.length})
              </Typography>
              <Table size="small">
                <TableBody>
                  {r.artefactos.map((a) => (
                    <TableRow key={a.idArtefacto}>
                      <TableCell sx={{ width: 30 }}>
                        {a.cumpleRollback
                          ? <CheckCircleIcon fontSize="small" color="success" />
                          : <Tooltip title="Falta rollback o justificacion">
                              <WarningAmberIcon fontSize="small" color="warning" />
                            </Tooltip>}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{a.nombre}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {a.tipo}
                          {a.ordenEjecucion !== null && ` - orden ${a.ordenEjecucion}`}
                          {a.nombreRollback && ` - reversa: ${a.nombreRollback}`}
                          {a.justificacionIrreversible && ` - ${a.justificacionIrreversible}`}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Paper>

            <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                Cadena de aprobacion
              </Typography>
              {r.aprobaciones.length === 0 && (
                <Typography variant="body2" color="text.secondary">
                  La cadena se crea al solicitar la aprobacion.
                </Typography>
              )}
              {r.aprobaciones.map((ap) => (
                <Stack key={ap.idAprobacion} direction="row" spacing={1}
                  sx={{ alignItems: "center", py: 0.75 }}>
                  <Chip size="small" label={ap.rolAprobacion} sx={{ minWidth: 78 }} />
                  <Chip size="small" label={ap.estatus}
                    color={ap.idEstatus === 2 ? "success" : ap.idEstatus === 3 ? "error" : "default"} />
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    {ap.aprobador && (
                      <Typography variant="caption" sx={{ display: "block" }}>
                        {ap.aprobador} - {formatearFecha(ap.fechaResolucion)}
                      </Typography>
                    )}
                    {ap.firmaHash && (
                      <Tooltip title={`Firma: ${ap.firmaHash}`}>
                        <Typography variant="caption" color="text.secondary">
                          firma {ap.firmaHash.slice(0, 12)}...
                        </Typography>
                      </Tooltip>
                    )}
                    {ap.comentario && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
                        {ap.comentario}
                      </Typography>
                    )}
                  </Box>
                  {ap.idEstatus === 1 && r.idEstatus === 2 && (
                    <Stack direction="row" spacing={0.5}>
                      <Button size="small" variant="contained" disabled={enviando}
                        onClick={() => void manejar(
                          () => resolverAprobacion(ap.idAprobacion, true, "Autorizado"),
                          "No se pudo firmar.")}>
                        Firmar
                      </Button>
                      <Button size="small" color="error"
                        onClick={() => setModalRechazo(ap.idAprobacion)}>
                        Rechazar
                      </Button>
                    </Stack>
                  )}
                </Stack>
              ))}

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                Despliegues ({r.despliegues.length})
              </Typography>
              {r.despliegues.length === 0 && (
                <Typography variant="body2" color="text.secondary">Sin despliegues registrados.</Typography>
              )}
              {r.despliegues.map((d) => (
                <Box key={d.idDespliegue} sx={{ py: 0.5 }}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <Chip size="small" label={d.ambiente} />
                    {d.esRollback && <Chip size="small" color="error" label="Rollback" />}
                    <Typography variant="caption">
                      {formatearFecha(d.fechaInicio)} - {d.ejecutor}
                    </Typography>
                  </Stack>
                  {d.bitacora && (
                    <Typography variant="caption" color="text.secondary">{d.bitacora}</Typography>
                  )}
                </Box>
              ))}
            </Paper>
          </Stack>
        </>
      )}

      <Paper variant="outlined" sx={{ p: 2, mt: 2 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Version viva por ambiente
        </Typography>
        <TextField size="small" placeholder="Buscar ambiente o proyecto..." value={busquedaMatriz}
          onChange={(e) => setBusquedaMatriz(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />
        <Table size="small">
          <TableHead>
            <TableRow sx={{ "& th": { fontWeight: 700 } }}>
              <EncabezadoOrdenable clave="ambiente" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Ambiente</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="claveProyecto" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Proyecto</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="versionDesplegada" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Version</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="fechaDespliegue" ordenActual={ordenMatriz} descendente={descMatriz} onOrdenar={ordenarMatriz}>Desplegado</EncabezadoOrdenable>
            </TableRow>
          </TableHead>
          <TableBody>
            {matrizOrdenada.map((fila) => (
              <TableRow key={fila.idAmbiente}>
                <TableCell><Chip size="small" label={fila.ambiente} /></TableCell>
                <TableCell>{fila.claveProyecto ?? "-"}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{fila.versionDesplegada ?? "-"}</TableCell>
                <TableCell>{formatearFecha(fila.fechaDespliegue)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nuevo release</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Proyecto"
            required
            value={idProyectoNuevo}
            onChange={(v) => setIdProyectoNuevo(v as number | "")}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
          />
          <TextField size="small" required label="Version" value={version} placeholder="2.11.0"
            onChange={(e) => setVersion(e.target.value)}
            helperText="Versionado semantico" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyectoNuevo === "" || !version.trim()}
            onClick={() => { setModalNuevo(false); void manejar(() => crearRelease({
              idProyecto: idProyectoNuevo as number, version: version.trim(), fechaPlan: null,
            }).then((res) => { setVersion(""); return res; }), "No se pudo crear el release."); }}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalContenido} onClose={() => setModalContenido(false)} fullWidth maxWidth="sm">
        <DialogTitle>Agregar contenido al release</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Solo aparecen los elementos terminados del proyecto.
          </Typography>
          <ComboBuscableMultiple
            label="Elementos"
            resumenSimple
            value={seleccionados}
            onChange={(valores) => setSeleccionados(valores as number[])}
            opciones={(candidatos.data?.items ?? []).map((item) => ({
              valor: item.idWorkItem, etiqueta: `${item.folio} - ${item.titulo}`,
            }))}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalContenido(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || seleccionados.length === 0}
            onClick={() => { setModalContenido(false); void manejar(
              () => agregarContenido(r!.idRelease, seleccionados).then((res) => {
                setSeleccionados([]); return res;
              }), "No se pudo agregar el contenido."); }}>
            Agregar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalArtefacto} onClose={() => setModalArtefacto(false)} fullWidth maxWidth="sm">
        <DialogTitle>Agregar artefacto</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre del archivo" value={nombreArtefacto}
            onChange={(e) => setNombreArtefacto(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Tipo</InputLabel>
            <Select label="Tipo" value={idTipoArtefacto}
              onChange={(e) => setIdTipoArtefacto(Number(e.target.value))}>
              <MenuItem value={1}>Paquete</MenuItem>
              <MenuItem value={2}>Script SQL</MenuItem>
              <MenuItem value={3}>Archivo de configuracion</MenuItem>
              <MenuItem value={4}>Otro</MenuItem>
            </Select>
          </FormControl>
          {idTipoArtefacto === 2 && (
            <TextField size="small" multiline minRows={2}
              label="Justificacion si no hay script de reversa"
              value={justificacion} onChange={(e) => setJustificacion(e.target.value)}
              helperText="Un script SQL necesita reversa o esta justificacion para poder aprobarse" />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalArtefacto(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || !nombreArtefacto.trim()}
            onClick={() => { setModalArtefacto(false); void manejar(
              () => agregarArtefacto(r!.idRelease, {
                nombre: nombreArtefacto.trim(),
                idTipoArtefacto,
                ordenEjecucion: null,
                idArtefactoRollback: null,
                justificacionIrreversible: justificacion.trim() || null,
              }).then((res) => { setNombreArtefacto(""); setJustificacion(""); return res; }),
              "No se pudo agregar el artefacto."); }}>
            Agregar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalDespliegue} onClose={() => setModalDespliegue(false)} fullWidth maxWidth="xs">
        <DialogTitle>Registrar despliegue</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Ambiente"
            required
            value={idAmbiente}
            onChange={(v) => setIdAmbiente(v as number | "")}
            opciones={(matriz.data ?? []).map((a) => ({ valor: a.idAmbiente, etiqueta: a.ambiente }))}
          />
          <FormControl size="small">
            <InputLabel>Tipo</InputLabel>
            <Select label="Tipo" value={esRollback ? 1 : 0}
              onChange={(e) => setEsRollback(Number(e.target.value) === 1)}>
              <MenuItem value={0}>Despliegue</MenuItem>
              <MenuItem value={1}>Rollback</MenuItem>
            </Select>
          </FormControl>
          <TextField size="small" multiline minRows={2} label="Bitacora"
            value={bitacora} onChange={(e) => setBitacora(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalDespliegue(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idAmbiente === ""}
            onClick={() => { setModalDespliegue(false); void manejar(
              () => registrarDespliegue(r!.idRelease, {
                idAmbiente: idAmbiente as number,
                esRollback,
                bitacora: bitacora.trim() || null,
              }).then((res) => { setBitacora(""); return res; }),
              "No se pudo registrar el despliegue."); }}>
            Registrar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalRechazo !== null} onClose={() => setModalRechazo(null)} fullWidth maxWidth="sm">
        <DialogTitle>Rechazar release</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Por que se rechaza (obligatorio)"
            value={comentario} onChange={(e) => setComentario(e.target.value)} />
          <Typography variant="caption" color="text.secondary">
            El release regresa a preparacion y el contenido se descongela.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalRechazo(null)}>Cancelar</Button>
          <Button variant="contained" color="error"
            disabled={enviando || !comentario.trim()}
            onClick={() => { const id = modalRechazo!; setModalRechazo(null); void manejar(
              () => resolverAprobacion(id, false, comentario.trim()).then((res) => {
                setComentario(""); return res;
              }), "No se pudo rechazar."); }}>
            Rechazar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalReabrir} onClose={() => setModalReabrir(false)} fullWidth maxWidth="sm">
        <DialogTitle>Reabrir release</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            El release regresa a preparacion para agregar contenido o artefactos. Esto invalida
            las firmas ya puestas: al volver a solicitar aprobacion, QA/Lider/Negocio deben firmar
            de nuevo.
          </Typography>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo de la reapertura (obligatorio)"
            value={motivoReabrir} onChange={(e) => setMotivoReabrir(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalReabrir(false)}>Cancelar</Button>
          <Button variant="contained" color="warning"
            disabled={enviando || !motivoReabrir.trim()}
            onClick={() => { setModalReabrir(false); void manejar(
              () => cambiarEstatusRelease(r!.idRelease, "REABRIR", motivoReabrir.trim()).then((res) => {
                setMotivoReabrir(""); return res;
              }), "No se pudo reabrir el release."); }}>
            Reabrir
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={8000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
