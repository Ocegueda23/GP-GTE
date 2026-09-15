import {
  Alert, Box, Chip, Divider, Drawer, IconButton, LinearProgress, Stack, Typography,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import { useQuery } from "@tanstack/react-query";
import { obtenerNotasVersionPublicadas, type NotaVersion } from "../../shared/api/notasVersion";

/** Colores por tipo de cambio; el eje es el mismo del versionado (Proyecto.Mejora.Defecto). */
function colorTipoCambio(idTipoCambio: number): "primary" | "success" | "warning" | "default" {
  switch (idTipoCambio) {
    case 1: return "primary";   // Proyecto
    case 2: return "success";   // Mejora
    case 3: return "warning";   // Defecto
    default: return "default";
  }
}

function Nota({ nota, esActual }: { nota: NotaVersion; esActual: boolean }) {
  // Se agrupa por tipo de cambio respetando el orden que ya trajo el back.
  const grupos = nota.renglones.reduce<{ id: number; nombre: string; descripciones: typeof nota.renglones }[]>(
    (acumulado, renglon) => {
      const grupo = acumulado.find((g) => g.id === renglon.idTipoCambioVersion);
      if (grupo) grupo.descripciones.push(renglon);
      else acumulado.push({ id: renglon.idTipoCambioVersion, nombre: renglon.tipoCambio, descripciones: [renglon] });
      return acumulado;
    }, []);

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: "baseline", flexWrap: "wrap" }}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>{nota.version}</Typography>
        {esActual && <Chip size="small" color="info" label="Tu version" />}
        <Typography variant="caption" color="text.secondary">
          {new Date(`${nota.fechaLiberacion}T00:00:00`).toLocaleDateString()}
        </Typography>
      </Stack>

      {nota.resumen && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{nota.resumen}</Typography>
      )}

      <Stack spacing={1.5} sx={{ mt: 1.5 }}>
        {grupos.map((grupo) => (
          <Box key={grupo.id}>
            <Chip size="small" label={grupo.nombre} color={colorTipoCambio(grupo.id)} sx={{ mb: 0.5 }} />
            <Stack component="ul" spacing={0.5} sx={{ pl: 2.5, m: 0 }}>
              {grupo.descripciones.map((renglon) => (
                <Typography key={renglon.idNotaVersionDetalle} component="li" variant="body2">
                  {renglon.modulo && (
                    <Typography component="span" variant="body2" sx={{ fontWeight: 600 }}>
                      {renglon.modulo}:{" "}
                    </Typography>
                  )}
                  {renglon.descripcion}
                </Typography>
              ))}
            </Stack>
          </Box>
        ))}
        {nota.renglones.length === 0 && (
          <Typography variant="body2" color="text.secondary">Sin detalle capturado.</Typography>
        )}
      </Stack>
    </Box>
  );
}

/**
 * Historial de lo que trae cada version, en un panel lateral. Se abre desde el sello de
 * version de la barra superior, que es donde el usuario ya mira para saber que trae.
 */
export function PanelNotasVersion({ abierto, alCerrar, versionActual }: {
  abierto: boolean; alCerrar: () => void; versionActual: string | null;
}) {
  const consulta = useQuery({
    queryKey: ["notas-version-publicadas"],
    queryFn: obtenerNotasVersionPublicadas,
    enabled: abierto,
    staleTime: 5 * 60_000,
  });

  return (
    <Drawer anchor="right" open={abierto} onClose={alCerrar}
      slotProps={{ paper: { sx: { width: { xs: "100%", sm: 460 } } } }}>
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", p: 2, pb: 1 }}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>Novedades</Typography>
        <IconButton size="small" onClick={alCerrar} aria-label="Cerrar"><CloseIcon fontSize="small" /></IconButton>
      </Stack>
      <Divider />

      <Box sx={{ p: 2, overflowY: "auto" }}>
        {consulta.isLoading && <LinearProgress />}
        {consulta.isError && <Alert severity="error">No se pudieron cargar las notas de version.</Alert>}
        {consulta.data?.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            Todavia no hay notas de version publicadas.
          </Typography>
        )}
        <Stack spacing={2} divider={<Divider />}>
          {consulta.data?.map((nota) => (
            <Nota key={nota.idNotaVersion} nota={nota} esActual={nota.version === versionActual} />
          ))}
        </Stack>
      </Box>
    </Drawer>
  );
}
