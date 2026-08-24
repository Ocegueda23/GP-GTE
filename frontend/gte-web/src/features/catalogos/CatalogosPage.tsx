import { useState } from "react";
import { Alert, Box, LinearProgress, Snackbar, Tab, Tabs, Typography } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  obtenerCatalogos, obtenerConfigCatalogo, type Registro,
} from "../../shared/api/catalogoGenerico";
import { useSesion } from "../../shared/api/sesion";
import { CatalogoGrid } from "./CatalogoGrid";
import { CatalogoFormModal } from "./CatalogoFormModal";

/**
 * Vista operativa de catalogos: el alta/edicion/baja se habilita por catalogo segun los
 * permisos CAT.<CLAVE>.Crear/Editar/Eliminar que emite el motor. Sin ellos el grid queda
 * en solo lectura. La administracion del motor (crear catalogos, configurar columnas)
 * sigue viviendo aparte, en CatalogosAdminPage.
 */
export function CatalogosPage() {
  const clienteQuery = useQueryClient();
  const catalogos = useQuery({ queryKey: ["catalogos-generico"], queryFn: obtenerCatalogos });
  const [clave, setClave] = useState<string | null>(null);
  const claveActual = clave ?? catalogos.data?.[0]?.clave ?? null;
  const [registroEnEdicion, setRegistroEnEdicion] = useState<Registro | null | undefined>(undefined);
  const [aviso, setAviso] = useState<string | null>(null);
  const { puede } = useSesion();

  const config = useQuery({
    queryKey: ["catalogo-generico-config", claveActual],
    queryFn: () => obtenerConfigCatalogo(claveActual!),
    enabled: claveActual !== null,
  });

  const puedeCrear = claveActual !== null && puede(`CAT.${claveActual}.Crear`);
  const puedeEditar = claveActual !== null && puede(`CAT.${claveActual}.Editar`);
  const puedeEliminar = claveActual !== null && puede(`CAT.${claveActual}.Eliminar`);

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Catalogos</Typography>

      {catalogos.isLoading && <LinearProgress />}

      {!catalogos.isLoading && (catalogos.data?.length ?? 0) === 0 && (
        <Alert severity="info">No tienes acceso a ningun catalogo. Pidele al administrador que te asigne el permiso.</Alert>
      )}

      {(catalogos.data?.length ?? 0) > 0 && (
        <>
          <Tabs value={claveActual} onChange={(_, valor: string) => setClave(valor)} sx={{ mb: 2 }}
            variant="scrollable" scrollButtons="auto" allowScrollButtonsMobile>
            {catalogos.data!.map((c) => <Tab key={c.clave} value={c.clave} label={c.titulo} />)}
          </Tabs>
          {config.isLoading && <LinearProgress />}
          {config.data && (
            <CatalogoGrid
              config={config.data}
              onCrear={puedeCrear ? () => setRegistroEnEdicion(null) : undefined}
              onEditar={puedeEditar ? (fila) => setRegistroEnEdicion(fila) : undefined}
              puedeEliminar={puedeEliminar}
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
            void clienteQuery.invalidateQueries({ queryKey: ["catalogo-registros", claveActual] });
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
