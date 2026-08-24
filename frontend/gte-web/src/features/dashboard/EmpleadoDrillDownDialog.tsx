import {
  Box, Chip, Dialog, DialogContent, DialogTitle, Grid, IconButton, LinearProgress, Paper,
  Stack, Tooltip as MuiTooltip, Typography,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import { useQuery } from "@tanstack/react-query";
import {
  Bar, BarChart, CartesianGrid, Line, LineChart, ResponsiveContainer,
  Tooltip as RechartsTooltip, XAxis, YAxis,
} from "recharts";
import { AvatarUsuario } from "../../shared/components/AvatarUsuario";
import { obtenerIndicadoresEmpleado } from "../../shared/api/dashboard";
import { useColorSerie } from "../../shared/graficas/coloresGrafica";

const NOMBRES_MES = [
  "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic",
];

const ETIQUETAS_DIMENSION: Record<string, string> = {
  Calidad: "Calidad",
  Productividad: "Productividad",
  Puntualidad: "Puntualidad",
  TrabajoEnEquipo: "Trabajo en equipo",
  Cumplimiento: "Cumplimiento",
  Comunicacion: "Comunicacion",
};

const EXPLICACION_DIMENSION: Record<string, string> = {
  Puntualidad: "Porcentaje de elementos con fecha compromiso que se entregaron a tiempo.",
  Cumplimiento: "Porcentaje de elementos asignados vigentes que NO estan vencidos al cierre del periodo.",
  Productividad: "Elementos terminados en el periodo comparado contra el promedio del equipo (100 = igual al promedio, tope 130 por sobre-desempeno).",
  Calidad: "100 menos el porcentaje de elementos que tuvieron un incidente/defecto detectado despues de cerrarse.",
  TrabajoEnEquipo: "Comentarios registrados en elementos de otros companeros del equipo, comparado contra el promedio del equipo.",
  Comunicacion: "Total de comentarios registrados (propios y en elementos ajenos), comparado contra el promedio del equipo.",
};

export function EmpleadoDrillDownDialog({ idUsuario, anio, mes, onClose }: {
  idUsuario: number | null; anio: number; mes: number; onClose: () => void;
}) {
  const colorSerie = useColorSerie();
  const indicadores = useQuery({
    queryKey: ["dashboard-empleado", idUsuario, anio, mes],
    queryFn: () => obtenerIndicadoresEmpleado(idUsuario as number, anio, mes),
    enabled: idUsuario !== null,
  });

  const datos = indicadores.data;

  return (
    <Dialog open={idUsuario !== null} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
        {datos && (
          <AvatarUsuario urlFoto={datos.empleado.urlFoto} nombre={datos.empleado.nombre} sx={{ width: 40, height: 40 }} />
        )}
        <Box sx={{ flex: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{datos?.empleado.nombre ?? "Indicadores del colaborador"}</Typography>
          {datos && (
            <Typography variant="caption" color="text.secondary">
              {[datos.empleado.puesto, datos.empleado.area].filter(Boolean).join(" - ")}
            </Typography>
          )}
        </Box>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {indicadores.isLoading && <LinearProgress />}
        {indicadores.isError && (
          <Typography color="error">No se pudieron cargar los indicadores de este colaborador.</Typography>
        )}
        {datos && (
          <Stack spacing={3}>
            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Eficiencia</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Horas estimadas</Typography>
                    <Typography variant="h6">{datos.horasEstimadas.toFixed(1)}h</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Horas reales</Typography>
                    <Typography variant="h6">{datos.horasReales.toFixed(1)}h</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Indice de rendimiento</Typography>
                    <Typography variant="h6">{datos.indiceEficiencia !== null ? `${datos.indiceEficiencia.toFixed(0)}%` : "Sin datos"}</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Terminados (equipo: {datos.promedioTerminadosEquipo.toFixed(1)})</Typography>
                    <Typography variant="h6">{datos.itemsTerminados}</Typography>
                  </Paper>
                </Grid>
              </Grid>
            </Box>

            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Entregas</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 4 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">A tiempo</Typography>
                    <Typography variant="h6" color="success.main">{datos.entregasATiempo}</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Retrasadas</Typography>
                    <Typography variant="h6" color="error.main">{datos.entregasRetrasadas}</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">% cumplimiento</Typography>
                    <Typography variant="h6">
                      {datos.porcentajeCumplimientoEntregas !== null ? `${datos.porcentajeCumplimientoEntregas.toFixed(0)}%` : "Sin datos"}
                    </Typography>
                  </Paper>
                </Grid>
              </Grid>
            </Box>

            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                Evaluacion mensual {datos.puntajeMensual !== null && `- Puntaje: ${datos.puntajeMensual.toFixed(1)}`}
              </Typography>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart
                  data={datos.evaluacion.map((d) => ({ dimension: ETIQUETAS_DIMENSION[d.dimension] ?? d.dimension, valor: d.valor ?? 0 }))}
                  layout="vertical"
                  margin={{ left: 16, right: 16 }}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" domain={[0, 130]} />
                  <YAxis type="category" dataKey="dimension" width={130} tick={{ fontSize: 12 }} />
                  <RechartsTooltip formatter={(v) => Number(v).toFixed(1)} />
                  <Bar dataKey="valor" name="Puntaje" fill={colorSerie("#0f766e")} radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
              <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", rowGap: 1, mt: 1 }}>
                {datos.evaluacion.map((d) => (
                  <MuiTooltip key={d.dimension} title={EXPLICACION_DIMENSION[d.dimension] ?? ""} arrow>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={`${ETIQUETAS_DIMENSION[d.dimension] ?? d.dimension}: ${d.valor !== null ? d.valor.toFixed(1) : "s/d"}`}
                    />
                  </MuiTooltip>
                ))}
              </Stack>
            </Box>

            <Box>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Evolucion del puntaje en el anio</Typography>
              <ResponsiveContainer width="100%" height={200}>
                <LineChart data={datos.evolucionPuntajeAnio.map((p) => ({ mes: NOMBRES_MES[p.mes - 1], puntaje: p.puntaje }))}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="mes" tick={{ fontSize: 12 }} />
                  <YAxis domain={[0, 130]} />
                  <RechartsTooltip />
                  <Line type="monotone" dataKey="puntaje" stroke={colorSerie("#334155")} strokeWidth={2} connectNulls dot={{ r: 3 }} />
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </Stack>
        )}
      </DialogContent>
    </Dialog>
  );
}
