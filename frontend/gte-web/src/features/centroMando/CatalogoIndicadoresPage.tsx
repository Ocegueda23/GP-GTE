import { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, IconButton, LinearProgress, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSesion } from "../../shared/api/sesion";
import {
  PERMISO_ADMINISTRAR_CENTRO_MANDO, actualizarIndicadorGestion, obtenerCatalogoIndicadores,
  type ActualizarIndicadorGestionRequest, type IndicadorGestion,
} from "../../shared/api/centroMando";
import { formatearValor } from "./comunes";
import { EtiquetaConTooltip } from "./EtiquetaConTooltip";

/** Texto de un numero opcional para el formulario: vacio significa "sin valor" (null). */
function aTexto(valor: number | null): string {
  return valor === null ? "" : String(valor);
}

function aNumeroOpcional(texto: string): number | null {
  const limpio = texto.trim();
  if (limpio === "") return null;
  const numero = Number(limpio);
  return Number.isFinite(numero) ? numero : null;
}

interface FormularioIndicador {
  meta: string;
  umbralAlerta: string;
  peso: string;
  ponderaEnScore: boolean;
  accionSugerida: string;
  activo: boolean;
}

function DialogoEditarIndicador({ indicador, alCerrar }: {
  indicador: IndicadorGestion | null; alCerrar: () => void;
}) {
  const clienteQuery = useQueryClient();
  const [formulario, setFormulario] = useState<FormularioIndicador>({
    meta: "", umbralAlerta: "", peso: "0", ponderaEnScore: true, accionSugerida: "", activo: true,
  });

  useEffect(() => {
    if (!indicador) return;
    setFormulario({
      meta: aTexto(indicador.meta),
      umbralAlerta: aTexto(indicador.umbralAlerta),
      peso: String(indicador.peso),
      ponderaEnScore: indicador.ponderaEnScore,
      accionSugerida: indicador.accionSugerida ?? "",
      activo: indicador.activo,
    });
  }, [indicador]);

  const guardar = useMutation({
    mutationFn: (cambios: ActualizarIndicadorGestionRequest) =>
      actualizarIndicadorGestion(indicador!.idIndicadorGestion, cambios),
    onSuccess: async () => {
      await clienteQuery.invalidateQueries({ queryKey: ["centro-mando-catalogo"] });
      alCerrar();
    },
  });

  const pesoInvalido = !Number.isFinite(Number(formulario.peso)) || formulario.peso.trim() === "";

  return (
    <Dialog open={indicador !== null} onClose={alCerrar} maxWidth="sm" fullWidth>
      <DialogTitle>{indicador?.clave} - {indicador?.nombre}</DialogTitle>
      <DialogContent dividers>
        {indicador && (
          <Stack spacing={2} sx={{ mt: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              {indicador.descripcion ?? "Sin descripcion capturada."}
            </Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", rowGap: 1 }}>
              <Chip size="small" variant="outlined" label={`Ambito: ${indicador.ambito}`} />
              <Chip size="small" variant="outlined" label={`Categoria: ${indicador.categoria}`} />
              <Chip size="small" variant="outlined" label={`Origen: ${indicador.origen}`} />
              <Chip size="small" variant="outlined" label={`Unidad: ${indicador.unidad}`} />
              <Chip size="small" variant="outlined" label={`Direccion: ${indicador.direccion}`} />
              <Chip size="small" variant="outlined" label={`Periodicidad: ${indicador.periodicidad}`} />
            </Stack>
            {indicador.formula && (
              <Typography variant="caption" color="text.secondary">Formula: {indicador.formula}</Typography>
            )}

            <Stack direction="row" spacing={2}>
              <TextField
                size="small" label="Meta" type="number" fullWidth value={formulario.meta}
                helperText="Vacio = sin meta definida"
                onChange={(e) => setFormulario({ ...formulario, meta: e.target.value })}
              />
              <TextField
                size="small" label="Umbral de alerta" type="number" fullWidth value={formulario.umbralAlerta}
                helperText="Vacio = sin umbral"
                onChange={(e) => setFormulario({ ...formulario, umbralAlerta: e.target.value })}
              />
              <TextField
                size="small" label="Peso" type="number" fullWidth value={formulario.peso}
                error={pesoInvalido} helperText={pesoInvalido ? "Requerido" : " "}
                onChange={(e) => setFormulario({ ...formulario, peso: e.target.value })}
              />
            </Stack>

            <TextField
              size="small" label="Accion sugerida" fullWidth multiline minRows={2}
              value={formulario.accionSugerida}
              onChange={(e) => setFormulario({ ...formulario, accionSugerida: e.target.value })}
            />

            <Stack direction="row" spacing={2}>
              <FormControlLabel
                control={(
                  <Checkbox
                    checked={formulario.ponderaEnScore}
                    onChange={(e) => setFormulario({ ...formulario, ponderaEnScore: e.target.checked })}
                  />
                )}
                label="Pondera en el score"
              />
              <FormControlLabel
                control={(
                  <Checkbox
                    checked={formulario.activo}
                    onChange={(e) => setFormulario({ ...formulario, activo: e.target.checked })}
                  />
                )}
                label="Activo"
              />
            </Stack>

            {guardar.isError && <Alert severity="error">No se pudo guardar el indicador.</Alert>}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={alCerrar}>Cancelar</Button>
        <Button
          variant="contained"
          disabled={pesoInvalido || guardar.isPending}
          onClick={() => guardar.mutate({
            meta: aNumeroOpcional(formulario.meta),
            umbralAlerta: aNumeroOpcional(formulario.umbralAlerta),
            peso: Number(formulario.peso),
            ponderaEnScore: formulario.ponderaEnScore,
            accionSugerida: formulario.accionSugerida.trim() === "" ? null : formulario.accionSugerida.trim(),
            activo: formulario.activo,
          })}
        >
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export function CatalogoIndicadoresPage() {
  const puede = useSesion((estado) => estado.puede);
  const [enEdicion, setEnEdicion] = useState<IndicadorGestion | null>(null);

  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_CENTRO_MANDO);

  const catalogo = useQuery({
    queryKey: ["centro-mando-catalogo"],
    queryFn: () => obtenerCatalogoIndicadores(),
    enabled: puedeAdministrar,
  });

  const grupos = useMemo(() => {
    const indicadores = catalogo.data ?? [];
    const ambitos = [...new Set(indicadores.map((i) => i.ambito))]
      .sort((a, b) => (a === "Comun" ? -1 : b === "Comun" ? 1 : a.localeCompare(b)));
    return ambitos.map((ambito) => {
      const delAmbito = indicadores.filter((i) => i.ambito === ambito);
      // Solo cuentan los que ponderan y estan activos: el resto no entra al score, asi que
      // no deberia contarse contra el 100 del ambito.
      const sumaPesos = delAmbito
        .filter((i) => i.ponderaEnScore && i.activo)
        .reduce((total, i) => total + i.peso, 0);
      return { ambito, indicadores: delAmbito, sumaPesos };
    });
  }, [catalogo.data]);

  if (!puedeAdministrar) {
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="warning">No tienes permiso para administrar el catalogo de indicadores de gestion.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 0.5 }}>Indicadores de gestion</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Clave, nombre, formula y ambito los gobierna el script de despliegue para que el modelo
        siga siendo comparable entre periodos. Aqui se calibran meta, umbral, peso, si pondera
        en el score y la accion sugerida.
      </Typography>

      {catalogo.isLoading && <LinearProgress sx={{ mb: 2 }} />}
      {catalogo.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo cargar el catalogo.</Alert>}

      {grupos.map((grupo) => (
        <Paper key={grupo.ambito} variant="outlined" sx={{ mb: 2 }}>
          <Stack direction="row" spacing={1}
            sx={{ alignItems: "center", p: 1.5, pb: 1, flexWrap: "wrap", rowGap: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{grupo.ambito}</Typography>
            <Chip size="small" variant="outlined" label={`${grupo.indicadores.length} indicadores`} />
            {Math.abs(grupo.sumaPesos - 100) > 0.01 ? (
              <Chip
                size="small" color="warning" icon={<WarningAmberIcon sx={{ fontSize: 16 }} />}
                label={`Los pesos que ponderan suman ${grupo.sumaPesos.toFixed(2)}, no 100`}
              />
            ) : (
              <Chip size="small" color="success" variant="outlined" label="Pesos suman 100" />
            )}
          </Stack>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Clave</TableCell>
                  <TableCell>Nombre</TableCell>
                  <TableCell>Categoria</TableCell>
                  <TableCell>Origen</TableCell>
                  <TableCell>Unidad</TableCell>
                  <TableCell>Direccion</TableCell>
                  <TableCell align="right">Meta</TableCell>
                  <TableCell align="right">Umbral</TableCell>
                  <TableCell align="right">Peso</TableCell>
                  <TableCell>Pondera</TableCell>
                  <TableCell>Activo</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {grupo.indicadores.map((indicador) => (
                  <TableRow key={indicador.idIndicadorGestion} hover sx={{ opacity: indicador.activo ? 1 : 0.55 }}>
                    <TableCell>{indicador.clave}</TableCell>
                    <TableCell>
                      <EtiquetaConTooltip texto={indicador.nombre} explicacion={indicador.formula} />
                    </TableCell>
                    <TableCell>{indicador.categoria}</TableCell>
                    <TableCell>{indicador.origen}</TableCell>
                    <TableCell>{indicador.unidad}</TableCell>
                    <TableCell>{indicador.direccion}</TableCell>
                    <TableCell align="right">{formatearValor(indicador.meta)}</TableCell>
                    <TableCell align="right">{formatearValor(indicador.umbralAlerta)}</TableCell>
                    <TableCell align="right">{indicador.peso}</TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined"
                        label={indicador.ponderaEnScore ? "Si" : "No"} />
                    </TableCell>
                    <TableCell>
                      <Chip size="small" variant="outlined" color={indicador.activo ? "success" : "default"}
                        label={indicador.activo ? "Activo" : "Baja"} />
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="Editar indicador">
                        <IconButton size="small" onClick={() => setEnEdicion(indicador)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      ))}

      {!catalogo.isLoading && grupos.length === 0 && (
        <Typography color="text.secondary">El catalogo de indicadores esta vacio.</Typography>
      )}

      <DialogoEditarIndicador indicador={enEdicion} alCerrar={() => setEnEdicion(null)} />
    </Box>
  );
}
