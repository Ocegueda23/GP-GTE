import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  IconButton, LinearProgress, Paper, Snackbar, Stack, Table, TableBody,
  TableCell, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  crearAmbiente, obtenerAmbientes, obtenerCatalogosAdministracion, obtenerProyectos, retirarAmbiente,
} from "../../shared/api/administracion";

/** P20 - Ambientes: catalogo por proyecto o globales (DEV, QA, PREPROD, PROD). */
export function AmbientesTab() {
  const [modal, setModal] = useState(false);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [nombre, setNombre] = useState("");
  const [url, setUrl] = useState("");
  const [servidor, setServidor] = useState("");
  const [baseDatos, setBaseDatos] = useState("");
  const [idResponsable, setIdResponsable] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [busqueda, setBusqueda] = useState("");
  const clienteQuery = useQueryClient();

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });
  const ambientes = useQuery({ queryKey: ["ambientes-admin"], queryFn: () => obtenerAmbientes() });

  const ambientesFiltrados = (ambientes.data ?? []).filter((a) => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return true;
    return a.nombre.toLowerCase().includes(texto) || (a.proyecto ?? "").toLowerCase().includes(texto);
  });
  const { datosOrdenados: ambientesOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(ambientesFiltrados);

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });

  const limpiar = () => {
    setIdProyecto(""); setNombre(""); setUrl(""); setServidor(""); setBaseDatos(""); setIdResponsable("");
  };

  const guardar = async () => {
    try {
      const { mensaje } = await crearAmbiente({
        idProyecto: idProyecto === "" ? null : (idProyecto as number),
        nombre: nombre.trim(), url: url.trim() || null, servidor: servidor.trim() || null,
        baseDatos: baseDatos.trim() || null,
        idResponsable: idResponsable === "" ? null : (idResponsable as number),
      });
      avisar(mensaje);
      setModal(false);
      limpiar();
      await clienteQuery.invalidateQueries({ queryKey: ["ambientes-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el ambiente.", true);
    }
  };

  const retirar = async (idAmbiente: number) => {
    try {
      await retirarAmbiente(idAmbiente);
      avisar("Ambiente retirado.");
      await clienteQuery.invalidateQueries({ queryKey: ["ambientes-admin"] });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el ambiente.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Ambientes</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModal(true)}>
          Nuevo ambiente
        </Button>
      </Stack>

      <TextField size="small" placeholder="Buscar nombre o proyecto..." value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)} sx={{ mb: 1.5, minWidth: 280 }} />

      <Paper variant="outlined">
        {ambientes.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="proyecto" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Proyecto</EncabezadoOrdenable>
              <TableCell>URL</TableCell>
              <TableCell>Servidor</TableCell>
              <TableCell>Base de datos</TableCell>
              <EncabezadoOrdenable clave="responsable" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Responsable</EncabezadoOrdenable>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {ambientesOrdenados.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay ambientes registrados.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {ambientesOrdenados.map((a) => (
              <TableRow key={a.idAmbiente}>
                <TableCell>{a.nombre}</TableCell>
                <TableCell>{a.proyecto ?? "Global"}</TableCell>
                <TableCell>{a.url ?? "-"}</TableCell>
                <TableCell>{a.servidor ?? "-"}</TableCell>
                <TableCell>{a.baseDatos ?? "-"}</TableCell>
                <TableCell>{a.responsable ?? "-"}</TableCell>
                <TableCell>
                  <Tooltip title="Retirar ambiente">
                    <IconButton size="small" color="error" onClick={() => void retirar(a.idAmbiente)}>
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo ambiente</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <ComboBuscable
            label="Proyecto"
            value={idProyecto}
            onChange={(v) => setIdProyecto(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Global" },
              ...(proyectos.data ?? []).map((p) => ({ valor: p.idProyecto, etiqueta: p.clave })),
            ]}
          />
          <TextField size="small" label="URL" value={url} onChange={(e) => setUrl(e.target.value)} />
          <TextField size="small" label="Servidor" value={servidor} onChange={(e) => setServidor(e.target.value)} />
          <TextField size="small" label="Base de datos" value={baseDatos} onChange={(e) => setBaseDatos(e.target.value)} />
          <ComboBuscable
            label="Responsable"
            value={idResponsable}
            onChange={(v) => setIdResponsable(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin responsable" },
              ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={() => setModal(false)}>Cancelar</Button>
          <Button variant="contained" disabled={nombre.trim().length === 0} onClick={() => void guardar()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
