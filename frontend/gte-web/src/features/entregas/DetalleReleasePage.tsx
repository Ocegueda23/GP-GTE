import { useEffect, useState } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, FormControl, FormControlLabel, IconButton, InputLabel, LinearProgress, Link,
  MenuItem, Paper, Select, Snackbar, Stack, Switch, Table, TableBody, TableCell,
  TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import DeleteOutlinedIcon from "@mui/icons-material/DeleteOutlined";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import {
  actualizarInstrucciones, agregarArtefacto, agregarContenido, agregarRespaldo, asignarLider,
  cambiarEstatusRelease, colorEstatusRelease, editarArtefacto, editarRespaldo, generarNotas,
  obtenerCandidatosContenido, obtenerCatalogosEntregas, obtenerMatrizAmbientes, obtenerRelease,
  quitarArtefacto, quitarContenido, quitarRespaldo, registrarDespliegue, resolverAprobacion,
} from "../../shared/api/entregas";
import type { Artefacto, Respaldo } from "../../shared/api/entregas";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import { formatearFecha } from "./formato";

/** Estatus En Preparacion: el unico en el que la entrega se puede seguir armando. */
const EN_PREPARACION = 1;

/** Borrador del formulario de artefacto, compartido por el alta y la edicion. */
interface FormArtefacto {
  /** Nulo en un artefacto nuevo; con id, se edita el existente. */
  idArtefacto: number | null;
  nombre: string;
  idTipoArtefacto: number | "";
  versionArtefacto: string;
  ordenEjecucion: string;
  idArtefactoRollback: number | "";
  justificacion: string;
  instrucciones: string;
}

const FORM_ARTEFACTO_VACIO: FormArtefacto = {
  idArtefacto: null, nombre: "", idTipoArtefacto: "", versionArtefacto: "",
  ordenEjecucion: "", idArtefactoRollback: "", justificacion: "", instrucciones: "",
};

interface FormRespaldo {
  idReleaseRespaldo: number | null;
  idTipoRespaldo: number | "";
  descripcion: string;
}

const FORM_RESPALDO_VACIO: FormRespaldo = {
  idReleaseRespaldo: null, idTipoRespaldo: "", descripcion: "",
};

/**
 * P13 y P14 - Detalle de un release: encabezado con acciones, lider asignado, instructivo,
 * contenido, artefactos y respaldos; a la derecha la cadena de firmas, los despliegues y la
 * version viva por ambiente. Ruta propia /releases/:id, igual que /wi/:folio en trabajo.
 */
export function DetalleReleasePage() {
  const { id } = useParams<{ id: string }>();
  const idRelease = Number(id);
  const { puede } = useSesion();
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalContenido, setModalContenido] = useState(false);
  const [modalArtefacto, setModalArtefacto] = useState(false);
  const [formArtefacto, setFormArtefacto] = useState<FormArtefacto>(FORM_ARTEFACTO_VACIO);
  const [modalRespaldo, setModalRespaldo] = useState(false);
  const [formRespaldo, setFormRespaldo] = useState<FormRespaldo>(FORM_RESPALDO_VACIO);
  const [modalDespliegue, setModalDespliegue] = useState(false);
  const [modalRechazo, setModalRechazo] = useState<number | null>(null);
  const [modalReabrir, setModalReabrir] = useState(false);
  const [motivoReabrir, setMotivoReabrir] = useState("");
  const [modalAutorizar, setModalAutorizar] = useState(false);
  const [motivoAutorizar, setMotivoAutorizar] = useState("");
  const [seleccionados, setSeleccionados] = useState<number[]>([]);
  const [idSprintFiltro, setIdSprintFiltro] = useState<number | "">("");
  const [textoCandidatos, setTextoCandidatos] = useState("");
  const [tipoCandidatos, setTipoCandidatos] = useState("");
  const [ocultarBloqueados, setOcultarBloqueados] = useState(false);
  const [instrucciones, setInstrucciones] = useState("");
  const [releaseInstrucciones, setReleaseInstrucciones] = useState<number | null>(null);
  const [idAmbiente, setIdAmbiente] = useState<number | "">("");
  const [esRollback, setEsRollback] = useState(false);
  const [bitacora, setBitacora] = useState("");
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });
  const catalogosEntregas = useQuery({
    queryKey: ["catalogos-entregas"], queryFn: obtenerCatalogosEntregas, staleTime: 5 * 60_000,
  });
  const detalle = useQuery({
    queryKey: ["release", idRelease],
    queryFn: () => obtenerRelease(idRelease),
    enabled: Number.isFinite(idRelease) && idRelease > 0,
  });
  const matriz = useQuery({ queryKey: ["matriz-ambientes"], queryFn: obtenerMatrizAmbientes });

  const candidatos = useQuery({
    queryKey: ["candidatos-release", idRelease],
    queryFn: () => obtenerCandidatosContenido(idRelease),
    enabled: modalContenido,
  });

  // El filtro por sprint se arma con los sprints que de verdad tienen candidatos: asi no
  // hay que pedir el catalogo de sprints ni ofrecer opciones que no traerian nada. El id 0
  // representa "terminados fuera de un sprint", que no tienen IdSprint.
  const sprintsConCandidatos = [...new Map(
    (candidatos.data ?? []).map((c) => [
      c.idSprint ?? 0,
      { id: c.idSprint, nombre: c.sprint ?? "Sin sprint" },
    ]),
  ).values()].sort((a, b) => a.nombre.localeCompare(b.nombre));

  const tiposConCandidatos = [...new Set((candidatos.data ?? []).map((c) => c.tipo))].sort();
  const hayBloqueados = (candidatos.data ?? []).some((c) => c.hallazgosPendientes > 0);

  const candidatosFiltrados = (candidatos.data ?? []).filter((c) => {
    if (idSprintFiltro !== "" && (c.idSprint ?? 0) !== idSprintFiltro) return false;
    if (tipoCandidatos && c.tipo !== tipoCandidatos) return false;
    if (ocultarBloqueados && c.hallazgosPendientes > 0) return false;
    const texto = textoCandidatos.trim().toLowerCase();
    if (texto && !c.folio.toLowerCase().includes(texto)
        && !c.titulo.toLowerCase().includes(texto)) return false;
    return true;
  });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["releases"] }),
    clienteQuery.invalidateQueries({ queryKey: ["release", idRelease] }),
    clienteQuery.invalidateQueries({ queryKey: ["candidatos-release", idRelease] }),
    clienteQuery.invalidateQueries({ queryKey: ["matriz-ambientes"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  const manejar = async (accion: () => Promise<{ mensaje: string }>, respaldo: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await accion();
      setAviso({ tipo: "success", mensaje });
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
      await refrescar();
      setEnviando(false);
    }
  };

  const r = detalle.data;
  const editable = r?.idEstatus === EN_PREPARACION;
  const artefactosIncompletos = r?.artefactos.filter((a) => !a.cumpleRollback) ?? [];

  // El editor de instrucciones es un borrador local: se recarga solo al cambiar de release,
  // no en cada refresco del detalle -- si no, lo que el usuario lleva escrito se perderia
  // cada vez que otra accion de la pantalla invalida la consulta.
  useEffect(() => {
    if (!r || r.idRelease === releaseInstrucciones) return;
    setInstrucciones(r.instruccionesImplementacion ?? "");
    setReleaseInstrucciones(r.idRelease);
  }, [r, releaseInstrucciones]);

  const abrirArtefactoNuevo = () => {
    setFormArtefacto({
      ...FORM_ARTEFACTO_VACIO,
      idTipoArtefacto: catalogosEntregas.data?.tiposArtefacto[0]?.id ?? "",
    });
    setModalArtefacto(true);
  };

  const abrirArtefactoEdicion = (a: Artefacto) => {
    setFormArtefacto({
      idArtefacto: a.idArtefacto,
      nombre: a.nombre,
      idTipoArtefacto: a.idTipoArtefacto,
      versionArtefacto: a.versionArtefacto ?? "",
      ordenEjecucion: a.ordenEjecucion === null ? "" : String(a.ordenEjecucion),
      idArtefactoRollback: a.idArtefactoRollback ?? "",
      justificacion: a.justificacionIrreversible ?? "",
      instrucciones: a.instruccionesImplementacion ?? "",
    });
    setModalArtefacto(true);
  };

  const guardarArtefacto = () => {
    const f = formArtefacto;
    const orden = f.ordenEjecucion.trim();
    const datos = {
      nombre: f.nombre.trim(),
      idTipoArtefacto: f.idTipoArtefacto as number,
      ordenEjecucion: orden === "" ? null : Number(orden),
      idArtefactoRollback: f.idArtefactoRollback === "" ? null : (f.idArtefactoRollback as number),
      justificacionIrreversible: f.justificacion.trim() || null,
      instruccionesImplementacion: f.instrucciones || null,
      versionArtefacto: f.versionArtefacto.trim() || null,
    };
    setModalArtefacto(false);
    void manejar(
      () => (f.idArtefacto === null
        ? agregarArtefacto(idRelease, datos).then((res) => ({ mensaje: res.mensaje }))
        : editarArtefacto(idRelease, f.idArtefacto, datos)),
      f.idArtefacto === null
        ? "No se pudo agregar el artefacto."
        : "No se pudo actualizar el artefacto.",
    );
    setFormArtefacto(FORM_ARTEFACTO_VACIO);
  };

  const abrirRespaldoNuevo = () => {
    setFormRespaldo({
      ...FORM_RESPALDO_VACIO,
      idTipoRespaldo: catalogosEntregas.data?.tiposRespaldo[0]?.id ?? "",
    });
    setModalRespaldo(true);
  };

  const abrirRespaldoEdicion = (rp: Respaldo) => {
    setFormRespaldo({
      idReleaseRespaldo: rp.idReleaseRespaldo,
      idTipoRespaldo: rp.idTipoRespaldo,
      descripcion: rp.descripcion,
    });
    setModalRespaldo(true);
  };

  const guardarRespaldo = () => {
    const f = formRespaldo;
    const datos = {
      idTipoRespaldo: f.idTipoRespaldo as number,
      descripcion: f.descripcion.trim(),
    };
    setModalRespaldo(false);
    void manejar(
      () => (f.idReleaseRespaldo === null
        ? agregarRespaldo(idRelease, datos).then((res) => ({ mensaje: res.mensaje }))
        : editarRespaldo(idRelease, f.idReleaseRespaldo, datos)),
      f.idReleaseRespaldo === null
        ? "No se pudo agregar el respaldo."
        : "No se pudo actualizar el respaldo.",
    );
    setFormRespaldo(FORM_RESPALDO_VACIO);
  };

  if (detalle.isLoading) {
    return <Box sx={{ p: 2 }}><LinearProgress /></Box>;
  }

  if (!r) {
    return (
      <Box sx={{ p: 2 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navegar("/releases")} sx={{ mb: 2 }}>
          Volver a releases
        </Button>
        <Alert severity="error">No se encontro el release solicitado.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 2 }}>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navegar("/releases")} sx={{ mb: 1.5 }}>
        Volver a releases
      </Button>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}
          sx={{ justifyContent: "space-between" }}>
          <Box>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", flexWrap: "wrap" }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {r.claveProyecto} {r.version}
              </Typography>
              <Chip size="small" label={r.estatus} color={colorEstatusRelease(r.idEstatus)} />
              {r.folio && <Chip size="small" variant="outlined" label={r.folio} />}
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {r.proyecto} - creado por {r.creadoPor} el {formatearFecha(r.fechaCreacion)}
              {r.fechaPlan && ` - planeado ${formatearFecha(r.fechaPlan)}`}
              {r.fechaLiberacion && ` - liberado ${formatearFecha(r.fechaLiberacion)}`}
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", alignItems: "flex-start" }}>
            {editable && (
              <>
                <Button size="small" variant="outlined" onClick={() => {
                  setIdSprintFiltro("");
                  setTextoCandidatos("");
                  setTipoCandidatos("");
                  setOcultarBloqueados(false);
                  setSeleccionados([]);
                  setModalContenido(true);
                }}>
                  Agregar contenido
                </Button>
                <Button size="small" variant="outlined" onClick={abrirArtefactoNuevo}>
                  Agregar artefacto
                </Button>
                <Button size="small" variant="outlined" onClick={abrirRespaldoNuevo}>
                  Agregar respaldo
                </Button>
                <Button size="small" variant="contained"
                  onClick={() => void manejar(
                    () => cambiarEstatusRelease(r.idRelease, "SOLICITAR_APROBACION"),
                    "No se pudo solicitar la aprobacion.")}>
                  Solicitar aprobacion
                </Button>
              </>
            )}
            {/* Paso de autorizacion: acceso propio (REL.Autorizar), no el de firmar.
                Da por cubiertas las firmas que sigan pendientes. */}
            {r.idEstatus === 2 && puede("REL.Autorizar") && (
              <Button size="small" color="warning" variant="contained"
                onClick={() => setModalAutorizar(true)}>
                Autorizar sin firmas
              </Button>
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
            {/* A partir de En Aprobacion el contenido, los artefactos y el instructivo
                ya no se pueden mover: hasta entonces el reporte imprimiria algo que
                todavia va a cambiar. */}
            {!editable && (
              <Button size="small" variant="outlined" startIcon={<DescriptionOutlinedIcon />}
                onClick={() => navegar(`/releases/${r.idRelease}/solicitud`)}>
                Solicitud de despliegue
              </Button>
            )}
            <Button size="small" onClick={() => void manejar(
              () => generarNotas(r.idRelease).then((res) => ({ mensaje: res.mensaje })),
              "No se pudieron generar las notas.")}>
              Generar notas
            </Button>
          </Stack>
        </Stack>

        {/* El lider se puede cambiar mientras el release no este liberado ni cancelado: es
            quien responde por la entrega y encabeza la Solicitud de despliegue impresa. */}
        <Box sx={{ mt: 2, maxWidth: 360 }}>
          <ComboBuscable
            label="Lider asignado"
            disabled={enviando || r.idEstatus === 4 || r.idEstatus === 6}
            value={r.idLiderAsignado ?? ""}
            onChange={(v) => void manejar(
              () => asignarLider(r.idRelease, v === "" ? null : Number(v))
                .then((res) => ({ mensaje: res.mensaje })),
              "No se pudo asignar el lider.")}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({
              valor: u.id, etiqueta: u.nombre,
            }))}
          />
        </Box>

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

      <Stack direction={{ xs: "column", lg: "row" }} spacing={2} sx={{ alignItems: "flex-start" }}>
        <Stack spacing={2} sx={{ flex: 2, minWidth: 0, width: "100%" }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
              Instrucciones de implementacion
            </Typography>
            {editable && !r.instruccionesImplementacion && (
              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
                Sin instrucciones capturadas. Son obligatorias para solicitar la aprobacion.
              </Typography>
            )}
            {editable ? (
              <>
                <EditorEnriquecido
                  soportaTablas
                  minHeight={200}
                  value={instrucciones}
                  onChange={setInstrucciones}
                  placeholder="Que tiene que hacer quien despliega: respaldos, orden de ejecucion, rutas destino, validaciones. Se puede pegar una tabla de Excel o una imagen."
                  onError={(mensaje) => setAviso({ tipo: "error", mensaje })}
                />
                <Stack direction="row" spacing={1} sx={{ mt: 1, justifyContent: "flex-end" }}>
                  <Button size="small" variant="contained" disabled={enviando}
                    onClick={() => void manejar(
                      () => actualizarInstrucciones(r.idRelease, instrucciones),
                      "No se pudieron guardar las instrucciones.")}>
                    Guardar instrucciones
                  </Button>
                </Stack>
              </>
            ) : r.instruccionesImplementacion ? (
              <ContenidoEnriquecido html={r.instruccionesImplementacion} />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Este release se mando a aprobacion sin instrucciones de implementacion.
              </Typography>
            )}
          </Paper>

          <Paper variant="outlined" sx={{ p: 2 }}>
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
                <Typography variant="body2" noWrap sx={{ flex: 1 }}>{item.titulo}</Typography>
                {editable && (
                  <Tooltip title="Quitar del release">
                    <IconButton size="small" disabled={enviando}
                      onClick={() => void manejar(
                        () => quitarContenido(r.idRelease, item.idWorkItem),
                        "No se pudo quitar el elemento del release.")}>
                      <DeleteOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                )}
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
                      <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap" }}>
                        <Typography variant="body2">{a.nombre}</Typography>
                        {a.versionArtefacto && (
                          <Chip size="small" variant="outlined" label={`v${a.versionArtefacto}`}
                            sx={{ height: 18 }} />
                        )}
                      </Stack>
                      <Typography variant="caption" color="text.secondary">
                        {a.tipo}
                        {a.ordenEjecucion !== null && ` - orden ${a.ordenEjecucion}`}
                        {a.nombreRollback && ` - reversa: ${a.nombreRollback}`}
                        {a.justificacionIrreversible && ` - ${a.justificacionIrreversible}`}
                      </Typography>
                      {a.instruccionesImplementacion && (
                        <Box sx={{ mt: 0.5, pl: 1, borderLeft: "2px solid", borderColor: "divider" }}>
                          <ContenidoEnriquecido html={a.instruccionesImplementacion} />
                        </Box>
                      )}
                    </TableCell>
                    {editable && (
                      <TableCell align="right" sx={{ width: 80, whiteSpace: "nowrap" }}>
                        <Tooltip title="Editar artefacto">
                          <IconButton size="small" disabled={enviando}
                            onClick={() => abrirArtefactoEdicion(a)}>
                            <EditOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Quitar artefacto">
                          <IconButton size="small" disabled={enviando}
                            onClick={() => void manejar(
                              () => quitarArtefacto(r.idRelease, a.idArtefacto),
                              "No se pudo quitar el artefacto.")}>
                            <DeleteOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            <Divider sx={{ my: 2 }} />

            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
              Respaldos previos ({r.respaldos.length})
            </Typography>
            {r.respaldos.length === 0 && (
              <Typography variant="caption" color="text.secondary">
                Sin respaldos capturados. Se necesita al menos uno para solicitar la aprobacion.
              </Typography>
            )}
            <Table size="small">
              <TableBody>
                {r.respaldos.map((rp) => (
                  <TableRow key={rp.idReleaseRespaldo}>
                    <TableCell>
                      <Typography variant="body2">{rp.descripcion}</Typography>
                      <Typography variant="caption" color="text.secondary">{rp.tipo}</Typography>
                    </TableCell>
                    {editable && (
                      <TableCell align="right" sx={{ width: 80, whiteSpace: "nowrap" }}>
                        <Tooltip title="Editar respaldo">
                          <IconButton size="small" disabled={enviando}
                            onClick={() => abrirRespaldoEdicion(rp)}>
                            <EditOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Quitar respaldo">
                          <IconButton size="small" disabled={enviando}
                            onClick={() => void manejar(
                              () => quitarRespaldo(r.idRelease, rp.idReleaseRespaldo),
                              "No se pudo quitar el respaldo.")}>
                            <DeleteOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        </Stack>

        <Stack spacing={2} sx={{ flex: 1, minWidth: 0, width: "100%" }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
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
                  color={ap.idEstatus === 2 ? "success" : ap.idEstatus === 3 ? "error"
                    : ap.idEstatus === 4 ? "warning" : "default"} />
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
          </Paper>

          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
              Version viva por ambiente
            </Typography>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                  <TableCell>Ambiente</TableCell>
                  <TableCell>Version</TableCell>
                  <TableCell>Desplegado</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {/* Solo los ambientes de ESTE proyecto: en el detalle de un release, la
                    matriz global de todos los proyectos no dice nada util. */}
                {(matriz.data ?? [])
                  .filter((f) => f.claveProyecto === null || f.claveProyecto === r.claveProyecto)
                  .map((fila) => (
                    <TableRow key={fila.idAmbiente}>
                      <TableCell><Chip size="small" label={fila.ambiente} /></TableCell>
                      <TableCell sx={{ fontWeight: 600 }}>{fila.versionDesplegada ?? "-"}</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>
                        {formatearFecha(fila.fechaDespliegue)}
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2 }}>
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
      </Stack>

      <Dialog open={modalContenido} onClose={() => setModalContenido(false)} fullWidth maxWidth="sm">
        <DialogTitle>Agregar contenido al release</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <Typography variant="body2" color="text.secondary">
            Solo aparecen los elementos terminados del proyecto que todavia no estan en ningun
            release, ordenados por folio.
          </Typography>

          {/* Los filtros se arman con los propios candidatos: acotan la lista Y el boton de
              seleccionar todo, que es lo que permite meter un sprint completo de un clic. */}
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <TextField size="small" label="Folio o titulo" value={textoCandidatos} sx={{ flex: 1 }}
              onChange={(e) => setTextoCandidatos(e.target.value)} />
            {sprintsConCandidatos.length > 0 && (
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel>Sprint</InputLabel>
                <Select label="Sprint" value={idSprintFiltro}
                  onChange={(e) => {
                    const valor = e.target.value as number | "";
                    setIdSprintFiltro(valor === "" ? "" : Number(valor));
                    setSeleccionados([]);
                  }}>
                  <MenuItem value="">Todos los sprints</MenuItem>
                  {sprintsConCandidatos.map((s) => (
                    <MenuItem key={s.id ?? 0} value={s.id ?? 0}>{s.nombre}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
            {tiposConCandidatos.length > 1 && (
              <FormControl size="small" sx={{ minWidth: 140 }}>
                <InputLabel>Tipo</InputLabel>
                <Select label="Tipo" value={tipoCandidatos}
                  onChange={(e) => { setTipoCandidatos(e.target.value); setSeleccionados([]); }}>
                  <MenuItem value="">Todos los tipos</MenuItem>
                  {tiposConCandidatos.map((t) => (
                    <MenuItem key={t} value={t}>{t}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Stack>

          {hayBloqueados && (
            <FormControlLabel
              control={<Switch size="small" checked={ocultarBloqueados}
                onChange={(e) => { setOcultarBloqueados(e.target.checked); setSeleccionados([]); }} />}
              label="Ocultar los que tienen hallazgos pendientes"
              slotProps={{ typography: { variant: "body2" } }}
            />
          )}

          <ComboBuscableMultiple
            label="Elementos"
            resumenSimple
            value={seleccionados}
            onChange={(valores) => setSeleccionados(valores as number[])}
            opciones={candidatosFiltrados.map((item) => ({
              valor: item.idWorkItem,
              etiqueta: item.hallazgosPendientes > 0
                ? `${item.folio} - ${item.titulo} (hallazgos pendientes)`
                : `${item.folio} - ${item.titulo}`,
            }))}
          />

          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
            <Typography variant="caption" color="text.secondary" sx={{ flex: 1 }}>
              {candidatosFiltrados.length} de {(candidatos.data ?? []).length} elemento(s)
              {seleccionados.length > 0 && ` - ${seleccionados.length} seleccionado(s)`}
            </Typography>
            {candidatosFiltrados.length > 1 && (
              <Button size="small"
                onClick={() => setSeleccionados(candidatosFiltrados.map((i) => i.idWorkItem))}>
                Seleccionar los {candidatosFiltrados.length}
              </Button>
            )}
            {seleccionados.length > 0 && (
              <Button size="small" color="inherit" onClick={() => setSeleccionados([])}>
                Limpiar
              </Button>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalContenido(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || seleccionados.length === 0}
            onClick={() => { setModalContenido(false); void manejar(
              () => agregarContenido(r.idRelease, seleccionados).then((res) => {
                setSeleccionados([]); return res;
              }), "No se pudo agregar el contenido."); }}>
            Agregar
          </Button>
        </DialogActions>
      </Dialog>

      {/* Mismo formulario para alta y edicion: los campos son los mismos y tener dos
          modales distintos garantizaba que uno se quedara sin un campo nuevo. */}
      <Dialog open={modalArtefacto} onClose={() => setModalArtefacto(false)} fullWidth maxWidth="md">
        <DialogTitle>
          {formArtefacto.idArtefacto === null ? "Agregar artefacto" : "Editar artefacto"}
        </DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <TextField size="small" required label="Nombre del archivo" sx={{ flex: 2 }}
              value={formArtefacto.nombre}
              onChange={(e) => setFormArtefacto((f) => ({ ...f, nombre: e.target.value }))} />
            <TextField size="small" label="Version que se libera" sx={{ flex: 1 }}
              placeholder="2.4.8.0"
              helperText="4 digitos en aplicaciones e instaladores, 3 en procedimientos"
              value={formArtefacto.versionArtefacto}
              onChange={(e) => setFormArtefacto((f) => ({ ...f, versionArtefacto: e.target.value }))} />
          </Stack>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <FormControl size="small" sx={{ flex: 1 }}>
              <InputLabel>Tipo</InputLabel>
              <Select label="Tipo" value={formArtefacto.idTipoArtefacto}
                onChange={(e) => setFormArtefacto((f) => ({
                  ...f, idTipoArtefacto: Number(e.target.value),
                }))}>
                {(catalogosEntregas.data?.tiposArtefacto ?? []).map((t) => (
                  <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField size="small" label="Orden de ejecucion" type="number" sx={{ flex: 1 }}
              value={formArtefacto.ordenEjecucion}
              onChange={(e) => setFormArtefacto((f) => ({ ...f, ordenEjecucion: e.target.value }))} />
          </Stack>
          {/* La reversa se elige entre los otros artefactos del release; el propio queda
              fuera de la lista porque no puede ser su propia reversa. */}
          <ComboBuscable
            label="Artefacto de reversa (opcional)"
            value={formArtefacto.idArtefactoRollback}
            onChange={(v) => setFormArtefacto((f) => ({
              ...f, idArtefactoRollback: v === "" ? "" : Number(v),
            }))}
            opciones={r.artefactos
              .filter((a) => a.idArtefacto !== formArtefacto.idArtefacto)
              .map((a) => ({ valor: a.idArtefacto, etiqueta: a.nombre }))}
          />
          {formArtefacto.idTipoArtefacto === catalogosEntregas.data?.idTipoArtefactoScriptSql && (
            <TextField size="small" multiline minRows={2}
              label="Justificacion si no hay script de reversa"
              value={formArtefacto.justificacion}
              onChange={(e) => setFormArtefacto((f) => ({ ...f, justificacion: e.target.value }))}
              helperText="Un script SQL necesita reversa o esta justificacion para poder aprobarse" />
          )}
          <EditorEnriquecido
            soportaTablas
            minHeight={160}
            label="Instrucciones de implementacion (opcional)"
            value={formArtefacto.instrucciones}
            onChange={(html) => setFormArtefacto((f) => ({ ...f, instrucciones: html }))}
            placeholder="Como se aplica este objeto en particular: base destino, ruta, accion, validaciones. Admite tablas e imagenes."
            onError={(mensaje) => setAviso({ tipo: "error", mensaje })}
          />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalArtefacto(false)}>Cancelar</Button>
          <Button variant="contained"
            disabled={enviando || !formArtefacto.nombre.trim() || formArtefacto.idTipoArtefacto === ""}
            onClick={guardarArtefacto}>
            {formArtefacto.idArtefacto === null ? "Agregar" : "Guardar"}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Respaldos previos: que se respalda antes de tocar produccion. El gate de
          SOLICITAR_APROBACION exige al menos uno, porque es un apartado propio de la
          Solicitud de despliegue que se manda a firmar. */}
      <Dialog open={modalRespaldo} onClose={() => setModalRespaldo(false)} fullWidth maxWidth="sm">
        <DialogTitle>
          {formRespaldo.idReleaseRespaldo === null ? "Agregar respaldo" : "Editar respaldo"}
        </DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          <FormControl size="small" fullWidth>
            <InputLabel>Tipo</InputLabel>
            <Select label="Tipo" value={formRespaldo.idTipoRespaldo}
              onChange={(e) => setFormRespaldo((f) => ({
                ...f, idTipoRespaldo: Number(e.target.value),
              }))}>
              {(catalogosEntregas.data?.tiposRespaldo ?? []).map((t) => (
                <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" required multiline minRows={2}
            label="Nombre o ubicacion exacta"
            helperText="Ejemplo: bdsGTE, Servicio GTE.WebApi, https://gte.interflo, C:\inetpub\gte"
            value={formRespaldo.descripcion}
            onChange={(e) => setFormRespaldo((f) => ({ ...f, descripcion: e.target.value }))} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalRespaldo(false)}>Cancelar</Button>
          <Button variant="contained"
            disabled={enviando || !formRespaldo.descripcion.trim() || formRespaldo.idTipoRespaldo === ""}
            onClick={guardarRespaldo}>
            {formRespaldo.idReleaseRespaldo === null ? "Agregar" : "Guardar"}
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
            onChange={(v) => setIdAmbiente(v === "" ? "" : Number(v))}
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
          <Button color="error" onClick={() => setModalDespliegue(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idAmbiente === ""}
            onClick={() => { setModalDespliegue(false); void manejar(
              () => registrarDespliegue(r.idRelease, {
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
          <Button color="error" onClick={() => setModalRechazo(null)}>Cancelar</Button>
          <Button variant="contained" color="error"
            disabled={enviando || !comentario.trim()}
            onClick={() => { const idAp = modalRechazo!; setModalRechazo(null); void manejar(
              () => resolverAprobacion(idAp, false, comentario.trim()).then((res) => {
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
            las firmas ya puestas: al volver a solicitar aprobacion, toda la cadena de firmantes
            debe firmar de nuevo.
          </Typography>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo de la reapertura (obligatorio)"
            value={motivoReabrir} onChange={(e) => setMotivoReabrir(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalReabrir(false)}>Cancelar</Button>
          <Button variant="contained" color="warning"
            disabled={enviando || !motivoReabrir.trim()}
            onClick={() => { setModalReabrir(false); void manejar(
              () => cambiarEstatusRelease(r.idRelease, "REABRIR", motivoReabrir.trim()).then((res) => {
                setMotivoReabrir(""); return res;
              }), "No se pudo reabrir el release."); }}>
            Reabrir
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalAutorizar} onClose={() => setModalAutorizar(false)} fullWidth maxWidth="sm">
        <DialogTitle>Autorizar release sin firmas</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            El release pasa a Aprobado de inmediato. Las firmas de la cadena que sigan
            pendientes quedan marcadas como omitidas por esta autorizacion, a tu nombre y con
            tu firma electronica; las que ya se pusieron se conservan tal cual. El motivo
            queda en la bitacora y en la Solicitud de despliegue.
          </Typography>
          <TextField autoFocus fullWidth multiline minRows={2} margin="dense"
            label="Motivo de la autorizacion (obligatorio)"
            value={motivoAutorizar} onChange={(e) => setMotivoAutorizar(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalAutorizar(false)}>Cancelar</Button>
          <Button variant="contained" color="warning"
            disabled={enviando || !motivoAutorizar.trim()}
            onClick={() => { setModalAutorizar(false); void manejar(
              () => cambiarEstatusRelease(r.idRelease, "AUTORIZAR", motivoAutorizar.trim()).then((res) => {
                setMotivoAutorizar(""); return res;
              }), "No se pudo autorizar el release."); }}>
            Autorizar
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
