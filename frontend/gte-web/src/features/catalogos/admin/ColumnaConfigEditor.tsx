import { useState } from "react";
import {
  Alert, Box, Button, Checkbox, Chip, FormControlLabel, Paper, Snackbar, Stack, TextField, Typography,
} from "@mui/material";
import { ErrorApi } from "../../../shared/api/http";
import { actualizarConfigColumnas, type ColumnaConfig } from "../../../shared/api/catalogoGenerico";

interface Props {
  clave: string;
  columnas: ColumnaConfig[];
  onGuardado: () => void;
}

/** Editor de la configuracion de presentacion por columna: el esquema (tipo/PK/identity) es de solo lectura. */
export function ColumnaConfigEditor({ clave, columnas: columnasIniciales, onGuardado }: Props) {
  const [columnas, setColumnas] = useState<ColumnaConfig[]>(
    () => [...columnasIniciales].sort((a, b) => a.ordinalPos - b.ordinalPos),
  );
  const [guardando, setGuardando] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);

  const actualizar = (nombreColumna: string, cambios: Partial<ColumnaConfig>) => {
    setColumnas((actual) => actual.map((c) => (c.nombreColumna === nombreColumna ? { ...c, ...cambios } : c)));
  };

  const guardar = async () => {
    setGuardando(true);
    try {
      const { dato, mensaje } = await actualizarConfigColumnas(clave, columnas);
      setColumnas([...dato.columnas].sort((a, b) => a.ordinalPos - b.ordinalPos));
      setAviso({ tipo: "success", mensaje });
      onGuardado();
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : "No se pudo guardar la configuracion." });
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Box>
      <Stack sx={{ gap: 2 }}>
        {columnas.map((c) => (
          <Paper key={c.nombreColumna} variant="outlined" sx={{ p: 2 }}>
            <Stack direction="row" sx={{ alignItems: "center", gap: 1, mb: 1, flexWrap: "wrap" }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{c.nombreColumna}</Typography>
              <Chip size="small" label={c.tipoSql} />
              {c.esPk && <Chip size="small" color="primary" label="PK" />}
              {c.esIdentity && <Chip size="small" label="Identity" />}
              {!c.esNulable && <Chip size="small" label="NOT NULL" />}
            </Stack>

            <Stack direction="row" sx={{ gap: 2, flexWrap: "wrap", mb: 1 }}>
              <TextField size="small" label="Nombre a mostrar" value={c.displayName}
                onChange={(e) => actualizar(c.nombreColumna, { displayName: e.target.value })} sx={{ minWidth: 220 }} />
              <TextField size="small" label="Orden" type="number" value={c.ordinalPos}
                onChange={(e) => actualizar(c.nombreColumna, { ordinalPos: Number(e.target.value) })} sx={{ width: 100 }} />
            </Stack>

            <Stack direction="row" sx={{ flexWrap: "wrap" }}>
              <FormControlLabel label="Visible"
                control={<Checkbox checked={c.esVisible} onChange={(e) => actualizar(c.nombreColumna, { esVisible: e.target.checked })} />} />
              <FormControlLabel label="Solo lectura"
                control={<Checkbox checked={c.esSoloLectura} onChange={(e) => actualizar(c.nombreColumna, { esSoloLectura: e.target.checked })} />} />
              <FormControlLabel label="Requerido"
                control={<Checkbox checked={c.esRequerido} onChange={(e) => actualizar(c.nombreColumna, { esRequerido: e.target.checked })} />} />
              <FormControlLabel label="Cifrado"
                control={<Checkbox checked={c.esCifrado} onChange={(e) => actualizar(c.nombreColumna, { esCifrado: e.target.checked })} />} />
            </Stack>

            <Stack direction="row" sx={{ flexWrap: "wrap" }}>
              <FormControlLabel label="Auto fecha (alta)"
                control={<Checkbox checked={c.autoFechaAlta} onChange={(e) => actualizar(c.nombreColumna, { autoFechaAlta: e.target.checked })} />} />
              <FormControlLabel label="Auto usuario (alta)"
                control={<Checkbox checked={c.autoUsuarioAlta} onChange={(e) => actualizar(c.nombreColumna, { autoUsuarioAlta: e.target.checked })} />} />
              <FormControlLabel label="Auto fecha (edicion)"
                control={<Checkbox checked={c.autoFechaEdicion} onChange={(e) => actualizar(c.nombreColumna, { autoFechaEdicion: e.target.checked })} />} />
              <FormControlLabel label="Auto usuario (edicion)"
                control={<Checkbox checked={c.autoUsuarioEdicion} onChange={(e) => actualizar(c.nombreColumna, { autoUsuarioEdicion: e.target.checked })} />} />
            </Stack>

            <Typography variant="caption" color="text.secondary">
              Combo de busqueda (FK) -- opcional: tabla, columna clave y columna a mostrar de otra tabla de bdsGTE.
            </Typography>
            <Stack direction="row" sx={{ gap: 2, flexWrap: "wrap", mt: 0.5 }}>
              <TextField size="small" label="Tabla FK" value={c.tablaFk ?? ""}
                onChange={(e) => actualizar(c.nombreColumna, { tablaFk: e.target.value || null })} sx={{ minWidth: 180 }} />
              <TextField size="small" label="Columna clave FK" value={c.columnaClaveFk ?? ""}
                onChange={(e) => actualizar(c.nombreColumna, { columnaClaveFk: e.target.value || null })} sx={{ minWidth: 180 }} />
              <TextField size="small" label="Columna a mostrar FK" value={c.columnaMostrarFk ?? ""}
                onChange={(e) => actualizar(c.nombreColumna, { columnaMostrarFk: e.target.value || null })} sx={{ minWidth: 180 }} />
            </Stack>
          </Paper>
        ))}
      </Stack>

      <Button variant="contained" sx={{ mt: 2 }} disabled={guardando} onClick={() => void guardar()}>
        Guardar configuracion de columnas
      </Button>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
