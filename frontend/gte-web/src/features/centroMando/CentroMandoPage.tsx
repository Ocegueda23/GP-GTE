import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Grid, LinearProgress, Paper, Stack, TextField, Typography,
} from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import ReportProblemIcon from "@mui/icons-material/ReportProblem";
import DoneAllIcon from "@mui/icons-material/DoneAll";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Bar, BarChart, CartesianGrid, Cell, Legend, Line, LineChart, ResponsiveContainer,
  Tooltip as RechartsTooltip, XAxis, YAxis,
} from "recharts";
import { AvatarUsuario } from "../../shared/components/AvatarUsuario";
import { useSesion } from "../../shared/api/sesion";
import { useColorSerie } from "../../shared/graficas/coloresGrafica";
import {
  PERMISO_ADMINISTRAR_CENTRO_MANDO, atenderAlertaGestion, obtenerCentroMando,
  recalcularPeriodoCentroMando, type AlertaGestion, type EvaluacionResponsable,
} from "../../shared/api/centroMando";
import { NOMBRES_MES, colorSemaforo, formatearValor, tonoSemaforo } from "./comunes";

/** Orden de lectura de las alertas: primero lo que urge, al final lo que salio bien. */
const ORDEN_SEVERIDAD: Record<string, number> = { Critica: 0, Atencion: 1, Positiva: 2 };

function colorSeveridad(severidad: string): "error" | "warning" | "success" | "info" {
  if (severidad === "Critica") return "error";
  if (severidad === "Atencion") return "warning";
  if (severidad === "Positiva") return "success";
  return "info";
}

const COLORES_SERIE = ["#334155", "#0f766e", "#b45309", "#7c3aed", "#0891b2", "#be123c", "#4d7c0f", "#a16207", "#0369a1", "#9333ea"];

function TarjetaResponsable({ evaluacion, alAbrir }: {
  evaluacion: EvaluacionResponsable; alAbrir: () => void;
}) {
  return (
    <Paper
      variant="outlined"
      onClick={alAbrir}
      sx={{
        p: 1.5, height: "100%", cursor: "pointer",
        borderLeft: 4, borderLeftColor: tonoSemaforo(evaluacion.semaforo),
        "&:hover": { bgcolor: "action.hover" },
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", mb: 1 }}>
        <AvatarUsuario
          urlFoto={evaluacion.urlFoto}
          nombre={evaluacion.responsable ?? evaluacion.equipo}
          sx={{ width: 44, height: 44 }}
        />
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>{evaluacion.equipo}</Typography>
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block" }}>
            {evaluacion.responsable ?? "Sin responsable asignado"}
          </Typography>
        </Box>
      </Stack>

      <Stack direction="row" spacing={1} sx={{ alignItems: "baseline", mb: 0.5 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, color: tonoSemaforo(evaluacion.semaforo) }}>
          {formatearValor(evaluacion.scoreGeneral)}
        </Typography>
        {evaluacion.semaforo && (
          <Chip size="small" color={colorSemaforo(evaluacion.semaforo)} label={evaluacion.semaforo} />
        )}
        {evaluacion.nivel && <Typography variant="caption" color="text.secondary">{evaluacion.nivel}</Typography>}
      </Stack>

      <Typography variant="body2" color="text.secondary">
        {evaluacion.conclusion ?? "Sin conclusion calculada para el periodo."}
      </Typography>
      <Typography variant="caption" color="text.disabled" sx={{ display: "block", mt: 0.5 }}>
        {evaluacion.indicadoresConDato} de {evaluacion.indicadoresTotales} indicadores con dato
      </Typography>
    </Paper>
  );
}

function FilaAlerta({ alerta, alAtender, atendiendo }: {
  alerta: AlertaGestion; alAtender: () => void; atendiendo: boolean;
}) {
  const color = colorSeveridad(alerta.severidad);
  return (
    <Paper
      variant="outlined"
      sx={{ p: 1.5, borderLeft: 4, borderLeftColor: `${color}.main`, opacity: alerta.atendida ? 0.6 : 1 }}
    >
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ alignItems: { sm: "center" } }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap", rowGap: 0.5 }}>
            <Chip size="small" color={color} label={alerta.severidad} />
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{alerta.titulo}</Typography>
            {alerta.requiereGerencia && (
              <Chip
                size="small" color="error" variant="outlined"
                icon={<ReportProblemIcon sx={{ fontSize: 16 }} />}
                label="Requiere gerencia"
              />
            )}
            {alerta.atendida && <Chip size="small" variant="outlined" label="Atendida" />}
          </Stack>
          {alerta.mensaje && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{alerta.mensaje}</Typography>
          )}
          <Typography variant="caption" color="text.disabled">
            {[alerta.equipo, alerta.indicador].filter(Boolean).join(" - ") || "Alcance general"}
          </Typography>
        </Box>
        {!alerta.atendida && (
          <Button
            size="small" variant="outlined" startIcon={<DoneAllIcon />}
            disabled={atendiendo} onClick={alAtender}
          >
            Marcar atendida
          </Button>
        )}
      </Stack>
    </Paper>
  );
}

export function CentroMandoPage() {
  const hoy = new Date();
  const navegar = useNavigate();
  const clienteQuery = useQueryClient();
  const colorSerie = useColorSerie();
  const puede = useSesion((estado) => estado.puede);
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);

  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_CENTRO_MANDO);

  const centroMando = useQuery({
    queryKey: ["centro-mando", anio, mes],
    queryFn: () => obtenerCentroMando(anio, mes),
  });

  const recalcular = useMutation({
    mutationFn: () => recalcularPeriodoCentroMando(anio, mes),
    onSuccess: () => clienteQuery.invalidateQueries({ queryKey: ["centro-mando"] }),
  });

  const atender = useMutation({
    mutationFn: (idAlerta: number) => atenderAlertaGestion(idAlerta),
    onSuccess: () => clienteQuery.invalidateQueries({ queryKey: ["centro-mando"] }),
  });

  const datos = centroMando.data;

  // Recharts necesita una fila por periodo con una columna por serie; el backend manda
  // una serie por equipo con sus propios puntos, asi que se pivotea aqui.
  const datosTendencia = useMemo(() => {
    if (!datos) return [] as Record<string, string | number | null>[];
    const filas = new Map<string, Record<string, string | number | null>>();
    for (const tendencia of datos.tendencias) {
      for (const punto of tendencia.puntos) {
        const clave = `${punto.anio}-${String(punto.mes).padStart(2, "0")}`;
        const fila = filas.get(clave)
          ?? { periodo: `${NOMBRES_MES[punto.mes - 1].slice(0, 3)} ${punto.anio}` };
        fila[tendencia.serie] = punto.valor;
        filas.set(clave, fila);
      }
    }
    return [...filas.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(([, fila]) => fila);
  }, [datos]);

  const alertasOrdenadas = useMemo(
    () => [...(datos?.alertas ?? [])].sort((a, b) => {
      const porSeveridad = (ORDEN_SEVERIDAD[a.severidad] ?? 9) - (ORDEN_SEVERIDAD[b.severidad] ?? 9);
      if (porSeveridad !== 0) return porSeveridad;
      return Number(a.atendida) - Number(b.atendida);
    }),
    [datos],
  );

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { sm: "center" }, mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Centro de Mando TI</Typography>
        {puedeAdministrar && (
          <Button
            size="small" variant="outlined" startIcon={<RefreshIcon />}
            disabled={recalcular.isPending} onClick={() => recalcular.mutate()}
          >
            {recalcular.isPending ? "Recalculando..." : "Recalcular periodo"}
          </Button>
        )}
      </Stack>

      <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ flexWrap: "wrap", alignItems: "center", rowGap: 1.5 }}>
          <TextField size="small" type="number" label="Anio" value={anio} sx={{ width: 100 }}
            onChange={(e) => setAnio(Number(e.target.value))} />
          <TextField size="small" select label="Mes" value={mes} sx={{ width: 150 }}
            onChange={(e) => setMes(Number(e.target.value))} slotProps={{ select: { native: true } }}>
            {NOMBRES_MES.map((nombre, i) => <option key={nombre} value={i + 1}>{nombre}</option>)}
          </TextField>
          {datos?.fechaUltimoCalculo && (
            <Chip size="small" sx={{ ml: "auto" }}
              label={`Ultimo calculo: ${new Date(datos.fechaUltimoCalculo).toLocaleString()}`} />
          )}
        </Stack>
      </Paper>

      {centroMando.isLoading && <LinearProgress sx={{ mb: 2 }} />}
      {centroMando.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo cargar el Centro de Mando.</Alert>}
      {recalcular.isError && <Alert severity="error" sx={{ mb: 2 }}>No se pudo recalcular el periodo.</Alert>}

      {datos && (
        <>
          {/* IT Health Score */}
          <Paper variant="outlined" sx={{ p: 2, mb: 2, borderLeft: 6, borderLeftColor: tonoSemaforo(datos.saludSemaforo) }}>
            <Typography variant="subtitle2" color="text.secondary">IT Health Score</Typography>
            <Stack direction="row" spacing={2} sx={{ alignItems: "baseline", flexWrap: "wrap", rowGap: 1 }}>
              <Typography sx={{ fontSize: 56, fontWeight: 800, lineHeight: 1.1, color: tonoSemaforo(datos.saludSemaforo) }}>
                {formatearValor(datos.saludTi)}
              </Typography>
              {datos.saludSemaforo && (
                <Chip color={colorSemaforo(datos.saludSemaforo)} label={datos.saludSemaforo} />
              )}
              {datos.saludNivel && <Typography variant="h6">{datos.saludNivel}</Typography>}
              <Typography variant="caption" color="text.secondary">
                {NOMBRES_MES[datos.mes - 1]} {datos.anio}
              </Typography>
            </Stack>

            {datos.saludTopada && (
              <Alert severity="error" icon={<ReportProblemIcon />} sx={{ mt: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                  El score quedo topado por la regla de piso
                </Typography>
                <Typography variant="body2">
                  {datos.razonTope ?? "Un indicador critico esta en rojo, asi que el score no puede subir aunque el resto este bien."}
                </Typography>
              </Alert>
            )}
          </Paper>

          {/* Responsables */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Responsables</Typography>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            {datos.responsables.map((responsable) => (
              <Grid key={responsable.idEquipo} size={{ xs: 12, sm: 6, lg: 3 }}>
                <TarjetaResponsable
                  evaluacion={responsable}
                  alAbrir={() => navegar(`/centro-mando/equipos/${responsable.idEquipo}?anio=${anio}&mes=${mes}`)}
                />
              </Grid>
            ))}
            {datos.responsables.length === 0 && (
              <Grid size={12}>
                <Typography color="text.secondary">Sin evaluaciones calculadas para el periodo.</Typography>
              </Grid>
            )}
          </Grid>

          <Grid container spacing={2} sx={{ mb: 3 }}>
            {/* Tendencia mensual */}
            <Grid size={{ xs: 12, lg: 7 }}>
              <Paper variant="outlined" sx={{ p: 1.5, height: "100%" }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Tendencia mensual del score</Typography>
                {datosTendencia.length === 0 ? (
                  <Typography color="text.secondary">Sin historico suficiente para graficar la tendencia.</Typography>
                ) : (
                  <ResponsiveContainer width="100%" height={280}>
                    <LineChart data={datosTendencia}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="periodo" tick={{ fontSize: 11 }} />
                      <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
                      <RechartsTooltip />
                      <Legend wrapperStyle={{ fontSize: 11 }} />
                      {datos.tendencias.map((tendencia, i) => (
                        <Line
                          key={tendencia.serie}
                          type="monotone"
                          dataKey={tendencia.serie}
                          name={tendencia.serie}
                          stroke={colorSerie(COLORES_SERIE[i % COLORES_SERIE.length])}
                          strokeWidth={2}
                          connectNulls
                          dot={{ r: 2 }}
                        />
                      ))}
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </Paper>
            </Grid>

            {/* Dimensiones transversales */}
            <Grid size={{ xs: 12, lg: 5 }}>
              <Paper variant="outlined" sx={{ p: 1.5, height: "100%" }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Dimensiones transversales</Typography>
                {datos.dimensiones.length === 0 ? (
                  <Typography color="text.secondary">Sin dimensiones calculadas para el periodo.</Typography>
                ) : (
                  <ResponsiveContainer width="100%" height={Math.max(160, datos.dimensiones.length * 42)}>
                    <BarChart data={datos.dimensiones} layout="vertical" margin={{ left: 16, right: 16 }}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis type="number" domain={[0, 100]} tick={{ fontSize: 11 }} />
                      <YAxis type="category" dataKey="dimension" width={140} tick={{ fontSize: 11 }} />
                      <RechartsTooltip
                        formatter={(valor, _nombre, item) => {
                          const fila = item?.payload as { peso?: number } | undefined;
                          return [`${valor} (peso ${fila?.peso ?? "-"})`, "Valor"];
                        }}
                      />
                      <Bar dataKey="valor" radius={[0, 4, 4, 0]}>
                        {datos.dimensiones.map((dimension) => (
                          <Cell
                            key={dimension.dimension}
                            fill={colorSerie(
                              dimension.semaforo === "Verde" ? "#4d7c0f"
                                : dimension.semaforo === "Amarillo" ? "#b45309"
                                  : dimension.semaforo === "Rojo" ? "#be123c" : "#334155",
                            )}
                          />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </Paper>
            </Grid>
          </Grid>

          {/* Alertas */}
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>Alertas del periodo</Typography>
          {atender.isError && <Alert severity="error" sx={{ mb: 1 }}>No se pudo marcar la alerta como atendida.</Alert>}
          <Stack spacing={1}>
            {alertasOrdenadas.map((alerta) => (
              <FilaAlerta
                key={alerta.idAlertaGestion}
                alerta={alerta}
                atendiendo={atender.isPending && atender.variables === alerta.idAlertaGestion}
                alAtender={() => atender.mutate(alerta.idAlertaGestion)}
              />
            ))}
            {alertasOrdenadas.length === 0 && (
              <Typography color="text.secondary">Sin alertas registradas para el periodo.</Typography>
            )}
          </Stack>
        </>
      )}
    </Box>
  );
}
