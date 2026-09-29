import { Fragment, useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, LinearProgress, List, ListItemButton, ListItemText,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import { useQuery } from "@tanstack/react-query";
import {
  Area, AreaChart, CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip as RechartsTooltip, XAxis, YAxis,
} from "recharts";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import { useSesion } from "../../shared/api/sesion";
import { formatearMinutos, obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { htmlATextoPlano } from "../../shared/editor/textoPlano";
import { useColorSerie } from "../../shared/graficas/coloresGrafica";
import {
  descargarReporteExcel,
  obtenerReporteBugsDefectos, obtenerReporteCargaTrabajo, obtenerReporteCostos,
  obtenerReporteFlujo, obtenerReporteHorasRegistradas, obtenerReporteKpisHistoricos,
  obtenerReporteProductividad, obtenerReporteRentabilidad, obtenerReporteReleases,
  obtenerReporteRetrabajo, obtenerReporteRiesgos, obtenerReporteSla, obtenerReporteSolicitantes,
  obtenerReporteAuditoria, obtenerReporteActividadesTerminadas,
  type TicketTerminado, type TicketsTerminadosTotales,
  type IncidenteTerminado, type IncidentesTerminadosTotales,
  obtenerReporteGanttActividades,
  type AgrupacionGantt, type FiltroGanttActividades,
} from "../../shared/api/reportes";
import { DiagramaGantt, LeyendaGantt } from "./DiagramaGantt";

type ClaveReporte =
  | "productividad" | "horas" | "retrabajo" | "bugs" | "releases" | "riesgos"
  | "solicitantes" | "costos" | "rentabilidad" | "sla" | "kpis" | "carga" | "flujo" | "auditoria"
  | "actividadesTerminadas" | "gantt";

/** Permiso 1:1 con el ExigirPermisoAsync de cada handler en GTE.Application.Reportes.Queries. */
const REPORTES: { clave: ClaveReporte; titulo: string; permiso: string }[] = [
  { clave: "productividad", titulo: "R01 - Productividad", permiso: "RPT.Ver" },
  { clave: "horas", titulo: "R02 - Horas registradas", permiso: "RPT.Ver" },
  { clave: "retrabajo", titulo: "R03 - Retrabajo", permiso: "RPT.Ver" },
  { clave: "bugs", titulo: "R04 - Bugs y defectos", permiso: "RPT.Ver" },
  { clave: "releases", titulo: "R05 - Versiones/Releases", permiso: "RPT.Ver" },
  { clave: "riesgos", titulo: "R06 - Riesgos", permiso: "RPT.Ver" },
  { clave: "solicitantes", titulo: "R07 - Clientes/solicitantes", permiso: "RPT.Ver" },
  { clave: "costos", titulo: "R08 - Costos", permiso: "RPT.Costos" },
  { clave: "rentabilidad", titulo: "R09 - Rentabilidad", permiso: "RPT.Costos" },
  { clave: "sla", titulo: "R10 - SLA", permiso: "RPT.Ver" },
  { clave: "kpis", titulo: "R11 - KPIs / DORA", permiso: "RPT.Ver" },
  { clave: "carga", titulo: "R12 - Carga de trabajo", permiso: "RPT.Ver" },
  { clave: "flujo", titulo: "R13 - Flujo (CFD)", permiso: "RPT.Ver" },
  { clave: "auditoria", titulo: "R14 - Auditoria", permiso: "RPT.Auditoria" },
  { clave: "actividadesTerminadas", titulo: "R15 - Actividades terminadas", permiso: "RPT.Ver" },
  { clave: "gantt", titulo: "R16 - Gantt de actividades", permiso: "RPT.Ver" },
];

function primerDiaMes(): string {
  const hoy = new Date();
  return `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, "0")}-01`;
}
function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function BarraFechas({ desde, hasta, onDesde, onHasta }: {
  desde: string; hasta: string; onDesde: (v: string) => void; onHasta: (v: string) => void;
}) {
  return (
    <>
      <TextField size="small" type="date" label="Desde" value={desde} onChange={(e) => onDesde(e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }} sx={{ width: 160 }} />
      <TextField size="small" type="date" label="Hasta" value={hasta} onChange={(e) => onHasta(e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }} sx={{ width: 160 }} />
    </>
  );
}

function BotonExportar({ ruta, filtro, nombreArchivo }: {
  ruta: string; filtro: Record<string, string | number | null | undefined>; nombreArchivo: string;
}) {
  const [exportando, setExportando] = useState(false);
  return (
    <Button size="small" variant="outlined" startIcon={<DownloadIcon />} loading={exportando}
      onClick={async () => {
        setExportando(true);
        try { await descargarReporteExcel(ruta, filtro, nombreArchivo); } finally { setExportando(false); }
      }}>
      Exportar Excel
    </Button>
  );
}

function useProyectosEquipos() {
  const catalogos = useQuery({ queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000 });
  const opcionesProyecto = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))],
    [catalogos.data],
  );
  const opcionesEquipo = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.equipos ?? []).map((e) => ({ valor: e.id, etiqueta: e.nombre }))],
    [catalogos.data],
  );
  return { opcionesProyecto, opcionesEquipo };
}

function ReporteProductividad() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-productividad", desde, hasta, idProyecto],
    queryFn: () => obtenerReporteProductividad(desde, hasta, idProyecto === "" ? null : idProyecto),
  });
  const { datosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(consulta.data?.personas, "itemsTerminados");

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="productividad" filtro={{ desde, hasta, idProyecto: idProyecto || null }} nombreArchivo="Productividad.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Usuario</TableCell>
                <EncabezadoOrdenable clave="itemsTerminados" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">Items terminados</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="puntosTotales" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">Puntos</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="porcentajeATiempo" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">% a tiempo</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="eficienciaPorcentaje" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">Eficiencia</EncabezadoOrdenable>
              </TableRow>
            </TableHead>
            <TableBody>
              {datosOrdenados.map((p) => (
                <TableRow key={p.idUsuario} hover>
                  <TableCell>{p.usuario}</TableCell>
                  <TableCell align="right">{p.itemsTerminados}</TableCell>
                  <TableCell align="right">{p.puntosTotales}</TableCell>
                  <TableCell align="right">{p.porcentajeATiempo.toFixed(1)}%</TableCell>
                  <TableCell align="right">{p.eficienciaPorcentaje !== null ? `${p.eficienciaPorcentaje.toFixed(1)}%` : "s/d"}</TableCell>
                </TableRow>
              ))}
              {datosOrdenados.length === 0 && <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin datos en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteHoras() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const { opcionesEquipo } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-horas", desde, hasta, idEquipo],
    queryFn: () => obtenerReporteHorasRegistradas(desde, hasta, idEquipo === "" ? null : idEquipo),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Equipo" value={idEquipo} onChange={(v) => setIdEquipo(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesEquipo} />
        <BotonExportar ruta="horas-registradas" filtro={{ desde, hasta, idEquipo: idEquipo || null }} nombreArchivo="HorasRegistradas.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Usuario</TableCell><TableCell align="right">Minutos totales</TableCell><TableCell align="right">Dias con registro</TableCell><TableCell align="right">Dias con ausencia</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.usuarios.map((u) => (
                <TableRow key={u.idUsuario} hover>
                  <TableCell>{u.usuario}</TableCell>
                  <TableCell align="right">{u.minutosTotales}</TableCell>
                  <TableCell align="right">{u.dias.length}</TableCell>
                  <TableCell align="right">{u.dias.filter((d) => d.esAusencia).length}</TableCell>
                </TableRow>
              ))}
              {consulta.data.usuarios.length === 0 && <TableRow><TableCell colSpan={4}><Typography color="text.secondary">Sin registros en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteRetrabajo() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-retrabajo", desde, hasta, idProyecto],
    queryFn: () => obtenerReporteRetrabajo(desde, hasta, idProyecto === "" ? null : idProyecto),
  });
  const { datosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(consulta.data?.detalle, "porcentaje");

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="retrabajo" filtro={{ desde, hasta, idProyecto: idProyecto || null }} nombreArchivo="Retrabajo.xlsx" />
        {consulta.data?.reaperturasSinDatos && <Chip size="small" label="Reaperturas: sin dato" />}
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Usuario</TableCell><TableCell>Proyecto</TableCell>
                <EncabezadoOrdenable clave="porcentaje" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">% retrabajo</EncabezadoOrdenable>
                <TableCell align="right">Min. correccion</TableCell><TableCell align="right">Min. totales</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {datosOrdenados.map((d, i) => (
                <TableRow key={i} hover>
                  <TableCell>{d.usuario}</TableCell><TableCell>{d.proyecto}</TableCell>
                  <TableCell align="right">{d.porcentaje.toFixed(1)}%</TableCell>
                  <TableCell align="right">{d.minutosCorreccion}</TableCell><TableCell align="right">{d.minutosTotales}</TableCell>
                </TableRow>
              ))}
              {datosOrdenados.length === 0 && <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin datos en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteBugs() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-bugs", desde, hasta, idProyecto],
    queryFn: () => obtenerReporteBugsDefectos(desde, hasta, idProyecto === "" ? null : idProyecto),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="bugs-defectos" filtro={{ desde, hasta, idProyecto: idProyecto || null }} nombreArchivo="BugsDefectos.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Proyecto</TableCell><TableCell align="right">Bugs</TableCell><TableCell align="right">Densidad %</TableCell><TableCell align="right">Aging (dias)</TableCell><TableCell align="right">Escapados</TableCell><TableCell align="right">Tasa escape %</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.proyectos.map((p) => (
                <TableRow key={p.idProyecto} hover>
                  <TableCell>{p.proyecto}</TableCell><TableCell align="right">{p.totalBugs}</TableCell>
                  <TableCell align="right">{p.densidadPorcentaje.toFixed(1)}%</TableCell><TableCell align="right">{p.agingPromedioDias.toFixed(1)}</TableCell>
                  <TableCell align="right">{p.escapados}</TableCell><TableCell align="right">{p.tasaEscapePorcentaje.toFixed(1)}%</TableCell>
                </TableRow>
              ))}
              {consulta.data.proyectos.length === 0 && <TableRow><TableCell colSpan={6}><Typography color="text.secondary">Sin bugs en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteReleases() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-releases", desde, hasta, idProyecto],
    queryFn: () => obtenerReporteReleases(desde, hasta, idProyecto === "" ? null : idProyecto),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="releases" filtro={{ desde, hasta, idProyecto: idProyecto || null }} nombreArchivo="Releases.xlsx" />
        {consulta.data && <Chip size="small" label={`${consulta.data.totalReleases} releases - ${consulta.data.frecuenciaPorSemana}/semana`} />}
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Version</TableCell><TableCell>Proyecto</TableCell><TableCell>Fecha liberacion</TableCell><TableCell align="right">Dias aprobacion</TableCell><TableCell align="right">Items</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.releases.map((r) => (
                <TableRow key={r.idRelease} hover>
                  <TableCell>{r.folio ?? r.version}</TableCell><TableCell>{r.proyecto}</TableCell>
                  <TableCell>{r.fechaLiberacion ? new Date(r.fechaLiberacion).toLocaleDateString() : "Sin liberar"}</TableCell>
                  <TableCell align="right">{r.diasAprobacion ?? "s/d"}</TableCell><TableCell align="right">{r.itemsIncluidos}</TableCell>
                </TableRow>
              ))}
              {consulta.data.releases.length === 0 && <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin releases en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteRiesgos() {
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-riesgos", idProyecto],
    queryFn: () => obtenerReporteRiesgos(idProyecto === "" ? null : idProyecto),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="riesgos" filtro={{ idProyecto: idProyecto || null }} nombreArchivo="Riesgos.xlsx" />
        {consulta.data && (
          <Stack direction="row" spacing={1}>
            <Chip size="small" color="warning" label={`Expuestos: ${consulta.data.totalExpuestos}`} />
            <Chip size="small" color="info" label={`En mitigacion: ${consulta.data.totalMitigados}`} />
            <Chip size="small" color="error" label={`Materializados: ${consulta.data.totalMaterializados}`} />
            <Chip size="small" label={`Cerrados: ${consulta.data.totalCerrados}`} />
          </Stack>
        )}
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Proyecto</TableCell><TableCell>Descripcion</TableCell><TableCell align="right">Exposicion</TableCell><TableCell>Estatus</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.riesgos.map((r) => (
                <TableRow key={r.idRiesgo} hover>
                  <TableCell>{r.proyecto}</TableCell><TableCell>{r.descripcion}</TableCell>
                  <TableCell align="right">{r.exposicion}</TableCell><TableCell>{r.estatus}</TableCell>
                </TableRow>
              ))}
              {consulta.data.riesgos.length === 0 && <TableRow><TableCell colSpan={4}><Typography color="text.secondary">Sin riesgos capturados.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteSolicitantes() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const consulta = useQuery({
    queryKey: ["reporte-solicitantes", desde, hasta],
    queryFn: () => obtenerReporteSolicitantes(desde, hasta),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <BotonExportar ruta="solicitantes" filtro={{ desde, hasta }} nombreArchivo="Solicitantes.xlsx" />
        {consulta.data?.satisfaccionSinDatos && <Chip size="small" label="Satisfaccion: sin dato" />}
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Area</TableCell><TableCell align="right">Solicitudes</TableCell><TableCell align="right">Triage (dias)</TableCell><TableCell align="right">Entrega (dias)</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.porArea.map((a, i) => (
                <TableRow key={i} hover>
                  <TableCell>{a.area}</TableCell><TableCell align="right">{a.totalSolicitudes}</TableCell>
                  <TableCell align="right">{a.tiempoTriagePromedioDias ?? "s/d"}</TableCell><TableCell align="right">{a.tiempoEntregaPromedioDias ?? "s/d"}</TableCell>
                </TableRow>
              ))}
              {consulta.data.porArea.length === 0 && <TableRow><TableCell colSpan={4}><Typography color="text.secondary">Sin solicitudes en el periodo.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteCostos() {
  const [anio, setAnio] = useState(new Date().getFullYear());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-costos", anio, idProyecto],
    queryFn: () => obtenerReporteCostos(anio, idProyecto === "" ? null : idProyecto),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <TextField size="small" type="number" label="Anio" value={anio} onChange={(e) => setAnio(Number(e.target.value))} sx={{ width: 100 }} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesProyecto} />
        <BotonExportar ruta="costos" filtro={{ anio, idProyecto: idProyecto || null }} nombreArchivo="Costos.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <Stack spacing={2}>
          <Typography variant="subtitle2">Por proyecto / mes</Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead><TableRow><TableCell>Proyecto</TableCell><TableCell align="right">Mes</TableCell><TableCell align="right">Horas</TableCell><TableCell align="right">Costo</TableCell></TableRow></TableHead>
              <TableBody>
                {consulta.data.porProyectoMes.map((c, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{c.proyecto}</TableCell><TableCell align="right">{c.mes}</TableCell>
                    <TableCell align="right">{c.horasReales}</TableCell><TableCell align="right">${c.costoReal.toLocaleString()}</TableCell>
                  </TableRow>
                ))}
                {consulta.data.porProyectoMes.length === 0 && <TableRow><TableCell colSpan={4}><Typography color="text.secondary">Sin costo registrado en el anio.</Typography></TableCell></TableRow>}
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="subtitle2">Por desarrollador</Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead><TableRow><TableCell>Usuario</TableCell><TableCell align="right">Horas</TableCell><TableCell align="right">Costo</TableCell></TableRow></TableHead>
              <TableBody>
                {consulta.data.porDesarrollador.map((d) => (
                  <TableRow key={d.idUsuario} hover>
                    <TableCell>{d.usuario}</TableCell><TableCell align="right">{d.horasReales}</TableCell><TableCell align="right">${d.costoReal.toLocaleString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      )}
    </Stack>
  );
}

function ReporteRentabilidad() {
  const [anio, setAnio] = useState(new Date().getFullYear());
  const consulta = useQuery({ queryKey: ["reporte-rentabilidad", anio], queryFn: () => obtenerReporteRentabilidad(anio) });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <TextField size="small" type="number" label="Anio" value={anio} onChange={(e) => setAnio(Number(e.target.value))} sx={{ width: 100 }} />
        <BotonExportar ruta="rentabilidad" filtro={{ anio }} nombreArchivo="Rentabilidad.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Proyecto</TableCell><TableCell align="right">Presupuesto</TableCell><TableCell align="right">Costo real</TableCell><TableCell align="right">% consumido</TableCell><TableCell>Semaforo</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.proyectos.map((p) => (
                <TableRow key={p.idProyecto} hover>
                  <TableCell>{p.proyecto}</TableCell>
                  <TableCell align="right">{p.montoAutorizado !== null ? `$${p.montoAutorizado.toLocaleString()}` : "s/d"}</TableCell>
                  <TableCell align="right">${p.costoReal.toLocaleString()}</TableCell>
                  <TableCell align="right">{p.porcentajeConsumido !== null ? `${p.porcentajeConsumido.toFixed(1)}%` : "s/d"}</TableCell>
                  <TableCell><Chip size="small" label={p.semaforo} color={p.semaforo === "Verde" ? "success" : p.semaforo === "Naranja" ? "warning" : p.semaforo === "Rojo" ? "error" : "default"} /></TableCell>
                </TableRow>
              ))}
              {consulta.data.proyectos.length === 0 && <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin costo/presupuesto en el anio.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteSla() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const consulta = useQuery({ queryKey: ["reporte-sla", desde, hasta], queryFn: () => obtenerReporteSla(desde, hasta) });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <BotonExportar ruta="sla" filtro={{ desde, hasta }} nombreArchivo="SLA.xlsx" />
        {consulta.data && <Chip size="small" label={`CSAT: ${consulta.data.csat ?? "s/d"}`} />}
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <Stack spacing={2}>
          <Typography variant="subtitle2">Por prioridad</Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead><TableRow><TableCell>Prioridad</TableCell><TableCell align="right">Total</TableCell><TableCell align="right">Dentro SLA</TableCell><TableCell align="right">% cumplimiento</TableCell></TableRow></TableHead>
              <TableBody>
                {consulta.data.porPrioridad.map((p, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{p.prioridad}</TableCell><TableCell align="right">{p.totalTickets}</TableCell>
                    <TableCell align="right">{p.dentroSla}</TableCell><TableCell align="right">{p.cumplimientoPorcentaje.toFixed(1)}%</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="subtitle2">Por agente</Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead><TableRow><TableCell>Agente</TableCell><TableCell align="right">Total</TableCell><TableCell align="right">% cumplimiento</TableCell><TableCell align="right">Incumplimientos</TableCell></TableRow></TableHead>
              <TableBody>
                {consulta.data.porAgente.map((a) => (
                  <TableRow key={a.idUsuario} hover>
                    <TableCell>{a.usuario}</TableCell><TableCell align="right">{a.totalTickets}</TableCell>
                    <TableCell align="right">{a.cumplimientoPorcentaje.toFixed(1)}%</TableCell><TableCell align="right">{a.incumplimientos}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      )}
    </Stack>
  );
}

function ReporteKpis() {
  const colorSerie = useColorSerie();
  const [anio, setAnio] = useState(new Date().getFullYear());
  const [anioComparativo, setAnioComparativo] = useState<number | "">(anio - 1);
  const consulta = useQuery({
    queryKey: ["reporte-kpis", anio, anioComparativo],
    queryFn: () => obtenerReporteKpisHistoricos(anio, anioComparativo === "" ? null : anioComparativo),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <TextField size="small" type="number" label="Anio" value={anio} onChange={(e) => setAnio(Number(e.target.value))} sx={{ width: 100 }} />
        <TextField size="small" type="number" label="Comparar contra" value={anioComparativo} onChange={(e) => setAnioComparativo(e.target.value === "" ? "" : Number(e.target.value))} sx={{ width: 130 }} />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && consulta.data.kpis.length === 0 && <Typography color="text.secondary">Sin KPIs personalizados definidos todavia.</Typography>}
      {consulta.data && consulta.data.kpis.map((kpi) => {
        const combinado = kpi.serieAnioActual.map((p, i) => ({
          fecha: p.fecha, actual: p.valor, comparativo: kpi.serieAnioComparativo[i]?.valor ?? null,
        }));
        return (
          <Paper key={kpi.clave} variant="outlined" sx={{ p: 1.5 }}>
            <Typography variant="subtitle2">{kpi.nombre}</Typography>
            <Box sx={{ height: 220 }}>
              <ResponsiveContainer>
                <LineChart data={combinado}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="fecha" tick={{ fontSize: 10 }} />
                  <YAxis tick={{ fontSize: 10 }} />
                  <RechartsTooltip />
                  <Legend />
                  <Line type="monotone" dataKey="actual" name={String(anio)} stroke={colorSerie("#334155")} dot={false} />
                  {anioComparativo !== "" && <Line type="monotone" dataKey="comparativo" name={String(anioComparativo)} stroke="#94a3b8" strokeDasharray="4 4" dot={false} />}
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
        );
      })}
    </Stack>
  );
}

function ReporteCarga() {
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const { opcionesEquipo } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-carga", idEquipo],
    queryFn: () => obtenerReporteCargaTrabajo(idEquipo === "" ? null : idEquipo),
  });
  const { datosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(consulta.data?.personas, "wip");

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <ComboBuscable label="Equipo" value={idEquipo} onChange={(v) => setIdEquipo(v as number | "")} sx={{ minWidth: 220 }} opciones={opcionesEquipo} />
        <BotonExportar ruta="carga-trabajo" filtro={{ idEquipo: idEquipo || null }} nombreArchivo="CargaTrabajo.xlsx" />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Usuario</TableCell>
                <EncabezadoOrdenable clave="wip" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar} align="right">WIP</EncabezadoOrdenable>
                <TableCell align="right">% ocupacion</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {datosOrdenados.map((p) => (
                <TableRow key={p.idUsuario} hover>
                  <TableCell>{p.usuario}</TableCell><TableCell align="right">{p.wip}</TableCell>
                  <TableCell align="right">{p.porcentajeOcupacion !== null ? `${p.porcentajeOcupacion.toFixed(1)}%` : "s/d"}</TableCell>
                </TableRow>
              ))}
              {datosOrdenados.length === 0 && <TableRow><TableCell colSpan={3}><Typography color="text.secondary">Sin elementos en proceso.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
}

function ReporteFlujo() {
  const colorSerie = useColorSerie();
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const { opcionesProyecto } = useProyectosEquipos();
  const consulta = useQuery({
    queryKey: ["reporte-flujo", desde, hasta, idProyecto],
    queryFn: () => obtenerReporteFlujo(desde, hasta, idProyecto as number),
    enabled: idProyecto !== "",
  });

  const datos = consulta.data?.puntos.map((p) => ({ fecha: p.fecha, ...p.conteoPorEstatus })) ?? [];

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <ComboBuscable label="Proyecto (obligatorio)" value={idProyecto} onChange={(v) => setIdProyecto(v as number | "")} sx={{ minWidth: 260 }}
          opciones={opcionesProyecto.filter((o) => o.valor !== "")} />
      </Stack>
      {idProyecto === "" && <Typography color="text.secondary">Selecciona un proyecto para ver su diagrama de flujo acumulado.</Typography>}
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <Paper variant="outlined" sx={{ p: 1.5 }}>
          <Box sx={{ height: 320 }}>
            <ResponsiveContainer>
              <AreaChart data={datos}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="fecha" tick={{ fontSize: 10 }} />
                <YAxis tick={{ fontSize: 10 }} />
                <RechartsTooltip />
                <Legend />
                {consulta.data.estatus.map((e, i) => (
                  <Area key={e} type="monotone" dataKey={e} stackId="1"
                    stroke={colorSerie(["#0f766e", "#334155", "#f59e0b", "#ef4444", "#94a3b8", "#8b5cf6", "#22c55e"][i % 7])}
                    fill={colorSerie(["#0f766e", "#334155", "#f59e0b", "#ef4444", "#94a3b8", "#8b5cf6", "#22c55e"][i % 7])} />
                ))}
              </AreaChart>
            </ResponsiveContainer>
          </Box>
        </Paper>
      )}
    </Stack>
  );
}

function ReporteAuditoria() {
  const [desde, setDesde] = useState<string>("");
  const [hasta, setHasta] = useState<string>("");
  const [usuario, setUsuario] = useState("");
  const [entidad, setEntidad] = useState("");
  const [page, setPage] = useState(1);
  const consulta = useQuery({
    queryKey: ["reporte-auditoria", desde, hasta, usuario, entidad, page],
    queryFn: () => obtenerReporteAuditoria({
      desde: desde || null, hasta: hasta || null, usuario: usuario || null, entidad: entidad || null, page, pageSize: 50,
    }),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center" }}>
        <BarraFechas desde={desde} hasta={hasta} onDesde={setDesde} onHasta={setHasta} />
        <TextField size="small" label="Usuario" value={usuario} onChange={(e) => setUsuario(e.target.value)} sx={{ width: 160 }} />
        <TextField size="small" label="Entidad" value={entidad} onChange={(e) => setEntidad(e.target.value)} sx={{ width: 160 }} />
      </Stack>
      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead><TableRow><TableCell>Fecha</TableCell><TableCell>Usuario</TableCell><TableCell>Entidad</TableCell><TableCell>Accion</TableCell><TableCell>Detalle</TableCell></TableRow></TableHead>
            <TableBody>
              {consulta.data.items.map((a) => (
                <TableRow key={a.idBitacora} hover>
                  <TableCell>{new Date(a.fecha).toLocaleString()}</TableCell><TableCell>{a.usuario}</TableCell>
                  <TableCell>{a.entidad ?? "-"}{a.idEntidad ? ` #${a.idEntidad}` : ""}</TableCell>
                  <TableCell>{a.accion}</TableCell><TableCell>{a.detalle ?? "-"}</TableCell>
                </TableRow>
              ))}
              {consulta.data.items.length === 0 && <TableRow><TableCell colSpan={5}><Typography color="text.secondary">Sin movimientos.</Typography></TableCell></TableRow>}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      {consulta.data && consulta.data.totalPages > 1 && (
        <Stack direction="row" spacing={1}>
          <Button size="small" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Anterior</Button>
          <Typography variant="body2" sx={{ alignSelf: "center" }}>Pagina {page} de {consulta.data.totalPages}</Typography>
          <Button size="small" disabled={page >= consulta.data.totalPages} onClick={() => setPage((p) => p + 1)}>Siguiente</Button>
        </Stack>
      )}
    </Stack>
  );
}

/**
 * R15: detalle renglon por renglon de lo terminado en el periodo. A diferencia de R01-R03,
 * que agregan por persona o proyecto, este es el que se entrega como evidencia de trabajo.
 */
function ReporteActividadesTerminadas() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [folio, setFolio] = useState("");
  const [pagina, setPagina] = useState(0);
  const [expandido, setExpandido] = useState<number | null>(null);

  const { opcionesProyecto, opcionesEquipo } = useProyectosEquipos();
  const catalogos = useQuery({ queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000 });
  const opcionesUsuario = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))],
    [catalogos.data],
  );
  const opcionesTipo = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.tipos ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre }))],
    [catalogos.data],
  );

  const filtro = {
    desde, hasta,
    idEquipo: idEquipo === "" ? null : idEquipo,
    idAsignado: idAsignado === "" ? null : idAsignado,
    idProyecto: idProyecto === "" ? null : idProyecto,
    idTipoWorkItem: idTipo === "" ? null : idTipo,
    folio: folio.trim() === "" ? null : folio.trim(),
  };

  const consulta = useQuery({
    queryKey: ["reporte-actividades-terminadas", filtro],
    queryFn: () => obtenerReporteActividadesTerminadas(filtro),
  });

  const items = consulta.data?.items ?? [];
  const totales = consulta.data?.totales;
  const visibles = items.slice(pagina * FILAS_POR_PAGINA, (pagina + 1) * FILAS_POR_PAGINA);

  function aplicarAtajo(atajo: "hoy" | "semana" | "mes" | "anio") {
    const hoy = new Date();
    setPagina(0);
    if (atajo === "hoy") { setDesde(hoyIso()); setHasta(hoyIso()); return; }
    if (atajo === "semana") { setDesde(lunesDeEstaSemana()); setHasta(hoyIso()); return; }
    if (atajo === "mes") { setDesde(primerDiaMes()); setHasta(hoyIso()); return; }
    setDesde(`${hoy.getFullYear()}-01-01`);
    setHasta(hoyIso());
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center", gap: 1 }}>
        <BarraFechas desde={desde} hasta={hasta}
          onDesde={(v) => { setDesde(v); setPagina(0); }} onHasta={(v) => { setHasta(v); setPagina(0); }} />
        <Stack direction="row" spacing={0.5}>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("hoy")}>Hoy</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("semana")}>Semana</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("mes")}>Mes</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("anio")}>Año</Button>
        </Stack>
        <ComboBuscable label="Equipo" value={idEquipo} onChange={(v) => { setIdEquipo(v as number | ""); setPagina(0); }}
          sx={{ minWidth: 180 }} opciones={opcionesEquipo} />
        <ComboBuscable label="Asignado" value={idAsignado} onChange={(v) => { setIdAsignado(v as number | ""); setPagina(0); }}
          sx={{ minWidth: 200 }} opciones={opcionesUsuario} />
        <ComboBuscable label="Proyecto" value={idProyecto} onChange={(v) => { setIdProyecto(v as number | ""); setPagina(0); }}
          sx={{ minWidth: 200 }} opciones={opcionesProyecto} />
        <ComboBuscable label="Tipo" value={idTipo} onChange={(v) => { setIdTipo(v as number | ""); setPagina(0); }}
          sx={{ minWidth: 160 }} opciones={opcionesTipo} />
        <TextField size="small" label="Folio" value={folio} sx={{ width: 150 }}
          onChange={(e) => { setFolio(e.target.value); setPagina(0); }} />
        <BotonExportar ruta="actividades-terminadas" filtro={filtro} nombreArchivo="ActividadesTerminadas.xlsx" />
      </Stack>

      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {consulta.data?.truncado && (
        <Alert severity="warning">
          El rango supera el tope de {TOPE_RENGLONES.toLocaleString()} actividades y la lista viene recortada.
          Acota el rango de fechas o filtra por equipo para ver el detalle completo.
        </Alert>
      )}

      {totales && (
        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
          <Chip size="small" label={`Actividades: ${totales.items}`} />
          <Chip size="small" label={`En proceso: ${formatearMinutos(totales.minutosInvertidos)}`} />
          <Chip size="small" label={`Registrado: ${formatearMinutos(totales.minutosRegistrados)}`} />
          <Chip size="small" color={totales.diferenciaMinutos < 0 ? "error" : "default"}
            label={`Diferencia: ${formatearDiferencia(totales.diferenciaMinutos)}`} />
          <Chip size="small"
            label={`Con tiempo capturado: ${totales.itemsConRegistro} de ${totales.items}`} />
          <Chip size="small" label={`Resolucion prom.: ${totales.promedioDiasNaturalesResolucion ?? "-"} d nat. / ${formatearMinutos(totales.promedioMinutosLaboralesResolucion)} habiles`} />
          <Chip size="small" label={`Espera prom.: ${totales.promedioDiasNaturalesEspera ?? "-"} d nat. / ${formatearMinutos(totales.promedioMinutosLaboralesEspera)} habiles`} />
          {totales.porcentajeATiempo !== null && <Chip size="small" label={`A tiempo: ${totales.porcentajeATiempo}%`} />}
        </Stack>
      )}

      {consulta.data && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Folio</TableCell>
                <TableCell>Tipo</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Asignado</TableCell>
                <TableCell>Equipo</TableCell>
                <Tooltip title="Tiempo habil que la tarea estuvo en estatus En Proceso">
                  <TableCell align="right">En proceso</TableCell>
                </Tooltip>
                <Tooltip title="Tiempo que la persona capturo a mano en registros de tiempo">
                  <TableCell align="right">Registrado</TableCell>
                </Tooltip>
                <Tooltip title="Registrado menos en proceso. Negativo = se capturo de menos">
                  <TableCell align="right">Dif.</TableCell>
                </Tooltip>
                <TableCell>Inicio</TableCell>
                <TableCell>Fin</TableCell>
                <TableCell>Compromiso</TableCell>
                <TableCell align="right">Espera</TableCell>
                <TableCell align="right">Resolucion</TableCell>
                <TableCell align="center">A tiempo</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {visibles.map((a) => (
                <Fragment key={a.idWorkItem}>
                  <TableRow hover sx={{ cursor: "pointer" }}
                    onClick={() => setExpandido(expandido === a.idWorkItem ? null : a.idWorkItem)}>
                    <TableCell>{a.folio}</TableCell>
                    <TableCell>{a.tipo}</TableCell>
                    <TableCell sx={{ maxWidth: 320 }}>{a.titulo}</TableCell>
                    <TableCell>{a.asignado ?? "-"}</TableCell>
                    <TableCell>{a.equipo ?? "-"}</TableCell>
                    <TableCell align="right">{formatearMinutos(a.minutosInvertidos)}</TableCell>
                    <TableCell align="right"
                      sx={{ color: a.minutosRegistrados === 0 ? "text.disabled" : undefined }}>
                      {formatearMinutos(a.minutosRegistrados)}
                    </TableCell>
                    <TableCell align="right" sx={{ color: colorDiferencia(a.diferenciaMinutos) }}>
                      {formatearDiferencia(a.diferenciaMinutos)}
                    </TableCell>
                    <TableCell>{a.fechaInicio ? new Date(a.fechaInicio).toLocaleDateString() : "-"}</TableCell>
                    <TableCell>{a.fechaFin ? new Date(a.fechaFin).toLocaleDateString() : "-"}</TableCell>
                    <TableCell sx={{ color: a.entregadoATiempo === false ? "error.main" : undefined }}>
                      {a.fechaCompromiso ? new Date(a.fechaCompromiso).toLocaleDateString() : "-"}
                    </TableCell>
                    <TableCell align="right">{formatearDuracion(a.diasNaturalesEspera, a.minutosLaboralesEspera)}</TableCell>
                    <TableCell align="right">{formatearDuracion(a.diasNaturalesResolucion, a.minutosLaboralesResolucion)}</TableCell>
                    <TableCell align="center">
                      {a.entregadoATiempo === null
                        ? "-"
                        : <Chip size="small" color={a.entregadoATiempo ? "success" : "error"}
                            label={a.entregadoATiempo ? "Si" : "No"} />}
                    </TableCell>
                  </TableRow>
                  {expandido === a.idWorkItem && (
                    <TableRow>
                      <TableCell colSpan={14} sx={{ bgcolor: "action.hover" }}>
                        <Stack spacing={0.5}>
                          <Typography variant="body2">
                            <b>Proyecto:</b> {a.proyecto} &nbsp;·&nbsp; <b>Prioridad:</b> {a.prioridad}
                            &nbsp;·&nbsp; <b>Sprint:</b> {a.sprint ?? "-"} &nbsp;·&nbsp; <b>Release:</b> {a.release ?? "-"}
                          </Typography>
                          <Typography variant="body2">
                            <b>Creada:</b> {new Date(a.fechaCreacion).toLocaleString()}
                          </Typography>
                          <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
                            {a.descripcion ? htmlATextoPlano(a.descripcion) : "Sin descripcion."}
                          </Typography>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  )}
                </Fragment>
              ))}
              {items.length === 0 && (
                <TableRow><TableCell colSpan={14}>
                  <Typography color="text.secondary">Sin actividades terminadas con esos filtros.</Typography>
                </TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {items.length > FILAS_POR_PAGINA && (
        <Stack direction="row" spacing={1}>
          <Button size="small" disabled={pagina === 0} onClick={() => setPagina((p) => p - 1)}>Anterior</Button>
          <Typography variant="body2" sx={{ alignSelf: "center" }}>
            Pagina {pagina + 1} de {Math.ceil(items.length / FILAS_POR_PAGINA)}
          </Typography>
          <Button size="small" disabled={(pagina + 1) * FILAS_POR_PAGINA >= items.length}
            onClick={() => setPagina((p) => p + 1)}>Siguiente</Button>
        </Stack>
      )}

      {consulta.data && (
        <SeccionTickets tickets={consulta.data.tickets} totales={consulta.data.totalesTickets}
          avisos={consulta.data.avisosTickets} />
      )}
      {consulta.data && (
        <SeccionIncidentes incidentes={consulta.data.incidentes} totales={consulta.data.totalesIncidentes}
          avisos={consulta.data.avisosIncidentes} />
      )}
    </Stack>
  );
}

/** Encabezado comun de las secciones de soporte/operacion del R15. */
function TituloSeccion({ texto, chips, avisos }: {
  texto: string; chips: string[]; avisos: string[];
}) {
  return (
    <Stack spacing={1} sx={{ mt: 1 }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{texto}</Typography>
      {avisos.map((aviso) => <Alert key={aviso} severity="info">{aviso}</Alert>)}
      {avisos.length === 0 && (
        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
          {chips.map((chip) => <Chip key={chip} size="small" label={chip} />)}
        </Stack>
      )}
    </Stack>
  );
}

function SeccionTickets({ tickets, totales, avisos }: {
  tickets: TicketTerminado[]; totales: TicketsTerminadosTotales; avisos: string[];
}) {
  return (
    <>
      <TituloSeccion texto="Tickets resueltos o cerrados" avisos={avisos}
        chips={[
          `Tickets: ${totales.items}`,
          `En atencion: ${formatearMinutos(totales.minutosEnAtencion)}`,
          `Resolucion prom.: ${totales.promedioDiasNaturalesResolucion ?? "-"} d nat.`,
          ...(totales.porcentajeDentroDeSla !== null ? [`Dentro de SLA: ${totales.porcentajeDentroDeSla}%`] : []),
        ]} />
      {avisos.length === 0 && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Folio</TableCell>
                <TableCell>Categoria</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Solicitante</TableCell>
                <TableCell>Asignado</TableCell>
                <TableCell align="right">En atencion</TableCell>
                <TableCell>Resolucion</TableCell>
                <TableCell align="right">Espera</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell align="center">SLA</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {tickets.map((t) => (
                <TableRow key={t.idTicket} hover>
                  <TableCell>{t.folio ?? "-"}</TableCell>
                  <TableCell>{t.categoria ?? "-"}</TableCell>
                  <TableCell sx={{ maxWidth: 320 }}>{t.titulo}</TableCell>
                  <TableCell>{t.solicitante}</TableCell>
                  <TableCell>{t.asignado ?? "-"}</TableCell>
                  <TableCell align="right">{formatearMinutos(t.minutosEnAtencion)}</TableCell>
                  <TableCell>{t.fechaResolucion ? new Date(t.fechaResolucion).toLocaleDateString() : "-"}</TableCell>
                  <TableCell align="right">{t.diasNaturalesEspera ?? "-"}</TableCell>
                  <TableCell align="right">{t.diasNaturalesResolucion ?? "-"}</TableCell>
                  <TableCell align="center">
                    {t.dentroDeSla === null
                      ? "-"
                      : <Chip size="small" color={t.dentroDeSla ? "success" : "error"}
                          label={t.dentroDeSla ? "Si" : "No"} />}
                  </TableCell>
                </TableRow>
              ))}
              {tickets.length === 0 && (
                <TableRow><TableCell colSpan={10}>
                  <Typography color="text.secondary">Sin tickets resueltos con esos filtros.</Typography>
                </TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}

function SeccionIncidentes({ incidentes, totales, avisos }: {
  incidentes: IncidenteTerminado[]; totales: IncidentesTerminadosTotales; avisos: string[];
}) {
  return (
    <>
      <TituloSeccion texto="Incidentes resueltos o cerrados" avisos={avisos}
        chips={[
          `Incidentes: ${totales.items}`,
          `En atencion: ${formatearMinutos(totales.minutosEnAtencion)}`,
          `Indisponibilidad: ${formatearMinutos(totales.minutosIndisponibilidad)}`,
          `Resolucion prom.: ${totales.promedioDiasNaturalesResolucion ?? "-"} d nat.`,
        ]} />
      {avisos.length === 0 && (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Folio</TableCell>
                <TableCell>Severidad</TableCell>
                <TableCell>Titulo</TableCell>
                <TableCell>Proyecto</TableCell>
                <TableCell align="right">En atencion</TableCell>
                <TableCell align="right">Indisponible</TableCell>
                <TableCell>Ocurrencia</TableCell>
                <TableCell>Resolucion</TableCell>
                <TableCell align="right">Deteccion</TableCell>
                <TableCell align="right">Total</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {incidentes.map((i) => (
                <TableRow key={i.idIncidente} hover>
                  <TableCell>{i.folio ?? "-"}</TableCell>
                  <TableCell>{i.severidad}</TableCell>
                  <TableCell sx={{ maxWidth: 320 }}>{i.titulo}</TableCell>
                  <TableCell>{i.proyecto}</TableCell>
                  <TableCell align="right">{formatearMinutos(i.minutosEnAtencion)}</TableCell>
                  <TableCell align="right">{formatearMinutos(i.minutosIndisponibilidad)}</TableCell>
                  <TableCell>{new Date(i.fechaOcurrencia).toLocaleDateString()}</TableCell>
                  <TableCell>{i.fechaResolucion ? new Date(i.fechaResolucion).toLocaleDateString() : "-"}</TableCell>
                  <TableCell align="right">{i.diasNaturalesDeteccion ?? "-"}</TableCell>
                  <TableCell align="right">{i.diasNaturalesResolucion ?? "-"}</TableCell>
                </TableRow>
              ))}
              {incidentes.length === 0 && (
                <TableRow><TableCell colSpan={10}>
                  <Typography color="text.secondary">Sin incidentes resueltos con esos filtros.</Typography>
                </TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}

const FILAS_POR_PAGINA = 50;

/** Espejo de ReportesQueryService.TopeRenglonesDetalle: solo para el texto de la alerta. */
const TOPE_RENGLONES = 5000;

/**
 * Lunes de la semana en curso. Se toma el lunes (no el domingo) porque el atajo sirve para
 * ver la semana laboral, y getDay() devuelve 0 en domingo: ese caso retrocede 6 dias, no 0.
 */
function lunesDeEstaSemana(): string {
  const hoy = new Date();
  const diaSemana = hoy.getDay();
  const diasAlLunes = diaSemana === 0 ? 6 : diaSemana - 1;
  const lunes = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() - diasAlLunes);
  const mes = String(lunes.getMonth() + 1).padStart(2, "0");
  const dia = String(lunes.getDate()).padStart(2, "0");
  return `${lunes.getFullYear()}-${mes}-${dia}`;
}

/** La diferencia se lee mejor con signo: "+2h" o "-45m". */
function formatearDiferencia(minutos: number): string {
  if (minutos === 0) return "0";
  return `${minutos > 0 ? "+" : "-"}${formatearMinutos(Math.abs(minutos))}`;
}

/**
 * Solo se pinta en rojo el caso accionable: se registro MENOS tiempo del que la tarea estuvo
 * En Proceso, que casi siempre significa captura faltante. Registrar de mas es raro pero no
 * es un error (trabajo hecho sin mover el estatus), asi que va neutro.
 */
function colorDiferencia(minutos: number): string | undefined {
  if (minutos < 0) return "error.main";
  return minutos > 0 ? "info.main" : undefined;
}

/** "3.5 d / 12h habiles"; el tiempo habil falta cuando el asignado no tiene horario configurado. */
function formatearDuracion(diasNaturales: number | null, minutosLaborales: number | null): string {
  if (diasNaturales === null) return "-";
  const natural = `${diasNaturales} d`;
  return minutosLaborales === null ? natural : `${natural} / ${formatearMinutos(minutosLaborales)}`;
}

const TAMANOS_PAGINA_GANTT = [25, 50, 100];

const OPCIONES_AGRUPACION: { valor: AgrupacionGantt; etiqueta: string }[] = [
  { valor: "Ninguno", etiqueta: "Sin agrupar" },
  { valor: "Proyecto", etiqueta: "Proyecto" },
  { valor: "Usuario", etiqueta: "Usuario" },
];

/**
 * R16: los tres niveles (proyecto, usuario y periodo) se combinan libremente -- son filtros
 * independientes, no un arbol de "primero elige proyecto". La agrupacion es aparte: cambia
 * como se ordenan y se separan las barras, no que se ve.
 */
function ReporteGantt() {
  const [desde, setDesde] = useState(primerDiaMes());
  const [hasta, setHasta] = useState(hoyIso());
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [agruparPor, setAgruparPor] = useState<AgrupacionGantt>("Proyecto");
  const [pageSize, setPageSize] = useState(TAMANOS_PAGINA_GANTT[0]);
  const [page, setPage] = useState(1);

  const { opcionesProyecto } = useProyectosEquipos();
  const catalogos = useQuery({ queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000 });
  const opcionesUsuario = useMemo(
    () => [{ valor: "", etiqueta: "Todos" }, ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))],
    [catalogos.data],
  );

  const filtroBase = {
    desde, hasta,
    idProyecto: idProyecto === "" ? null : idProyecto,
    idAsignado: idAsignado === "" ? null : idAsignado,
    agruparPor,
  };
  const filtro: FiltroGanttActividades = { ...filtroBase, page, pageSize };

  const consulta = useQuery({
    queryKey: ["reporte-gantt-actividades", filtro],
    queryFn: () => obtenerReporteGanttActividades(filtro),
  });

  // Cualquier cambio de filtro regresa a la pagina 1: quedarse en la 7 de un resultado que
  // ahora tiene 2 paginas deja la pantalla vacia sin explicar por que.
  function alFiltrar<T>(asignar: (valor: T) => void) {
    return (valor: T) => { asignar(valor); setPage(1); };
  }

  function aplicarAtajo(atajo: "semana" | "mes" | "trimestre" | "anio") {
    const hoy = new Date();
    setPage(1);
    if (atajo === "semana") { setDesde(lunesDeEstaSemana()); setHasta(hoyIso()); return; }
    if (atajo === "mes") { setDesde(primerDiaMes()); setHasta(hoyIso()); return; }
    if (atajo === "trimestre") {
      const inicio = new Date(hoy.getFullYear(), hoy.getMonth() - 2, 1);
      setDesde(`${inicio.getFullYear()}-${String(inicio.getMonth() + 1).padStart(2, "0")}-01`);
      setHasta(hoyIso());
      return;
    }
    setDesde(`${hoy.getFullYear()}-01-01`);
    setHasta(hoyIso());
  }

  const pagina = consulta.data?.pagina;
  const totalItems = pagina?.totalItems ?? 0;
  const totalPaginas = pagina?.totalPages ?? 0;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center", gap: 1 }}>
        <BarraFechas desde={desde} hasta={hasta}
          onDesde={alFiltrar(setDesde)} onHasta={alFiltrar(setHasta)} />
        <Stack direction="row" spacing={0.5}>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("semana")}>Semana</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("mes")}>Mes</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("trimestre")}>Trimestre</Button>
          <Button size="small" variant="outlined" onClick={() => aplicarAtajo("anio")}>Año</Button>
        </Stack>
        <ComboBuscable label="Proyecto" value={idProyecto} sx={{ minWidth: { xs: "100%", sm: 220 } }}
          opciones={opcionesProyecto} onChange={alFiltrar((v) => setIdProyecto(v as number | ""))} />
        <ComboBuscable label="Usuario" value={idAsignado} sx={{ minWidth: { xs: "100%", sm: 200 } }}
          opciones={opcionesUsuario} onChange={alFiltrar((v) => setIdAsignado(v as number | ""))} />
        <ComboBuscable label="Agrupar por" value={agruparPor} sx={{ minWidth: { xs: "100%", sm: 160 } }}
          opciones={OPCIONES_AGRUPACION} onChange={alFiltrar((v) => setAgruparPor(v as AgrupacionGantt))} />
        <ComboBuscable label="Renglones" value={pageSize} sx={{ minWidth: { xs: "100%", sm: 120 } }}
          opciones={TAMANOS_PAGINA_GANTT.map((n) => ({ valor: n, etiqueta: String(n) }))}
          onChange={alFiltrar((v) => setPageSize(Number(v) || TAMANOS_PAGINA_GANTT[0]))} />
        <BotonExportar ruta="gantt-actividades" filtro={filtroBase} nombreArchivo="GanttActividades.xlsx" />
      </Stack>

      {consulta.isLoading && <LinearProgress />}
      {consulta.isError && <Alert severity="error">No se pudo cargar el reporte.</Alert>}
      {totalItems > TOPE_RENGLONES && (
        <Alert severity="info">
          El filtro devuelve {totalItems.toLocaleString()} actividades. El diagrama se recorre por
          paginas, pero la exportacion a Excel se corta en {TOPE_RENGLONES.toLocaleString()} renglones:
          acota el periodo o filtra por proyecto o usuario si necesitas el detalle completo.
        </Alert>
      )}

      {consulta.data && (
        <>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1, alignItems: "center" }}>
            <Chip size="small" label={`Actividades: ${totalItems}`} />
            <Chip size="small" color={consulta.data.totalEnProgreso > 0 ? "success" : "default"}
              label={`Sin cerrar: ${consulta.data.totalEnProgreso}`} />
            {totalPaginas > 1 && (
              <Chip size="small" variant="outlined"
                label={`Mostrando ${pagina?.items.length ?? 0} de ${totalItems}`} />
            )}
          </Stack>

          <DiagramaGantt actividades={pagina?.items ?? []} desde={consulta.data.desde}
            hasta={consulta.data.hasta} agruparPor={agruparPor} />

          {totalItems > 0 && <LeyendaGantt />}
        </>
      )}

      {totalPaginas > 1 && (
        <Stack direction="row" spacing={1}>
          <Button size="small" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Anterior</Button>
          <Typography variant="body2" sx={{ alignSelf: "center" }}>Pagina {page} de {totalPaginas}</Typography>
          <Button size="small" disabled={page >= totalPaginas} onClick={() => setPage((p) => p + 1)}>Siguiente</Button>
        </Stack>
      )}
    </Stack>
  );
}

export function CatalogoReportesPage() {
  const { puede } = useSesion();
  const disponibles = REPORTES.filter((r) => puede(r.permiso));
  const [reporteElegido, setReporteElegido] = useState<ClaveReporte | null>(null);
  // Si el reporte elegido ya no esta disponible (o nunca se eligio), cae en el primero
  // disponible -- nunca en uno oculto por permiso, ni siquiera navegando directo a la URL.
  const reporteActivo = disponibles.some((r) => r.clave === reporteElegido)
    ? reporteElegido
    : disponibles[0]?.clave;

  if (disponibles.length === 0) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Reportes</Typography>
        <Alert severity="warning">No tienes permiso para ver esta sección.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ display: "flex", p: 2, gap: 2 }}>
      <Paper variant="outlined" sx={{ width: 240, flexShrink: 0 }}>
        <List dense>
          {disponibles.map((r) => (
            <ListItemButton key={r.clave} selected={reporteActivo === r.clave} onClick={() => setReporteElegido(r.clave)}>
              <ListItemText primary={r.titulo} sx={{ "& .MuiListItemText-primary": { fontSize: 13 } }} />
            </ListItemButton>
          ))}
        </List>
      </Paper>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>
          {REPORTES.find((r) => r.clave === reporteActivo)?.titulo}
        </Typography>
        {reporteActivo === "productividad" && <ReporteProductividad />}
        {reporteActivo === "horas" && <ReporteHoras />}
        {reporteActivo === "retrabajo" && <ReporteRetrabajo />}
        {reporteActivo === "bugs" && <ReporteBugs />}
        {reporteActivo === "releases" && <ReporteReleases />}
        {reporteActivo === "riesgos" && <ReporteRiesgos />}
        {reporteActivo === "solicitantes" && <ReporteSolicitantes />}
        {reporteActivo === "costos" && <ReporteCostos />}
        {reporteActivo === "rentabilidad" && <ReporteRentabilidad />}
        {reporteActivo === "sla" && <ReporteSla />}
        {reporteActivo === "kpis" && <ReporteKpis />}
        {reporteActivo === "carga" && <ReporteCarga />}
        {reporteActivo === "flujo" && <ReporteFlujo />}
        {reporteActivo === "auditoria" && <ReporteAuditoria />}
        {reporteActivo === "actividadesTerminadas" && <ReporteActividadesTerminadas />}
        {reporteActivo === "gantt" && <ReporteGantt />}
      </Box>
    </Box>
  );
}
