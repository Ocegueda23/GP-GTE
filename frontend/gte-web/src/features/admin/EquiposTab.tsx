import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControl,
  IconButton, InputLabel, LinearProgress, MenuItem, Paper, Select, Snackbar, Stack, Table,
  TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  agregarMiembroEquipo, crearEquipo, obtenerCatalogosAdministracion, obtenerEquipo, obtenerEquipos,
  retirarMiembroEquipo,
} from "../../shared/api/administracion";

/** P20 - Equipos con miembros, lider y porcentaje de dedicacion. */
export function EquiposTab() {
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [modalEquipo, setModalEquipo] = useState(false);
  const [modalMiembro, setModalMiembro] = useState(false);
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [idLider, setIdLider] = useState<number | "">("");
  const [idUsuarioNuevo, setIdUsuarioNuevo] = useState<number | "">("");
  const [porcentaje, setPorcentaje] = useState(100);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const equipos = useQuery({ queryKey: ["equipos-admin"], queryFn: obtenerEquipos });
  const equipoActual = idEquipo === "" ? equipos.data?.[0]?.idEquipo : (idEquipo as number);
  const detalle = useQuery({
    queryKey: ["equipo-detalle", equipoActual],
    queryFn: () => obtenerEquipo(equipoActual!),
    enabled: equipoActual !== undefined,
  });

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });
  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["equipos-admin"] }),
    clienteQuery.invalidateQueries({ queryKey: ["equipo-detalle"] }),
  ]);

  const guardarEquipo = async () => {
    try {
      const { mensaje, dato } = await crearEquipo({
        nombre: nombre.trim(), descripcion: descripcion.trim() || null,
        idLider: idLider === "" ? null : (idLider as number),
      });
      avisar(mensaje);
      setModalEquipo(false);
      setNombre(""); setDescripcion(""); setIdLider("");
      setIdEquipo(dato.idEquipo);
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el equipo.", true);
    }
  };

  const agregarMiembro = async () => {
    try {
      const { mensaje } = await agregarMiembroEquipo(equipoActual!, {
        idUsuario: idUsuarioNuevo as number, rolEquipo: null, porcentajeDedicacion: porcentaje,
      });
      avisar(mensaje);
      setModalMiembro(false);
      setIdUsuarioNuevo(""); setPorcentaje(100);
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo agregar el miembro.", true);
    }
  };

  const quitarMiembro = async (idEquipoMiembro: number) => {
    try {
      await retirarMiembroEquipo(equipoActual!, idEquipoMiembro);
      avisar("Miembro retirado del equipo.");
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el miembro.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Equipos</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalEquipo(true)}>
          Nuevo equipo
        </Button>
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <Paper variant="outlined" sx={{ p: 2, minWidth: 260 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Equipos</Typography>
          {equipos.isLoading && <LinearProgress />}
          {equipos.data?.map((eq) => (
            <Box key={eq.idEquipo} onClick={() => setIdEquipo(eq.idEquipo)}
              sx={{
                p: 1, borderRadius: 1, cursor: "pointer",
                bgcolor: eq.idEquipo === equipoActual ? "action.selected" : undefined,
                "&:hover": { bgcolor: "action.hover" },
              }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>{eq.nombre}</Typography>
              <Typography variant="caption" color="text.secondary">
                {eq.lider ? `Lider: ${eq.lider}` : "Sin lider"} - {eq.totalMiembros} miembro(s)
              </Typography>
            </Box>
          ))}
          {equipos.data?.length === 0 && (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>No hay equipos aun.</Typography>
          )}
        </Paper>

        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}>
          {detalle.data ? (
            <>
              <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{detalle.data.nombre}</Typography>
                <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => setModalMiembro(true)}>
                  Agregar miembro
                </Button>
              </Stack>
              {detalle.data.descripcion && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  {detalle.data.descripcion}
                </Typography>
              )}
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Usuario</TableCell>
                    <TableCell>Rol</TableCell>
                    <TableCell>% Dedicacion</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {detalle.data.miembros.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4}>
                        <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                          Este equipo no tiene miembros.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                  {detalle.data.miembros.map((m) => (
                    <TableRow key={m.idEquipoMiembro}>
                      <TableCell>{m.usuario}</TableCell>
                      <TableCell>{m.rolEquipo ?? "-"}</TableCell>
                      <TableCell>{m.porcentajeDedicacion}%</TableCell>
                      <TableCell>
                        <IconButton size="small" aria-label="Retirar miembro"
                          onClick={() => void quitarMiembro(m.idEquipoMiembro)}>
                          <DeleteOutlineIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </>
          ) : (
            <Typography variant="body2" color="text.secondary">
              Selecciona un equipo para ver sus miembros.
            </Typography>
          )}
        </Paper>
      </Stack>

      <Dialog open={modalEquipo} onClose={() => setModalEquipo(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo equipo</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <TextField size="small" label="Descripcion" multiline minRows={2} value={descripcion}
            onChange={(e) => setDescripcion(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Lider</InputLabel>
            <Select label="Lider" value={idLider} onChange={(e) => setIdLider(e.target.value as number)}>
              <MenuItem value="">Sin lider</MenuItem>
              {catalogos.data?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalEquipo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombre.trim().length === 0} onClick={() => void guardarEquipo()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalMiembro} onClose={() => setModalMiembro(false)} fullWidth maxWidth="xs">
        <DialogTitle>Agregar miembro</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <FormControl size="small" required>
            <InputLabel>Usuario</InputLabel>
            <Select label="Usuario" value={idUsuarioNuevo} onChange={(e) => setIdUsuarioNuevo(e.target.value as number)}>
              {catalogos.data?.usuarios.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField size="small" type="number" label="% Dedicacion" value={porcentaje}
            onChange={(e) => setPorcentaje(Number(e.target.value))}
            slotProps={{ htmlInput: { min: 1, max: 100 } }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalMiembro(false)}>Cancelar</Button>
          <Button variant="contained" disabled={idUsuarioNuevo === ""} onClick={() => void agregarMiembro()}>
            Agregar
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
