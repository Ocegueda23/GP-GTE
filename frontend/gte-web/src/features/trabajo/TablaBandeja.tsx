import {
  Badge, Box, Chip, Link, Paper, Table, TableBody, TableCell, TableContainer,
  TableHead, TablePagination, TableRow, Tooltip, Typography,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import RateReviewIcon from "@mui/icons-material/RateReview";
import type { ResultadoPaginado } from "../../shared/api/http";
import { formatearMinutos, type BandejaItem } from "../../shared/api/workitems";
import { useFiltrosBandeja } from "./storeFiltros";
import { MenuAcciones } from "./MenuAcciones";

interface Props {
  datos: ResultadoPaginado<BandejaItem> | undefined;
  cargando: boolean;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Colores de chip por estatus (contrato de IDs del motor). */
function colorEstatus(idEstatus: number): "default" | "success" | "info" | "warning" | "error" {
  switch (idEstatus) {
    case 2: return "success";   // En Proceso
    case 3: return "info";      // En Pruebas
    case 4: return "warning";   // Correccion
    case 7: return "error";     // Cancelado
    default: return "default";
  }
}

/** Semantica visual heredada del GT: vencida en rojo suave, En Proceso en verde suave. */
function fondoFila(item: BandejaItem): string | undefined {
  if (item.esVencida) return "#fdecea";
  if (item.idEstatus === 2) return "#eaf6ec";
  return undefined;
}

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  // DateOnly (yyyy-MM-dd) se interpreta como UTC; forzar hora local
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

export function TablaBandeja({ datos, cargando, alExito, alError }: Props) {
  const { filtro, cambiarPagina, establecer } = useFiltrosBandeja();

  return (
    <Paper variant="outlined">
      <TableContainer sx={{ overflowX: "auto" }}>
        <Table size="small" aria-label="Bandeja de trabajo">
          <TableHead>
            <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
              <TableCell>Folio</TableCell>
              <TableCell>Tipo</TableCell>
              <TableCell>Titulo</TableCell>
              <TableCell>Proyecto</TableCell>
              <TableCell>Asignado</TableCell>
              <TableCell>Estatus</TableCell>
              <TableCell>Prioridad</TableCell>
              <TableCell>Compromiso</TableCell>
              <TableCell align="right">Presupuesto</TableCell>
              <TableCell align="right">Invertido</TableCell>
              <TableCell align="center">Rev.</TableCell>
              <TableCell align="center">Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {!cargando && datos?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={12}>
                  <Box sx={{ py: 4, textAlign: "center" }}>
                    <Typography color="text.secondary">
                      No hay elementos con los filtros actuales. Ajusta la busqueda o crea uno nuevo.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
            {datos?.items.map((item) => (
              <TableRow key={item.idWorkItem} hover sx={{ backgroundColor: fondoFila(item) }}>
                <TableCell sx={{ whiteSpace: "nowrap", fontWeight: 600 }}>
                  <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover">
                    {item.folio}
                  </Link>
                </TableCell>
                <TableCell>{item.tipo}</TableCell>
                <TableCell sx={{ maxWidth: 320 }}>
                  <Tooltip title={item.titulo}>
                    <Typography noWrap variant="body2">{item.titulo}</Typography>
                  </Tooltip>
                </TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{item.claveProyecto}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{item.asignado ?? "-"}</TableCell>
                <TableCell>
                  <Chip size="small" label={item.estatus} color={colorEstatus(item.idEstatus)}
                    variant={item.idEstatus === 6 ? "outlined" : "filled"} />
                </TableCell>
                <TableCell>{item.prioridad}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap", color: item.esVencida ? "error.main" : undefined, fontWeight: item.esVencida ? 700 : 400 }}>
                  {formatearFecha(item.fechaCompromiso)}
                </TableCell>
                <TableCell align="right">{formatearMinutos(item.minutosPresupuesto)}</TableCell>
                <TableCell align="right">{formatearMinutos(item.minutosInvertidos)}</TableCell>
                <TableCell align="center">
                  {item.revisionesPendientes > 0 && (
                    <Tooltip title={`${item.revisionesPendientes} revision(es) pendiente(s)`}>
                      <Badge badgeContent={item.revisionesPendientes} color="warning">
                        <RateReviewIcon fontSize="small" color="action" />
                      </Badge>
                    </Tooltip>
                  )}
                </TableCell>
                <TableCell align="center">
                  <MenuAcciones item={item} alExito={alExito} alError={alError} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component="div"
        count={datos?.totalItems ?? 0}
        page={(datos?.page ?? filtro.page) - 1}
        rowsPerPage={filtro.pageSize}
        rowsPerPageOptions={[10, 25, 50, 100]}
        onPageChange={(_, paginaCero) => cambiarPagina(paginaCero + 1)}
        onRowsPerPageChange={(e) => establecer({ pageSize: Number(e.target.value) })}
        labelRowsPerPage="Filas por pagina"
        labelDisplayedRows={({ from, to, count }) => `${from}-${to} de ${count}`}
      />
    </Paper>
  );
}
