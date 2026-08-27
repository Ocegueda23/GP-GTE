import { useEffect, useState } from "react";
import { Alert, Box, CircularProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { descargarCentroMando } from "../../shared/api/ayuda";

/**
 * Centro de Mando TI. A diferencia del Manual de usuario (archivo estatico en public/),
 * este documento exige el permiso AYU.CentroMando, asi que se descarga por el endpoint
 * autenticado y se muestra desde un blob local: nunca queda expuesto como URL adivinable.
 */
export function CentroMandoPage() {
  const { data, isPending, isError, error } = useQuery({
    queryKey: ["ayuda", "centro-mando-ti"],
    queryFn: descargarCentroMando,
    staleTime: Infinity,
  });

  const [urlDocumento, establecerUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!data) return;

    const url = URL.createObjectURL(data);
    establecerUrl(url);
    return () => {
      URL.revokeObjectURL(url);
      establecerUrl(null);
    };
  }, [data]);

  if (isPending) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", p: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : "No se pudo abrir el Centro de Mando TI."}
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ height: "calc(100vh - 48px)" }}>
      {urlDocumento && (
        <iframe
          src={urlDocumento}
          title="Centro de Mando TI"
          style={{ width: "100%", height: "100%", border: "none", display: "block" }}
        />
      )}
    </Box>
  );
}
