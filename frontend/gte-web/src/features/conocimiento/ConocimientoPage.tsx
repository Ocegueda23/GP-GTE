import { useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, InputAdornment, Pagination, Paper, Snackbar, Stack, TextField,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import SearchIcon from "@mui/icons-material/Search";
import ImageOutlinedIcon from "@mui/icons-material/ImageOutlined";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { useSesion } from "../../shared/api/sesion";
import { obtenerArticulos, type ArticuloLista } from "../../shared/api/conocimiento";
import { PERMISO_ADMINISTRAR_CONOCIMIENTO } from "./permisos";
import { ArticuloModal } from "./ArticuloModal";

type Vista = "todos" | "articulos" | "glosario";

const TAMANO_PAGINA = 25;

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/** P23 - Base de conocimiento: buscador de articulos e indice del glosario. */
export function ConocimientoPage() {
  const [vista, setVista] = useState<Vista>("todos");
  const [busqueda, setBusqueda] = useState("");
  const [pagina, setPagina] = useState(1);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);

  const puede = useSesion((estado) => estado.puede);
  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_CONOCIMIENTO);

  const esGlosario = vista === "todos" ? undefined : vista === "glosario";

  const articulos = useQuery({
    queryKey: ["conocimiento", { esGlosario, texto: busqueda, pagina }],
    queryFn: () => obtenerArticulos({
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
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Base de conocimiento</Typography>
        {puedeAdministrar && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalAbierto(true)}>
            Nuevo articulo
          </Button>
        )}
      </Stack>

      {articulos.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>{(articulos.error as Error).message}</Alert>
      )}

      <TextField
        fullWidth size="small" placeholder="Buscar articulos o terminos del glosario..."
        value={busqueda}
        onChange={(e) => { setBusqueda(e.target.value); setPagina(1); }}
        sx={{ mb: 1.5 }}
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

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
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

      {/* isSuccess, no !isLoading: entre reintentos de la consulta isLoading vuelve a
          false sin que haya error todavia, y el vacio se pintaria como si la peticion
          hubiera respondido "no hay nada". */}
      {articulos.isSuccess && items.length === 0 && (
        <Paper variant="outlined" sx={{ p: 4 }}>
          <Typography color="text.secondary" sx={{ textAlign: "center" }}>
            {busqueda.trim()
              ? "No hay articulos que coincidan con la busqueda."
              : "Todavia no hay articulos. Crea el primero con el boton Nuevo articulo."}
          </Typography>
        </Paper>
      )}

      {/* El glosario se lee como un diccionario (indice alfabetico), no como resultados
          de busqueda: son definiciones cortas, no articulos largos. */}
      {vista === "glosario"
        ? <IndiceGlosario items={items} />
        : <ListaArticulos items={items} />}

      {totalPaginas > 1 && (
        <Stack sx={{ alignItems: "center", mt: 2 }}>
          <Pagination count={totalPaginas} page={pagina} onChange={(_, p) => setPagina(p)} size="small" />
        </Stack>
      )}

      <ArticuloModal
        abierto={modalAbierto}
        articulo={null}
        onCerrar={() => setModalAbierto(false)}
        onExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
      />

      <Snackbar open={aviso !== null} autoHideDuration={5000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}

/** Tarjetas de resultado: para articulos largos, donde el fragmento ayuda a elegir. */
function ListaArticulos({ items }: { items: ArticuloLista[] }) {
  return (
    <Stack spacing={1.5}>
      {items.map((articulo) => (
        <Paper key={articulo.idArticuloConocimiento} variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap", mb: 0.5 }}>
            <Typography
              component={RouterLink}
              to={`/conocimiento/${articulo.idArticuloConocimiento}`}
              variant="subtitle2"
              sx={{ fontWeight: 600, color: "info.main", textDecoration: "none" }}
            >
              {articulo.titulo}
            </Typography>
            {articulo.esGlosario && <Chip size="small" label="Glosario" color="secondary" variant="outlined" />}
            {articulo.esPublico && <Chip size="small" label="Publico" color="success" variant="outlined" />}
            {articulo.tieneImagen && <ImageOutlinedIcon fontSize="small" color="disabled" />}
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            {articulo.fragmento}
          </Typography>
          <Typography variant="caption" color="text.disabled">
            Actualizado el {formatearFecha(articulo.fechaMovto ?? articulo.fechaRegistro)}
            {articulo.ultimoAutor && ` por ${articulo.ultimoAutor}`}
          </Typography>
        </Paper>
      ))}
    </Stack>
  );
}

/** Indice alfabetico agrupado por letra inicial. */
function IndiceGlosario({ items }: { items: ArticuloLista[] }) {
  const grupos = useMemo(() => {
    const mapa = new Map<string, ArticuloLista[]>();
    items.forEach((articulo) => {
      // Normaliza acentos para que "Ambito" y "Ámbito" caigan en la misma letra.
      const letra = articulo.titulo.trim().charAt(0)
        .normalize("NFD").replace(/\p{Diacritic}/gu, "")
        .toUpperCase();
      const clave = /[A-Z]/.test(letra) ? letra : "#";
      const grupo = mapa.get(clave);
      if (grupo) grupo.push(articulo);
      else mapa.set(clave, [articulo]);
    });
    return Array.from(mapa.entries()).sort(([a], [b]) => a.localeCompare(b, "es"));
  }, [items]);

  if (grupos.length === 0) return null;

  return (
    <Paper variant="outlined" sx={{ px: 3, py: 1, pb: 2 }}>
      {grupos.map(([letra, articulos]) => (
        <Box key={letra}>
          <Typography
            variant="caption"
            sx={{
              display: "block", fontWeight: 700, color: "secondary.main", letterSpacing: 1.5,
              mt: 2, pb: 0.75, borderBottom: 2, borderColor: "divider",
            }}
          >
            {letra}
          </Typography>
          {articulos.map((articulo) => (
            <Stack
              key={articulo.idArticuloConocimiento}
              direction="row" spacing={1}
              sx={{ alignItems: "center", py: 1.25, borderBottom: 1, borderColor: "grey.100" }}
            >
              {articulo.tieneImagen && <ImageOutlinedIcon fontSize="small" color="disabled" />}
              <Typography
                component={RouterLink}
                to={`/conocimiento/${articulo.idArticuloConocimiento}`}
                variant="body2"
                sx={{ fontWeight: 600, color: "info.main", textDecoration: "none", flexShrink: 0 }}
              >
                {articulo.titulo}
              </Typography>
              {articulo.esPublico && <Chip size="small" label="Publico" color="success" variant="outlined" />}
              <Typography variant="body2" color="text.secondary" noWrap sx={{ minWidth: 0 }}>
                {articulo.fragmento}
              </Typography>
            </Stack>
          ))}
        </Box>
      ))}
    </Paper>
  );
}
