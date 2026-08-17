import { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, Collapse, Grid, LinearProgress, Paper, Stack, Table,
  TableBody, TableCell, TableContainer, TableHead, TableRow, Tabs, Tab, TextField,
  Tooltip as MuiTooltip, Typography,
} from "@mui/material";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import DownloadIcon from "@mui/icons-material/Download";
import PrintIcon from "@mui/icons-material/Print";
import EmojiEventsIcon from "@mui/icons-material/EmojiEvents";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { useQuery } from "@tanstack/react-query";
import {
  Bar, BarChart, CartesianGrid, Cell, Legend, Line, LineChart, Pie, PieChart, ResponsiveContainer,
  Tooltip as RechartsTooltip, XAxis, YAxis,
} from "recharts";
import { AvatarUsuario } from "../../shared/components/AvatarUsuario";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { useSesion } from "../../shared/api/sesion";
import {
  obtenerDashboard, obtenerFiltrosDashboard, obtenerTendenciasDashboard,
  type CargaTrabajoDetalle, type CargaTrabajoEmpleado, type DashboardData, type KpiEstado,
  type RankingItem,
} from "../../shared/api/dashboard";
import { EmpleadoDrillDownDialog } from "./EmpleadoDrillDownDialog";

const NOMBRES_MES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

const ESTADOS_NEGATIVOS = new Set(["Vencidos", "Vencidas", "Retrasadas"]);
const ESTADOS_POSITIVOS = new Set(["Terminados", "Terminadas", "Liberadas", "Cerrados"]);

/** Explicacion en lenguaje llano de como se calcula cada metrica -- para que el usuario sepa en que mejorar. */
const EXPLICACION_METRICA: Record<string, string> = {
  Pendientes: "Elementos que aun no se han iniciado, abiertos al cierre del periodo seleccionado.",
  "En proceso": "Elementos en trabajo activo (incluye en pruebas, correccion y suspendidos), abiertos al cierre del periodo.",
  Terminados: "Elementos cerrados (terminados o cancelados) durante el periodo seleccionado.",
  Terminadas: "Elementos cerrados durante el periodo seleccionado.",
  Cerrados: "Cerrados durante el periodo seleccionado.",
  Liberadas: "Releases liberados durante el periodo seleccionado.",
  Vencidos: "Elementos abiertos cuya fecha compromiso ya paso, al cierre del periodo.",
  Vencidas: "Elementos abiertos que llevan mas de 3 dias sin cerrarse, al cierre del periodo.",
  Retrasadas: "Releases con fecha plan vencida que todavia no se liberan.",
};

const EXPLICACION_RANKING: Record<string, string> = {
  Puntaje: "Promedio de las 6 dimensiones de evaluacion mensual (Calidad, Productividad, Puntualidad, Trabajo en equipo, Cumplimiento, Comunicacion) que tengan dato suficiente. 100 = desempeno esperado.",
  Productividad: "Elementos terminados en el periodo, comparado contra el promedio del equipo. 100 = igual al promedio del equipo.",
  Eficiencia: "Horas estimadas (presupuesto) entre horas realmente registradas en el periodo, en porcentaje. Mas de 100% = uso menos horas de las presupuestadas.",
  "Entregas a tiempo": "Porcentaje de elementos con fecha compromiso que se cerraron a tiempo (sin pasarse de la fecha).",
};

function EtiquetaConTooltip({ texto, explicacion }: { texto: string; explicacion?: string }) {
  if (!explicacion) return <>{texto}</>;
  return (
    <MuiTooltip title={explicacion} arrow>
      <Stack direction="row" spacing={0.4} sx={{ alignItems: "center", cursor: "help" }} component="span">
        <span>{texto}</span>
        <InfoOutlinedIcon sx={{ fontSize: 13, color: "text.disabled" }} />
      </Stack>
    </MuiTooltip>
  );
}

function colorVariacion(estado: string, variacion: number | null): "success.main" | "error.main" | "text.secondary" {
  if (variacion === null || variacion === 0) return "text.secondary";
  if (ESTADOS_NEGATIVOS.has(estado)) return variacion > 0 ? "error.main" : "success.main";
  if (ESTADOS_POSITIVOS.has(estado)) return variacion > 0 ? "success.main" : "error.main";
  return "text.secondary";
}

function IndicadorVariacion({ estado }: { estado: KpiEstado }) {
  const color = colorVariacion(estado.estado, estado.variacionPorcentaje);
  return (
    <Stack direction="row" spacing={0.25} sx={{ color, alignItems: "center" }}>
      {estado.variacionPorcentaje !== null && estado.variacionPorcentaje !== 0 && (
        estado.variacionPorcentaje > 0
          ? <ArrowUpwardIcon sx={{ fontSize: 14 }} />
          : <ArrowDownwardIcon sx={{ fontSize: 14 }} />
      )}
      <Typography variant="caption" sx={{ color }}>
        {estado.variacionPorcentaje !== null ? `${estado.variacionPorcentaje.toFixed(0)}%` : "s/d"}
      </Typography>
    </Stack>
  );
}

function exportarCsv(data: DashboardData) {
  const filas: string[] = ["Resumen ejecutivo"];
  filas.push("Grupo,Estado,Total,Total mes anterior,Variacion %");
  data.resumenEjecutivo.forEach((g) => g.estados.forEach((e) => {
    filas.push(`${g.grupo},${e.estado},${e.total},${e.totalMesAnterior},${e.variacionPorcentaje ?? ""}`);
  }));
  filas.push("");
  filas.push("Carga de trabajo");
  filas.push("Empleado,Area,Horas asignadas,Horas consumidas,Horas disponibles,% Utilizacion");
  data.cargaTrabajo.forEach((c) => {
    filas.push(`${c.nombre},${c.area ?? ""},${c.horasAsignadas},${c.horasConsumidas},${c.horasDisponibles ?? ""},${c.porcentajeUtilizacion ?? ""}`);
  });
  filas.push("");
  filas.push("Carga de trabajo por elementos");
  filas.push("Asignado,Pendiente,En proceso,Terminado,Retrasos,Prom dias,Total,Eficiencia entrega %");
  data.desgloseCargaTrabajo.forEach((d) => {
    filas.push(`${d.nombre},${d.pendiente},${d.enProceso},${d.terminado},${d.retrasos},${d.promedioDuracionDias ?? ""},${d.total},${d.eficienciaEntrega ?? ""}`);
  });

  const blob = new Blob([filas.join("\n")], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.download = `dashboard-${data.anio}-${String(data.mes).padStart(2, "0")}.csv`;
  enlace.click();
  URL.revokeObjectURL(url);
}

type OrdenCarga = "mayor" | "menor" | "area" | "proyecto";

function ordenarCarga(carga: CargaTrabajoEmpleado[], orden: OrdenCarga): CargaTrabajoEmpleado[] {
  const copia = [...carga];
  switch (orden) {
    case "mayor": return copia.sort((a, b) => b.horasConsumidas - a.horasConsumidas);
    case "menor": return copia.sort((a, b) => a.horasConsumidas - b.horasConsumidas);
    case "area": return copia.sort((a, b) => (a.area ?? "").localeCompare(b.area ?? ""));
    case "proyecto": return copia.sort((a, b) => (a.proyectoPrincipal ?? "").localeCompare(b.proyectoPrincipal ?? ""));
  }
}

export function DashboardEjecutivoPage() {
  const hoy = new Date();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idArea, setIdArea] = useState<number | "">("");
  const [idUsuarioFiltro, setIdUsuarioFiltro] = useState<number | "">("");
  const [ordenCarga, setOrdenCarga] = useState<OrdenCarga>("mayor");
  const [metricaRanking, setMetricaRanking] = useState(0);
  const [tendenciasAbiertas, setTendenciasAbiertas] = useState(false);
  const [metricaTendencia, setMetricaTendencia] = useState(0);
  const [empleadoDrillDown, setEmpleadoDrillDown] = useState<number | null>(null);
  const sesion = useSesion((estado) => estado.sesion);

  const filtros = useQuery({ queryKey: ["dashboard-filtros"], queryFn: obtenerFiltrosDashboard, staleTime: 5 * 60_000 });

  // Al entrar, si el usuario firmado es un colaborador medible (aparece en el catalogo de
  // empleados del dashboard), el filtro parte viendo su propio dashboard -- igual que el
  // resto de bandejas de la app. Administradores/externos (excluidos de las metricas) no
  // aparecen en el catalogo, asi que para ellos simplemente arranca en "Todos".
  useEffect(() => {
    if (idUsuarioFiltro === "" && sesion && filtros.data?.empleados.some((u) => u.id === sesion.idUsuario)) {
      setIdUsuarioFiltro(sesion.idUsuario);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo cuando llega el catalogo o cambia la sesion
  }, [filtros.data, sesion]);

  const dashboard = useQuery({
    queryKey: ["dashboard", anio, mes, idProyecto, idArea, idUsuarioFiltro],
    queryFn: () => obtenerDashboard({
      anio, mes,
      idProyecto: idProyecto === "" ? null : idProyecto,
      idArea: idArea === "" ? null : idArea,
      idUsuario: idUsuarioFiltro === "" ? null : idUsuarioFiltro,
    }),
  });

  const tendencias = useQuery({
    queryKey: ["dashboard-tendencias", anio, idProyecto, idArea],
    queryFn: () => obtenerTendenciasDashboard(anio, idProyecto === "" ? null : idProyecto, idArea === "" ? null : idArea),
    enabled: tendenciasAbiertas,
  });

  const cargaOrdenada = useMemo(
    () => (dashboard.data ? ordenarCarga(dashboard.data.cargaTrabajo, ordenCarga) : []),
    [dashboard.data, ordenCarga],
  );

  const datos = dashboard.data;
  const ranking = datos?.rankings[metricaRanking];
  const tendenciaSeleccionada = tendencias.data?.[metricaTendencia];

  return (
    <Box sx={{ p: 2 }}>
      <Box sx={{ position: "sticky", top: 0, zIndex: 2, bgcolor: "background.default", pb: 1 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", alignItems: { sm: "center" }, mb: 2 }} spacing={1}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>Dashboard ejecutivo</Typography>
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" startIcon={<DownloadIcon />} disabled={!datos} onClick={() => datos && exportarCsv(datos)}>
              Exportar Excel
            </Button>
            <Button size="small" variant="outlined" startIcon={<PrintIcon />} onClick={() => window.print()}>
              Exportar PDF
            </Button>
          </Stack>
        </Stack>

        <Paper variant="outlined" sx={{ p: 1.5 }}>
          <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center", rowGap: 1.5 }}>
            <TextField size="small" type="number" label="Anio" value={anio} sx={{ width: 100 }}
              onChange={(e) => setAnio(Number(e.target.value))} />
            <TextField size="small" select label="Mes" value={mes} sx={{ width: 150 }}
              onChange={(e) => setMes(Number(e.target.value))} slotProps={{ select: { native: true } }}>
              {NOMBRES_MES.map((nombre, i) => <option key={nombre} value={i + 1}>{nombre}</option>)}
            </TextField>
            <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }}
              opciones={[{ valor: "", etiqueta: "Todos" }, ...(filtros.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))]} />
            <ComboBuscable label="Area" value={idArea} onChange={(v) => setIdArea(v as number | "")} sx={{ minWidth: 180 }}
              opciones={[{ valor: "", etiqueta: "Todas" }, ...(filtros.data?.areas ?? []).map((a) => ({ valor: a.id, etiqueta: a.nombre }))]} />
            <ComboBuscable label="Empleado" value={idUsuarioFiltro} onChange={(v) => setIdUsuarioFiltro(v as number | "")} sx={{ minWidth: 220 }}
              opciones={[{ valor: "", etiqueta: "Todos" }, ...(filtros.data?.empleados ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))]} />
            {datos && (
              <Chip
                size="small"
                icon={<EmojiEventsIcon sx={{ fontSize: 16 }} />}
                label={`Alcance: ${datos.alcanceVisibilidad}`}
                sx={{ ml: "auto" }}
              />
            )}
          </Stack>
        </Paper>
      </Box>

      {dashboard.isLoading && <LinearProgress sx={{ mb: 2 }} />}
      {dashboard.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo cargar el dashboard.</Alert>}

      {datos && (
        <>
          {/* Empleado del mes */}
          <Grid container spacing={2} sx={{ mb: 2 }}>
            <Grid size={{ xs: 12, md: 5 }}>
              <Paper variant="outlined" sx={{ p: 2, height: "100%" }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: "baseline", mb: 1.5 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Empleado del mes</Typography>
                  {datos.empleadoDelMes && (
                    <Typography variant="caption" color="text.secondary">
                      {NOMBRES_MES[datos.empleadoDelMes.mes - 1]} {datos.empleadoDelMes.anio} (mes cerrado)
                    </Typography>
                  )}
                </Stack>
                {datos.empleadoDelMes?.empleado ? (
                  <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
                    <AvatarUsuario
                      urlFoto={datos.empleadoDelMes.empleado.urlFoto}
                      nombre={datos.empleadoDelMes.empleado.nombre}
                      sx={{ width: 64, height: 64, fontSize: 22, bgcolor: "secondary.main" }}
                    />
                    <Box>
                      <Typography variant="h6">{datos.empleadoDelMes.empleado.nombre}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {[datos.empleadoDelMes.empleado.puesto, datos.empleadoDelMes.empleado.area].filter(Boolean).join(" - ") || "Sin area/puesto capturado"}
                      </Typography>
                      <Typography variant="body2" sx={{ mt: 0.5 }}>
                        Puntaje: <b>{datos.empleadoDelMes.puntaje?.toFixed(1) ?? "s/d"}</b>
                      </Typography>
                      <Typography variant="caption" color="text.secondary">{datos.empleadoDelMes.motivo}</Typography>
                    </Box>
                  </Stack>
                ) : (
                  <Typography color="text.secondary">Sin datos suficientes para calcular el empleado del mes.</Typography>
                )}
              </Paper>
            </Grid>
            <Grid size={{ xs: 12, md: 7 }}>
              <Paper variant="outlined" sx={{ p: 2, height: "100%" }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Historico</Typography>
                <TableContainer sx={{ maxHeight: 180 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell>Empleado</TableCell>
                        <TableCell>Mes</TableCell>
                        <TableCell align="right">Puntaje</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {datos.historicoEmpleadoDelMes.length === 0 && (
                        <TableRow><TableCell colSpan={3}><Typography variant="body2" color="text.secondary">Sin historico disponible.</Typography></TableCell></TableRow>
                      )}
                      {datos.historicoEmpleadoDelMes.map((h) => (
                        <TableRow key={`${h.anio}-${h.mes}`}>
                          <TableCell>{h.empleado?.nombre ?? "-"}</TableCell>
                          <TableCell>{NOMBRES_MES[h.mes - 1]} {h.anio}</TableCell>
                          <TableCell align="right">{h.puntaje?.toFixed(1) ?? "s/d"}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Paper>
            </Grid>
          </Grid>

          {/* Resumen ejecutivo */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Resumen ejecutivo</Typography>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            {datos.resumenEjecutivo.map((grupo) => (
              <Grid key={grupo.grupo} size={{ xs: 12, sm: 6, lg: 3 }}>
                <Paper variant="outlined" sx={{ p: 1.5, height: "100%" }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>{grupo.grupo}</Typography>
                  <Grid container spacing={1}>
                    {grupo.estados.map((estado) => (
                      <Grid key={estado.estado} size={6}>
                        <Box sx={{ p: 1, bgcolor: "action.hover", borderRadius: 1 }}>
                          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block" }}>
                            <EtiquetaConTooltip texto={estado.estado} explicacion={EXPLICACION_METRICA[estado.estado]} />
                          </Typography>
                          <Typography variant="h6" sx={{ lineHeight: 1.2 }}>{estado.total}</Typography>
                          <IndicadorVariacion estado={estado} />
                        </Box>
                      </Grid>
                    ))}
                  </Grid>
                </Paper>
              </Grid>
            ))}
          </Grid>

          {/* Carga de trabajo */}
          <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Carga de trabajo</Typography>
            <TextField size="small" select label="Ordenar por" value={ordenCarga} sx={{ width: 180 }}
              onChange={(e) => setOrdenCarga(e.target.value as OrdenCarga)} slotProps={{ select: { native: true } }}>
              <option value="mayor">Mayor carga</option>
              <option value="menor">Menor carga</option>
              <option value="area">Area</option>
              <option value="proyecto">Proyecto</option>
            </TextField>
          </Stack>
          <Paper variant="outlined" sx={{ p: 1.5, mb: 1 }}>
            <ResponsiveContainer width="100%" height={Math.max(160, cargaOrdenada.length * 34)}>
              <BarChart data={cargaOrdenada} layout="vertical" margin={{ left: 24, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" />
                <YAxis type="category" dataKey="nombre" width={150} tick={{ fontSize: 11 }} />
                <RechartsTooltip formatter={(v) => `${Number(v).toFixed(1)}h`} />
                <Legend />
                <Bar dataKey="horasAsignadas" name="Asignadas" fill="#334155" />
                <Bar dataKey="horasConsumidas" name="Consumidas" fill="#0f766e" />
                <Bar dataKey="horasDisponibles" name="Disponibles" fill="#94a3b8" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>
          <Paper variant="outlined" sx={{ mb: 3 }}>
            <TableContainer sx={{ maxHeight: 320 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Empleado</TableCell>
                    <TableCell>Area</TableCell>
                    <TableCell>Proyecto principal</TableCell>
                    <TableCell align="right">
                      <EtiquetaConTooltip texto="Asignadas" explicacion="Horas presupuesto de los elementos asignados vigentes en el periodo (abiertos o cerrados dentro del mes)." />
                    </TableCell>
                    <TableCell align="right">
                      <EtiquetaConTooltip texto="Consumidas" explicacion="Horas realmente registradas por la persona durante el periodo (registro de tiempo)." />
                    </TableCell>
                    <TableCell align="right">
                      <EtiquetaConTooltip texto="Disponibles" explicacion="Minutos laborables del periodo segun su horario asignado (calendario laboral). Sin horario asignado no se puede calcular." />
                    </TableCell>
                    <TableCell sx={{ width: 160 }}>
                      <EtiquetaConTooltip texto="% Utilizacion" explicacion="Horas consumidas entre horas disponibles. Mas de 100% indica sobrecarga; menos de 50% indica capacidad libre." />
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {cargaOrdenada.map((c) => (
                    <TableRow key={c.idUsuario} hover sx={{ cursor: "pointer" }} onClick={() => setEmpleadoDrillDown(c.idUsuario)}>
                      <TableCell>{c.nombre}</TableCell>
                      <TableCell>{c.area ?? "-"}</TableCell>
                      <TableCell>{c.proyectoPrincipal ?? "-"}</TableCell>
                      <TableCell align="right">{c.horasAsignadas.toFixed(1)}</TableCell>
                      <TableCell align="right">{c.horasConsumidas.toFixed(1)}</TableCell>
                      <TableCell align="right">{c.horasDisponibles?.toFixed(1) ?? "s/d"}</TableCell>
                      <TableCell>
                        {c.porcentajeUtilizacion !== null ? (
                          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                            <LinearProgress
                              variant="determinate"
                              value={Math.min(100, c.porcentajeUtilizacion)}
                              color={c.porcentajeUtilizacion > 100 ? "error" : c.porcentajeUtilizacion < 50 ? "warning" : "success"}
                              sx={{ flex: 1, height: 8, borderRadius: 4 }}
                            />
                            <Typography variant="caption">{c.porcentajeUtilizacion.toFixed(0)}%</Typography>
                          </Stack>
                        ) : <Typography variant="caption" color="text.secondary">Sin horario</Typography>}
                      </TableCell>
                    </TableRow>
                  ))}
                  {cargaOrdenada.length === 0 && (
                    <TableRow><TableCell colSpan={7}><Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>Sin colaboradores en el alcance/filtro actual.</Typography></TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>

          {/* Carga de trabajo por elementos (conteo, estilo GT) */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Carga de trabajo por elementos</Typography>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid size={{ xs: 12, md: 7 }}>
              <Paper variant="outlined">
                <TableContainer sx={{ maxHeight: 360 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell>Asignado</TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Pendiente" explicacion={EXPLICACION_METRICA.Pendientes} />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="En proceso" explicacion={EXPLICACION_METRICA["En proceso"]} />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Terminado" explicacion={EXPLICACION_METRICA.Terminados} />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Retrasos" explicacion="De los terminados con fecha compromiso, cuantos se entregaron despues de esa fecha." />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Prom. dias" explicacion="Dias promedio entre el inicio y el cierre de los elementos terminados en el periodo." />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Total" explicacion="Pendiente + En proceso + Terminado." />
                        </TableCell>
                        <TableCell align="right">
                          <EtiquetaConTooltip texto="Eficiencia entrega" explicacion="Porcentaje de los terminados con fecha compromiso que se entregaron a tiempo." />
                        </TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {(datos.desgloseCargaTrabajo ?? []).length === 0 && (
                        <TableRow><TableCell colSpan={7}><Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>Sin datos en el alcance/filtro actual.</Typography></TableCell></TableRow>
                      )}
                      {(datos.desgloseCargaTrabajo ?? []).map((d) => (
                        <TableRow key={d.idUsuario} hover sx={{ cursor: "pointer" }} onClick={() => setEmpleadoDrillDown(d.idUsuario)}>
                          <TableCell>{d.nombre}</TableCell>
                          <TableCell align="right">{d.pendiente}</TableCell>
                          <TableCell align="right">{d.enProceso}</TableCell>
                          <TableCell align="right">{d.terminado}</TableCell>
                          <TableCell align="right" sx={{ color: d.retrasos > 0 ? "error.main" : undefined }}>{d.retrasos}</TableCell>
                          <TableCell align="right">{d.promedioDuracionDias?.toFixed(1) ?? "s/d"}</TableCell>
                          <TableCell align="right">{d.total}</TableCell>
                          <TableCell align="right">{d.eficienciaEntrega !== null ? `${d.eficienciaEntrega.toFixed(1)}%` : "s/d"}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Paper>
            </Grid>
            <Grid size={{ xs: 12, md: 5 }}>
              <Paper variant="outlined" sx={{ p: 1.5, height: "100%" }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1, textAlign: "center" }}>Carga total (por elementos)</Typography>
                <GraficaCargaTotal datos={datos.desgloseCargaTrabajo ?? []} onVer={setEmpleadoDrillDown} />
              </Paper>
            </Grid>
          </Grid>

          {/* Rankings */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Rankings</Typography>
          <Paper variant="outlined" sx={{ mb: 3 }}>
            <Tabs value={metricaRanking} onChange={(_, v) => setMetricaRanking(v)} variant="scrollable" scrollButtons="auto">
              {datos.rankings.map((r) => (
                <Tab key={r.metrica} label={<EtiquetaConTooltip texto={r.metrica} explicacion={EXPLICACION_RANKING[r.metrica]} />} />
              ))}
            </Tabs>
            <Grid container>
              <Grid size={{ xs: 12, md: 6 }} sx={{ p: 1.5, borderRight: { md: 1 }, borderColor: "divider" }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Top 10</Typography>
                <GraficaRanking items={ranking?.top10 ?? []} onVer={setEmpleadoDrillDown} color="#0f766e" />
                <TablaRanking items={ranking?.top10 ?? []} onVer={setEmpleadoDrillDown} />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }} sx={{ p: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Bottom 10</Typography>
                <GraficaRanking items={ranking?.bottom10 ?? []} onVer={setEmpleadoDrillDown} color="#b45309" />
                <TablaRanking items={ranking?.bottom10 ?? []} onVer={setEmpleadoDrillDown} />
              </Grid>
            </Grid>
          </Paper>

          {/* Comparativos */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Comparativos</Typography>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            {datos.comparativos.empleadoVsPromedioEquipo.length > 0 && (
              <Grid size={{ xs: 12, md: 6 }}>
                <Paper variant="outlined" sx={{ p: 1.5 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Empleado vs promedio del equipo</Typography>
                  <GraficaBarraSimple datos={datos.comparativos.empleadoVsPromedioEquipo} color="#0f766e" />
                </Paper>
              </Grid>
            )}
            <Grid size={{ xs: 12, md: 6 }}>
              <Paper variant="outlined" sx={{ p: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Puntaje promedio por area</Typography>
                <GraficaBarraSimple datos={datos.comparativos.porArea} color="#334155" />
              </Paper>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <Paper variant="outlined" sx={{ p: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>Horas registradas por proyecto</Typography>
                <GraficaBarraSimple datos={datos.comparativos.porProyecto} color="#0f766e" />
              </Paper>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <Paper variant="outlined" sx={{ p: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>WorkItems terminados: anio actual vs anterior</Typography>
                <GraficaBarraSimple datos={datos.comparativos.anioActualVsAnterior} color="#334155" />
              </Paper>
            </Grid>
          </Grid>

          {/* Tendencias */}
          <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Tendencias del anio</Typography>
            <Button size="small" onClick={() => setTendenciasAbiertas((v) => !v)}>
              {tendenciasAbiertas ? "Ocultar" : "Mostrar"}
            </Button>
          </Stack>
          <Collapse in={tendenciasAbiertas} unmountOnExit>
            <Paper variant="outlined" sx={{ p: 1.5, mb: 3 }}>
              {tendencias.isLoading && <LinearProgress />}
              {tendencias.data && (
                <>
                  <Tabs value={metricaTendencia} onChange={(_, v) => setMetricaTendencia(v)} variant="scrollable" scrollButtons="auto" sx={{ mb: 1 }}>
                    {tendencias.data.map((t) => <Tab key={t.metrica} label={t.metrica} />)}
                  </Tabs>
                  <ResponsiveContainer width="100%" height={260}>
                    <LineChart data={tendenciaSeleccionada?.puntos.map((p) => ({ mes: NOMBRES_MES[p.mes - 1].slice(0, 3), valor: p.valor }))}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="mes" tick={{ fontSize: 12 }} />
                      <YAxis />
                      <RechartsTooltip />
                      <Line type="monotone" dataKey="valor" stroke="#0f766e" strokeWidth={2} dot={{ r: 3 }} />
                    </LineChart>
                  </ResponsiveContainer>
                </>
              )}
            </Paper>
          </Collapse>
        </>
      )}

      <EmpleadoDrillDownDialog idUsuario={empleadoDrillDown} anio={anio} mes={mes} onClose={() => setEmpleadoDrillDown(null)} />
    </Box>
  );
}

function TablaRanking({ items, onVer }: { items: { idUsuario: number; nombre: string; area: string | null; valor: number }[]; onVer: (id: number) => void }) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Empleado</TableCell>
            <TableCell>Area</TableCell>
            <TableCell align="right">Valor</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.length === 0 && (
            <TableRow><TableCell colSpan={3}><Typography variant="body2" color="text.secondary">Sin datos suficientes.</Typography></TableCell></TableRow>
          )}
          {items.map((it) => (
            <TableRow key={it.idUsuario} hover sx={{ cursor: "pointer" }} onClick={() => onVer(it.idUsuario)}>
              <TableCell>{it.nombre}</TableCell>
              <TableCell>{it.area ?? "-"}</TableCell>
              <TableCell align="right">{it.valor.toFixed(1)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function GraficaRanking({ items, onVer, color }: { items: RankingItem[]; onVer: (id: number) => void; color: string }) {
  if (items.length === 0) {
    return null;
  }
  return (
    <ResponsiveContainer width="100%" height={Math.max(120, items.length * 30)}>
      <BarChart data={items} layout="vertical" margin={{ left: 16, right: 16 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis type="number" />
        <YAxis type="category" dataKey="nombre" width={140} tick={{ fontSize: 11 }} />
        <RechartsTooltip />
        <Bar
          dataKey="valor"
          fill={color}
          radius={[0, 4, 4, 0]}
          cursor="pointer"
          onClick={(_, index) => onVer(items[index].idUsuario)}
        />
      </BarChart>
    </ResponsiveContainer>
  );
}

const COLORES_PIE = ["#334155", "#0f766e", "#b45309", "#7c3aed", "#0891b2", "#be123c", "#4d7c0f", "#a16207", "#0369a1", "#9333ea"];

function GraficaCargaTotal({ datos, onVer }: { datos: CargaTrabajoDetalle[]; onVer: (id: number) => void }) {
  const conCarga = datos.filter((d) => d.total > 0);
  if (conCarga.length === 0) {
    return <Typography variant="body2" color="text.secondary" sx={{ textAlign: "center" }}>Sin elementos asignados en el alcance/filtro actual.</Typography>;
  }
  return (
    <ResponsiveContainer width="100%" height={320}>
      <PieChart>
        <Pie
          data={conCarga}
          dataKey="total"
          nameKey="nombre"
          cx="50%"
          cy="50%"
          outerRadius={110}
          label={(entrada: { percent?: number }) => `${((entrada.percent ?? 0) * 100).toFixed(1)}%`}
          onClick={(entrada: { payload?: CargaTrabajoDetalle }) => entrada.payload && onVer(entrada.payload.idUsuario)}
          cursor="pointer"
        >
          {conCarga.map((d, i) => <Cell key={d.idUsuario} fill={COLORES_PIE[i % COLORES_PIE.length]} />)}
        </Pie>
        <RechartsTooltip formatter={(v, _n, item) => [`${v} elementos`, (item?.payload as CargaTrabajoDetalle | undefined)?.nombre ?? ""]} />
        <Legend layout="vertical" align="right" verticalAlign="middle" wrapperStyle={{ fontSize: 11 }} />
      </PieChart>
    </ResponsiveContainer>
  );
}

function GraficaBarraSimple({ datos, color }: { datos: { etiqueta: string; valor: number }[]; color: string }) {
  if (datos.length === 0) {
    return <Typography variant="body2" color="text.secondary">Sin datos suficientes para este comparativo.</Typography>;
  }
  return (
    <ResponsiveContainer width="100%" height={Math.max(120, datos.length * 40)}>
      <BarChart data={datos} layout="vertical" margin={{ left: 16, right: 16 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis type="number" />
        <YAxis type="category" dataKey="etiqueta" width={140} tick={{ fontSize: 11 }} />
        <RechartsTooltip />
        <Bar dataKey="valor" fill={color} radius={[0, 4, 4, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
