import { useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Divider, Paper, Snackbar, Stack, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import {
  eliminarArticulo, obtenerArticulo, obtenerVersionArticulo, obtenerVersionesArticulo,
} from "../../shared/api/conocimiento";
import { PERMISO_ADMINISTRAR_CONOCIMIENTO } from "./permisos";
import { ArticuloModal } from "./ArticuloModal";
import { PanelAdjuntosArticulo } from "./PanelAdjuntosArticulo";

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** P23 - Detalle de articulo: contenido, historial de versiones y adjuntos. */
export function DetalleArticuloPage() {
  const { id } = useParams<{ id: string }>();
  const idArticulo = Number(id);
  const navegar = useNavigate();
  const clienteQuery = useQueryClient();

  const [modalAbierto, setModalAbierto] = useState(false);
  const [confirmandoBaja, setConfirmandoBaja] = useState(false);
  const [versionVista, setVersionVista] = useState<number | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);

  const puede = useSesion((estado) => estado.puede);
  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_CONOCIMIENTO);

  const articulo = useQuery({
    queryKey: ["articulo", idArticulo],
    queryFn: () => obtenerArticulo(idArticulo),
    enabled: Number.isFinite(idArticulo),
  });

  const versiones = useQuery({
    queryKey: ["articulo-versiones", idArticulo],
    queryFn: () => obtenerVersionesArticulo(idArticulo),
    enabled: Number.isFinite(idArticulo),
  });

  const versionHistorica = useQuery({
    queryKey: ["articulo-version", idArticulo, versionVista],
    queryFn: () => obtenerVersionArticulo(idArticulo, versionVista as number),
    enabled: versionVista !== null,
  });

  const eliminar = async () => {
    try {
      const mensaje = await eliminarArticulo(idArticulo);
      await clienteQuery.invalidateQueries({ queryKey: ["conocimiento"] });
      setConfirmandoBaja(false);
      navegar("/conocimiento", { state: { mensaje } });
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo eliminar el articulo.",
      });
    }
  };

  if (articulo.isLoading) {
    return (
      <Stack sx={{ alignItems: "center", p: 6 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (articulo.isError || !articulo.data) {
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="error">
          {articulo.error instanceof Error ? articulo.error.message : "No se encontro el articulo."}
        </Alert>
      </Box>
    );
  }

  const dato = articulo.data;

  return (
    <Box sx={{ p: 2 }}>
      <Button component={RouterLink} to="/conocimiento" size="small" startIcon={<ArrowBackIcon />}
        sx={{ mb: 1.5, color: "text.secondary" }}>
        Volver a Base de conocimiento
      </Button>

      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 1, mb: 2 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap" }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>{dato.titulo}</Typography>
          {dato.esGlosario && <Chip size="small" label="Glosario" color="secondary" variant="outlined" />}
          {dato.esPublico && <Chip size="small" label="Publico" color="success" variant="outlined" />}
        </Stack>
        {puedeAdministrar && (
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" startIcon={<EditOutlinedIcon />}
              onClick={() => setModalAbierto(true)}>
              Editar
            </Button>
            <Button size="small" variant="outlined" color="error" startIcon={<DeleteOutlineOutlinedIcon />}
              onClick={() => setConfirmandoBaja(true)}>
              Eliminar
            </Button>
          </Stack>
        )}
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ alignItems: "flex-start" }}>
        <Paper variant="outlined" sx={{ p: 3, flex: 1, minWidth: 0, width: "100%" }}>
          <ContenidoEnriquecido html={dato.contenido} />
        </Paper>

        <Stack spacing={2} sx={{ width: { xs: "100%", md: 280 }, flexShrink: 0 }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>Historial de versiones</Typography>
            {versiones.data?.map((version) => (
              <Box
                key={version.idArticuloVersion}
                onClick={() => setVersionVista(version.esVersionActual ? null : version.version)}
                sx={{
                  px: 1, py: 0.75, borderRadius: 1, cursor: version.esVersionActual ? "default" : "pointer",
                  bgcolor: version.esVersionActual ? "action.hover" : "transparent",
                  "&:hover": { bgcolor: "action.hover" },
                }}
              >
                <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    Version {version.version}
                  </Typography>
                  {version.esVersionActual && (
                    <Chip size="small" label="Actual" color="secondary" />
                  )}
                </Stack>
                <Typography variant="caption" color="text.disabled">
                  {formatearFecha(version.fechaRegistro)} - {version.autor}
                </Typography>
              </Box>
            ))}
          </Paper>

          <Paper variant="outlined" sx={{ p: 2 }}>
            <PanelAdjuntosArticulo
              idArticulo={idArticulo}
              soloLectura={!puedeAdministrar}
              alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
            />
          </Paper>

          <Typography variant="caption" color="text.disabled">
            Actualizado el {formatearFecha(dato.fechaMovto ?? dato.fechaRegistro)}
            {dato.ultimoAutor && ` por ${dato.ultimoAutor}`}
          </Typography>
        </Stack>
      </Stack>

      <ArticuloModal
        abierto={modalAbierto}
        articulo={dato}
        onCerrar={() => setModalAbierto(false)}
        onExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
      />

      {/* Ver una version anterior sin restaurarla: el contenido vigente no se toca. */}
      <Dialog open={versionVista !== null} onClose={() => setVersionVista(null)} fullWidth maxWidth="md">
        <DialogTitle>Version {versionVista} de {dato.titulo}</DialogTitle>
        <DialogContent>
          {versionHistorica.isLoading && <CircularProgress size={20} />}
          {versionHistorica.data && (
            <>
              <Typography variant="caption" color="text.disabled">
                {formatearFecha(versionHistorica.data.fechaRegistro)} - {versionHistorica.data.autor}
              </Typography>
              <Divider sx={{ my: 1.5 }} />
              <ContenidoEnriquecido html={versionHistorica.data.contenido} />
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setVersionVista(null)}>Cerrar</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={confirmandoBaja} onClose={() => setConfirmandoBaja(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Eliminar articulo</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            "{dato.titulo}" dejara de aparecer en la base de conocimiento
            {dato.esPublico && " y de estar publicado sin sesion"}. El historial de versiones se conserva.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmandoBaja(false)}>Cancelar</Button>
          <Button variant="contained" color="error" onClick={() => void eliminar()}>Eliminar</Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={5000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
