import type { ReactNode } from "react";
import {
  Badge, Box, Chip, Link, Paper, Stack, Table, TableBody, TableCell, TableContainer,
  TableHead, TablePagination, TableRow, TableSortLabel, Tooltip, Typography,
} from "@mui/material";
import { alpha, type Theme } from "@mui/material/styles";
import { Link as RouterLink } from "react-router-dom";
import RateReviewIcon from "@mui/icons-material/RateReview";
import type { ResultadoPaginado } from "../../shared/api/http";
import { OrdenMovil } from "../../shared/components/OrdenMovil";
import { TarjetaListado } from "../../shared/components/TarjetaListado";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import {
  colorEstatus, formatearMinutos, type BandejaItem, type CatalogosBandeja, type FiltroBandeja,
} from "../../shared/api/workitems";
import { useFiltrosBandeja } from "./storeFiltros";
import { MenuAcciones } from "./MenuAcciones";

interface Props {
  datos: ResultadoPaginado<BandejaItem> | undefined;
  cargando: boolean;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Mismas claves que los encabezados ordenables de la tabla, para el selector de movil. */
const COLUMNAS_ORDEN = [
  { valor: "folio", etiqueta: "Folio" },
  { valor: "tipo", etiqueta: "Tipo" },
  { valor: "titulo", etiqueta: "Titulo" },
  { valor: "proyecto", etiqueta: "Proyecto" },
  { valor: "asignado", etiqueta: "Asignado" },
  { valor: "estatus", etiqueta: "Estatus" },
  { valor: "sprint", etiqueta: "Sprint" },
  { valor: "prioridad", etiqueta: "Prioridad" },
  { valor: "compromiso", etiqueta: "Compromiso" },
  { valor: "invertido", etiqueta: "Invertido" },
];

/** Semantica visual heredada del GT: vencida en rojo suave, En Proceso en verde suave. */
function tinteItem(item: BandejaItem): "error" | "success" | undefined {
  if (item.esVencida) return "error";
  if (item.idEstatus === 2) return "success";
  return undefined;
}

/**
 * Con alpha() sobre los colores del theme en vez de hex fijos, para que el tinte se vea
 * bien tanto en modo claro como oscuro (un pastel solido se rompe contra fondo oscuro).
 */
function fondoFila(item: BandejaItem, theme: Theme): string | undefined {
  const tinte = tinteItem(item);
  if (!tinte) return undefined;
  return alpha(theme.palette[tinte].main, theme.palette.mode === "dark" ? 0.18 : 0.08);
}

function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  // DateOnly (yyyy-MM-dd) se interpreta como UTC; forzar hora local
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** Encabezado de columna ordenable: el orden real lo aplica el backend (la bandeja es paginada). */
function EncabezadoOrdenable({
  clave, filtro, alOrdenar, align, children,
}: {
  clave: string;
  filtro: FiltroBandeja;
  alOrdenar: (clave: string) => void;
  align?: "right" | "center";
  children: ReactNode;
}) {
  const activo = filtro.ordenarPor === clave;
  const direccion = activo && filtro.ordenDescendente ? "desc" : "asc";
  return (
    <TableCell align={align} sortDirection={activo ? direccion : false}>
      <TableSortLabel
        active={activo}
        direction={direccion}
        onClick={() => alOrdenar(clave)}
        sx={align === "right" ? { flexDirection: "row-reverse" } : undefined}
      >
        {children}
      </TableSortLabel>
    </TableCell>
  );
}

export function TablaBandeja({ datos, cargando, catalogos, alExito, alError }: Props) {
  const { filtro, cambiarPagina, establecer } = useFiltrosBandeja();
  const esMovil = useEsMovil();

  const manejarOrden = (clave: string) => {
    // Cadena vacia = el selector de movil se quedo sin columna (boton de limpiar del combo).
    if (clave === "") {
      establecer({ ordenarPor: null, ordenDescendente: false });
    } else if (filtro.ordenarPor === clave) {
      establecer({ ordenarPor: clave, ordenDescendente: !filtro.ordenDescendente });
    } else {
      establecer({ ordenarPor: clave, ordenDescendente: false });
    }
  };

  const vacia = !cargando && datos?.items.length === 0;

  const paginacion = (
    <TablePagination
      component="div"
      // En movil se esconde el "Filas por pagina": no cabe junto al conteo y los controles.
      sx={{
        flexShrink: 0,
        ".MuiTablePagination-selectLabel": { display: { xs: "none", sm: "block" } },
        ".MuiTablePagination-toolbar": { pl: { xs: 1, sm: 2 } },
      }}
      count={datos?.totalItems ?? 0}
      page={(datos?.page ?? filtro.page) - 1}
      rowsPerPage={filtro.pageSize}
      rowsPerPageOptions={[10, 25, 50, 100]}
      onPageChange={(_, paginaCero) => cambiarPagina(paginaCero + 1)}
      onRowsPerPageChange={(e) => establecer({ pageSize: Number(e.target.value) })}
      labelRowsPerPage="Filas por pagina"
      labelDisplayedRows={({ from, to, count }) => `${from}-${to} de ${count}`}
    />
  );

  if (esMovil) {
    return (
      <Stack spacing={1}>
        <OrdenMovil opciones={COLUMNAS_ORDEN} ordenarPor={filtro.ordenarPor}
          descendente={filtro.ordenDescendente} onOrdenar={manejarOrden} />

        {vacia && (
          <Paper variant="outlined" sx={{ p: 3 }}>
            <Typography color="text.secondary" sx={{ textAlign: "center" }}>
              No hay elementos con los filtros actuales. Ajusta la busqueda o crea uno nuevo.
            </Typography>
          </Paper>
        )}

        {datos?.items.map((item) => (
          <TarjetaListado
            key={item.idWorkItem}
            tinte={tinteItem(item)}
            encabezado={(
              <>
                <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover" color="info"
                  variant="body2" sx={{ fontWeight: 700 }}>
                  {item.folio}
                </Link>
                <Chip size="small" label={item.estatus} color={colorEstatus(item.idEstatus)}
                  variant={item.idEstatus === 6 ? "outlined" : "filled"} />
                {item.sprint
                  ? <Chip size="small" variant="outlined" label={item.folioSprint ?? item.sprint} />
                  : <Chip size="small" label="Backlog" />}
              </>
            )}
            titulo={(
              <Link component={RouterLink} to={`/wi/${item.folio}`} underline="none"
                variant="body2" sx={{ fontWeight: 600, color: "text.primary" }}>
                {item.titulo}
              </Link>
            )}
            campos={[
              { etiqueta: "Tipo", valor: item.tipo },
              { etiqueta: "Proyecto", valor: item.claveProyecto },
              { etiqueta: "Asignado", valor: item.asignado ?? "-" },
              { etiqueta: "Prioridad", valor: item.prioridad },
              { etiqueta: "Complejidad", valor: item.complejidad ?? "-" },
              { etiqueta: "Compromiso", valor: formatearFecha(item.fechaCompromiso), resaltar: item.esVencida },
              { etiqueta: "Invertido", valor: formatearMinutos(item.minutosInvertidos) },
            ]}
            acciones={(
              <>
                {item.revisionesPendientes > 0 && (
                  <Tooltip title={`${item.revisionesPendientes} revision(es) pendiente(s)`}>
                    <Badge badgeContent={item.revisionesPendientes} color="warning" sx={{ mr: 1.5 }}>
                      <RateReviewIcon fontSize="small" color="action" />
                    </Badge>
                  </Tooltip>
                )}
                <MenuAcciones item={item} catalogos={catalogos} alExito={alExito} alError={alError} />
              </>
            )}
          />
        ))}

        <Paper variant="outlined">{paginacion}</Paper>
      </Stack>
    );
  }

  return (
    <Paper variant="outlined" sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <TableContainer sx={{ flex: 1, minHeight: 0, overflow: "auto" }}>
        <Table size="small" stickyHeader aria-label="Bandeja de trabajo">
          <TableHead>
            <TableRow sx={{ "& th": { fontWeight: 700, whiteSpace: "nowrap" } }}>
              <EncabezadoOrdenable clave="folio" filtro={filtro} alOrdenar={manejarOrden}>Folio</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="tipo" filtro={filtro} alOrdenar={manejarOrden}>Tipo</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="titulo" filtro={filtro} alOrdenar={manejarOrden}>Titulo</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="proyecto" filtro={filtro} alOrdenar={manejarOrden}>Proyecto</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="asignado" filtro={filtro} alOrdenar={manejarOrden}>Asignado</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="estatus" filtro={filtro} alOrdenar={manejarOrden}>Estatus</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="sprint" filtro={filtro} alOrdenar={manejarOrden}>Sprint</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="prioridad" filtro={filtro} alOrdenar={manejarOrden}>Prioridad</EncabezadoOrdenable>
              <TableCell>Complejidad</TableCell>
              <EncabezadoOrdenable clave="compromiso" filtro={filtro} alOrdenar={manejarOrden}>Compromiso</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="invertido" filtro={filtro} alOrdenar={manejarOrden} align="right">Invertido</EncabezadoOrdenable>
              <TableCell align="center">Rev.</TableCell>
              <TableCell align="center">Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {vacia && (
              <TableRow>
                <TableCell colSpan={13}>
                  <Box sx={{ py: 4, textAlign: "center" }}>
                    <Typography color="text.secondary">
                      No hay elementos con los filtros actuales. Ajusta la busqueda o crea uno nuevo.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
            {datos?.items.map((item) => (
              <TableRow key={item.idWorkItem} hover
                sx={(theme) => ({ backgroundColor: fondoFila(item, theme) })}>
                <TableCell sx={{ whiteSpace: "nowrap", fontWeight: 600 }}>
                  <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover" color="info">
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
                <TableCell sx={{ whiteSpace: "nowrap" }}>
                  {item.sprint
                    ? <Chip size="small" variant="outlined" label={item.folioSprint ?? item.sprint} />
                    : <Chip size="small" label="Backlog" />}
                </TableCell>
                <TableCell>{item.prioridad}</TableCell>
                <TableCell>{item.complejidad ?? "-"}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap", color: item.esVencida ? "error.main" : undefined, fontWeight: item.esVencida ? 700 : 400 }}>
                  {formatearFecha(item.fechaCompromiso)}
                </TableCell>
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
                  <MenuAcciones item={item} catalogos={catalogos} alExito={alExito} alError={alError} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      {paginacion}
    </Paper>
  );
}
