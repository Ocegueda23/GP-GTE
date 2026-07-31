import { useEffect, useState } from "react";
import {
  Alert, Box, Button, Checkbox, FormControl, FormControlLabel, InputLabel, LinearProgress,
  MenuItem, Paper, Select, Snackbar, Stack, Typography,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  guardarMatrizPermisos, obtenerMatrizPermisos, obtenerRoles, type PermisoMatrizItem,
} from "../../shared/api/administracion";

/** P20 - Matriz rol-permiso: checkboxes agrupados por modulo, guardado en lote. */
export function RolesTab() {
  const [idRol, setIdRol] = useState<number | "">("");
  const [seleccion, setSeleccion] = useState<Set<number>>(new Set());
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const roles = useQuery({ queryKey: ["roles-admin"], queryFn: obtenerRoles });
  const rolActual = idRol === "" ? roles.data?.[0]?.idRol : (idRol as number);
  const matriz = useQuery({
    queryKey: ["matriz-permisos", rolActual],
    queryFn: () => obtenerMatrizPermisos(rolActual!),
    enabled: rolActual !== undefined,
  });

  useEffect(() => {
    if (matriz.data) {
      setSeleccion(new Set(matriz.data.permisos.filter((p) => p.asignado).map((p) => p.idPermiso)));
    }
  }, [matriz.data]);

  const alternar = (idPermiso: number) => {
    setSeleccion((actual) => {
      const nuevo = new Set(actual);
      if (nuevo.has(idPermiso)) nuevo.delete(idPermiso); else nuevo.add(idPermiso);
      return nuevo;
    });
  };

  const guardar = async () => {
    try {
      await guardarMatrizPermisos(rolActual!, [...seleccion]);
      setAviso({ tipo: "success", mensaje: "Matriz de permisos guardada." });
      await clienteQuery.invalidateQueries({ queryKey: ["matriz-permisos"] });
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo guardar la matriz.",
      });
    }
  };

  const porModulo = new Map<string, PermisoMatrizItem[]>();
  matriz.data?.permisos.forEach((p) => {
    const lista = porModulo.get(p.modulo) ?? [];
    lista.push(p);
    porModulo.set(p.modulo, lista);
  });

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Roles y permisos</Typography>
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel>Rol</InputLabel>
          <Select label="Rol" value={rolActual ?? ""} onChange={(e) => setIdRol(e.target.value as number)}>
            {roles.data?.map((r) => <MenuItem key={r.idRol} value={r.idRol}>{r.nombre}</MenuItem>)}
          </Select>
        </FormControl>
      </Stack>

      {matriz.isLoading && <LinearProgress />}

      {[...porModulo.entries()].map(([modulo, permisos]) => (
        <Paper key={modulo} variant="outlined" sx={{ p: 2, mb: 2 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>{modulo}</Typography>
          <Stack direction="row" sx={{ flexWrap: "wrap" }}>
            {permisos.map((p) => (
              <FormControlLabel key={p.idPermiso} sx={{ width: { xs: "100%", sm: "50%", md: "33%" } }}
                control={<Checkbox checked={seleccion.has(p.idPermiso)} onChange={() => alternar(p.idPermiso)} />}
                label={p.clave} />
            ))}
          </Stack>
        </Paper>
      ))}

      <Button variant="contained" disabled={rolActual === undefined} onClick={() => void guardar()}>
        Guardar matriz de permisos
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
