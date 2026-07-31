import { useState } from "react";
import {
  Alert, Box, Button, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, FormControlLabel, InputLabel, LinearProgress, MenuItem, Paper, Select,
  Snackbar, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  cambiarEstatusProyecto, crearProyecto, obtenerAccionesProyecto, obtenerCatalogosAdministracion,
  obtenerProyectos, type Proyecto,
} from "../../shared/api/administracion";

function BotonesAccionProyecto({
  proyecto, onCambio,
}: {
  proyecto: Proyecto;
  onCambio: (mensaje: string, error?: boolean) => void;
}) {
  const acciones = useQuery({
    queryKey: ["acciones-proyecto", proyecto.idProyecto],
    queryFn: () => obtenerAccionesProyecto(proyecto.idProyecto),
  });
  const clienteQuery = useQueryClient();

  const ejecutar = async (accion: string) => {
    try {
      const { mensaje } = await cambiarEstatusProyecto(proyecto.idProyecto, accion);
      onCambio(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["proyectos"] });
      await clienteQuery.invalidateQueries({ queryKey: ["acciones-proyecto"] });
    } catch (error) {
      onCambio(error instanceof ErrorApi ? error.message : "No se pudo cambiar el estatus.", true);
    }
  };

  if (!acciones.data || acciones.data.length === 0) return null;

  return (
    <Stack direction="row" spacing={0.5} sx={{ flexWrap: "wrap" }}>
      {acciones.data.map((a) => (
        <Button key={a.accion} size="small" variant={a.esAccionPrincipal ? "contained" : "outlined"}
          onClick={() => void ejecutar(a.accion)}>
          {a.etiqueta}
        </Button>
      ))}
    </Stack>
  );
}

/** P20 - Alta/edicion de proyectos y cambio de estatus por el motor de workflow. */
export function ProyectosTab() {
  const [modal, setModal] = useState(false);
  const [clave, setClave] = useState("");
  const [nombre, setNombre] = useState("");
  const [idCategoria, setIdCategoria] = useState<number | "">("");
  const [idResponsable, setIdResponsable] = useState<number | "">("");
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [fechaInicioPlan, setFechaInicioPlan] = useState("");
  const [fechaFinPlan, setFechaFinPlan] = useState("");
  const [esMantenimiento, setEsMantenimiento] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const limpiar = () => {
    setClave(""); setNombre(""); setIdCategoria(""); setIdResponsable(""); setIdEquipo("");
    setFechaInicioPlan(""); setFechaFinPlan(""); setEsMantenimiento(false);
  };

  const guardar = async () => {
    try {
      const { mensaje } = await crearProyecto({
        clave: clave.trim(),
        nombre: nombre.trim(),
        idPrograma: null,
        idCategoriaProyecto: idCategoria as number,
        idResponsable: idResponsable === "" ? null : (idResponsable as number),
        idEquipo: idEquipo === "" ? null : (idEquipo as number),
        fechaInicioPlan: fechaInicioPlan || null,
        fechaFinPlan: fechaFinPlan || null,
        esMantenimiento,
      });
      avisar(mensaje);
      setModal(false);
      limpiar();
      await clienteQuery.invalidateQueries({ queryKey: ["proyectos"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el proyecto.", true);
    }
  };

  const valido = clave.trim().length > 0 && nombre.trim().length > 0 && idCategoria !== "";

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Proyectos</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo proyecto
        </Button>
      </Stack>

      <Paper variant="outlined">
        {proyectos.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Clave</TableCell>
              <TableCell>Nombre</TableCell>
              <TableCell>Categoria</TableCell>
              <TableCell>Estatus</TableCell>
              <TableCell>Equipo</TableCell>
              <TableCell>Responsable</TableCell>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {proyectos.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay proyectos. Crea el primero.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {proyectos.data?.map((p) => (
              <TableRow key={p.idProyecto}>
                <TableCell>
                  {p.clave}
                  {p.folio && (
                    <Typography variant="caption" sx={{ display: "block" }} color="text.secondary">{p.folio}</Typography>
                  )}
                </TableCell>
                <TableCell>
                  {p.nombre}
                  {p.esMantenimiento && <Chip size="small" label="Mantenimiento" sx={{ ml: 1, height: 18 }} />}
                </TableCell>
                <TableCell>{p.categoriaProyecto}</TableCell>
                <TableCell><Chip size="small" label={p.estatus} /></TableCell>
                <TableCell>{p.equipo ?? "-"}</TableCell>
                <TableCell>{p.responsable ?? "-"}</TableCell>
                <TableCell><BotonesAccionProyecto proyecto={p} onCambio={avisar} /></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo proyecto</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Clave" value={clave} onChange={(e) => setClave(e.target.value)} />
          <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <FormControl size="small" required>
            <InputLabel>Categoria</InputLabel>
            <Select label="Categoria" value={idCategoria} onChange={(e) => setIdCategoria(e.target.value as number)}>
              {catalogos.data?.categoriasProyecto.map((c) => (
                <MenuItem key={c.id} value={c.id}>{c.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Equipo</InputLabel>
            <Select label="Equipo" value={idEquipo} onChange={(e) => setIdEquipo(e.target.value as number)}>
              <MenuItem value="">Sin equipo</MenuItem>
              {catalogos.data?.equipos.map((eq) => (
                <MenuItem key={eq.id} value={eq.id}>{eq.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Responsable</InputLabel>
            <Select label="Responsable" value={idResponsable} onChange={(e) => setIdResponsable(e.target.value as number)}>
              <MenuItem value="">Sin responsable</MenuItem>
              {catalogos.data?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" type="date" label="Inicio plan" value={fechaInicioPlan}
            onChange={(e) => setFechaInicioPlan(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" label="Fin plan" value={fechaFinPlan}
            onChange={(e) => setFechaFinPlan(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <FormControlLabel
            control={<Checkbox checked={esMantenimiento} onChange={(e) => setEsMantenimiento(e.target.checked)} />}
            label="Proyecto de mantenimiento" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={!valido} onClick={() => void guardar()}>Crear</Button>
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
