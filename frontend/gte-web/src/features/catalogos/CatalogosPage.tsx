import { useState } from "react";
import { Alert, Box, LinearProgress, Tab, Tabs, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { obtenerCatalogos, obtenerConfigCatalogo } from "../../shared/api/catalogoGenerico";
import { CatalogoGrid } from "./CatalogoGrid";

/** Vista de consulta: catalogos ya configurados por el administrador, solo lectura. */
export function CatalogosPage() {
  const catalogos = useQuery({ queryKey: ["catalogos-generico"], queryFn: obtenerCatalogos });
  const [clave, setClave] = useState<string | null>(null);
  const claveActual = clave ?? catalogos.data?.[0]?.clave ?? null;

  const config = useQuery({
    queryKey: ["catalogo-generico-config", claveActual],
    queryFn: () => obtenerConfigCatalogo(claveActual!),
    enabled: claveActual !== null,
  });

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
          {config.data && <CatalogoGrid config={config.data} />}
        </>
      )}
    </Box>
  );
}
