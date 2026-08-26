import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  IconButton, LinearProgress, Paper, Snackbar, Stack, Table,
  TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
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
  const [busqueda, setBusqueda] = useState("");
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const equipos = useQuery({ queryKey: ["equipos-admin"], queryFn: obtenerEquipos });
  const equiposFiltrados = (equipos.data ?? []).filter((eq) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return eq.nombre.toLowerCase().includes(texto) || (eq.lider ?? "").toLowerCase().includes(texto);
  });
  const equipoActual = idEquipo === "" ? equipos.data?.[0]?.idEquipo : (idEquipo as number);
  const detalle = useQuery({
    queryKey: ["equipo-detalle", equipoActual],
    queryFn: () => obtenerEquipo(equipoActual!),
    enabled: equipoActual !== undefined,
  });
  const { datosOrdenados: miembrosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(detalle.data?.miembros);

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
          <TextField size="small" placeholder="Buscar equipo o lider..." value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)} sx={{ mb: 1, width: "100%" }} />
          {equipos.isLoading && <LinearProgress />}
          {equiposFiltrados.map((eq) => (
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
          {equiposFiltrados.length === 0 && (
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
                    <EncabezadoOrdenable clave="usuario" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Usuario</EncabezadoOrdenable>
                    <EncabezadoOrdenable clave="rolEquipo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Rol</EncabezadoOrdenable>
                    <EncabezadoOrdenable clave="porcentajeDedicacion" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>% Dedicacion</EncabezadoOrdenable>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {miembrosOrdenados.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4}>
                        <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                          Este equipo no tiene miembros.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                  {miembrosOrdenados.map((m) => (
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
          <ComboBuscable
            label="Lider"
            value={idLider}
            onChange={(v) => setIdLider(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin lider" },
              ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalEquipo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombre.trim().length === 0} onClick={() => void guardarEquipo()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalMiembro} onClose={() => setModalMiembro(false)} fullWidth maxWidth="xs">
        <DialogTitle>Agregar miembro</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Usuario"
            required
            value={idUsuarioNuevo}
            onChange={(v) => setIdUsuarioNuevo(v as number | "")}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))}
          />
          <TextField size="small" type="number" label="% Dedicacion" value={porcentaje}
            onChange={(e) => setPorcentaje(Number(e.target.value))}
            slotProps={{ htmlInput: { min: 1, max: 100 } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalMiembro(false)}>Cancelar</Button>
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
