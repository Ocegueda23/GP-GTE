import { useEffect, useState } from "react";
import {
  Alert, Box, Button, Checkbox, Divider, IconButton, LinearProgress, Paper, Snackbar, Stack,
  Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import SaveIcon from "@mui/icons-material/Save";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { obtenerProyectos, obtenerRoles } from "../../shared/api/administracion";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import {
  guardarCadenaAprobacion, guardarTransicionesWorkflow, obtenerCadenaAprobacion,
  obtenerDefinicionWorkflow, obtenerPermisosWorkflow, obtenerProcesosWorkflow,
  type TransicionConfigGuardar, type TransicionWorkflow,
} from "../../shared/api/workflow";

const CADENA_DEFAULT = ["QA", "Lider", "Negocio"];

/**
 * P21 - Editor de Workflows (permiso ADM.Workflows): lista de procesos -> ver el grafo
 * completo de transiciones -> editar los metadatos de UI de cada una (etiqueta, permiso,
 * motivo, accion principal, orden). No crea ni elimina flechas del grafo -- eso se define
 * por script SQL (InterfloClaude.md seccion 9.3, "no tocar CambiarST").
 */
export function WorkflowsPage() {
  const [proceso, setProceso] = useState<string>("");
  const [filas, setFilas] = useState<TransicionWorkflow[]>([]);
  const [guardando, setGuardando] = useState(false);
  const [idProyectoCadena, setIdProyectoCadena] = useState<number | "">("");
  const [rolesCadena, setRolesCadena] = useState<string[]>([]);
  const [guardandoCadena, setGuardandoCadena] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const procesos = useQuery({ queryKey: ["workflow-procesos"], queryFn: obtenerProcesosWorkflow });
  const permisos = useQuery({
    queryKey: ["workflow-permisos"], queryFn: obtenerPermisosWorkflow, staleTime: 5 * 60_000,
  });
  const definicion = useQuery({
    queryKey: ["workflow-definicion", proceso],
    queryFn: () => obtenerDefinicionWorkflow(proceso),
    enabled: proceso !== "",
  });
  const proyectos = useQuery({
    queryKey: ["proyectos-cadena-aprobacion"], queryFn: () => obtenerProyectos(true), staleTime: 5 * 60_000,
  });
  const catalogosRoles = useQuery({ queryKey: ["roles-cadena-aprobacion"], queryFn: obtenerRoles, staleTime: 5 * 60_000 });
  const cadena = useQuery({
    queryKey: ["cadena-aprobacion", idProyectoCadena],
    queryFn: () => obtenerCadenaAprobacion(idProyectoCadena as number),
    enabled: idProyectoCadena !== "",
  });

  useEffect(() => {
    if (procesos.data && procesos.data.length > 0 && proceso === "") {
      setProceso(procesos.data[0].proceso);
    }
  }, [procesos.data, proceso]);

  useEffect(() => {
    if (definicion.data) {
      setFilas(definicion.data.transiciones);
    }
  }, [definicion.data]);

  useEffect(() => {
    if (cadena.data) {
      setRolesCadena(cadena.data.length > 0 ? cadena.data : CADENA_DEFAULT);
    }
  }, [cadena.data]);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const moverRol = (indice: number, delta: number) => {
    setRolesCadena((prev) => {
      const destino = indice + delta;
      if (destino < 0 || destino >= prev.length) return prev;
      const copia = [...prev];
      [copia[indice], copia[destino]] = [copia[destino], copia[indice]];
      return copia;
    });
  };

  const guardarCadena = async (roles: string[]) => {
    if (idProyectoCadena === "") return;
    setGuardandoCadena(true);
    try {
      const { mensaje } = await guardarCadenaAprobacion(idProyectoCadena, roles);
      avisar(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["cadena-aprobacion", idProyectoCadena] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo guardar la cadena de aprobacion.", true);
    } finally {
      setGuardandoCadena(false);
    }
  };

  const actualizarFila = (idEstatusOrigen: number, accion: string, cambios: Partial<TransicionWorkflow>) => {
    setFilas((previas) => previas.map((f) =>
      (f.idEstatusOrigen === idEstatusOrigen && f.accion === accion) ? { ...f, ...cambios } : f));
  };

  const guardar = async () => {
    setGuardando(true);
    try {
      const datos: TransicionConfigGuardar[] = filas.map((f) => ({
        idEstatusOrigen: f.idEstatusOrigen,
        accion: f.accion,
        etiquetaBoton: f.etiquetaBoton.trim(),
        requierePermiso: f.requierePermiso,
        requiereMotivo: f.requiereMotivo,
        esAccionPrincipal: f.esAccionPrincipal,
        orden: f.orden,
      }));
      const { mensaje } = await guardarTransicionesWorkflow(proceso, datos);
      avisar(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["workflow-definicion", proceso] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo guardar la configuracion.", true);
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Workflows</Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <ComboBuscable
          label="Proceso"
          value={proceso}
          onChange={(v) => setProceso(String(v))}
          opciones={(procesos.data ?? []).map((p) => ({ valor: p.proceso, etiqueta: p.proceso }))}
          sx={{ minWidth: 260 }}
        />
        <Button variant="contained" startIcon={<SaveIcon />} disabled={guardando || filas.length === 0}
          onClick={() => void guardar()}>
          Guardar cambios
        </Button>
      </Stack>

      {(definicion.isLoading || procesos.isLoading) && <LinearProgress sx={{ mb: 2 }} />}
      {definicion.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(definicion.error as Error).message}</Alert>
      )}

      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
              <TableCell>Origen</TableCell>
              <TableCell>Accion</TableCell>
              <TableCell>Destino</TableCell>
              <TableCell>Etiqueta del boton</TableCell>
              <TableCell>Permiso requerido</TableCell>
              <TableCell align="center">Motivo</TableCell>
              <TableCell align="center">Principal</TableCell>
              <TableCell align="center">Orden</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filas.length === 0 && !definicion.isLoading && (
              <TableRow>
                <TableCell colSpan={8}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
                    Este proceso no tiene transiciones en el grafo.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {filas.map((f) => (
              <TableRow key={`${f.idEstatusOrigen}-${f.accion}`} hover>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{f.estatusOrigen}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap", fontFamily: "monospace" }}>{f.accion}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{f.estatusDestino}</TableCell>
                <TableCell sx={{ minWidth: 180 }}>
                  <TextField size="small" fullWidth value={f.etiquetaBoton}
                    onChange={(e) => actualizarFila(f.idEstatusOrigen, f.accion, { etiquetaBoton: e.target.value })} />
                </TableCell>
                <TableCell sx={{ minWidth: 220 }}>
                  <ComboBuscable
                    label=""
                    value={f.requierePermiso ?? ""}
                    onChange={(v) => actualizarFila(f.idEstatusOrigen, f.accion, { requierePermiso: v === "" ? null : String(v) })}
                    opciones={[
                      { valor: "", etiqueta: "Sin permiso (cualquiera)" },
                      ...(permisos.data ?? []).map((p) => ({ valor: p.clave, etiqueta: `${p.clave} - ${p.modulo}` })),
                    ]}
                  />
                </TableCell>
                <TableCell align="center">
                  <Checkbox size="small" checked={f.requiereMotivo}
                    onChange={(e) => actualizarFila(f.idEstatusOrigen, f.accion, { requiereMotivo: e.target.checked })} />
                </TableCell>
                <TableCell align="center">
                  <Checkbox size="small" checked={f.esAccionPrincipal}
                    onChange={(e) => actualizarFila(f.idEstatusOrigen, f.accion, { esAccionPrincipal: e.target.checked })} />
                </TableCell>
                <TableCell sx={{ minWidth: 70 }}>
                  <TextField size="small" type="number" value={f.orden}
                    onChange={(e) => actualizarFila(f.idEstatusOrigen, f.accion, { orden: Number(e.target.value) })} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Divider sx={{ my: 3 }} />

      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Cadena de aprobacion de releases</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Orden de firmas que exige SOLICITAR_APROBACION en un release. Sin configuracion propia,
        el proyecto usa el default fijo: {CADENA_DEFAULT.join(" -> ")}.
      </Typography>

      <ComboBuscable
        label="Proyecto"
        value={idProyectoCadena}
        onChange={(v) => setIdProyectoCadena(v as number | "")}
        opciones={(proyectos.data ?? []).map((p) => ({ valor: p.idProyecto, etiqueta: `${p.clave} - ${p.nombre}` }))}
        sx={{ minWidth: 320, mb: 2 }}
      />

      {idProyectoCadena === "" ? (
        <Alert severity="info">Elige un proyecto para ver o editar su cadena de aprobacion.</Alert>
      ) : (
        <Paper variant="outlined" sx={{ p: 2, maxWidth: 480 }}>
          {cadena.isLoading && <LinearProgress sx={{ mb: 1 }} />}
          <Stack spacing={1}>
            {rolesCadena.map((rol, indice) => (
              <Stack key={indice} direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Typography variant="body2" sx={{ minWidth: 24 }}>{indice + 1}.</Typography>
                <ComboBuscable
                  label=""
                  value={rol}
                  onChange={(v) => setRolesCadena((prev) => prev.map((r, i) => (i === indice ? String(v) : r)))}
                  opciones={(catalogosRoles.data ?? []).map((r) => ({ valor: r.nombre, etiqueta: r.nombre }))}
                  sx={{ flex: 1 }}
                />
                <IconButton size="small" disabled={indice === 0} onClick={() => moverRol(indice, -1)}>
                  <ArrowUpwardIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" disabled={indice === rolesCadena.length - 1}
                  onClick={() => moverRol(indice, 1)}>
                  <ArrowDownwardIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" onClick={() => setRolesCadena((prev) => prev.filter((_, i) => i !== indice))}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button size="small" startIcon={<AddIcon />} onClick={() => setRolesCadena((prev) => [...prev, ""])}>
              Agregar rol
            </Button>
          </Stack>
          <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
            <Button
              variant="contained" startIcon={<SaveIcon />}
              disabled={guardandoCadena || rolesCadena.some((r) => r.trim().length === 0)}
              onClick={() => void guardarCadena(rolesCadena)}
            >
              Guardar cadena
            </Button>
            <Button disabled={guardandoCadena} onClick={() => void guardarCadena([])}>
              Restaurar default
            </Button>
          </Stack>
        </Paper>
      )}

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
