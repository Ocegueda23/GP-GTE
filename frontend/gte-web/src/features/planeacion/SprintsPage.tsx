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
import { crearSprint, obtenerSprints } from "../../shared/api/planeacion";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { formatearFecha } from "../entregas/formato";

/** IDs fijos de dbo.tblEstatusSprint (ver EstatusSprint del backend). */
const ESTATUS_SPRINT = [
  { id: 1, nombre: "Planeado" },
  { id: 2, nombre: "Activo" },
  { id: 3, nombre: "Cerrado" },
];

/**
 * Listado de sprints: filtros por sprint, estatus y lider asignado. El detalle vive en
 * su propia ruta (/sprints/:id, DetalleSprintPage), igual que releases y trabajo.
 */
export function SprintsPage() {
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [nombre, setNombre] = useState("");
  const [objetivo, setObjetivo] = useState("");
  const [fechaInicio, setFechaInicio] = useState("");
  const [fechaFin, setFechaFin] = useState("");
  const [idLiderNuevo, setIdLiderNuevo] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const [filtroSprint, setFiltroSprint] = useState<number | "">("");
  const [filtroEstatus, setFiltroEstatus] = useState<number | "">("");
  const [filtroLider, setFiltroLider] = useState<number | "">("");
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

  // Los tres filtros se resuelven en el backend (van en la queryKey), igual que en Releases.
  const sprints = useQuery({
    queryKey: ["sprints", filtroSprint, filtroEstatus, filtroLider],
    queryFn: () => obtenerSprints({
      idSprint: filtroSprint === "" ? undefined : filtroSprint,
      idEstatus: filtroEstatus === "" ? undefined : filtroEstatus,
      idLider: filtroLider === "" ? undefined : filtroLider,
      soloAbiertos: false,
    }),
  });

  const sprintsFiltrados = (sprints.data ?? []).filter((s) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return s.nombre.toLowerCase().includes(texto)
      || (s.folio ?? "").toLowerCase().includes(texto)
      || (s.lider ?? "").toLowerCase().includes(texto)
      || s.creadoPor.toLowerCase().includes(texto);
  });
  const { datosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(sprintsFiltrados);

  const hayFiltros = filtroSprint !== "" || filtroEstatus !== "" || filtroLider !== ""
    || busqueda.trim() !== "";

  const crear = async () => {
    setEnviando(true);
    try {
      const { dato, mensaje } = await crearSprint({
        nombre: nombre.trim(),
        objetivo: objetivo.trim() || null,
        fechaInicio,
        fechaFin,
        idLider: idLiderNuevo === "" ? null : (idLiderNuevo as number),
      });
      setNombre(""); setObjetivo(""); setFechaInicio(""); setFechaFin(""); setIdLiderNuevo("");
      setAviso({ tipo: "success", mensaje });
      await clienteQuery.invalidateQueries({ queryKey: ["sprints"] });
      navegar(`/sprints/${dato.idSprint}`);
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo crear el sprint.",
      });
      await clienteQuery.invalidateQueries({ queryKey: ["sprints"] });
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Sprints</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
          Nuevo sprint
        </Button>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} sx={{ mb: 2 }}>
          <TextField size="small" placeholder="Buscar folio, nombre o persona..."
            value={busqueda} onChange={(e) => setBusqueda(e.target.value)}
            sx={{ flex: 1, minWidth: 240 }} />
          <ComboBuscable
            label="Sprint"
            value={filtroSprint}
            onChange={(v) => setFiltroSprint(v === "" ? "" : Number(v))}
            opciones={(sprints.data ?? []).map((s) => ({ valor: s.idSprint, etiqueta: s.nombre }))}
            sx={{ minWidth: 200 }}
          />
          <ComboBuscable
            label="Estatus"
            value={filtroEstatus}
            onChange={(v) => setFiltroEstatus(v === "" ? "" : Number(v))}
            opciones={ESTATUS_SPRINT.map((e) => ({ valor: e.id, etiqueta: e.nombre }))}
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
          {sprintsFiltrados.length} sprint(s)
        </Typography>

        {sprints.isLoading && <LinearProgress sx={{ mb: 1 }} />}

        <TableContainer>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                <EncabezadoOrdenable clave="folio" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Folio</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="estatus" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Estatus</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="lider" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Lider asignado</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="creadoPor" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Creado por</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaCreacion" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Creado</EncabezadoOrdenable>
                <EncabezadoOrdenable clave="fechaCierre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Terminado</EncabezadoOrdenable>
                <TableCell>Avance</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {!sprints.isLoading && datosOrdenados.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography variant="body2" color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
                      {hayFiltros
                        ? "Ningun sprint coincide con los filtros."
                        : "No hay sprints. Crea uno para empezar a planear."}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {datosOrdenados.map((s) => (
                <TableRow key={s.idSprint} hover sx={{ cursor: "pointer" }}
                  onClick={() => navegar(`/sprints/${s.idSprint}`)}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{s.folio ?? "-"}</TableCell>
                  <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{s.nombre}</TableCell>
                  <TableCell>
                    <Chip size="small" label={s.estatus} color={s.idEstatus === 2 ? "success" : "default"} />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {s.lider ?? (
                      <Typography variant="caption" color="text.secondary">Sin asignar</Typography>
                    )}
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{s.creadoPor}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(s.fechaCreacion)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatearFecha(s.fechaCierre)}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {s.puntosTerminados}/{s.puntosComprometidos} pts
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo sprint</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <ComboBuscable
            label="Lider asignado"
            value={idLiderNuevo}
            onChange={(v) => setIdLiderNuevo(v as number | "")}
            opciones={(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))}
          />
          <TextField size="small" required label="Nombre" value={nombre}
            onChange={(e) => setNombre(e.target.value)} />
          <TextField size="small" label="Objetivo del sprint" multiline minRows={2}
            value={objetivo} onChange={(e) => setObjetivo(e.target.value)} />
          <TextField size="small" type="date" required label="Inicio" value={fechaInicio}
            onChange={(e) => setFechaInicio(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" required label="Fin" value={fechaFin}
            onChange={(e) => setFechaFin(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }} />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained"
            disabled={enviando || nombre.trim().length === 0 || !fechaInicio || !fechaFin}
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
