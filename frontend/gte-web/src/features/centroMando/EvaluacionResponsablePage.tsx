import { Fragment, useMemo } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Grid, LinearProgress, Paper, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import PersonSearchIcon from "@mui/icons-material/PersonSearch";
import SettingsSuggestIcon from "@mui/icons-material/SettingsSuggest";
import { useQuery } from "@tanstack/react-query";
import { AvatarUsuario } from "../../shared/components/AvatarUsuario";
import {
  obtenerEvaluacionResponsable, type CausaDiagnostico, type IndicadorEvaluado,
} from "../../shared/api/centroMando";
import { NOMBRES_MES, colorSemaforo, formatearValor, tonoSemaforo } from "./comunes";
import { EtiquetaConTooltip } from "./EtiquetaConTooltip";

/** Semaforo de la situacion de carga (el backend manda la etiqueta, no el color). */
function colorSituacionCarga(situacion: string | null): "success" | "warning" | "error" | "default" {
  if (situacion === "Adecuado") return "success";
  if (situacion === "Subutilizado" || situacion === "SobrecargaModerada") return "warning";
  if (situacion === "SobrecargaCritica") return "error";
  return "default";
}

const TEXTO_SITUACION: Record<string, string> = {
  Subutilizado: "Subutilizado",
  Adecuado: "Adecuado",
  SobrecargaModerada: "Sobrecarga moderada",
  SobrecargaCritica: "Sobrecarga critica",
};

/**
 * "Persona" se pinta distinto del resto a proposito: el modelo separa lo que depende de la
 * persona de lo que depende del proceso, los recursos, una dependencia externa o la
 * prioridad -- que es justamente lo que evita leer un score bajo como una falla individual.
 */
function esCausaDePersona(causa: string) {
  return causa === "Persona";
}

function TarjetaCausa({ causa }: { causa: CausaDiagnostico }) {
  const dePersona = esCausaDePersona(causa.causa);
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 1.5, height: "100%",
        borderLeft: 4,
        borderLeftColor: dePersona ? "secondary.main" : "warning.main",
        bgcolor: "action.hover",
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 0.5 }}>
        {dePersona
          ? <PersonSearchIcon fontSize="small" color="secondary" />
          : <SettingsSuggestIcon fontSize="small" color="warning" />}
        <Chip
          size="small"
          color={dePersona ? "secondary" : "warning"}
          variant={dePersona ? "filled" : "outlined"}
          label={causa.causa}
        />
        <Typography variant="caption" color="text.secondary">{causa.indiceClave}</Typography>
      </Stack>
      <Typography variant="body2">{causa.evidencia}</Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 0.5 }}>
        Valor: {formatearValor(causa.valor)} - Umbral: {formatearValor(causa.umbral)}
      </Typography>
    </Paper>
  );
}

function CeldaTendencia({ tendencia }: { tendencia: string | null }) {
  if (!tendencia) return <Typography variant="caption" color="text.disabled">Sin comparativo</Typography>;
  const color = tendencia === "Mejora" ? "success.main" : tendencia === "Empeora" ? "error.main" : "text.secondary";
  return (
    <Stack direction="row" spacing={0.25} sx={{ alignItems: "center", color }}>
      {tendencia === "Mejora" && <ArrowUpwardIcon sx={{ fontSize: 14 }} />}
      {tendencia === "Empeora" && <ArrowDownwardIcon sx={{ fontSize: 14 }} />}
      <Typography variant="caption" sx={{ color }}>{tendencia}</Typography>
    </Stack>
  );
}

function FilaIndicador({ indicador }: { indicador: IndicadorEvaluado }) {
  const explicacion = [indicador.accionSugerida, indicador.interpretacionMala]
    .filter(Boolean).join(" | ") || null;

  return (
    <TableRow hover sx={{ opacity: indicador.sinDatos ? 0.55 : 1 }}>
      <TableCell>
        <EtiquetaConTooltip texto={indicador.nombre} explicacion={explicacion} />
        <Typography variant="caption" color="text.disabled" sx={{ display: "block" }}>{indicador.clave}</Typography>
      </TableCell>
      <TableCell align="right">
        {indicador.sinDatos ? (
          <Typography variant="caption" color="text.disabled" sx={{ fontStyle: "italic" }}>Sin datos</Typography>
        ) : (
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {formatearValor(indicador.valor, indicador.unidad)}
          </Typography>
        )}
      </TableCell>
      <TableCell align="right">{formatearValor(indicador.meta, indicador.unidad)}</TableCell>
      <TableCell>
        {indicador.sinDatos || !indicador.semaforo
          ? <Chip size="small" variant="outlined" label="Sin datos" />
          : <Chip size="small" color={colorSemaforo(indicador.semaforo)} label={indicador.semaforo} />}
      </TableCell>
      <TableCell align="right">{indicador.peso}</TableCell>
      <TableCell><CeldaTendencia tendencia={indicador.sinDatos ? null : indicador.tendencia} /></TableCell>
    </TableRow>
  );
}

export function EvaluacionResponsablePage() {
  const { idEquipo } = useParams();
  const navegar = useNavigate();
  const [parametros, setParametros] = useSearchParams();
  const hoy = new Date();

  const anio = Number(parametros.get("anio")) || hoy.getFullYear();
  const mes = Number(parametros.get("mes")) || hoy.getMonth() + 1;
  const equipo = Number(idEquipo);

  const cambiarPeriodo = (nuevoAnio: number, nuevoMes: number) => {
    setParametros({ anio: String(nuevoAnio), mes: String(nuevoMes) });
  };

  const evaluacion = useQuery({
    queryKey: ["centro-mando-equipo", equipo, anio, mes],
    queryFn: () => obtenerEvaluacionResponsable(equipo, anio, mes),
    enabled: Number.isFinite(equipo),
  });

  const datos = evaluacion.data;

  // Primero "Comun" (lo que se le mide a todos) y despues el ambito tecnico del equipo.
  const gruposPorAmbito = useMemo(() => {
    if (!datos) return [] as { ambito: string; categorias: { categoria: string; indicadores: IndicadorEvaluado[] }[] }[];
    const ambitos = [...new Set(datos.indicadores.map((i) => i.ambito))]
      .sort((a, b) => (a === "Comun" ? -1 : b === "Comun" ? 1 : a.localeCompare(b)));
    return ambitos.map((ambito) => {
      const delAmbito = datos.indicadores.filter((i) => i.ambito === ambito);
      const categorias = [...new Set(delAmbito.map((i) => i.categoria))].sort((a, b) => a.localeCompare(b));
      return {
        ambito,
        categorias: categorias.map((categoria) => ({
          categoria,
          indicadores: delAmbito.filter((i) => i.categoria === categoria),
        })),
      };
    });
  }, [datos]);

  const diferencia = datos?.scoreGeneral !== null && datos?.scoreGeneral !== undefined
    && datos.scoreMesAnterior !== null && datos.scoreMesAnterior !== undefined
    ? datos.scoreGeneral - datos.scoreMesAnterior
    : null;

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 2 }}>
        <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navegar("/centro-mando")}>
          Centro de Mando
        </Button>
        <Box sx={{ flex: 1 }} />
        <TextField size="small" type="number" label="Anio" value={anio} sx={{ width: 100 }}
          onChange={(e) => cambiarPeriodo(Number(e.target.value), mes)} />
        <TextField size="small" select label="Mes" value={mes} sx={{ width: 150 }}
          onChange={(e) => cambiarPeriodo(anio, Number(e.target.value))} slotProps={{ select: { native: true } }}>
          {NOMBRES_MES.map((nombre, i) => <option key={nombre} value={i + 1}>{nombre}</option>)}
        </TextField>
      </Stack>

      {evaluacion.isLoading && <LinearProgress sx={{ mb: 2 }} />}
      {evaluacion.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo cargar la evaluacion del equipo.</Alert>}

      {datos && (
        <>
          {/* Encabezado */}
          <Paper variant="outlined" sx={{ p: 2, mb: 2, borderLeft: 6, borderLeftColor: tonoSemaforo(datos.semaforo) }}>
            <Grid container spacing={2} sx={{ alignItems: "center" }}>
              <Grid size={{ xs: 12, md: 7 }}>
                <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
                  <AvatarUsuario
                    urlFoto={datos.urlFoto}
                    nombre={datos.responsable ?? datos.equipo}
                    sx={{ width: 64, height: 64, fontSize: 22 }}
                  />
                  <Box sx={{ minWidth: 0 }}>
                    <Typography variant="h5" sx={{ fontWeight: 700 }}>{datos.equipo}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {datos.responsable ?? "Sin responsable asignado"}
                    </Typography>
                    <Stack direction="row" spacing={1} sx={{ mt: 0.5 }}>
                      <Chip size="small" variant="outlined" label={datos.ambito ?? "Sin ambito"} />
                      <Chip size="small" variant="outlined" label={`${NOMBRES_MES[datos.mes - 1]} ${datos.anio}`} />
                    </Stack>
                  </Box>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, md: 5 }}>
                <Stack direction="row" spacing={2} sx={{ alignItems: "baseline", flexWrap: "wrap", rowGap: 1 }}>
                  <Typography sx={{ fontSize: 52, fontWeight: 800, lineHeight: 1.1, color: tonoSemaforo(datos.semaforo) }}>
                    {formatearValor(datos.scoreGeneral)}
                  </Typography>
                  {datos.semaforo && <Chip color={colorSemaforo(datos.semaforo)} label={datos.semaforo} />}
                  {datos.nivel && <Typography variant="h6">{datos.nivel}</Typography>}
                </Stack>
                {diferencia !== null ? (
                  <Stack direction="row" spacing={0.5}
                    sx={{ alignItems: "center", color: diferencia >= 0 ? "success.main" : "error.main" }}>
                    {diferencia >= 0 ? <ArrowUpwardIcon sx={{ fontSize: 16 }} /> : <ArrowDownwardIcon sx={{ fontSize: 16 }} />}
                    <Typography variant="body2">
                      {diferencia >= 0 ? "+" : ""}{diferencia.toFixed(1)} contra el mes anterior
                      ({formatearValor(datos.scoreMesAnterior)})
                    </Typography>
                  </Stack>
                ) : (
                  <Typography variant="body2" color="text.disabled">Sin score del mes anterior para comparar.</Typography>
                )}
                <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 0.5 }}>
                  {datos.indicadoresConDato} de {datos.indicadoresTotales} indicadores con dato
                </Typography>
              </Grid>
            </Grid>
          </Paper>

          {/* Conclusion y diagnostico: lo mas importante de la pantalla */}
          <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 0.5 }}>Conclusion</Typography>
            <Typography variant="h6" sx={{ fontWeight: 500, mb: 2 }}>
              {datos.conclusion ?? "Sin conclusion calculada para el periodo."}
            </Typography>

            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
              Diagnostico: por que el score quedo asi
            </Typography>
            {datos.diagnostico.length === 0 ? (
              <Typography color="text.secondary">
                El modelo no detecto causas atribuibles en el periodo.
              </Typography>
            ) : (
              <Grid container spacing={1.5}>
                {datos.diagnostico.map((causa, i) => (
                  <Grid key={`${causa.causa}-${causa.indiceClave}-${i}`} size={{ xs: 12, md: 6 }}>
                    <TarjetaCausa causa={causa} />
                  </Grid>
                ))}
              </Grid>
            )}
          </Paper>

          {/* Carga de trabajo */}
          <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Carga de trabajo</Typography>
            {!datos.carga ? (
              <Typography color="text.secondary">Sin datos de carga para el periodo.</Typography>
            ) : (
              <Grid container spacing={2}>
                <Grid size={{ xs: 6, md: 2.4 }}>
                  <Typography variant="caption" color="text.secondary">Horas disponibles</Typography>
                  <Typography variant="h6">{datos.carga.horasDisponibles.toFixed(1)}</Typography>
                </Grid>
                <Grid size={{ xs: 6, md: 2.4 }}>
                  <Typography variant="caption" color="text.secondary">Horas asignadas</Typography>
                  <Typography variant="h6">{datos.carga.horasAsignadas.toFixed(1)}</Typography>
                </Grid>
                <Grid size={{ xs: 6, md: 2.4 }}>
                  <Typography variant="caption" color="text.secondary">Horas ejecutadas</Typography>
                  <Typography variant="h6">{datos.carga.horasEjecutadas.toFixed(1)}</Typography>
                </Grid>
                <Grid size={{ xs: 6, md: 2.4 }}>
                  <Typography variant="caption" color="text.secondary">Indice de carga</Typography>
                  <Typography variant="h6">{formatearValor(datos.carga.indiceCarga)}</Typography>
                </Grid>
                <Grid size={{ xs: 12, md: 2.4 }}>
                  <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>Situacion</Typography>
                  {datos.carga.situacion ? (
                    <Chip
                      color={colorSituacionCarga(datos.carga.situacion)}
                      label={TEXTO_SITUACION[datos.carga.situacion] ?? datos.carga.situacion}
                    />
                  ) : (
                    <Chip variant="outlined" label="Sin datos" />
                  )}
                  <Typography variant="caption" color="text.disabled" sx={{ display: "block", mt: 0.5 }}>
                    {datos.carga.integrantes} integrantes
                  </Typography>
                </Grid>
              </Grid>
            )}
          </Paper>

          {/* Indicadores */}
          {gruposPorAmbito.map((grupo) => (
            <Paper key={grupo.ambito} variant="outlined" sx={{ mb: 2 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, p: 1.5, pb: 1 }}>
                Indicadores - {grupo.ambito}
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Indicador</TableCell>
                      <TableCell align="right">Valor</TableCell>
                      <TableCell align="right">Meta</TableCell>
                      <TableCell>Semaforo</TableCell>
                      <TableCell align="right">Peso</TableCell>
                      <TableCell>Tendencia</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {grupo.categorias.map((categoria) => (
                      <Fragment key={categoria.categoria}>
                        <TableRow>
                          <TableCell colSpan={6} sx={{ bgcolor: "action.hover", py: 0.5 }}>
                            <Typography variant="caption" sx={{ fontWeight: 700 }}>{categoria.categoria}</Typography>
                          </TableCell>
                        </TableRow>
                        {categoria.indicadores.map((indicador) => (
                          <FilaIndicador key={indicador.idIndicadorGestion} indicador={indicador} />
                        ))}
                      </Fragment>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          ))}
          {gruposPorAmbito.length === 0 && (
            <Typography color="text.secondary">Sin indicadores evaluados para el periodo.</Typography>
          )}
        </>
      )}
    </Box>
  );
}
