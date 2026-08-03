import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControl,
  IconButton, InputLabel, LinearProgress, MenuItem, Paper, Select, Snackbar, Stack,
  TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditIcon from "@mui/icons-material/Edit";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerEquipos, obtenerProyectos } from "../../shared/api/administracion";
import {
  actualizarResultadoClave, crearObjetivo, crearResultadoClave, obtenerObjetivos,
  retirarObjetivo, retirarResultadoClave, type ObjetivoOkr, type ResultadoClave,
} from "../../shared/api/okr";

const ANIO_ACTUAL = new Date().getFullYear();

/** OKRs: objetivos trimestrales por proyecto o equipo, con resultados clave medibles. */
export function OkrTab() {
  const [anio, setAnio] = useState<number | "">(ANIO_ACTUAL);
  const [modalObjetivo, setModalObjetivo] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });
  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["objetivos-okr"] });

  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });
  const equipos = useQuery({ queryKey: ["equipos"], queryFn: obtenerEquipos });
  const objetivos = useQuery({
    queryKey: ["objetivos-okr", anio],
    queryFn: () => obtenerObjetivos({ anio: anio === "" ? undefined : anio }),
  });

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <TextField size="small" type="number" label="Anio" value={anio}
          onChange={(e) => setAnio(e.target.value ? Number(e.target.value) : "")} sx={{ width: 120 }} />
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalObjetivo(true)}>
          Nuevo objetivo
        </Button>
      </Stack>

      {objetivos.isLoading && <LinearProgress />}
      {objetivos.data?.length === 0 && (
        <Typography color="text.secondary">No hay objetivos registrados para este anio.</Typography>
      )}
      <Stack spacing={2}>
        {objetivos.data?.map((o) => (
          <TarjetaObjetivo key={o.idObjetivoOkr} objetivo={o}
            alExito={(m) => { avisar(m); void refrescar(); }}
            alError={(m) => avisar(m, true)} />
        ))}
      </Stack>

      <ModalNuevoObjetivo
        abierto={modalObjetivo}
        proyectos={proyectos.data ?? []}
        equipos={equipos.data ?? []}
        anioSugerido={anio === "" ? ANIO_ACTUAL : anio}
        onCerrar={() => setModalObjetivo(false)}
        alExito={(m) => { avisar(m); void refrescar(); setModalObjetivo(false); }}
        alError={(m) => avisar(m, true)}
      />

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}

function TarjetaObjetivo({ objetivo, alExito, alError }: {
  objetivo: ObjetivoOkr; alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [modalResultado, setModalResultado] = useState(false);

  const retirar = async () => {
    try {
      await retirarObjetivo(objetivo.idObjetivoOkr);
      alExito("Objetivo retirado.");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo retirar el objetivo.");
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start" }}>
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{objetivo.nombre}</Typography>
          <Typography variant="caption" color="text.secondary">
            {objetivo.proyecto ?? objetivo.equipo ?? "-"} - {objetivo.anio} Q{objetivo.trimestre}
          </Typography>
          {objetivo.descripcion && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{objetivo.descripcion}</Typography>
          )}
        </Box>
        <Stack direction="row" spacing={1}>
          <Button size="small" startIcon={<AddIcon />} onClick={() => setModalResultado(true)}>
            Resultado clave
          </Button>
          <Button size="small" color="error" onClick={() => void retirar()}>Retirar</Button>
        </Stack>
      </Stack>

      <Stack spacing={1.5} sx={{ mt: 2 }}>
        {objetivo.resultadosClave.length === 0 && (
          <Typography variant="body2" color="text.secondary">Sin resultados clave todavia.</Typography>
        )}
        {objetivo.resultadosClave.map((rc) => (
          <FilaResultadoClave key={rc.idResultadoClave} idObjetivoOkr={objetivo.idObjetivoOkr} resultado={rc}
            alExito={alExito} alError={alError} />
        ))}
      </Stack>

      <ModalNuevoResultado
        abierto={modalResultado}
        idObjetivoOkr={objetivo.idObjetivoOkr}
        onCerrar={() => setModalResultado(false)}
        alExito={(m) => { alExito(m); setModalResultado(false); }}
        alError={alError}
      />
    </Paper>
  );
}

function FilaResultadoClave({ idObjetivoOkr, resultado, alExito, alError }: {
  idObjetivoOkr: number; resultado: ResultadoClave; alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [modal, setModal] = useState(false);
  const [nombre, setNombre] = useState(resultado.nombre);
  const [valorMeta, setValorMeta] = useState(String(resultado.valorMeta));
  const [valorActual, setValorActual] = useState(String(resultado.valorActual));
  const [claveKpi, setClaveKpi] = useState(resultado.claveKpi ?? "");

  const avance = resultado.valorMeta > 0 ? Math.min(100, (resultado.valorActual / resultado.valorMeta) * 100) : 0;

  const abrir = () => {
    setNombre(resultado.nombre); setValorMeta(String(resultado.valorMeta));
    setValorActual(String(resultado.valorActual)); setClaveKpi(resultado.claveKpi ?? ""); setModal(true);
  };

  const guardar = async () => {
    try {
      await actualizarResultadoClave(idObjetivoOkr, resultado.idResultadoClave, {
        nombre: nombre.trim(), valorMeta: Number(valorMeta), valorActual: Number(valorActual),
        claveKpi: claveKpi.trim() || null,
      });
      alExito("Resultado clave actualizado.");
      setModal(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo actualizar el resultado clave.");
    }
  };

  const retirar = async () => {
    try {
      await retirarResultadoClave(idObjetivoOkr, resultado.idResultadoClave);
      alExito("Resultado clave retirado.");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo retirar el resultado clave.");
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center" }}>
        <Typography variant="body2">
          {resultado.nombre} — {resultado.valorActual} / {resultado.valorMeta}
          {resultado.claveKpi && (
            <Tooltip title={`KPI: ${resultado.claveKpi}`}>
              <span style={{ marginLeft: 4, color: "gray" }}>(KPI)</span>
            </Tooltip>
          )}
        </Typography>
        <Stack direction="row" spacing={0.5}>
          <IconButton size="small" onClick={abrir} aria-label="Editar resultado clave">
            <EditIcon fontSize="small" />
          </IconButton>
          <IconButton size="small" onClick={() => void retirar()} aria-label="Retirar resultado clave">
            <DeleteOutlineOutlinedIcon fontSize="small" />
          </IconButton>
        </Stack>
      </Stack>
      <LinearProgress variant="determinate" value={avance}
        color={avance >= 100 ? "success" : avance >= 50 ? "info" : "warning"} sx={{ height: 8, borderRadius: 1 }} />

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="xs">
        <DialogTitle>Editar resultado clave</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <TextField size="small" type="number" required label="Valor meta" value={valorMeta}
            onChange={(e) => setValorMeta(e.target.value)} />
          <TextField size="small" type="number" required label="Valor actual" value={valorActual}
            onChange={(e) => setValorActual(e.target.value)} />
          <TextField size="small" label="Clave KPI (opcional)" value={claveKpi}
            onChange={(e) => setClaveKpi(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={!nombre.trim() || !valorMeta} onClick={() => void guardar()}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function ModalNuevoResultado({ abierto, idObjetivoOkr, onCerrar, alExito, alError }: {
  abierto: boolean; idObjetivoOkr: number; onCerrar: () => void;
  alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [nombre, setNombre] = useState("");
  const [valorMeta, setValorMeta] = useState("");
  const [claveKpi, setClaveKpi] = useState("");

  const guardar = async () => {
    try {
      await crearResultadoClave(idObjetivoOkr, { nombre: nombre.trim(), valorMeta: Number(valorMeta), claveKpi: claveKpi.trim() || null });
      alExito("Resultado clave agregado.");
      setNombre(""); setValorMeta(""); setClaveKpi("");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo agregar el resultado clave.");
    }
  };

  return (
    <Dialog open={abierto} onClose={onCerrar} fullWidth maxWidth="xs">
      <DialogTitle>Nuevo resultado clave</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
        <TextField size="small" type="number" required label="Valor meta" value={valorMeta}
          onChange={(e) => setValorMeta(e.target.value)} />
        <TextField size="small" label="Clave KPI (opcional)" value={claveKpi} onChange={(e) => setClaveKpi(e.target.value)} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={!nombre.trim() || !valorMeta} onClick={() => void guardar()}>
          Agregar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ModalNuevoObjetivo({ abierto, proyectos, equipos, anioSugerido, onCerrar, alExito, alError }: {
  abierto: boolean;
  proyectos: { idProyecto: number; clave: string; nombre: string }[];
  equipos: { idEquipo: number; nombre: string }[];
  anioSugerido: number;
  onCerrar: () => void; alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [tipo, setTipo] = useState<"proyecto" | "equipo">("proyecto");
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [anio, setAnio] = useState(anioSugerido);
  const [trimestre, setTrimestre] = useState(1);

  const valido = nombre.trim().length > 0 && (tipo === "proyecto" ? idProyecto !== "" : idEquipo !== "");

  const guardar = async () => {
    if (!valido) return;
    try {
      const { mensaje } = await crearObjetivo({
        idProyecto: tipo === "proyecto" ? (idProyecto as number) : null,
        idEquipo: tipo === "equipo" ? (idEquipo as number) : null,
        nombre: nombre.trim(), descripcion: descripcion.trim() || null, anio, trimestre,
      });
      alExito(mensaje);
      setNombre(""); setDescripcion(""); setIdProyecto(""); setIdEquipo("");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo crear el objetivo.");
    }
  };

  return (
    <Dialog open={abierto} onClose={onCerrar} fullWidth maxWidth="sm">
      <DialogTitle>Nuevo objetivo</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <FormControl size="small">
          <InputLabel>Aplica a</InputLabel>
          <Select label="Aplica a" value={tipo} onChange={(e) => setTipo(e.target.value as "proyecto" | "equipo")}>
            <MenuItem value="proyecto">Proyecto</MenuItem>
            <MenuItem value="equipo">Equipo</MenuItem>
          </Select>
        </FormControl>
        {tipo === "proyecto" ? (
          <FormControl size="small" required>
            <InputLabel>Proyecto</InputLabel>
            <Select label="Proyecto" value={idProyecto} onChange={(e) => setIdProyecto(e.target.value as number)}>
              {proyectos.map((p) => <MenuItem key={p.idProyecto} value={p.idProyecto}>{p.clave} - {p.nombre}</MenuItem>)}
            </Select>
          </FormControl>
        ) : (
          <FormControl size="small" required>
            <InputLabel>Equipo</InputLabel>
            <Select label="Equipo" value={idEquipo} onChange={(e) => setIdEquipo(e.target.value as number)}>
              {equipos.map((eq) => <MenuItem key={eq.idEquipo} value={eq.idEquipo}>{eq.nombre}</MenuItem>)}
            </Select>
          </FormControl>
        )}
        <TextField size="small" required label="Nombre del objetivo" value={nombre}
          onChange={(e) => setNombre(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
        <TextField size="small" label="Descripcion" multiline minRows={2} value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)} />
        <Stack direction="row" spacing={2}>
          <TextField size="small" type="number" label="Anio" value={anio}
            onChange={(e) => setAnio(Number(e.target.value))} sx={{ flex: 1 }} />
          <FormControl size="small" sx={{ flex: 1 }}>
            <InputLabel>Trimestre</InputLabel>
            <Select label="Trimestre" value={trimestre} onChange={(e) => setTrimestre(Number(e.target.value))}>
              <MenuItem value={1}>Q1</MenuItem>
              <MenuItem value={2}>Q2</MenuItem>
              <MenuItem value={3}>Q3</MenuItem>
              <MenuItem value={4}>Q4</MenuItem>
            </Select>
          </FormControl>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={!valido} onClick={() => void guardar()}>Crear</Button>
      </DialogActions>
    </Dialog>
  );
}
