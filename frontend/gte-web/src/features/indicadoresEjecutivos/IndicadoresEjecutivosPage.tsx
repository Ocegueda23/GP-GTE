import { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, Grid, IconButton, LinearProgress, LinearProgress as Bar,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip as MuiTooltip, Typography,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import PrintIcon from "@mui/icons-material/Print";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import VisibilityIcon from "@mui/icons-material/Visibility";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  DndContext, PointerSensor, closestCenter, useSensor, useSensors, type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext, arrayMove, useSortable, verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip as RechartsTooltip, XAxis, YAxis,
} from "recharts";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import {
  guardarLayoutDashboardEjecutivo, obtenerIndicadoresEjecutivos, obtenerLayoutDashboardEjecutivo,
  type IndicadoresEjecutivos,
} from "../../shared/api/indicadoresEjecutivos";

const NOMBRES_MES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

type ClaveWidget = "kpis" | "dora" | "proyectos" | "okr" | "kpisPersonalizados" | "riesgos" | "burndown";

const TITULO_WIDGET: Record<ClaveWidget, string> = {
  kpis: "Indicadores generales",
  dora: "DORA metrics",
  proyectos: "Semaforo de proyectos",
  okr: "OKR",
  kpisPersonalizados: "KPIs personalizados",
  riesgos: "Top riesgos",
  burndown: "Burndown del sprint activo",
};

const ORDEN_DEFAULT: ClaveWidget[] = ["kpis", "dora", "proyectos", "okr", "kpisPersonalizados", "riesgos", "burndown"];

interface LayoutDashboard {
  orden: ClaveWidget[];
  ocultos: ClaveWidget[];
}

const LAYOUT_DEFAULT: LayoutDashboard = { orden: ORDEN_DEFAULT, ocultos: [] };

function colorSemaforo(semaforo: string): "success" | "warning" | "error" {
  if (semaforo === "Verde") return "success";
  if (semaforo === "Naranja") return "warning";
  return "error";
}

function EtiquetaConTooltip({ texto, explicacion }: { texto: string; explicacion: string }) {
  return (
    <MuiTooltip title={explicacion} arrow>
      <Stack direction="row" spacing={0.4} sx={{ alignItems: "center", cursor: "help" }} component="span">
        <span>{texto}</span>
        <InfoOutlinedIcon sx={{ fontSize: 13, color: "text.disabled" }} />
      </Stack>
    </MuiTooltip>
  );
}

function Tile({ titulo, valor, explicacion, sufijo }: {
  titulo: string; valor: string; explicacion: string; sufijo?: string;
}) {
  return (
    <Paper variant="outlined" sx={{ p: 1.5, minWidth: 150, flex: 1 }}>
      <Typography variant="caption" color="text.secondary">
        <EtiquetaConTooltip texto={titulo} explicacion={explicacion} />
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700 }}>
        {valor}{sufijo && <Typography component="span" variant="caption" sx={{ ml: 0.5 }}>{sufijo}</Typography>}
      </Typography>
    </Paper>
  );
}

function ContenidoWidget({ clave, datos }: { clave: ClaveWidget; datos: IndicadoresEjecutivos }) {
  switch (clave) {
    case "kpis":
      return (
        <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
          <Tile titulo="Lead Time P50" valor={datos.leadCycleTime.leadTimeHorasP50.toFixed(1)} sufijo="h"
            explicacion="Horas laborales entre el registro y el cierre del elemento, mediana de los items terminados en el periodo." />
          <Tile titulo="Lead Time P85" valor={datos.leadCycleTime.leadTimeHorasP85.toFixed(1)} sufijo="h"
            explicacion="Percentil 85 del Lead Time: 85% de los items terminaron en menos de este tiempo." />
          <Tile titulo="Cycle Time" valor={datos.leadCycleTime.cycleTimeHorasPromedio.toFixed(1)} sufijo="h"
            explicacion="Promedio de horas laborales realmente invertidas En Proceso por item terminado." />
          <Tile titulo="Entrega a tiempo" valor={`${datos.entregaATiempo.porcentaje.toFixed(1)}%`}
            explicacion="Porcentaje de items con fecha compromiso que se cerraron a tiempo. Verde >=90%, Naranja >=80%, Rojo <80%." />
          <Tile titulo="Eficiencia" valor={`${datos.eficienciaPorcentaje.toFixed(1)}%`}
            explicacion="Minutos presupuesto entre minutos invertidos, en porcentaje. Mas de 100% = se uso menos tiempo del presupuestado." />
          <Tile titulo="Retrabajo" valor={`${datos.retrabajo.porcentaje.toFixed(1)}%`}
            explicacion="Porcentaje de tiempo invertido en items tipo Correccion sobre el tiempo total invertido." />
          <Tile titulo="Productividad" valor={datos.productividad.puntosPromedioPorPersona.toFixed(1)} sufijo="pts/persona"
            explicacion="Puntos de historia completados entre el numero de personas distintas que terminaron algo en el periodo." />
          <Tile titulo="SLA" valor={`${datos.sla.cumplimientoPorcentaje.toFixed(1)}%`}
            explicacion="Porcentaje de tickets resueltos dentro de su fecha limite de resolucion (global, no filtra por proyecto/equipo)." />
          <Tile titulo="CSAT" valor={datos.sla.csat !== null ? datos.sla.csat.toFixed(1) : "s/d"} sufijo={datos.sla.csat !== null ? "/5" : undefined}
            explicacion="Promedio de calificacion de encuestas de satisfaccion de tickets resueltos en el periodo (global)." />
        </Stack>
      );
    case "dora":
      return (
        <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", rowGap: 1.5 }}>
          <Tile titulo="Deployment Frequency" valor={datos.dora.deploymentsPorSemana.toFixed(2)} sufijo="/semana"
            explicacion="Despliegues exitosos a un ambiente PROD por semana, en el periodo." />
          <Tile titulo="Lead Time for Changes" valor={datos.dora.leadTimeCambiosSinDatos ? "Sin datos" : `${datos.dora.leadTimeCambiosHoras?.toFixed(1)} h`}
            explicacion="Tiempo de merge de un PR a despliegue en PROD. Sin datos: requiere integracion Git (pendiente, resto de Fase 3)." />
          <Tile titulo="Change Failure Rate" valor={`${datos.dora.changeFailureRatePorcentaje.toFixed(1)}%`}
            explicacion="Porcentaje de releases liberados con un incidente ligado dentro de los 7 dias siguientes." />
          <Tile titulo="MTTR" valor={datos.dora.mttrHoras !== null ? `${datos.dora.mttrHoras.toFixed(1)} h` : "s/d"}
            explicacion="Tiempo promedio de resolucion de incidentes (FechaResolucion - FechaOcurrencia) en el periodo." />
        </Stack>
      );
    case "proyectos":
      return (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Proyecto</TableCell>
                <TableCell>Semaforo</TableCell>
                <TableCell align="right">Entrega a tiempo</TableCell>
                <TableCell align="right">Presupuesto</TableCell>
                <TableCell align="right">Costo real</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {datos.proyectos.map((p) => (
                <TableRow key={p.idProyecto} hover>
                  <TableCell>{p.clave} - {p.proyecto}</TableCell>
                  <TableCell><Chip size="small" color={colorSemaforo(p.semaforo)} label={p.semaforo} /></TableCell>
                  <TableCell align="right">{p.entregaATiempoPorcentaje.toFixed(1)}%</TableCell>
                  <TableCell align="right">{p.montoAutorizado !== null ? `$${p.montoAutorizado.toLocaleString()}` : "s/d"}</TableCell>
                  <TableCell align="right">{p.costoReal !== null ? `$${p.costoReal.toLocaleString()}` : "s/d"}</TableCell>
                </TableRow>
              ))}
              {datos.proyectos.length === 0 && (
                <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin proyectos activos en el alcance actual.</Typography></TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      );
    case "okr":
      return (
        <Stack spacing={2}>
          {datos.okr.map((o) => (
            <Paper key={o.idObjetivoOkr} variant="outlined" sx={{ p: 1.5 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{o.nombre}</Typography>
              <Typography variant="caption" color="text.secondary">
                {o.proyecto ?? o.equipo ?? "Sin proyecto/equipo"} - Q{o.trimestre} {o.anio}
              </Typography>
              <Stack spacing={1} sx={{ mt: 1 }}>
                {o.resultadosClave.map((rc) => {
                  const avance = rc.valorMeta === 0 ? 0 : Math.min(100, (rc.valorActual / rc.valorMeta) * 100);
                  return (
                    <Box key={rc.idResultadoClave}>
                      <Stack direction="row" sx={{ justifyContent: "space-between" }}>
                        <Typography variant="caption">{rc.nombre}</Typography>
                        <Typography variant="caption">{rc.valorActual} / {rc.valorMeta}</Typography>
                      </Stack>
                      <Bar variant="determinate" value={avance} sx={{ height: 6, borderRadius: 3 }} />
                    </Box>
                  );
                })}
              </Stack>
            </Paper>
          ))}
          {datos.okr.length === 0 && <Typography color="text.secondary">Sin objetivos OKR para el periodo/filtro actual.</Typography>}
        </Stack>
      );
    case "kpisPersonalizados":
      return (
        <Grid container spacing={2}>
          {datos.kpisPersonalizados.map((kpi) => (
            <Grid key={kpi.clave} size={{ xs: 12, md: 6 }}>
              <Paper variant="outlined" sx={{ p: 1.5 }}>
                <Typography variant="subtitle2">{kpi.nombre}</Typography>
                <Typography variant="caption" color="text.secondary">
                  Meta: {kpi.meta ?? "s/d"} ({kpi.direccion === "Subir" ? "mayor es mejor" : "menor es mejor"})
                </Typography>
                <Box sx={{ height: 160 }}>
                  <ResponsiveContainer>
                    <LineChart data={kpi.serie}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="fecha" tick={{ fontSize: 10 }} />
                      <YAxis tick={{ fontSize: 10 }} />
                      <RechartsTooltip />
                      <Line type="monotone" dataKey="valor" stroke="#334155" dot={false} />
                    </LineChart>
                  </ResponsiveContainer>
                </Box>
              </Paper>
            </Grid>
          ))}
          {datos.kpisPersonalizados.length === 0 && (
            <Grid size={12}><Typography color="text.secondary">Sin KPIs personalizados definidos todavia.</Typography></Grid>
          )}
        </Grid>
      );
    case "riesgos":
      return (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Proyecto</TableCell>
                <TableCell>Descripcion</TableCell>
                <TableCell align="right">Exposicion</TableCell>
                <TableCell>Estatus</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {datos.topRiesgos.map((r) => (
                <TableRow key={r.idRiesgo} hover>
                  <TableCell>{r.proyecto}</TableCell>
                  <TableCell>{r.descripcion}</TableCell>
                  <TableCell align="right">{r.exposicion}</TableCell>
                  <TableCell>{r.estatus}</TableCell>
                </TableRow>
              ))}
              {datos.topRiesgos.length === 0 && (
                <TableRow><TableCell colSpan={4}><Typography color="text.secondary">Sin riesgos capturados.</Typography></TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      );
    case "burndown":
      if (!datos.burndownSprintActivo) {
        return <Typography color="text.secondary">Selecciona un equipo con sprint activo para ver su burndown.</Typography>;
      }
      return (
        <Box>
          <Typography variant="subtitle2">{datos.burndownSprintActivo.nombre} - {datos.burndownSprintActivo.equipo}</Typography>
          <Box sx={{ height: 260 }}>
            <ResponsiveContainer>
              <LineChart data={datos.burndownSprintActivo.puntos}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="fecha" tick={{ fontSize: 10 }} />
                <YAxis tick={{ fontSize: 10 }} />
                <RechartsTooltip />
                <Legend />
                <Line type="monotone" dataKey="restanteIdeal" name="Ideal" stroke="#94a3b8" strokeDasharray="4 4" dot={false} />
                <Line type="monotone" dataKey="restanteReal" name="Real" stroke="#0f766e" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </Box>
        </Box>
      );
  }
}

function WidgetOrdenable({ clave, oculto, onOcultar, children }: {
  clave: ClaveWidget; oculto: boolean; onOcultar: () => void; children: React.ReactNode;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: clave });

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{ p: 2, mb: 2, opacity: isDragging ? 0.5 : 1, transform: CSS.Transform.toString(transform), transition }}
    >
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: oculto ? 0 : 1.5 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <Box {...attributes} {...listeners} sx={{ cursor: "grab", display: "flex" }}>
            <DragIndicatorIcon fontSize="small" color="disabled" />
          </Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{TITULO_WIDGET[clave]}</Typography>
        </Stack>
        <IconButton size="small" onClick={onOcultar}>
          {oculto ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
        </IconButton>
      </Stack>
      {!oculto && children}
    </Paper>
  );
}

export function IndicadoresEjecutivosPage() {
  const hoy = new Date();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [layout, setLayout] = useState<LayoutDashboard>(LAYOUT_DEFAULT);
  const clienteQuery = useQueryClient();
  const sensores = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const catalogos = useQuery({ queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000 });

  const layoutGuardado = useQuery({ queryKey: ["indicadores-ejecutivos-layout"], queryFn: obtenerLayoutDashboardEjecutivo });

  useEffect(() => {
    if (layoutGuardado.data) {
      try {
        const parsed = JSON.parse(layoutGuardado.data) as LayoutDashboard;
        const ordenValido = ORDEN_DEFAULT.filter((c) => parsed.orden.includes(c));
        const faltantes = ORDEN_DEFAULT.filter((c) => !ordenValido.includes(c));
        setLayout({ orden: [...ordenValido, ...faltantes], ocultos: parsed.ocultos.filter((c) => ORDEN_DEFAULT.includes(c)) });
      } catch {
        setLayout(LAYOUT_DEFAULT);
      }
    }
  }, [layoutGuardado.data]);

  const indicadores = useQuery({
    queryKey: ["indicadores-ejecutivos", anio, mes, idEquipo, idProyecto],
    queryFn: () => obtenerIndicadoresEjecutivos({
      anio, mes,
      idEquipo: idEquipo === "" ? null : idEquipo,
      idProyecto: idProyecto === "" ? null : idProyecto,
    }),
  });

  const guardarLayout = (nuevo: LayoutDashboard) => {
    setLayout(nuevo);
    void guardarLayoutDashboardEjecutivo(JSON.stringify(nuevo)).then(() => {
      void clienteQuery.invalidateQueries({ queryKey: ["indicadores-ejecutivos-layout"] });
    });
  };

  const alTerminarArrastre = (evento: DragEndEvent) => {
    const { active, over } = evento;
    if (!over || active.id === over.id) return;
    const desde = layout.orden.indexOf(active.id as ClaveWidget);
    const hasta = layout.orden.indexOf(over.id as ClaveWidget);
    guardarLayout({ ...layout, orden: arrayMove(layout.orden, desde, hasta) });
  };

  const alternarVisibilidad = (clave: ClaveWidget) => {
    const ocultos = layout.ocultos.includes(clave)
      ? layout.ocultos.filter((c) => c !== clave)
      : [...layout.ocultos, clave];
    guardarLayout({ ...layout, ocultos });
  };

  const datos = indicadores.data;

  const opcionesEquipo = useMemo(
    () => [{ valor: "", etiqueta: "Ninguno (sin burndown)" }, ...(catalogos.data?.equipos ?? []).map((e) => ({ valor: e.id, etiqueta: e.nombre }))],
    [catalogos.data],
  );
  const opcionesProyecto = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))],
    [catalogos.data],
  );

  return (
    <Box sx={{ p: 2 }}>
      <Box sx={{ position: "sticky", top: 0, zIndex: 2, bgcolor: "background.default", pb: 1 }}>
        <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", alignItems: { sm: "center" }, mb: 2 }} spacing={1}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>Indicadores ejecutivos</Typography>
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" startIcon={<DownloadIcon />} disabled={!datos}
              onClick={() => datos && exportarCsv(datos)}>
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
              opciones={opcionesProyecto} />
            <ComboBuscable label="Equipo (burndown)" value={idEquipo} onChange={(v) => setIdEquipo(v as number | "")} sx={{ minWidth: 220 }}
              opciones={opcionesEquipo} />
            {datos && <Chip size="small" label={`Alcance: ${datos.alcance}`} sx={{ ml: "auto" }} />}
          </Stack>
        </Paper>
      </Box>

      {indicadores.isLoading && <LinearProgress sx={{ mb: 2 }} />}
      {indicadores.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo cargar el dashboard.</Alert>}

      {datos && (
        <DndContext sensors={sensores} collisionDetection={closestCenter} onDragEnd={alTerminarArrastre}>
          <SortableContext items={layout.orden} strategy={verticalListSortingStrategy}>
            {layout.orden.map((clave) => (
              <WidgetOrdenable key={clave} clave={clave} oculto={layout.ocultos.includes(clave)} onOcultar={() => alternarVisibilidad(clave)}>
                <ContenidoWidget clave={clave} datos={datos} />
              </WidgetOrdenable>
            ))}
          </SortableContext>
        </DndContext>
      )}
    </Box>
  );
}

function exportarCsv(datos: IndicadoresEjecutivos) {
  const filas = [
    ["Indicador", "Valor"],
    ["Lead Time P50 (h)", datos.leadCycleTime.leadTimeHorasP50.toFixed(1)],
    ["Lead Time P85 (h)", datos.leadCycleTime.leadTimeHorasP85.toFixed(1)],
    ["Cycle Time (h)", datos.leadCycleTime.cycleTimeHorasPromedio.toFixed(1)],
    ["Entrega a tiempo (%)", datos.entregaATiempo.porcentaje.toFixed(1)],
    ["Eficiencia (%)", datos.eficienciaPorcentaje.toFixed(1)],
    ["Retrabajo (%)", datos.retrabajo.porcentaje.toFixed(1)],
    ["Productividad (pts/persona)", datos.productividad.puntosPromedioPorPersona.toFixed(1)],
    ["SLA (%)", datos.sla.cumplimientoPorcentaje.toFixed(1)],
    ["CSAT", datos.sla.csat !== null ? datos.sla.csat.toFixed(1) : "s/d"],
    ["Deployment Frequency (/semana)", datos.dora.deploymentsPorSemana.toFixed(2)],
    ["Change Failure Rate (%)", datos.dora.changeFailureRatePorcentaje.toFixed(1)],
    ["MTTR (h)", datos.dora.mttrHoras !== null ? datos.dora.mttrHoras.toFixed(1) : "s/d"],
  ];
  const csv = filas.map((fila) => fila.map((v) => `"${v}"`).join(",")).join("\n");
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.download = `indicadores-ejecutivos-${datos.anio}-${String(datos.mes).padStart(2, "0")}.csv`;
  enlace.click();
  URL.revokeObjectURL(url);
}
