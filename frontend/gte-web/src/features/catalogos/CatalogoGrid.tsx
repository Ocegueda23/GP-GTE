import { useState } from "react";
import {
  Alert, Box, Button, Chip, IconButton, LinearProgress, Paper, Snackbar, Stack, Table,
  TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import SearchIcon from "@mui/icons-material/Search";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import {
  descifrarValor, eliminarRegistro, listarRegistros, obtenerValoresDistintos,
  type ColumnaConfig, type ConfiguracionCatalogo, type FiltroColumnaDiscreto, type Registro,
} from "../../shared/api/catalogoGenerico";

const TIPOS_FECHA = new Set(["date", "datetime", "datetime2", "smalldatetime"]);

function esTipoFecha(tipoSql: string) {
  return TIPOS_FECHA.has(tipoSql.toLowerCase());
}

function formatearValor(valor: unknown, columna: ColumnaConfig): string {
  if (columna.esCifrado) return "•••••";
  if (valor === null || valor === undefined) return "";
  if (columna.tipoSql.toLowerCase() === "bit") return valor ? "Si" : "No";
  if (esTipoFecha(columna.tipoSql)) {
    const fecha = new Date(valor as string);
    return Number.isNaN(fecha.getTime()) ? String(valor) : fecha.toLocaleString();
  }
  return String(valor);
}

interface Props {
  config: ConfiguracionCatalogo;
  /** Si se omite, el grid queda en solo lectura (vista de consulta). */
  onCrear?: () => void;
  onEditar?: (fila: Registro) => void;
  puedeEliminar?: boolean;
}

/** Grid dinamico: columnas/orden desde la config reconciliada, datos y paginacion desde el backend. */
export function CatalogoGrid({ config, onCrear, onEditar, puedeEliminar }: Props) {
  const clienteQuery = useQueryClient();
  const [pagina, setPagina] = useState(0);
  const [tamanoPagina, setTamanoPagina] = useState(25);
  const [texto, setTexto] = useState("");
  const [textoAplicado, setTextoAplicado] = useState("");
  const [ordenarPor, setOrdenarPor] = useState<string | null>(null);
  const [ordenDescendente, setOrdenDescendente] = useState(false);
  const [filtrosColumna, setFiltrosColumna] = useState<FiltroColumnaDiscreto[]>([]);
  const [filtroFecha, setFiltroFecha] = useState<{ nombreColumna: string; desde: string; hasta: string } | null>(null);
  const [columnaFiltro, setColumnaFiltro] = useState<string>("");
  const [valoresFiltro, setValoresFiltro] = useState<string[]>([]);
  const [fechaDesde, setFechaDesde] = useState("");
  const [fechaHasta, setFechaHasta] = useState("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [revelados, setRevelados] = useState<Record<string, string | null>>({});
  const { puede } = useSesion();
  const puedeDescifrar = puede(`CAT.${config.clave}.Descifrar`);

  const columnasVisibles = [...config.columnas]
    .filter((c) => c.esVisible)
    .sort((a, b) => a.ordinalPos - b.ordinalPos);

  const columnaFiltroInfo = columnasVisibles.find((c) => c.nombreColumna === columnaFiltro);
  const filtroEsFecha = columnaFiltroInfo ? esTipoFecha(columnaFiltroInfo.tipoSql) : false;

  const valoresDistintos = useQuery({
    queryKey: ["catalogo-valores-distintos", config.clave, columnaFiltro],
    queryFn: () => obtenerValoresDistintos(config.clave, columnaFiltro),
    enabled: columnaFiltro !== "" && !filtroEsFecha,
  });

  const registros = useQuery({
    queryKey: [
      "catalogo-registros", config.clave, pagina, tamanoPagina, textoAplicado,
      ordenarPor, ordenDescendente, filtrosColumna, filtroFecha,
    ],
    queryFn: () => listarRegistros(config.clave, {
      texto: textoAplicado || undefined,
      filtrosColumna,
      fecha: filtroFecha
        ? { nombreColumna: filtroFecha.nombreColumna, desde: filtroFecha.desde || null, hasta: filtroFecha.hasta || null }
        : null,
      ordenarPor: ordenarPor ?? undefined,
      ordenDescendente,
      pagina: pagina + 1,
      tamanoPagina,
    }),
  });

  const aplicarFiltroColumna = () => {
    if (!columnaFiltro) return;
    if (filtroEsFecha) {
      if (fechaDesde || fechaHasta) {
        setFiltroFecha({ nombreColumna: columnaFiltro, desde: fechaDesde, hasta: fechaHasta });
      }
    } else if (valoresFiltro.length > 0) {
      setFiltrosColumna((actual) => [
        ...actual.filter((f) => f.nombreColumna !== columnaFiltro),
        { nombreColumna: columnaFiltro, valores: valoresFiltro },
      ]);
    }
    setPagina(0);
    setColumnaFiltro("");
    setValoresFiltro([]);
    setFechaDesde("");
    setFechaHasta("");
  };

  const quitarFiltro = (nombreColumna: string) => {
    setFiltrosColumna((actual) => actual.filter((f) => f.nombreColumna !== nombreColumna));
  };

  const ordenar = (clave: string) => {
    if (ordenarPor === clave) {
      setOrdenDescendente((actual) => !actual);
    } else {
      setOrdenarPor(clave);
      setOrdenDescendente(false);
    }
  };

  const eliminar = async (fila: Registro) => {
    if (!window.confirm("¿Eliminar este registro?")) return;
    const clavesPk: Registro = {};
    config.columnas.filter((c) => c.esPk).forEach((c) => { clavesPk[c.nombreColumna] = fila[c.nombreColumna]; });
    try {
      const mensaje = await eliminarRegistro(config.clave, clavesPk);
      setAviso({ tipo: "success", mensaje });
      await clienteQuery.invalidateQueries({ queryKey: ["catalogo-registros", config.clave] });
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : "No se pudo eliminar." });
    }
  };

  const revelar = async (indice: number, fila: Registro, columna: ColumnaConfig) => {
    const clavesPk: Registro = {};
    config.columnas.filter((c) => c.esPk).forEach((c) => { clavesPk[c.nombreColumna] = fila[c.nombreColumna]; });
    const llave = `${indice}:${columna.nombreColumna}`;
    try {
      const valor = await descifrarValor(config.clave, clavesPk, columna.nombreColumna);
      setRevelados((actual) => ({ ...actual, [llave]: valor }));
    } catch (error) {
      setAviso({ tipo: "error", mensaje: error instanceof ErrorApi ? error.message : "No se pudo descifrar el valor." });
    }
  };

  const mostrarAcciones = Boolean(onEditar) || Boolean(puedeEliminar);

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 2, flexWrap: "wrap", gap: 1 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{config.titulo}</Typography>
        {onCrear && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={onCrear}>Nuevo</Button>
        )}
      </Stack>

      <Stack direction="row" sx={{ gap: 1, mb: 1, flexWrap: "wrap", alignItems: "center" }}>
        <TextField
          size="small"
          label="Buscar"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          onKeyDown={(e) => { if (e.key === "Enter") { setTextoAplicado(texto); setPagina(0); } }}
        />
        <IconButton onClick={() => { setTextoAplicado(texto); setPagina(0); }}><SearchIcon /></IconButton>

        <ComboBuscable
          label="Filtrar columna"
          value={columnaFiltro}
          onChange={(v) => { setColumnaFiltro(String(v)); setValoresFiltro([]); setFechaDesde(""); setFechaHasta(""); }}
          opciones={columnasVisibles.filter((c) => !c.esCifrado).map((c) => ({ valor: c.nombreColumna, etiqueta: c.displayName }))}
          sx={{ minWidth: 200 }}
        />

        {columnaFiltro && !filtroEsFecha && (
          <ComboBuscableMultiple
            label="Valores"
            value={valoresFiltro}
            onChange={(v) => setValoresFiltro(v as string[])}
            opciones={(valoresDistintos.data ?? []).map((v) => ({ valor: v, etiqueta: v }))}
            resumenSimple
            sx={{ minWidth: 220 }}
          />
        )}
        {columnaFiltro && filtroEsFecha && (
          <>
            <TextField size="small" label="Desde" type="date" slotProps={{ inputLabel: { shrink: true } }}
              value={fechaDesde} onChange={(e) => setFechaDesde(e.target.value)} />
            <TextField size="small" label="Hasta" type="date" slotProps={{ inputLabel: { shrink: true } }}
              value={fechaHasta} onChange={(e) => setFechaHasta(e.target.value)} />
          </>
        )}
        {columnaFiltro && (
          <Button size="small" variant="outlined" onClick={aplicarFiltroColumna}>Aplicar</Button>
        )}
      </Stack>

      {(filtrosColumna.length > 0 || filtroFecha) && (
        <Stack direction="row" sx={{ gap: 1, mb: 1, flexWrap: "wrap" }}>
          {filtrosColumna.map((f) => (
            <Chip
              key={f.nombreColumna}
              label={`${columnasVisibles.find((c) => c.nombreColumna === f.nombreColumna)?.displayName ?? f.nombreColumna}: ${f.valores.join(", ")}`}
              onDelete={() => quitarFiltro(f.nombreColumna)}
            />
          ))}
          {filtroFecha && (
            <Chip
              label={`${columnasVisibles.find((c) => c.nombreColumna === filtroFecha.nombreColumna)?.displayName ?? filtroFecha.nombreColumna}: ${filtroFecha.desde || "…"} a ${filtroFecha.hasta || "…"}`}
              onDelete={() => setFiltroFecha(null)}
            />
          )}
        </Stack>
      )}

      {registros.isLoading && <LinearProgress />}

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              {columnasVisibles.map((c) => (
                <EncabezadoOrdenable key={c.nombreColumna} clave={c.nombreColumna}
                  ordenActual={ordenarPor} descendente={ordenDescendente} onOrdenar={ordenar}>
                  {c.displayName}
                </EncabezadoOrdenable>
              ))}
              {mostrarAcciones && <TableCell align="right">Acciones</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {(registros.data?.items ?? []).map((fila, indice) => (
              // eslint-disable-next-line react/no-array-index-key -- no hay id unico garantizado del lado del cliente
              <TableRow key={indice} hover>
                {columnasVisibles.map((c) => {
                  const llave = `${indice}:${c.nombreColumna}`;
                  const revelado = revelados[llave];
                  return (
                    <TableCell key={c.nombreColumna}>
                      {c.esCifrado && revelado !== undefined ? (revelado ?? "") : formatearValor(fila[c.nombreColumna], c)}
                      {c.esCifrado && puedeDescifrar && revelado === undefined && fila[c.nombreColumna] !== null && (
                        <IconButton size="small" onClick={() => void revelar(indice, fila, c)}>
                          <VisibilityOutlinedIcon fontSize="inherit" />
                        </IconButton>
                      )}
                    </TableCell>
                  );
                })}
                {mostrarAcciones && (
                  <TableCell align="right">
                    {onEditar && (
                      <IconButton size="small" onClick={() => onEditar(fila)}><EditOutlinedIcon fontSize="small" /></IconButton>
                    )}
                    {puedeEliminar && (
                      <IconButton size="small" onClick={() => void eliminar(fila)}><DeleteOutlineIcon fontSize="small" /></IconButton>
                    )}
                  </TableCell>
                )}
              </TableRow>
            ))}
            {!registros.isLoading && (registros.data?.items.length ?? 0) === 0 && (
              <TableRow>
                <TableCell colSpan={columnasVisibles.length + (mostrarAcciones ? 1 : 0)} align="center">
                  Sin registros.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={registros.data?.totalItems ?? 0}
          page={pagina}
          onPageChange={(_, nueva) => setPagina(nueva)}
          rowsPerPage={tamanoPagina}
          onRowsPerPageChange={(e) => { setTamanoPagina(Number(e.target.value)); setPagina(0); }}
          rowsPerPageOptions={[10, 25, 50, 100]}
        />
      </TableContainer>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
