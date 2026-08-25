import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, IconButton, MenuItem, Paper, Snackbar,
  Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { useSesion } from "../../shared/api/sesion";
import {
  agregarImpacto, derogarRegla, ESTADO_REGLA, obtenerAmbitos, obtenerCatalogosReglas,
  obtenerRegla, obtenerVersionesRegla, quitarImpacto, reactivarRegla,
} from "../../shared/api/reglasNegocio";
import { obtenerProyectos } from "../../shared/api/administracion";
import { PERMISO_ADMINISTRAR_REGLAS } from "./permisos";
import { ReglaModal } from "./ReglaModal";

/**
 * Ficha de una regla.
 *
 * El panel de impacto es navegable en los dos sentidos: desde aqui se salta al catalogo de
 * cada proyecto afectado, y desde una regla heredada en un proyecto secundario se llega a
 * esta ficha (su origen) para editarla. Ver a quien le pega un cambio ANTES de tocar el
 * enunciado es la mitad que hace util el modelo.
 */
export function DetalleReglaPage() {
  const { id } = useParams<{ id: string }>();
  const idRegla = Number(id);
  const navegar = useNavigate();
  const clienteQuery = useQueryClient();

  const puede = useSesion((estado) => estado.puede);
  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_REGLAS);

  const [modalEditar, setModalEditar] = useState(false);
  const [idProyectoAfectado, setIdProyectoAfectado] = useState<number | "">("");
  const [descripcionImpacto, setDescripcionImpacto] = useState("");
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const regla = useQuery({
    queryKey: ["reglasNegocio", "regla", idRegla],
    queryFn: () => obtenerRegla(idRegla),
    enabled: Number.isFinite(idRegla),
  });

  const versiones = useQuery({
    queryKey: ["reglasNegocio", "versiones", idRegla],
    queryFn: () => obtenerVersionesRegla(idRegla),
    enabled: Number.isFinite(idRegla),
  });

  const ambitos = useQuery({
    queryKey: ["reglasNegocio", "ambitos", regla.data?.idProyecto],
    queryFn: () => obtenerAmbitos(regla.data!.idProyecto),
    enabled: regla.data !== undefined,
  });

  const catalogos = useQuery({
    queryKey: ["reglasNegocio", "catalogos"],
    queryFn: obtenerCatalogosReglas,
  });

  const proyectos = useQuery({
    queryKey: ["reglasNegocio", "todosProyectos"],
    queryFn: () => obtenerProyectos(true),
    enabled: puedeAdministrar,
  });

  const refrescar = () => {
    void clienteQuery.invalidateQueries({ queryKey: ["reglasNegocio"] });
  };

  const agregar = useMutation({
    mutationFn: () => agregarImpacto(idRegla, {
      idProyectoAfectado: idProyectoAfectado as number,
      descripcionImpacto: descripcionImpacto.trim() || null,
      idAmbitoRegla: null,
    }),
    onSuccess: ({ mensaje }) => {
      setIdProyectoAfectado("");
      setDescripcionImpacto("");
      setError(null);
      setAviso(mensaje);
      refrescar();
    },
    onError: (e: Error) => setError(e.message),
  });

  const quitar = useMutation({
    mutationFn: (idImpacto: number) => quitarImpacto(idImpacto),
    onSuccess: ({ mensaje }) => { setAviso(mensaje); refrescar(); },
    onError: (e: Error) => setError(e.message),
  });

  const derogar = useMutation({
    mutationFn: () => derogarRegla(idRegla),
    onSuccess: ({ mensaje }) => { setAviso(mensaje); refrescar(); },
    onError: (e: Error) => setError(e.message),
  });

  const reactivar = useMutation({
    mutationFn: () => reactivarRegla(idRegla),
    onSuccess: ({ mensaje }) => { setAviso(mensaje); refrescar(); },
    onError: (e: Error) => setError(e.message),
  });

  if (regla.isLoading) return <Box sx={{ p: 3 }}><CircularProgress size={24} /></Box>;
  if (regla.isError) {
    return <Box sx={{ p: 3 }}><Alert severity="error">{(regla.error as Error).message}</Alert></Box>;
  }
  if (!regla.data) return null;

  const r = regla.data;
  const yaAfectados = new Set(r.impactos.map((i) => i.idProyectoAfectado));
  const candidatos = (proyectos.data ?? [])
    .filter((p) => p.idProyecto !== r.idProyecto && !yaAfectados.has(p.idProyecto));

  return (
    <Box sx={{ p: 3 }}>
      <Button size="small" onClick={() => navegar("/reglas-negocio")} sx={{ mb: 2 }}>
        Volver al catalogo
      </Button>

      <Stack direction="row" sx={{ alignItems: "flex-start", justifyContent: "space-between", mb: 1 }}>
        <Box>
          <Typography variant="h5">{r.clave} - {r.nombre}</Typography>
          <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: "wrap", gap: 1 }}>
            <Chip
              size="small"
              color={r.idEstadoReglaNegocio === ESTADO_REGLA.vigente ? "success" : "default"}
              label={r.nombreEstado}
            />
            <Tooltip title="Proyecto dueno: aqui es donde se edita esta regla">
              <Chip size="small" variant="outlined" label={`Dueno: ${r.claveProyecto}`} />
            </Tooltip>
            {r.nombreAmbito && (
              <Chip size="small" variant="outlined" label={`${r.nombreTipoAmbito}: ${r.nombreAmbito}`} />
            )}
            <Chip size="small" variant="outlined" label={`Version ${r.versionActual}`} />
          </Stack>
        </Box>

        {puedeAdministrar && (
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" onClick={() => setModalEditar(true)}>Editar</Button>
            {r.activo ? (
              <Button color="error" onClick={() => derogar.mutate()} disabled={derogar.isPending}>
                Derogar
              </Button>
            ) : (
              <Button onClick={() => reactivar.mutate()} disabled={reactivar.isPending}>
                Reactivar
              </Button>
            )}
          </Stack>
        )}
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>{error}</Alert>}

      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary">Enunciado</Typography>
        <Typography sx={{ whiteSpace: "pre-wrap", mb: 2 }}>{r.enunciado}</Typography>

        <Typography variant="subtitle2" color="text.secondary">Justificacion</Typography>
        <Typography sx={{ whiteSpace: "pre-wrap", mb: 2 }}>
          {r.justificacion ?? "Sin justificacion capturada."}
        </Typography>

        <Divider sx={{ my: 2 }} />

        <Stack direction="row" spacing={4} sx={{ flexWrap: "wrap", gap: 2 }}>
          <Box>
            <Typography variant="caption" color="text.secondary">Permiso que la omite</Typography>
            <Typography variant="body2">{r.permisoBypass ?? "Ninguno"}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">Donde se aplica</Typography>
            <Typography variant="body2">{r.ubicacionCodigo ?? "Sin ubicar"}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">Mensaje al bloquear</Typography>
            <Typography variant="body2">{r.mensajeError ?? "Sin mensaje"}</Typography>
          </Box>
        </Stack>
      </Paper>

      <Typography variant="subtitle1" sx={{ mb: 1 }}>
        Proyectos afectados ({r.impactos.length})
      </Typography>
      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        {r.impactos.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Esta regla solo aplica a su propio proyecto.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ width: 160 }}>Proyecto</TableCell>
                <TableCell>Como le afecta</TableCell>
                <TableCell sx={{ width: 60 }} />
              </TableRow>
            </TableHead>
            <TableBody>
              {r.impactos.map((i) => (
                <TableRow key={i.idReglaNegocioImpacto}>
                  <TableCell>
                    <RouterLink to={`/reglas-negocio?proyecto=${i.idProyectoAfectado}`}>
                      {i.claveProyectoAfectado}
                    </RouterLink>
                    <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
                      {i.nombreProyectoAfectado}
                    </Typography>
                  </TableCell>
                  <TableCell>{i.descripcionImpacto ?? "Sin descripcion"}</TableCell>
                  <TableCell align="right">
                    {puedeAdministrar && (
                      <IconButton
                        size="small"
                        aria-label={`Quitar ${i.claveProyectoAfectado}`}
                        disabled={quitar.isPending}
                        onClick={() => quitar.mutate(i.idReglaNegocioImpacto)}
                      >
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}

        {puedeAdministrar && (
          <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: "wrap", gap: 1 }}>
            <TextField
              select
              label="Agregar proyecto afectado"
              value={idProyectoAfectado}
              onChange={(e) => setIdProyectoAfectado(Number(e.target.value))}
              size="small"
              sx={{ minWidth: 300 }}
            >
              {candidatos.map((p) => (
                <MenuItem key={p.idProyecto} value={p.idProyecto}>
                  {p.clave} - {p.nombre}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Como le afecta"
              value={descripcionImpacto}
              onChange={(e) => setDescripcionImpacto(e.target.value)}
              size="small"
              sx={{ minWidth: 320, flexGrow: 1 }}
            />
            <Button
              variant="contained"
              disabled={idProyectoAfectado === "" || agregar.isPending}
              onClick={() => agregar.mutate()}
            >
              Agregar
            </Button>
          </Stack>
        )}
      </Paper>

      {r.relaciones.length > 0 && (
        <>
          <Typography variant="subtitle1" sx={{ mb: 1 }}>Reglas relacionadas</Typography>
          <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
            <Table size="small">
              <TableBody>
                {r.relaciones.map((rel) => (
                  <TableRow key={rel.idReglaNegocioRelacion}>
                    <TableCell sx={{ width: 180 }}>{rel.nombreTipoRelacion}</TableCell>
                    <TableCell sx={{ width: 140 }}>
                      <RouterLink to={`/reglas-negocio/${rel.idReglaNegocioRelacionada}`}>
                        {rel.claveRelacionada}
                      </RouterLink>
                    </TableCell>
                    <TableCell>{rel.nota ?? rel.nombreRelacionada}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        </>
      )}

      <Typography variant="subtitle1" sx={{ mb: 1 }}>
        Historial del enunciado ({versiones.data?.length ?? 0})
      </Typography>
      <Paper variant="outlined" sx={{ p: 2 }}>
        {(versiones.data ?? []).map((v) => (
          <Box key={v.idReglaNegocioVersion} sx={{ mb: 2 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Chip size="small" label={`v${v.numeroVersion}`} />
              <Typography variant="caption" color="text.secondary">
                {new Date(v.fechaRegistro).toLocaleString()} - {v.usuarioRegistro}
                {v.motivoCambio ? ` - ${v.motivoCambio}` : ""}
              </Typography>
            </Stack>
            <Typography variant="body2" sx={{ whiteSpace: "pre-wrap", mt: 0.5 }}>
              {v.enunciado}
            </Typography>
            <Divider sx={{ mt: 1 }} />
          </Box>
        ))}
      </Paper>

      <ReglaModal
        abierto={modalEditar}
        idProyecto={r.idProyecto}
        regla={r}
        ambitos={ambitos.data ?? []}
        catalogos={catalogos.data}
        onCerrar={() => setModalEditar(false)}
        onGuardado={setAviso}
      />

      <Snackbar
        open={aviso !== null}
        autoHideDuration={4000}
        onClose={() => setAviso(null)}
        message={aviso ?? ""}
      />
    </Box>
  );
}
