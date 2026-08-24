import { useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, LinearProgress, List, ListItemButton, ListItemText,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Typography,
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
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import {
  descargarReporteExcel,
  obtenerReporteBugsDefectos, obtenerReporteCargaTrabajo, obtenerReporteCostos,
  obtenerReporteFlujo, obtenerReporteHorasRegistradas, obtenerReporteKpisHistoricos,
  obtenerReporteProductividad, obtenerReporteRentabilidad, obtenerReporteReleases,
  obtenerReporteRetrabajo, obtenerReporteRiesgos, obtenerReporteSla, obtenerReporteSolicitantes,
  obtenerReporteAuditoria,
} from "../../shared/api/reportes";

type ClaveReporte =
  | "productividad" | "horas" | "retrabajo" | "bugs" | "releases" | "riesgos"
  | "solicitantes" | "costos" | "rentabilidad" | "sla" | "kpis" | "carga" | "flujo" | "auditoria";

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
                  <Line type="monotone" dataKey="actual" name={String(anio)} stroke="#334155" dot={false} />
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
                    stroke={["#0f766e", "#334155", "#f59e0b", "#ef4444", "#94a3b8", "#8b5cf6", "#22c55e"][i % 7]}
                    fill={["#0f766e", "#334155", "#f59e0b", "#ef4444", "#94a3b8", "#8b5cf6", "#22c55e"][i % 7]} />
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
      </Box>
    </Box>
  );
}
