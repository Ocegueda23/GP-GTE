import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, LinearProgress, Link, List, ListItem, ListItemText,
  Paper, Snackbar, Stack, Typography,
} from "@mui/material";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  cambiarEstatus, formatearMinutos, obtenerMiDia, type MiDiaItem,
} from "../../shared/api/workitems";
import { ModalTiempo } from "../trabajo/ModalTiempo";
import { BotonesAcciones } from "../workitem/BotonesAcciones";

function formatearFecha(iso: string | null): string {
  if (!iso) return "sin compromiso";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short" });
}

interface PropsLista {
  titulo: string;
  items: MiDiaItem[];
  color?: "error" | "text.primary";
  alIniciar: (item: MiDiaItem) => void;
  vacio: string;
}

function ListaItems({ titulo, items, color, alIniciar, vacio }: PropsLista) {
  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1, minWidth: 280 }}>
      <Typography variant="subtitle2" sx={{ color: color ?? "text.primary", fontWeight: 700, mb: 1 }}>
        {titulo} ({items.length})
      </Typography>
      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">{vacio}</Typography>
      ) : (
        <List dense disablePadding>
          {items.map((item) => (
            <ListItem key={item.idWorkItem} disableGutters sx={{ gap: 1 }}>
              <ListItemText
                sx={{ flex: 1, minWidth: 0 }}
                primary={
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover"
                      sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                      {item.folio}
                    </Link>
                    <Typography variant="body2" noWrap>{item.titulo}</Typography>
                  </Stack>
                }
                secondary={`${item.claveProyecto} - vence ${formatearFecha(item.fechaCompromiso)} - ${item.prioridad}`}
              />
              {item.accionInicio && (
                <Button size="small" startIcon={<PlayArrowIcon />}
                  sx={{ flexShrink: 0 }} onClick={() => alIniciar(item)}>
                  {item.etiquetaAccionInicio ?? "Iniciar"}
                </Button>
              )}
            </ListItem>
          ))}
        </List>
      )}
    </Paper>
  );
}

/** P02 - Mi Dia: que estoy haciendo, que vence y cuanto llevo registrado hoy. */
export function MiDiaPage() {
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [modalTiempo, setModalTiempo] = useState<MiDiaItem | null>(null);
  const clienteQuery = useQueryClient();

  const miDia = useQuery({ queryKey: ["mi-dia"], queryFn: obtenerMiDia });

  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["mi-dia"] }),
    clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
  ]);

  // La accion viene del motor (INICIAR o REANUDAR segun el estatus): el front no la deduce.
  const iniciar = async (item: MiDiaItem) => {
    if (!item.accionInicio) return;
    try {
      const { mensaje } = await cambiarEstatus(item.idWorkItem, item.accionInicio);
      setAviso({ tipo: "success", mensaje });
      await refrescar();
    } catch (error) {
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo iniciar el elemento.",
      });
    }
  };

  if (miDia.isLoading) {
    return <Box sx={{ p: 3 }}><LinearProgress /></Box>;
  }
  if (miDia.isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">{(miDia.error as Error).message}</Alert>
      </Box>
    );
  }

  const datos = miDia.data!;
  const fechaLarga = new Date(datos.fecha).toLocaleDateString("es-MX", {
    weekday: "long", day: "numeric", month: "long",
  });

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "baseline", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          Hola {datos.usuario.split(" ")[0]}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {fechaLarga} - {formatearMinutos(datos.minutosHoy)} registrado hoy -{" "}
          {datos.totalAbiertos === 1 ? "1 elemento abierto" : `${datos.totalAbiertos} elementos abiertos`}
        </Typography>
      </Stack>

      <Paper variant="outlined"
        sx={{ p: 2, mb: 2, borderColor: datos.enProceso ? "success.main" : undefined }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>En proceso ahora</Typography>
        {datos.enProceso ? (
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}
            sx={{ justifyContent: "space-between", alignItems: { md: "center" } }}>
            <Box>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Link component={RouterLink} to={`/wi/${datos.enProceso.folio}`} underline="hover"
                  sx={{ fontWeight: 700 }}>
                  {datos.enProceso.folio}
                </Link>
                <Chip size="small" label={datos.enProceso.tipo} variant="outlined" />
                {datos.enProceso.esVencida && <Chip size="small" color="error" label="Vencida" />}
              </Stack>
              <Typography variant="body1">{datos.enProceso.titulo}</Typography>
              <Typography variant="body2" color="text.secondary">
                {datos.enProceso.claveProyecto} - vence {formatearFecha(datos.enProceso.fechaCompromiso)}
                {" - invertido "}{formatearMinutos(datos.enProceso.minutosInvertidos)}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
              <Button size="small" variant="outlined" onClick={() => setModalTiempo(datos.enProceso)}>
                Registrar tiempo
              </Button>
              <BotonesAcciones
                idWorkItem={datos.enProceso.idWorkItem}
                folio={datos.enProceso.folio}
                alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescar(); }}
                alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
              />
            </Stack>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary">
            No tienes ningun elemento en proceso. Inicia uno de la lista para empezar a medir tiempo.
          </Typography>
        )}
      </Paper>

      <Stack direction={{ xs: "column", lg: "row" }} spacing={2}>
        <ListaItems titulo="Vencidas" items={datos.vencidas} color="error" alIniciar={iniciar}
          vacio="Nada vencido. Bien ahi." />
        <ListaItems titulo="Para hoy" items={datos.paraHoy} alIniciar={iniciar}
          vacio="Nada vence hoy." />
        <ListaItems titulo="Proximos 7 dias" items={datos.proximas} alIniciar={iniciar}
          vacio="Sin pendientes proximos." />
      </Stack>

      {modalTiempo && (
        <ModalTiempo
          abierto
          item={{ idWorkItem: modalTiempo.idWorkItem, folio: modalTiempo.folio }}
          alCerrar={() => setModalTiempo(null)}
          alExito={(mensaje) => { setAviso({ tipo: "success", mensaje }); void refrescar(); }}
          alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
        />
      )}

      <Snackbar open={aviso !== null} autoHideDuration={5000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
