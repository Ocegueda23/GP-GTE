import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  LinearProgress, Paper, Snackbar, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  colorEstatusRelease, crearRelease, obtenerCatalogosEntregas, obtenerReleases,
} from "../../shared/api/entregas";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { formatearFecha } from "./formato";

/**
 * P13 - Bandeja de releases: listado con filtros por proyecto, estatus y lider asignado.
 * El detalle vive en su propia ruta (/releases/:id, DetalleReleasePage), igual que la
 * bandeja de trabajo abre /wi/:folio. Antes lista y detalle compartian pantalla y estado
 * local, asi que no se podia mandar el enlace de un release ni volver a el con el boton
 * de atras del navegador.
 */
export function ReleasesPage() {
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [idProyectoNuevo, setIdProyectoNuevo] = useState<number | "">("");
  const [version, setVersion] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const [filtroProyecto, setFiltroProyecto] = useState<number | "">("");
  const [filtroEstatus, setFiltroEstatus] = useState<number | "">("");
  const [filtroLider, setFiltroLider] = useState<number | "">("");
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });
  const catalogosEntregas = useQuery({
    queryKey: ["catalogos-entregas"], queryFn: obtenerCatalogosEntregas, staleTime: 5 * 60_000,
  });

  // Los tres filtros se resuelven en el backend (van en la queryKey), no recortando en
  // memoria una lista completa: el listado crece con cada entrega y el filtro por lider
  // necesita comparar por id, no por como se escribio el nombre.
  const releases = useQuery({
    queryKey: ["releases", filtroProyecto, filtroEstatus, filtroLider],
    queryFn: () => obtenerReleases({
      idProyecto: filtroProyecto === "" ? undefined : filtroProyecto,
      idEstatus: filtroEstatus === "" ? undefined : filtroEstatus,
      idLiderAsignado: filtroLider === "" ? undefined : filtroLider,
    }),
  });

  // La busqueda libre si es local: acota lo ya traido por folio, proyecto o version.
  const releasesFiltrados = (releases.data ?? []).filter((rel) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return rel.claveProyecto.toLowerCase().includes(texto)
      || rel.proyecto.toLowerCase().includes(texto)
      || rel.version.toLowerCase().includes(texto)
      || (rel.folio ?? "").toLowerCase().includes(texto)
      || (rel.liderAsignado ?? "").toLowerCase().includes(texto)
      || rel.creadoPor.toLowerCase().includes(texto);
  });
  const { datosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(releasesFiltrados);

  const hayFiltros = filtroProyecto !== "" || filtroEstatus !== "" || filtroLider !== ""
    || busqueda.trim() !== "";

  const crear = async () => {
    setEnviando(true);
    try {
      const { dato, mensaje } = await crearRelease({
        idProyecto: idProyectoNuevo as number, version: version.trim(), fechaPlan: null,
      });
      setVersion("");
      setAviso({ tipo: "success", mensaje });
      await clienteQuery.invalidateQueries({ queryKey: ["releases"] });
      // Se entra directo al release nuevo: lo siguiente siempre es capturarle lider,
      // contenido y artefactos, y todo eso vive en el detalle.
      navegar(`/releases/${dato.idRelease}`);
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo crear el release.",
      });
      await clienteQuery.invalidateQueries({ queryKey: ["releases"] });
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Releases</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
          Nuevo release
        </Button>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} sx={{ mb: 2 }}>
          <TextField size="small" placeholder="Buscar folio, proyecto, version o persona..."
            value={busqueda} onChange={(e) => setBusqueda(e.target.value)}
            sx={{ flex: 1, minWidth: 240 }} />
          <ComboBuscable
            label="Proyecto"
            value={filtroProyecto}
            onChange={(v) => setFiltroProyecto(v === "" ? "" : Number(v))}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({
              valor: p.id, etiqueta: `${p.clave} - ${p.nombre}`,
            }))}
            sx={{ minWidth: 220 }}
          />
          <ComboBuscable
            label="Estatus"
            value={filtroEstatus}
            onChange={(v) => setFiltroEstatus(v === "" ? "" : Number(v))}
            opciones={(catalogosEntregas.data?.estatusRelease ?? []).map((e) => ({
              valor: e.id, etiqueta: e.nombre,
            }))}
            sx={{ minWidth: 170 }}
          />
          <ComboBuscable
            label="Lider asignado"
            value={filtroLider}
            onChange={(v) => setFiltroLider(v === "" ? "" : Number(v))}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({
              valor: u.id, etiqueta: u.nombre,
            }))}
            sx={{ minWidth: 200 }}
          />
        </Stack>

        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          {releasesFiltrados.length} release(s)
        </Typography>

        {releases.isLoading && <LinearProgress sx={{ mb: 1 }} />}

        <TableContainer>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="claveProyecto" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Proyecto</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="version" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Version</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="liderAsignado" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Lider asignado</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="creadoPor" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Creado por</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaCreacion" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Creado</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaPlan" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Fecha plan</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaLiberacion" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Liberado</EncabezadoOrdenable>
              </TableRow>
            </TableHead>
            <TableBody>
              {!releases.isLoading && datosOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography variant="body2" color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
                      {hayFiltros
                        ? "Ningun release coincide con los filtros."
                        : "No hay releases. Crea uno para empezar a preparar una entrega."}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {datosOrdenados.map((rel) => (
                <TableRow key={rel.idRelease} hover sx={{ cursor: "pointer" }}
                  onClick={() => navegar(`/releases/${rel.idRelease}`)}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{rel.folio ?? "-"}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{rel.claveProyecto} - {rel.proyecto}</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{rel.version}</TableCell>
                  <TableCell>
                    <Chip size="small" label={rel.estatus} color={colorEstatusRelease(rel.idEstatus)} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {rel.liderAsignado ?? (
                      <Typography variant="caption" color="text.secondary">Sin asignar</Typography>
                    )}
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{rel.creadoPor}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(rel.fechaCreacion)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(rel.fechaPlan)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(rel.fechaLiberacion)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nuevo release</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Proyecto"
            required
            value={idProyectoNuevo}
            onChange={(v) => setIdProyectoNuevo(v === "" ? "" : Number(v))}
            opciones={(catalogos.data?.proyectos ?? []).map((p) => ({
              valor: p.id, etiqueta: `${p.clave} - ${p.nombre}`,
            }))}
          />
          <TextField size="small" required label="Version" value={version} placeholder="2.11.0"
            onChange={(e) => setVersion(e.target.value)}
            helperText="Versionado semantico" />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={enviando || idProyectoNuevo === "" || !version.trim()}
            onClick={() => { setModalNuevo(false); void crear(); }}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={8000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
