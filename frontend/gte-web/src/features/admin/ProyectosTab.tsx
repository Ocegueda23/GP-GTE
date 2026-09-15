import { useState } from "react";
import {
  Alert, Box, Button, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, IconButton, LinearProgress, Paper,
  Snackbar, Stack, Tab, Table, TableBody, TableCell, TableHead, TableRow, Tabs, TextField,
  Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  actualizarProyecto, cambiarEstatusProyecto, crearProyecto, obtenerAccionesProyecto,
  obtenerCatalogosAdministracion, obtenerProyectos, type Proyecto,
} from "../../shared/api/administracion";
import { useSesion } from "../../shared/api/sesion";
import { AccesosProyectoTab } from "./AccesosProyectoTab";

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
  const [administrado, setAdministrado] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const [proyectoEditar, setProyectoEditar] = useState<Proyecto | null>(null);
  const [pestanaEditar, setPestanaEditar] = useState("datos");
  const [nombreEditar, setNombreEditar] = useState("");
  const [idCategoriaEditar, setIdCategoriaEditar] = useState<number | "">("");
  const [idResponsableEditar, setIdResponsableEditar] = useState<number | "">("");
  const [idEquipoEditar, setIdEquipoEditar] = useState<number | "">("");
  const [fechaInicioPlanEditar, setFechaInicioPlanEditar] = useState("");
  const [fechaFinPlanEditar, setFechaFinPlanEditar] = useState("");
  const [esMantenimientoEditar, setEsMantenimientoEditar] = useState(false);
  const [administradoEditar, setAdministradoEditar] = useState(false);

  const [busqueda, setBusqueda] = useState("");
  const puede = useSesion((estado) => estado.puede);

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });

  const proyectosFiltrados = (proyectos.data ?? []).filter((p) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return p.clave.toLowerCase().includes(texto) || p.nombre.toLowerCase().includes(texto);
  });
  const { datosOrdenados: proyectosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(proyectosFiltrados);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const limpiar = () => {
    setClave(""); setNombre(""); setIdCategoria(""); setIdResponsable(""); setIdEquipo("");
    setFechaInicioPlan(""); setFechaFinPlan(""); setEsMantenimiento(false); setAdministrado(false);
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
        administrado,
      });
      avisar(mensaje);
      setModal(false);
      limpiar();
      await clienteQuery.invalidateQueries({ queryKey: ["proyectos"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el proyecto.", true);
    }
  };

  const abrirEditar = (p: Proyecto) => {
    setProyectoEditar(p);
    setPestanaEditar("datos");
    setNombreEditar(p.nombre);
    setIdCategoriaEditar(p.idCategoriaProyecto);
    setIdResponsableEditar(p.idResponsable ?? "");
    setIdEquipoEditar(p.idEquipo ?? "");
    setFechaInicioPlanEditar(p.fechaInicioPlan?.slice(0, 10) ?? "");
    setFechaFinPlanEditar(p.fechaFinPlan?.slice(0, 10) ?? "");
    setEsMantenimientoEditar(p.esMantenimiento);
    setAdministradoEditar(p.administrado);
  };

  const guardarEdicion = async () => {
    try {
      const { mensaje } = await actualizarProyecto(proyectoEditar!.idProyecto, {
        nombre: nombreEditar.trim(),
        idCategoriaProyecto: idCategoriaEditar as number,
        idResponsable: idResponsableEditar === "" ? null : (idResponsableEditar as number),
        idEquipo: idEquipoEditar === "" ? null : (idEquipoEditar as number),
        fechaInicioPlan: fechaInicioPlanEditar || null,
        fechaFinPlan: fechaFinPlanEditar || null,
        esMantenimiento: esMantenimientoEditar,
        administrado: administradoEditar,
      });
      avisar(mensaje);
      setProyectoEditar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["proyectos"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo actualizar el proyecto.", true);
    }
  };

  const valido = clave.trim().length > 0 && nombre.trim().length > 0 && idCategoria !== "";
  const validoEditar = nombreEditar.trim().length > 0 && idCategoriaEditar !== "";

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Proyectos</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo proyecto
        </Button>
      </Stack>

      <TextField size="small" placeholder="Buscar clave o nombre..." value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />

      <Paper variant="outlined">
        {proyectos.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <EncabezadoOrdenable clave="clave" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Clave</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="categoriaProyecto" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Categoria</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Estatus</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="equipo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Equipo</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="responsable" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Responsable</EncabezadoOrdenable>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {proyectosOrdenados.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay proyectos. Crea el primero.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {proyectosOrdenados.map((p) => (
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
                  {p.administrado && <Chip size="small" color="primary" label="Administrado" sx={{ ml: 1, height: 18 }} />}
                </TableCell>
                <TableCell>{p.categoriaProyecto}</TableCell>
                <TableCell><Chip size="small" label={p.estatus} /></TableCell>
                <TableCell>{p.equipo ?? "-"}</TableCell>
                <TableCell>{p.responsable ?? "-"}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} sx={{ flexWrap: "wrap" }}>
                    <Tooltip title="Editar proyecto">
                      <IconButton size="small" onClick={() => abrirEditar(p)}>
                        <EditOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <BotonesAccionProyecto proyecto={p} onCambio={avisar} />
                  </Stack>
                </TableCell>
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
          <ComboBuscable
            label="Categoria"
            required
            value={idCategoria}
            onChange={(v) => setIdCategoria(v as number | "")}
            opciones={(catalogos.data?.categoriasProyecto ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre }))}
          />
          <ComboBuscable
            label="Equipo"
            value={idEquipo}
            onChange={(v) => setIdEquipo(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin equipo" },
              ...(catalogos.data?.equipos ?? []).map((eq) => ({ valor: eq.id, etiqueta: eq.nombre })),
            ]}
          />
          <ComboBuscable
            label="Responsable"
            value={idResponsable}
            onChange={(v) => setIdResponsable(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin responsable" },
              ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
          <TextField size="small" type="date" label="Inicio plan" value={fechaInicioPlan}
            onChange={(e) => setFechaInicioPlan(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" label="Fin plan" value={fechaFinPlan}
            onChange={(e) => setFechaFinPlan(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <FormControlLabel
            control={<Checkbox checked={esMantenimiento} onChange={(e) => setEsMantenimiento(e.target.checked)} />}
            label="Proyecto de mantenimiento" />
          <FormControlLabel
            control={<Checkbox checked={administrado} onChange={(e) => setAdministrado(e.target.checked)} />}
            label="Proyecto administrado (solo con acceso especial crea o elimina tareas)" />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={!valido} onClick={() => void guardar()}>Crear</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={proyectoEditar !== null} onClose={() => setProyectoEditar(null)} fullWidth
        maxWidth={pestanaEditar === "accesos" ? "md" : "sm"}>
        <DialogTitle sx={{ pb: 0 }}>{proyectoEditar?.clave}</DialogTitle>
        <Tabs value={pestanaEditar} onChange={(_, valor: string) => setPestanaEditar(valor)}
          sx={{ px: 3, borderBottom: 1, borderColor: "divider" }}>
          <Tab value="datos" label="Datos" />
          {puede("ADM.Roles") && <Tab value="accesos" label="Accesos" />}
        </Tabs>

        {pestanaEditar === "datos" && (
          <>
            <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
              <TextField size="small" required label="Nombre" value={nombreEditar}
                onChange={(e) => setNombreEditar(e.target.value)} />
              <ComboBuscable
                label="Categoria"
                required
                value={idCategoriaEditar}
                onChange={(v) => setIdCategoriaEditar(v as number | "")}
                opciones={(catalogos.data?.categoriasProyecto ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre }))}
              />
              <ComboBuscable
                label="Equipo"
                value={idEquipoEditar}
                onChange={(v) => setIdEquipoEditar(v as number | "")}
                opciones={[
                  { valor: "", etiqueta: "Sin equipo" },
                  ...(catalogos.data?.equipos ?? []).map((eq) => ({ valor: eq.id, etiqueta: eq.nombre })),
                ]}
              />
              <ComboBuscable
                label="Responsable"
                value={idResponsableEditar}
                onChange={(v) => setIdResponsableEditar(v as number | "")}
                opciones={[
                  { valor: "", etiqueta: "Sin responsable" },
                  ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
                ]}
              />
              <TextField size="small" type="date" label="Inicio plan" value={fechaInicioPlanEditar}
                onChange={(e) => setFechaInicioPlanEditar(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
              <TextField size="small" type="date" label="Fin plan" value={fechaFinPlanEditar}
                onChange={(e) => setFechaFinPlanEditar(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
              <FormControlLabel
                control={<Checkbox checked={esMantenimientoEditar} onChange={(e) => setEsMantenimientoEditar(e.target.checked)} />}
                label="Proyecto de mantenimiento" />
              <FormControlLabel
                control={<Checkbox checked={administradoEditar} onChange={(e) => setAdministradoEditar(e.target.checked)} />}
                label="Proyecto administrado (solo con acceso especial crea o elimina tareas)" />
            </DialogContent>
            <DialogActions>
              <Button color="error" onClick={() => setProyectoEditar(null)}>Cancelar</Button>
              <Button variant="contained" disabled={!validoEditar} onClick={() => void guardarEdicion()}>Guardar</Button>
            </DialogActions>
          </>
        )}

        {pestanaEditar === "accesos" && proyectoEditar !== null && (
          <>
            <DialogContent sx={{ pt: "16px !important" }}>
              <AccesosProyectoTab
                idProyecto={proyectoEditar.idProyecto}
                alExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
                alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
              />
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setProyectoEditar(null)}>Cerrar</Button>
            </DialogActions>
          </>
        )}
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
