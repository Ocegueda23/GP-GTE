import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, LinearProgress, Link, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { obtenerActividadUsuario } from "../../shared/api/reportes";
import { formatearMinutos, obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { ErrorApi } from "../../shared/api/http";

function formatearFecha(iso: string): string {
  const fecha = new Date(iso + "T00:00:00");
  return fecha.toLocaleDateString("es-MX", {
    weekday: "long", day: "2-digit", month: "short", year: "numeric",
  });
}

function fechaHace(dias: number): string {
  const fecha = new Date();
  fecha.setDate(fecha.getDate() - dias);
  return fecha.toISOString().slice(0, 10);
}

/** Reporte para administrador/gerente: actividad diaria de un usuario en un rango de fechas, con horas reales trabajadas. */
export function ActividadUsuarioPage() {
  const [idUsuario, setIdUsuario] = useState<number | "">("");
  const [fechaInicio, setFechaInicio] = useState(fechaHace(6));
  const [fechaFin, setFechaFin] = useState(fechaHace(0));

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });

  const rangoValido = idUsuario !== "" && fechaInicio !== "" && fechaFin !== "" && fechaInicio <= fechaFin;

  const actividad = useQuery({
    queryKey: ["actividad-usuario", idUsuario, fechaInicio, fechaFin],
    queryFn: () => obtenerActividadUsuario(idUsuario as number, fechaInicio, fechaFin),
    enabled: rangoValido,
  });

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Actividad diaria por usuario</Typography>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", flexWrap: "wrap" }}>
          <ComboBuscable
            label="Usuario"
            required
            value={idUsuario}
            onChange={(v) => setIdUsuario(v as number | "")}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))}
            sx={{ minWidth: 220 }}
          />
          <TextField size="small" type="date" label="Desde" value={fechaInicio}
            onChange={(e) => setFechaInicio(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" label="Hasta" value={fechaFin}
            onChange={(e) => setFechaFin(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </Stack>
        {idUsuario !== "" && fechaInicio > fechaFin && (
          <Alert severity="warning" sx={{ mt: 2 }}>La fecha "Desde" no puede ser posterior a "Hasta".</Alert>
        )}
      </Paper>

      {idUsuario === "" && (
        <Alert severity="info">Elige un usuario y un rango de fechas para ver su actividad.</Alert>
      )}

      {actividad.isLoading && <LinearProgress />}
      {actividad.isError && (
        <Alert severity="error">
          {actividad.error instanceof ErrorApi ? actividad.error.message : "No se pudo obtener la actividad."}
        </Alert>
      )}

      {actividad.data && (
        <>
          <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
            <Stack direction="row" spacing={3} sx={{ alignItems: "baseline", flexWrap: "wrap" }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{actividad.data.usuario}</Typography>
              <Typography variant="body2" color="text.secondary">
                {formatearFecha(actividad.data.fechaInicio)} al {formatearFecha(actividad.data.fechaFin)}
              </Typography>
              <Typography variant="body2">
                Total: <strong>{formatearMinutos(actividad.data.minutosTotales)}</strong> reales trabajadas
              </Typography>
            </Stack>
          </Paper>

          {actividad.data.dias.length === 0 && (
            <Alert severity="info">Sin registros de tiempo en ese rango de fechas.</Alert>
          )}

          {actividad.data.dias.map((dia) => (
            <Paper key={dia.fecha} variant="outlined" sx={{ mb: 2 }}>
              <Box sx={{ p: 1.5, display: "flex", justifyContent: "space-between", alignItems: "center", backgroundColor: "action.hover" }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, textTransform: "capitalize" }}>
                  {formatearFecha(dia.fecha)}
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>{formatearMinutos(dia.minutosDia)}</Typography>
              </Box>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                      <TableCell>Folio</TableCell>
                      <TableCell>Titulo</TableCell>
                      <TableCell>Descripcion</TableCell>
                      <TableCell align="right">Tiempo</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {dia.registros.map((r) => (
                      <TableRow key={r.idRegistroTiempo}>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>
                          <Link component={RouterLink} to={`/wi/${r.folio}`} underline="hover" sx={{ fontWeight: 600 }}>
                            {r.folio}
                          </Link>
                        </TableCell>
                        <TableCell sx={{ maxWidth: 320 }}>
                          <Typography noWrap variant="body2">{r.titulo}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">{r.descripcion ?? "-"}</Typography>
                        </TableCell>
                        <TableCell align="right">{formatearMinutos(r.minutos)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          ))}
        </>
      )}
    </Box>
  );
}
