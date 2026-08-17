import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  IconButton, LinearProgress, Paper, Snackbar, Stack, Table, TableBody,
  TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  actualizarPuesto, crearPuesto, obtenerAreas, obtenerPuestos, retirarPuesto, type Puesto,
} from "../../shared/api/administracion";

/** Catalogo de Puestos (por Area). Alta/edicion/baja logica. */
export function PuestosTab() {
  const [modal, setModal] = useState(false);
  const [nombre, setNombre] = useState("");
  const [idArea, setIdArea] = useState<number | "">("");
  const [puestoEditar, setPuestoEditar] = useState<Puesto | null>(null);
  const [nombreEditar, setNombreEditar] = useState("");
  const [idAreaEditar, setIdAreaEditar] = useState<number | "">("");
  const [busqueda, setBusqueda] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const areas = useQuery({ queryKey: ["areas-admin"], queryFn: obtenerAreas });
  const puestos = useQuery({ queryKey: ["puestos-admin"], queryFn: obtenerPuestos });
  const puestosFiltrados = (puestos.data ?? []).filter((p) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return p.nombre.toLowerCase().includes(texto) || (p.area ?? "").toLowerCase().includes(texto);
  });
  const { datosOrdenados: puestosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(puestosFiltrados);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const guardar = async () => {
    try {
      const { mensaje } = await crearPuesto({
        nombre: nombre.trim(), idArea: idArea === "" ? null : (idArea as number),
      });
      avisar(mensaje);
      setModal(false);
      setNombre(""); setIdArea("");
      await clienteQuery.invalidateQueries({ queryKey: ["puestos-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el puesto.", true);
    }
  };

  const abrirEditar = (p: Puesto) => {
    setPuestoEditar(p);
    setNombreEditar(p.nombre);
    setIdAreaEditar(p.idArea ?? "");
  };

  const guardarEdicion = async () => {
    try {
      const { mensaje } = await actualizarPuesto(puestoEditar!.idPuesto, {
        nombre: nombreEditar.trim(), idArea: idAreaEditar === "" ? null : (idAreaEditar as number),
      });
      avisar(mensaje);
      setPuestoEditar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["puestos-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo actualizar el puesto.", true);
    }
  };

  const retirar = async (idPuesto: number) => {
    try {
      await retirarPuesto(idPuesto);
      avisar("Puesto retirado.");
      await clienteQuery.invalidateQueries({ queryKey: ["puestos-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el puesto.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Puestos</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo puesto
        </Button>
      </Stack>

      <TextField size="small" placeholder="Buscar puesto o area..." value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />

      <Paper variant="outlined">
        {puestos.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="area" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Area</EncabezadoOrdenable>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {puestosOrdenados.length === 0 && (
              <TableRow>
                <TableCell colSpan={3}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay puestos registrados.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {puestosOrdenados.map((p) => (
              <TableRow key={p.idPuesto} hover sx={{ cursor: "pointer" }} onClick={() => abrirEditar(p)}>
                <TableCell>{p.nombre}</TableCell>
                <TableCell>{p.area ?? "-"}</TableCell>
                <TableCell align="right">
                  <IconButton size="small" aria-label="Retirar puesto"
                    onClick={(e) => { e.stopPropagation(); void retirar(p.idPuesto); }}>
                    <DeleteOutlineIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nuevo puesto</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" fullWidth required label="Nombre" value={nombre}
            onChange={(e) => setNombre(e.target.value)} />
          <ComboBuscable
            label="Area"
            value={idArea}
            onChange={(v) => setIdArea(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin area" },
              ...(areas.data ?? []).map((a) => ({ valor: a.idArea, etiqueta: a.nombre })),
            ]}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombre.trim().length === 0} onClick={() => void guardar()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={puestoEditar !== null} onClose={() => setPuestoEditar(null)} fullWidth maxWidth="xs">
        <DialogTitle>Editar puesto</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" fullWidth required label="Nombre" value={nombreEditar}
            onChange={(e) => setNombreEditar(e.target.value)} />
          <ComboBuscable
            label="Area"
            value={idAreaEditar}
            onChange={(v) => setIdAreaEditar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin area" },
              ...(areas.data ?? []).map((a) => ({ valor: a.idArea, etiqueta: a.nombre })),
            ]}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPuestoEditar(null)}>Cancelar</Button>
          <Button variant="contained" disabled={nombreEditar.trim().length === 0} onClick={() => void guardarEdicion()}>
            Guardar
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
