import { useCallback } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Paper, Stack, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink, useParams } from "react-router-dom";
import { ContenidoEnriquecido } from "../../../shared/editor/ContenidoEnriquecido";
import { obtenerArticuloPublico, urlImagenPublica } from "../../../shared/api/conocimiento";
import { LayoutPublico } from "./LayoutPublico";

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/**
 * Detalle publico de un articulo (sin sesion). Solo lectura: no hay editar, ni historial
 * de versiones, ni lista de adjuntos descargables -- las imagenes del texto SI se ven
 * (son parte del articulo), pero los archivos adjuntos no se publican.
 */
export function DetalleArticuloPublicoPage() {
  const { id } = useParams<{ id: string }>();
  const idArticulo = Number(id);

  const articulo = useQuery({
    queryKey: ["articulo-publico", idArticulo],
    queryFn: () => obtenerArticuloPublico(idArticulo),
    enabled: Number.isFinite(idArticulo),
  });

  // Referencia estable: ContenidoEnriquecido la usa como dependencia del efecto que
  // resuelve las imagenes.
  const resolverImagen = useCallback((guid: string) => urlImagenPublica(guid), []);

  return (
    <LayoutPublico>
      <Box sx={{ maxWidth: 760, mx: "auto", px: { xs: 2, md: 5 }, py: { xs: 3, md: 4 } }}>
        <Button component={RouterLink} to="/publico/conocimiento" size="small" startIcon={<ArrowBackIcon />}
          sx={{ mb: 1.5, color: "text.secondary" }}>
          Volver a Base de Conocimiento
        </Button>

        {articulo.isLoading && (
          <Stack sx={{ alignItems: "center", p: 6 }}>
            <CircularProgress />
          </Stack>
        )}

        {/* Un articulo interno responde 404 igual que uno inexistente: el mensaje es el
            mismo a proposito, para no revelar que el articulo existe pero es privado. */}
        {articulo.isError && (
          <Alert severity="info">
            Este articulo no esta disponible.
          </Alert>
        )}

        {articulo.data && (
          <>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap" }}>
              <Typography variant="h4" sx={{ fontWeight: 700 }}>{articulo.data.titulo}</Typography>
              {articulo.data.esGlosario && (
                <Chip size="small" label="Glosario" color="secondary" variant="outlined" />
              )}
            </Stack>
            <Typography variant="caption" color="text.disabled" sx={{ display: "block", mt: 0.5, mb: 2 }}>
              Actualizado el {formatearFecha(articulo.data.fechaActualizacion)}
            </Typography>

            <Paper variant="outlined" sx={{ p: 3 }}>
              <ContenidoEnriquecido
                html={articulo.data.contenido}
                urlPublicaImagen={resolverImagen}
              />
            </Paper>
          </>
        )}
      </Box>
    </LayoutPublico>
  );
}
