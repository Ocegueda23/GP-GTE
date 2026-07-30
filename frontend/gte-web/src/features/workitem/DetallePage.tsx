import { useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Divider, LinearProgress, Link, Paper, Snackbar,
  Stack, Tab, Table, TableBody, TableCell, TableHead, TableRow, Tabs, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useQuery } from "@tanstack/react-query";
import {
  formatearMinutos, obtenerTiempos, obtenerWorkItem,
} from "../../shared/api/workitems";
import { ModalTiempo } from "../trabajo/ModalTiempo";
import { BotonesAcciones } from "./BotonesAcciones";

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  // DateOnly (yyyy-MM-dd) se interpreta como UTC; forzar hora local
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

function Campo({ etiqueta, valor, resaltar }: { etiqueta: string; valor: string; resaltar?: boolean }) {
  return (
    <Box sx={{ display: "flex", justifyContent: "space-between", gap: 2, py: 0.5 }}>
      <Typography variant="body2" color="text.secondary">{etiqueta}</Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, color: resaltar ? "error.main" : undefined }}>
        {valor}
      </Typography>
    </Box>
  );
}

/** P04 - Detalle de WorkItem (sucesor de FrmDetalle/FrmTareaSTS unificados). */
export function DetallePage() {
  const { folio = "" } = useParams();
  const [pestana, setPestana] = useState(0);
  const [modalTiempo, setModalTiempo] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);

  const detalle = useQuery({
    queryKey: ["workitem", folio],
    queryFn: () => obtenerWorkItem(folio),
    enabled: folio.length > 0,
  });

  const tiempos = useQuery({
    queryKey: ["tiempos", detalle.data?.idWorkItem],
    queryFn: () => obtenerTiempos(detalle.data!.idWorkItem),
    enabled: detalle.data !== undefined && pestana === 1,
  });

  if (detalle.isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">{(detalle.error as Error).message}</Alert>
      </Box>
    );
  }
  const item = detalle.data;
  if (!item) {
    return <Box sx={{ p: 3 }}><LinearProgress /></Box>;
  }

  const consumo = item.minutosPresupuesto
    ? Math.min(100, Math.round(((item.minutosInvertidos ?? 0) / item.minutosPresupuesto) * 100))
    : null;

  return (
    <Box sx={{ p: 2 }}>
      <Link component={RouterLink} to="/trabajo" underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mb: 1 }}>
        <ArrowBackIcon fontSize="small" /> Bandeja de trabajo
      </Link>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1} sx={{ justifyContent: "space-between" }}>
          <Box>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>{item.folio}</Typography>
              <Chip size="small" label={item.tipo} variant="outlined" />
              <Chip size="small" label={item.estatus}
                color={item.idEstatus === 2 ? "success" : item.idEstatus === 4 ? "warning" : "default"} />
              {item.esVencida && <Chip size="small" label="Vencida" color="error" />}
            </Stack>
            <Typography variant="h6" sx={{ fontWeight: 400, mt: 0.5 }}>{item.titulo}</Typography>
            <Typography variant="body2" color="text.secondary">
              {item.claveProyecto} - {item.proyecto}
            </Typography>
          </Box>
          <Box sx={{ pt: 0.5 }}>
            <BotonesAcciones
              idWorkItem={item.idWorkItem}
              folio={item.folio}
              alExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
              alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
            />
          </Box>
        </Stack>
      </Paper>

      <Box sx={{ display: "flex", gap: 2, flexDirection: { xs: "column", md: "row" } }}>
        <Paper variant="outlined" sx={{ flex: 2, p: 2 }}>
          <Tabs value={pestana} onChange={(_, valor) => setPestana(valor)} sx={{ mb: 2 }}>
            <Tab label="Descripcion" />
            <Tab label="Tiempo" />
          </Tabs>

          {pestana === 0 && (
            <Box>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>Descripcion</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap", mb: 3 }}>
                {item.descripcion?.trim() || "Sin descripcion."}
              </Typography>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>Criterios de aceptacion</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                {item.criteriosAceptacion?.trim() || "Sin criterios capturados."}
              </Typography>
            </Box>
          )}

          {pestana === 1 && (
            <Box>
              <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
                <Typography variant="subtitle2">Registros de tiempo</Typography>
                <Button size="small" variant="contained" onClick={() => setModalTiempo(true)}>
                  Registrar tiempo
                </Button>
              </Stack>
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                    <TableCell>Fecha</TableCell>
                    <TableCell align="right">Minutos</TableCell>
                    <TableCell>Descripcion</TableCell>
                    <TableCell>Usuario</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {tiempos.data?.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4}>
                        <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                          Sin registros de tiempo. El cierre exige avance registrado.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                  {tiempos.data?.map((registro) => (
                    <TableRow key={registro.idRegistroTiempo}>
                      <TableCell>{formatearFecha(registro.fecha)}</TableCell>
                      <TableCell align="right">{formatearMinutos(registro.minutos)}</TableCell>
                      <TableCell>{registro.descripcion ?? "-"}</TableCell>
                      <TableCell>{registro.usuario}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          )}
        </Paper>

        <Paper variant="outlined" sx={{ flex: 1, p: 2, alignSelf: "flex-start", minWidth: 280 }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>Datos</Typography>
          <Campo etiqueta="Asignado" valor={item.asignado ?? "-"} />
          <Campo etiqueta="Solicitante" valor={item.solicitante ?? "-"} />
          <Campo etiqueta="Prioridad" valor={item.prioridad} />
          <Campo etiqueta="Sprint" valor={item.sprint ?? "-"} />
          <Campo etiqueta="Compromiso" valor={formatearFecha(item.fechaCompromiso)} resaltar={item.esVencida} />
          <Campo etiqueta="Inicio" valor={formatearFecha(item.fechaInicio)} />
          <Campo etiqueta="Fin" valor={formatearFecha(item.fechaFin)} />
          <Campo etiqueta="Registro" valor={formatearFecha(item.fechaRegistro)} />
          <Campo etiqueta="Puntos" valor={item.puntosHistoria?.toString() ?? "-"} />
          <Divider sx={{ my: 1.5 }} />
          <Campo etiqueta="Presupuesto" valor={formatearMinutos(item.minutosPresupuesto)} />
          <Campo etiqueta="Invertido" valor={formatearMinutos(item.minutosInvertidos)} />
          {consumo !== null && (
            <Box sx={{ mt: 1 }}>
              <LinearProgress
                variant="determinate"
                value={consumo}
                color={consumo < 80 ? "success" : consumo < 100 ? "warning" : "error"}
              />
              <Typography variant="caption" color="text.secondary">{consumo}% del presupuesto</Typography>
            </Box>
          )}
          {item.revisionesPendientes > 0 && (
            <Alert severity="warning" sx={{ mt: 2 }}>
              {item.revisionesPendientes} revision(es) pendiente(s) bloquean el cierre.
            </Alert>
          )}
        </Paper>
      </Box>

      <ModalTiempo
        abierto={modalTiempo}
        item={{ idWorkItem: item.idWorkItem, folio: item.folio }}
        alCerrar={() => setModalTiempo(false)}
        alExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
        alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
      />

      <Snackbar
        open={aviso !== null}
        autoHideDuration={5000}
        onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
