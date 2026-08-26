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
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  actualizarArea, crearArea, obtenerAreas, retirarArea, type Area,
} from "../../shared/api/administracion";

/** Catalogo de Areas (usado por Puestos y por Usuarios). Alta/edicion/baja logica. */
export function AreasTab() {
  const [modal, setModal] = useState(false);
  const [nombre, setNombre] = useState("");
  const [areaEditar, setAreaEditar] = useState<Area | null>(null);
  const [nombreEditar, setNombreEditar] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const areas = useQuery({ queryKey: ["areas-admin"], queryFn: obtenerAreas });
  const areasFiltradas = (areas.data ?? []).filter((a) =>
    a.nombre.toLowerCase().includes(busqueda.trim().toLowerCase()));
  const { datosOrdenados: areasOrdenadas, ordenarPor, descendente, ordenar } = useOrdenTabla(areasFiltradas);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const guardar = async () => {
    try {
      const { mensaje } = await crearArea({ nombre: nombre.trim() });
      avisar(mensaje);
      setModal(false);
      setNombre("");
      await clienteQuery.invalidateQueries({ queryKey: ["areas-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el area.", true);
    }
  };

  const guardarEdicion = async () => {
    try {
      const { mensaje } = await actualizarArea(areaEditar!.idArea, { nombre: nombreEditar.trim() });
      avisar(mensaje);
      setAreaEditar(null);
      await clienteQuery.invalidateQueries({ queryKey: ["areas-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo actualizar el area.", true);
    }
  };

  const retirar = async (idArea: number) => {
    try {
      await retirarArea(idArea);
      avisar("Area retirada.");
      await clienteQuery.invalidateQueries({ queryKey: ["areas-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el area.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Areas</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nueva area
        </Button>
      </Stack>

      <TextField size="small" placeholder="Buscar area..." value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />

      <Paper variant="outlined">
        {areas.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {areasOrdenadas.length === 0 && (
              <TableRow>
                <TableCell colSpan={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay areas registradas.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {areasOrdenadas.map((a) => (
              <TableRow key={a.idArea} hover sx={{ cursor: "pointer" }}
                onClick={() => { setAreaEditar(a); setNombreEditar(a.nombre); }}>
                <TableCell>{a.nombre}</TableCell>
                <TableCell align="right">
                  <IconButton size="small" aria-label="Retirar area"
                    onClick={(e) => { e.stopPropagation(); void retirar(a.idArea); }}>
                    <DeleteOutlineIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nueva area</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <TextField size="small" fullWidth required label="Nombre" value={nombre}
            onChange={(e) => setNombre(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombre.trim().length === 0} onClick={() => void guardar()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={areaEditar !== null} onClose={() => setAreaEditar(null)} fullWidth maxWidth="xs">
        <DialogTitle>Editar area</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <TextField size="small" fullWidth required label="Nombre" value={nombreEditar}
            onChange={(e) => setNombreEditar(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setAreaEditar(null)}>Cancelar</Button>
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
