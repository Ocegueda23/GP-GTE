import { useState } from "react";
import {
  Alert, Box, Chip, InputAdornment, Pagination, Paper, Stack, TextField, Typography,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import ImageOutlinedIcon from "@mui/icons-material/ImageOutlined";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { obtenerArticulosPublicos } from "../../../shared/api/conocimiento";
import { LayoutPublico } from "./LayoutPublico";

type Vista = "todos" | "articulos" | "glosario";

const TAMANO_PAGINA = 25;

/**
 * Base de conocimiento publica (sin sesion). Solo lista articulos marcados como publicos;
 * el backend es el que aplica ese filtro (endpoint /api/v1/publico/conocimiento).
 */
export function ConocimientoPublicoPage() {
  const [vista, setVista] = useState<Vista>("todos");
  const [busqueda, setBusqueda] = useState("");
  const [pagina, setPagina] = useState(1);

  const esGlosario = vista === "todos" ? undefined : vista === "glosario";

  const articulos = useQuery({
    queryKey: ["conocimiento-publico", { esGlosario, texto: busqueda, pagina }],
    queryFn: () => obtenerArticulosPublicos({
      page: pagina,
      pageSize: TAMANO_PAGINA,
      texto: busqueda,
      esGlosario,
    }),
  });

  const cambiarVista = (siguiente: Vista) => {
    setVista(siguiente);
    setPagina(1);
  };

  const items = articulos.data?.items ?? [];
  const totalPaginas = articulos.data?.totalPages ?? 0;

  return (
    <LayoutPublico>
      <Box sx={{ maxWidth: 760, mx: "auto", px: { xs: 2, md: 5 }, py: { xs: 4, md: 6 } }}>
        <Stack sx={{ alignItems: "center", textAlign: "center", mb: 3 }}>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>Base de Conocimiento</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Definiciones y procedimientos de nuestros procesos de desarrollo.
          </Typography>
        </Stack>

        <TextField
          fullWidth placeholder="Buscar articulos o terminos..."
          value={busqueda}
          onChange={(e) => { setBusqueda(e.target.value); setPagina(1); }}
          sx={{ mb: 2, bgcolor: "background.paper" }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" color="disabled" />
                </InputAdornment>
              ),
            },
          }}
        />

        <Stack direction="row" spacing={1} sx={{ justifyContent: "center", mb: 3 }}>
          <Chip label="Todos" clickable
            color={vista === "todos" ? "primary" : "default"}
            variant={vista === "todos" ? "filled" : "outlined"}
            onClick={() => cambiarVista("todos")} />
          <Chip label="Articulos" clickable
            color={vista === "articulos" ? "primary" : "default"}
            variant={vista === "articulos" ? "filled" : "outlined"}
            onClick={() => cambiarVista("articulos")} />
          <Chip label="Glosario" clickable
            color={vista === "glosario" ? "secondary" : "default"}
            variant={vista === "glosario" ? "filled" : "outlined"}
            onClick={() => cambiarVista("glosario")} />
        </Stack>

        {articulos.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            No se pudo cargar la base de conocimiento. Intenta de nuevo en un momento.
          </Alert>
        )}

        {/* isSuccess, no !isLoading: entre reintentos de la consulta isLoading vuelve a
            false sin que haya error todavia, y el vacio se pintaria como si la peticion
            hubiera respondido "no hay nada". */}
        {articulos.isSuccess && items.length === 0 && (
          <Paper variant="outlined" sx={{ p: 4 }}>
            <Typography color="text.secondary" sx={{ textAlign: "center" }}>
              {busqueda.trim()
                ? "No hay resultados para esta busqueda."
                : "Todavia no hay articulos publicados."}
            </Typography>
          </Paper>
        )}

        <Stack spacing={1.5}>
          {items.map((articulo) => (
            <Paper key={articulo.idArticuloConocimiento} variant="outlined" sx={{ p: 2 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap", mb: 0.5 }}>
                <Typography
                  component={RouterLink}
                  to={`/publico/conocimiento/${articulo.idArticuloConocimiento}`}
                  variant="subtitle1"
                  sx={{ fontWeight: 600, color: "info.main", textDecoration: "none" }}
                >
                  {articulo.titulo}
                </Typography>
                {articulo.esGlosario && (
                  <Chip size="small" label="Glosario" color="secondary" variant="outlined" />
                )}
                {articulo.tieneImagen && <ImageOutlinedIcon fontSize="small" color="disabled" />}
              </Stack>
              <Typography variant="body2" color="text.secondary">{articulo.fragmento}</Typography>
            </Paper>
          ))}
        </Stack>

        {totalPaginas > 1 && (
          <Stack sx={{ alignItems: "center", mt: 3 }}>
            <Pagination count={totalPaginas} page={pagina} onChange={(_, p) => setPagina(p)} size="small" />
          </Stack>
        )}
      </Box>
    </LayoutPublico>
  );
}
