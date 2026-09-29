import { useMemo, useState } from "react";
import { Box, Chip, Paper, Stack, Tooltip, Typography, useTheme } from "@mui/material";
import EventBusyIcon from "@mui/icons-material/EventBusy";
import { colorEstatus } from "../../shared/api/workitems";
import { htmlATextoPlano } from "../../shared/editor/textoPlano";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import type { AgrupacionGantt, GanttActividad } from "../../shared/api/reportes";

/**
 * Gantt propio, sin libreria: son barras absolutas sobre un eje de tiempo en porcentaje.
 * Se eligio asi en vez de meter una dependencia de Gantt (gantt-task-react, frappe-gantt)
 * porque lo unico que hace falta es posicionar un rectangulo por actividad, y una libreria
 * traeria su propio tema, su propio modelo de datos y su propio riesgo de compatibilidad
 * con React 19 a cambio de nada. Todo lo visual sale del tema de MUI, asi que el diagrama
 * sigue al modo claro/oscuro sin codigo extra.
 */

const MS_DIA = 86_400_000;
const ALTO_FILA = 56;
const ALTO_BANDA = 30;
const ALTO_BARRA = 22;
const ANCHO_ETIQUETA = 260;
const ANCHO_ETIQUETA_MOVIL = 132;
const ANCHO_MINIMO_TICK = 40;
const ALTO_MAXIMO = 560;

interface Props {
  actividades: GanttActividad[];
  /** Extremos del eje, en formato yyyy-MM-dd (los mismos que se mandaron al filtro). */
  desde: string;
  hasta: string;
  agruparPor: AgrupacionGantt;
}

/**
 * "2026-09-01" a secas lo interpreta el navegador como UTC y en un huso negativo retrocede
 * al dia anterior; se le pega la medianoche local a proposito. Los datetime del API ya
 * traen hora y se dejan como vienen.
 */
function fechaLocal(iso: string): Date {
  return new Date(iso.length === 10 ? `${iso}T00:00:00` : iso);
}

function finDelDiaLocal(iso: string): Date {
  const fecha = fechaLocal(iso);
  fecha.setHours(23, 59, 59, 999);
  return fecha;
}

function diaMes(fecha: Date): string {
  return `${fecha.getDate()}/${fecha.getMonth() + 1}`;
}

function fechaCorta(fecha: Date): string {
  return fecha.toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
}

interface Tick {
  clave: string;
  posicion: number;
  etiqueta: string;
}

/**
 * Densidad del eje segun el ancho del periodo: dia a dia hasta un mes, semanal hasta unos
 * siete meses y mensual de ahi en adelante. Sin esto, un rango de dos anios pintaria
 * setecientas rayitas y uno de una semana no pintaria ninguna.
 */
function calcularTicks(ejeInicio: Date, ejeFin: Date): Tick[] {
  const total = ejeFin.getTime() - ejeInicio.getTime();
  if (total <= 0) return [];

  const dias = total / MS_DIA;
  const marcas: Date[] = [];
  const cursor = new Date(ejeInicio);
  cursor.setHours(0, 0, 0, 0);

  if (dias <= 31) {
    while (cursor.getTime() <= ejeFin.getTime()) {
      marcas.push(new Date(cursor));
      cursor.setDate(cursor.getDate() + 1);
    }
  } else if (dias <= 220) {
    // Arranca en el primer lunes del rango: las semanas se leen mejor alineadas al dia habil.
    cursor.setDate(cursor.getDate() + ((8 - cursor.getDay()) % 7));
    while (cursor.getTime() <= ejeFin.getTime()) {
      marcas.push(new Date(cursor));
      cursor.setDate(cursor.getDate() + 7);
    }
  } else {
    cursor.setDate(1);
    if (cursor.getTime() < ejeInicio.getTime()) cursor.setMonth(cursor.getMonth() + 1);
    while (cursor.getTime() <= ejeFin.getTime()) {
      marcas.push(new Date(cursor));
      cursor.setMonth(cursor.getMonth() + 1);
    }
  }

  const mensual = dias > 220;
  return marcas.map((marca) => ({
    clave: marca.toISOString(),
    posicion: ((marca.getTime() - ejeInicio.getTime()) / total) * 100,
    etiqueta: mensual
      ? marca.toLocaleDateString(undefined, { month: "short", year: "2-digit" })
      : diaMes(marca),
  }));
}

interface Barra {
  actividad: GanttActividad;
  izquierda: number;
  ancho: number;
  abierta: boolean;
  /** Falso cuando la actividad queda entera fuera del eje: esa fila no dibuja barra. */
  traslapa: boolean;
  recortadaIzquierda: boolean;
  recortadaDerecha: boolean;
}

type Fila =
  | { tipo: "banda"; clave: string; nombre: string; conteo: number }
  | { tipo: "barra"; clave: string; barra: Barra };

export function DiagramaGantt({ actividades, desde, hasta, agruparPor }: Props) {
  const esMovil = useEsMovil();
  // Una sola lectura del reloj por montaje: la linea de "hoy" y el extremo de las barras
  // abiertas tienen que salir del mismo instante, y leerlo en cada render deja el diagrama
  // moviendose solo (ademas de ser una impureza en render).
  const [ahora] = useState(() => Date.now());
  const anchoEtiqueta = esMovil ? ANCHO_ETIQUETA_MOVIL : ANCHO_ETIQUETA;

  const ejeInicio = useMemo(() => fechaLocal(desde), [desde]);
  const ejeFin = useMemo(() => finDelDiaLocal(hasta), [hasta]);
  const ticks = useMemo(() => calcularTicks(ejeInicio, ejeFin), [ejeInicio, ejeFin]);

  const posicionHoy = useMemo(() => {
    const total = ejeFin.getTime() - ejeInicio.getTime();
    if (total <= 0 || ahora < ejeInicio.getTime() || ahora > ejeFin.getTime()) return null;
    return ((ahora - ejeInicio.getTime()) / total) * 100;
  }, [ejeInicio, ejeFin, ahora]);

  const filas = useMemo<Fila[]>(() => {
    const total = ejeFin.getTime() - ejeInicio.getTime();
    if (total <= 0) return [];

    const nombreBanda = (a: GanttActividad) =>
      agruparPor === "Proyecto" ? a.proyecto
        : agruparPor === "Usuario" ? (a.asignado ?? "Sin asignar")
          : null;

    const conteos = new Map<string, number>();
    for (const actividad of actividades) {
      const nombre = nombreBanda(actividad);
      if (nombre !== null) conteos.set(nombre, (conteos.get(nombre) ?? 0) + 1);
    }

    const resultado: Fila[] = [];
    let bandaActual: string | null = null;

    for (const actividad of actividades) {
      const nombre = nombreBanda(actividad);
      if (nombre !== null && nombre !== bandaActual) {
        bandaActual = nombre;
        resultado.push({ tipo: "banda", clave: `banda-${nombre}`, nombre, conteo: conteos.get(nombre) ?? 0 });
      }

      const inicio = fechaLocal(actividad.fechaInicio);
      // Sin fecha de fin la actividad sigue viva: la barra corre hasta hoy (recortada al eje),
      // no hasta el final del rango, que fingiria trabajo que todavia no ocurre.
      const abierta = actividad.fechaFin === null;
      const finCrudo = abierta ? new Date(ahora) : fechaLocal(actividad.fechaFin as string);
      const fin = new Date(Math.max(finCrudo.getTime(), inicio.getTime()));

      const visibleInicio = Math.max(inicio.getTime(), ejeInicio.getTime());
      const visibleFin = Math.min(fin.getTime(), ejeFin.getTime());

      resultado.push({
        tipo: "barra",
        clave: `wi-${actividad.idWorkItem}`,
        barra: {
          actividad,
          izquierda: ((visibleInicio - ejeInicio.getTime()) / total) * 100,
          ancho: Math.max(((visibleFin - visibleInicio) / total) * 100, 0),
          abierta,
          // El ancho minimo de la barra hace visible una actividad de un instante, pero
          // tambien pintaria una astilla enganosa para una que no toca el eje. El API solo
          // devuelve actividades que se traslapan; aun asi el componente no da eso por hecho.
          traslapa: fin.getTime() >= ejeInicio.getTime() && inicio.getTime() <= ejeFin.getTime(),
          recortadaIzquierda: inicio.getTime() < ejeInicio.getTime(),
          recortadaDerecha: fin.getTime() > ejeFin.getTime(),
        },
      });
    }
    return resultado;
  }, [actividades, agruparPor, ejeInicio, ejeFin, ahora]);

  if (actividades.length === 0) {
    return (
      <Paper variant="outlined" sx={{ p: 4 }}>
        <Stack spacing={1} sx={{ alignItems: "center", textAlign: "center" }}>
          <EventBusyIcon sx={{ fontSize: 40, color: "text.disabled" }} />
          <Typography variant="subtitle1">Sin actividades en el periodo</Typography>
          <Typography variant="body2" color="text.secondary">
            Ningun trabajo iniciado se traslapa con {fechaCorta(ejeInicio)} - {fechaCorta(ejeFin)} con
            los filtros elegidos. Amplia el rango de fechas o quita el filtro de proyecto o de usuario.
          </Typography>
        </Stack>
      </Paper>
    );
  }

  const anchoMinimo = anchoEtiqueta + Math.max(ticks.length * ANCHO_MINIMO_TICK, 360);

  return (
    <Paper variant="outlined" sx={{ maxHeight: ALTO_MAXIMO, overflow: "auto" }}>
      <Box sx={{ minWidth: anchoMinimo, position: "relative" }}>
        {/* Encabezado del eje: se queda pegado arriba al recorrer la lista en vertical. */}
        <Box sx={{
          display: "flex", position: "sticky", top: 0, zIndex: 3,
          bgcolor: "background.paper", borderBottom: 1, borderColor: "divider",
        }}>
          <Box sx={{
            width: anchoEtiqueta, flexShrink: 0, position: "sticky", left: 0, zIndex: 4,
            bgcolor: "background.paper", borderRight: 1, borderColor: "divider", px: 1, py: 0.75,
          }}>
            <Typography variant="caption" sx={{ fontWeight: 700 }}>Actividad</Typography>
          </Box>
          <Box sx={{ flex: 1, position: "relative", height: 30 }}>
            {ticks.map((tick) => (
              <Typography key={tick.clave} variant="caption" color="text.secondary" sx={{
                position: "absolute", left: `${tick.posicion}%`, top: 6, pl: 0.5,
                whiteSpace: "nowrap", fontSize: 11, lineHeight: "18px",
                borderLeft: 1, borderColor: "divider",
              }}>
                {tick.etiqueta}
              </Typography>
            ))}
          </Box>
        </Box>

        {/* Rejilla y linea de hoy: una sola capa para todo el cuerpo, no una por renglon. */}
        <Box sx={{ position: "relative" }}>
          <Box sx={{
            position: "absolute", top: 0, bottom: 0, left: `${anchoEtiqueta}px`, right: 0,
            pointerEvents: "none", zIndex: 0,
          }}>
            {ticks.map((tick) => (
              <Box key={tick.clave} sx={{
                position: "absolute", left: `${tick.posicion}%`, top: 0, bottom: 0,
                width: "1px", bgcolor: "divider", opacity: 0.6,
              }} />
            ))}
            {posicionHoy !== null && (
              <Box sx={{
                position: "absolute", left: `${posicionHoy}%`, top: 0, bottom: 0,
                width: "2px", bgcolor: "error.main", opacity: 0.55,
              }} />
            )}
          </Box>

          {filas.map((fila) => fila.tipo === "banda" ? (
            <Box key={fila.clave} sx={{
              display: "flex", alignItems: "center", height: ALTO_BANDA, position: "relative", zIndex: 1,
              bgcolor: "action.hover", borderBottom: 1, borderColor: "divider",
            }}>
              <Box sx={{
                position: "sticky", left: 0, px: 1, height: "100%", minWidth: anchoEtiqueta,
                display: "flex", alignItems: "center", gap: 1, bgcolor: "action.hover",
              }}>
                <Typography variant="caption" sx={{ fontWeight: 700 }} noWrap>{fila.nombre}</Typography>
                <Typography variant="caption" color="text.secondary">({fila.conteo})</Typography>
              </Box>
            </Box>
          ) : (
            <FilaBarra key={fila.clave} barra={fila.barra} anchoEtiqueta={anchoEtiqueta} ahora={ahora} />
          ))}
        </Box>
      </Box>
    </Paper>
  );
}

function FilaBarra({ barra, anchoEtiqueta, ahora }: { barra: Barra; anchoEtiqueta: number; ahora: number }) {
  const tema = useTheme();
  const { actividad, izquierda, ancho, abierta, traslapa, recortadaIzquierda, recortadaDerecha } = barra;

  const clave = colorEstatus(actividad.idEstatusWorkItem);
  const color = clave === "default"
    ? tema.palette.grey[tema.palette.mode === "dark" ? 600 : 500]
    : tema.palette[clave].main;

  const inicio = fechaLocal(actividad.fechaInicio);
  const fin = actividad.fechaFin ? fechaLocal(actividad.fechaFin) : null;
  const dias = Math.max(Math.round(((fin?.getTime() ?? ahora) - inicio.getTime()) / MS_DIA), 0);
  const descripcion = actividad.descripcion ? htmlATextoPlano(actividad.descripcion) : "";
  const responsable = actividad.asignado ?? "Sin asignar";

  const detalle = (
    <Box sx={{ maxWidth: 320 }}>
      <Typography variant="caption" sx={{ display: "block", fontWeight: 700 }}>
        {actividad.folio} - {actividad.titulo}
      </Typography>
      {descripcion && (
        <Typography variant="caption" sx={{ display: "block", mt: 0.5, opacity: 0.85 }}>
          {descripcion.length > 220 ? `${descripcion.slice(0, 220)}...` : descripcion}
        </Typography>
      )}
      <Typography variant="caption" sx={{ display: "block", mt: 0.5 }}>Responsable: {responsable}</Typography>
      <Typography variant="caption" sx={{ display: "block" }}>Proyecto: {actividad.proyecto}</Typography>
      <Typography variant="caption" sx={{ display: "block" }}>Tipo: {actividad.tipo} - {actividad.estatus}</Typography>
      <Typography variant="caption" sx={{ display: "block" }}>Inicio: {fechaCorta(inicio)}</Typography>
      <Typography variant="caption" sx={{ display: "block" }}>
        Fin: {fin ? fechaCorta(fin) : `sin fecha de fin (${dias} d en curso)`}
      </Typography>
      {actividad.fechaCompromiso && (
        <Typography variant="caption" sx={{ display: "block" }}>
          Compromiso: {fechaCorta(fechaLocal(actividad.fechaCompromiso))}
        </Typography>
      )}
      {(recortadaIzquierda || recortadaDerecha) && (
        <Typography variant="caption" sx={{ display: "block", mt: 0.5, fontStyle: "italic" }}>
          La barra se recorta: la actividad se sale del periodo filtrado.
        </Typography>
      )}
    </Box>
  );

  return (
    <Box sx={{
      display: "flex", height: ALTO_FILA, position: "relative", zIndex: 1,
      borderBottom: 1, borderColor: "divider",
    }}>
      <Box sx={{
        width: anchoEtiqueta, flexShrink: 0, position: "sticky", left: 0, zIndex: 2, minWidth: 0,
        bgcolor: "background.paper", borderRight: 1, borderColor: "divider", px: 1, py: 0.5,
        display: "flex", flexDirection: "column", justifyContent: "center",
      }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap title={actividad.titulo}>
          {actividad.titulo}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap title={responsable}>
          {actividad.folio} - {responsable}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap sx={{ fontSize: 11 }}>
          {fechaCorta(inicio)} - {fin ? fechaCorta(fin) : "en curso"}
        </Typography>
      </Box>

      <Box sx={{ flex: 1, position: "relative", minWidth: 0 }}>
        {traslapa && (
          <Tooltip title={detalle} placement="top" arrow>
            <Box sx={{
              position: "absolute", left: `${izquierda}%`, width: `${ancho}%`, minWidth: 8,
              top: (ALTO_FILA - ALTO_BARRA) / 2, height: ALTO_BARRA, bgcolor: color,
              display: "flex", alignItems: "center", overflow: "hidden", cursor: "default",
              // Extremo plano = la barra viene de antes del rango o sigue despues de el; la
              // punta redonda solo aparece donde la fecha real cae dentro del periodo.
              borderTopLeftRadius: recortadaIzquierda ? 0 : 4,
              borderBottomLeftRadius: recortadaIzquierda ? 0 : 4,
              borderTopRightRadius: recortadaDerecha || abierta ? 0 : 4,
              borderBottomRightRadius: recortadaDerecha || abierta ? 0 : 4,
              // Sin fecha de fin la barra se difumina contra el fondo en vez de cortar en seco:
              // es la forma de decir "esto sigue abierto" sin inventar una fecha de cierre.
              "&::after": abierta ? {
                content: '""', position: "absolute", right: 0, top: 0, bottom: 0, width: 18,
                background: `linear-gradient(90deg, transparent, ${tema.palette.background.paper})`,
              } : undefined,
            }}>
              <Typography sx={{
                fontSize: 11, fontWeight: 600, px: 0.75, whiteSpace: "nowrap",
                color: tema.palette.getContrastText(color),
              }}>
                {actividad.titulo}
              </Typography>
            </Box>
          </Tooltip>
        )}
      </Box>
    </Box>
  );
}

/** Leyenda de colores: el mismo mapa de estatus que usan los chips de las bandejas. */
export function LeyendaGantt() {
  const tema = useTheme();
  const entradas = [
    { id: 1, nombre: "Pendiente / Terminado" },
    { id: 2, nombre: "En proceso" },
    { id: 3, nombre: "En pruebas" },
    { id: 4, nombre: "Correccion" },
    { id: 7, nombre: "Cancelado" },
  ];

  return (
    <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", gap: 1, alignItems: "center" }}>
      {entradas.map((entrada) => {
        const clave = colorEstatus(entrada.id);
        const color = clave === "default"
          ? tema.palette.grey[tema.palette.mode === "dark" ? 600 : 500]
          : tema.palette[clave].main;
        return (
          <Stack key={entrada.id} direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
            <Box sx={{ width: 14, height: 10, borderRadius: 0.5, bgcolor: color }} />
            <Typography variant="caption" color="text.secondary">{entrada.nombre}</Typography>
          </Stack>
        );
      })}
      <Chip size="small" variant="outlined" label="Barra difuminada = sin fecha de fin" />
      <Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
        <Box sx={{ width: 2, height: 12, bgcolor: "error.main", opacity: 0.55 }} />
        <Typography variant="caption" color="text.secondary">Hoy</Typography>
      </Stack>
    </Stack>
  );
}
