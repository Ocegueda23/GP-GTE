import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, LinearProgress,
  Snackbar, Stack, Tab, Tabs, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../../shared/api/http";
import { ComboBuscable } from "../../../shared/components/ComboBuscable";
import {
  crearCatalogo, obtenerCatalogos, obtenerConfigCatalogo, obtenerTablasDisponibles, type Registro,
} from "../../../shared/api/catalogoGenerico";
import { CatalogoGrid } from "../CatalogoGrid";
import { CatalogoFormModal } from "../CatalogoFormModal";
import { ColumnaConfigEditor } from "./ColumnaConfigEditor";

function NuevoCatalogoDialog({ onClose, onCreado }: { onClose: () => void; onCreado: (clave: string) => void }) {
  const tablas = useQuery({ queryKey: ["catalogo-generico-tablas-disponibles"], queryFn: obtenerTablasDisponibles });
  const [nombreTabla, setNombreTabla] = useState("");
  const [clave, setClave] = useState("");
  const [titulo, setTitulo] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const crear = async () => {
    if (!nombreTabla || !clave || !titulo) {
      setError("Completa tabla, clave y titulo.");
      return;
    }
    setGuardando(true);
    setError(null);
    try {
      const { dato } = await crearCatalogo(clave.toUpperCase(), nombreTabla, titulo);
      onCreado(dato.clave);
    } catch (err) {
      setError(err instanceof ErrorApi ? err.message : "No se pudo crear el catalogo.");
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Nuevo catalogo</DialogTitle>
      <DialogContent>
        <Stack sx={{ gap: 2, mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <ComboBuscable
            label="Tabla de bdsGTE"
            value={nombreTabla}
            onChange={(v) => setNombreTabla(String(v))}
            opciones={(tablas.data ?? []).map((t) => ({ valor: t, etiqueta: t }))}
            required
          />
          <TextField label="Clave" required value={clave}
            onChange={(e) => setClave(e.target.value.toUpperCase())}
            helperText="Identificador estable (letras/numeros/guion bajo); genera los permisos CAT.<CLAVE>.*" />
          <TextField label="Titulo" required value={titulo} onChange={(e) => setTitulo(e.target.value)} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={guardando}>Cancelar</Button>
        <Button variant="contained" onClick={() => void crear()} disabled={guardando}>Crear</Button>
      </DialogActions>
    </Dialog>
  );
}

/** Administracion del motor de catalogos genericos: alta, config de columnas y CRUD completo de datos. */
export function CatalogosAdminPage() {
  const clienteQuery = useQueryClient();
  const catalogos = useQuery({ queryKey: ["catalogos-generico-admin"], queryFn: obtenerCatalogos });
  const [clave, setClave] = useState<string | null>(null);
  const claveActual = clave ?? catalogos.data?.[0]?.clave ?? null;
  const [subPestana, setSubPestana] = useState<"datos" | "columnas">("datos");
  const [mostrarNuevo, setMostrarNuevo] = useState(false);
  const [registroEnEdicion, setRegistroEnEdicion] = useState<Registro | null | undefined>(undefined);
  const [aviso, setAviso] = useState<string | null>(null);

  const config = useQuery({
    queryKey: ["catalogo-generico-config-admin", claveActual],
    queryFn: () => obtenerConfigCatalogo(claveActual!),
    enabled: claveActual !== null,
  });

  const refrescarRegistros = () => clienteQuery.invalidateQueries({ queryKey: ["catalogo-registros", claveActual] });

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Administracion de catalogos</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setMostrarNuevo(true)}>Nuevo catalogo</Button>
      </Stack>

      {catalogos.isLoading && <LinearProgress />}

      {!catalogos.isLoading && (catalogos.data?.length ?? 0) === 0 && (
        <Alert severity="info">Todavia no hay catalogos configurados. Crea el primero.</Alert>
      )}

      {(catalogos.data?.length ?? 0) > 0 && (
        <>
          <Tabs value={claveActual} onChange={(_, valor: string) => setClave(valor)} sx={{ mb: 1 }}
            variant="scrollable" scrollButtons="auto" allowScrollButtonsMobile>
            {catalogos.data!.map((c) => <Tab key={c.clave} value={c.clave} label={c.titulo} />)}
          </Tabs>
          <Tabs value={subPestana} onChange={(_, v: "datos" | "columnas") => setSubPestana(v)} sx={{ mb: 2 }}>
            <Tab value="datos" label="Datos" />
            <Tab value="columnas" label="Configuracion de columnas" />
          </Tabs>

          {config.isLoading && <LinearProgress />}

          {config.data && subPestana === "datos" && (
            <CatalogoGrid
              config={config.data}
              onCrear={() => setRegistroEnEdicion(null)}
              onEditar={(fila) => setRegistroEnEdicion(fila)}
              puedeEliminar
            />
          )}

          {config.data && subPestana === "columnas" && (
            <ColumnaConfigEditor
              clave={config.data.clave}
              columnas={config.data.columnas}
              onGuardado={() => clienteQuery.invalidateQueries({ queryKey: ["catalogo-generico-config-admin", claveActual] })}
            />
          )}
        </>
      )}

      {config.data && registroEnEdicion !== undefined && (
        <CatalogoFormModal
          clave={config.data.clave}
          config={config.data}
          registro={registroEnEdicion}
          onClose={() => setRegistroEnEdicion(undefined)}
          onGuardado={(mensaje) => {
            setRegistroEnEdicion(undefined);
            setAviso(mensaje);
            void refrescarRegistros();
          }}
        />
      )}

      {mostrarNuevo && (
        <NuevoCatalogoDialog
          onClose={() => setMostrarNuevo(false)}
          onCreado={(claveCreada) => {
            setMostrarNuevo(false);
            setClave(claveCreada);
            void clienteQuery.invalidateQueries({ queryKey: ["catalogos-generico-admin"] });
            void clienteQuery.invalidateQueries({ queryKey: ["catalogos-generico"] });
          }}
        />
      )}

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity="success" variant="filled" onClose={() => setAviso(null)}>{aviso}</Alert>
      </Snackbar>
    </Box>
  );
}
