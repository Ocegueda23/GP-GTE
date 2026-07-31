import { useEffect, useState } from "react";
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
  crearFestivo, crearHorario, guardarTramosHorario, obtenerFestivos, obtenerHorario, obtenerHorarios,
  retirarFestivo,
} from "../../shared/api/administracion";

const DIAS = ["Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo"];

interface TramoEditable { diaSemana: number; horaInicio: string; horaFin: string }

/** P20 - Horarios: tramos por dia (turnos partidos) y dias festivos. */
export function HorariosTab() {
  const [idHorario, setIdHorario] = useState<number | "">("");
  const [modalHorario, setModalHorario] = useState(false);
  const [nombreHorario, setNombreHorario] = useState("");
  const [tramos, setTramos] = useState<TramoEditable[]>([]);
  const [modalFestivo, setModalFestivo] = useState(false);
  const [fechaFestivo, setFechaFestivo] = useState("");
  const [descripcionFestivo, setDescripcionFestivo] = useState("");
  const [festivoGlobal, setFestivoGlobal] = useState(true);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const horarios = useQuery({ queryKey: ["horarios-admin"], queryFn: obtenerHorarios });
  const horarioActual = idHorario === "" ? horarios.data?.[0]?.idHorario : (idHorario as number);
  const detalle = useQuery({
    queryKey: ["horario-detalle", horarioActual],
    queryFn: () => obtenerHorario(horarioActual!),
    enabled: horarioActual !== undefined,
  });
  const festivosGlobales = useQuery({ queryKey: ["festivos-globales"], queryFn: () => obtenerFestivos() });

  useEffect(() => {
    if (detalle.data) {
      setTramos(detalle.data.tramos.map((t) => ({
        diaSemana: t.diaSemana, horaInicio: t.horaInicio.slice(0, 5), horaFin: t.horaFin.slice(0, 5),
      })));
    }
  }, [detalle.data]);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const crearNuevoHorario = async () => {
    try {
      const { mensaje, dato } = await crearHorario({ nombre: nombreHorario.trim() });
      avisar(mensaje);
      setModalHorario(false);
      setNombreHorario("");
      setIdHorario(dato.idHorario);
      await clienteQuery.invalidateQueries({ queryKey: ["horarios-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el horario.", true);
    }
  };

  const agregarTramo = () => setTramos((t) => [...t, { diaSemana: 1, horaInicio: "08:00", horaFin: "17:00" }]);
  const quitarTramo = (indice: number) => setTramos((t) => t.filter((_, i) => i !== indice));
  const actualizarTramo = (indice: number, campo: keyof TramoEditable, valor: string | number) => {
    setTramos((t) => t.map((tramo, i) => (i === indice ? { ...tramo, [campo]: valor } : tramo)));
  };

  const guardarTramos = async () => {
    try {
      const { mensaje } = await guardarTramosHorario(horarioActual!, tramos.map((t) => ({
        diaSemana: t.diaSemana, horaInicio: `${t.horaInicio}:00`, horaFin: `${t.horaFin}:00`,
      })));
      avisar(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["horario-detalle"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudieron guardar los tramos.", true);
    }
  };

  const agregarFestivo = async () => {
    try {
      const { mensaje } = await crearFestivo({
        fecha: fechaFestivo, descripcion: descripcionFestivo.trim(),
        idHorario: festivoGlobal ? null : (horarioActual ?? null),
      });
      avisar(mensaje);
      setModalFestivo(false);
      setFechaFestivo(""); setDescripcionFestivo("");
      await Promise.all([
        clienteQuery.invalidateQueries({ queryKey: ["festivos-globales"] }),
        clienteQuery.invalidateQueries({ queryKey: ["horario-detalle"] }),
      ]);
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo agregar el festivo.", true);
    }
  };

  const quitarFestivo = async (idDiaFestivo: number) => {
    try {
      await retirarFestivo(idDiaFestivo);
      avisar("Dia festivo retirado.");
      await Promise.all([
        clienteQuery.invalidateQueries({ queryKey: ["festivos-globales"] }),
        clienteQuery.invalidateQueries({ queryKey: ["horario-detalle"] }),
      ]);
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el festivo.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2, flexWrap: "wrap", gap: 1 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Horarios</Typography>
        <Stack direction="row" spacing={1}>
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Horario</InputLabel>
            <Select label="Horario" value={horarioActual ?? ""} onChange={(e) => setIdHorario(e.target.value as number)}>
              {horarios.data?.map((h) => <MenuItem key={h.idHorario} value={h.idHorario}>{h.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setModalHorario(true)}>
            Nuevo horario
          </Button>
        </Stack>
      </Stack>

      {detalle.isLoading && <LinearProgress />}

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Tramos (soporta turnos partidos)</Typography>
          <Button size="small" startIcon={<AddIcon />} onClick={agregarTramo}>Agregar tramo</Button>
        </Stack>
        {tramos.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>Sin tramos configurados.</Typography>
        )}
        {tramos.map((t, i) => (
          <Stack key={i} direction="row" spacing={1} sx={{ alignItems: "center", mb: 1 }}>
            <FormControl size="small" sx={{ minWidth: 130 }}>
              <Select value={t.diaSemana} onChange={(e) => actualizarTramo(i, "diaSemana", Number(e.target.value))}>
                {DIAS.map((d, indice) => <MenuItem key={d} value={indice + 1}>{d}</MenuItem>)}
              </Select>
            </FormControl>
            <TextField size="small" type="time" value={t.horaInicio}
              onChange={(e) => actualizarTramo(i, "horaInicio", e.target.value)} />
            <Typography variant="body2">a</Typography>
            <TextField size="small" type="time" value={t.horaFin}
              onChange={(e) => actualizarTramo(i, "horaFin", e.target.value)} />
            <IconButton size="small" aria-label="Quitar tramo" onClick={() => quitarTramo(i)}>
              <DeleteOutlineIcon fontSize="small" />
            </IconButton>
          </Stack>
        ))}
        <Button variant="contained" disabled={horarioActual === undefined} onClick={() => void guardarTramos()}>
          Guardar tramos
        </Button>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Dias festivos</Typography>
          <Button size="small" startIcon={<AddIcon />} onClick={() => setModalFestivo(true)}>Agregar festivo</Button>
        </Stack>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Fecha</TableCell>
              <TableCell>Descripcion</TableCell>
              <TableCell>Alcance</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {festivosGlobales.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    Sin dias festivos registrados.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {festivosGlobales.data?.map((f) => (
              <TableRow key={f.idDiaFestivo}>
                <TableCell>{f.fecha}</TableCell>
                <TableCell>{f.descripcion}</TableCell>
                <TableCell>{f.horario ?? "Global"}</TableCell>
                <TableCell>
                  <IconButton size="small" aria-label="Retirar festivo" onClick={() => void quitarFestivo(f.idDiaFestivo)}>
                    <DeleteOutlineIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modalHorario} onClose={() => setModalHorario(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nuevo horario</DialogTitle>
        <DialogContent sx={{ pt: "12px !important" }}>
          <TextField size="small" fullWidth required label="Nombre" value={nombreHorario}
            onChange={(e) => setNombreHorario(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalHorario(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombreHorario.trim().length === 0}
            onClick={() => void crearNuevoHorario()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={modalFestivo} onClose={() => setModalFestivo(false)} fullWidth maxWidth="xs">
        <DialogTitle>Agregar dia festivo</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" type="date" required label="Fecha" value={fechaFestivo}
            onChange={(e) => setFechaFestivo(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" required label="Descripcion" value={descripcionFestivo}
            onChange={(e) => setDescripcionFestivo(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Alcance</InputLabel>
            <Select label="Alcance" value={festivoGlobal ? "global" : "horario"}
              onChange={(e) => setFestivoGlobal(e.target.value === "global")}>
              <MenuItem value="global">Global (todos los horarios)</MenuItem>
              <MenuItem value="horario">Solo el horario seleccionado</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalFestivo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={!fechaFestivo || descripcionFestivo.trim().length === 0}
            onClick={() => void agregarFestivo()}>
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
