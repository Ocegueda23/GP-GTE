import { useEffect, useState } from "react";
import { Alert, Box, Button, Snackbar, Stack, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery } from "@tanstack/react-query";
import { obtenerBandeja, obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import { useSesion } from "../../shared/api/sesion";
import { BarraFiltros } from "./BarraFiltros";
import { NuevoItemModal } from "./NuevoItemModal";
import { TablaBandeja } from "./TablaBandeja";
import { useFiltrosBandeja } from "./storeFiltros";

interface Aviso {
  tipo: "success" | "error";
  mensaje: string;
}

/** P03 - Bandeja de trabajo: sucesora directa de FrmRegistro del GT. */
export function BandejaPage() {
  const { filtro, establecer } = useFiltrosBandeja();
  const sesion = useSesion((estado) => estado.sesion);
  const [aviso, setAviso] = useState<Aviso | null>(null);
  const [modalNuevo, setModalNuevo] = useState(false);
  const esMovil = useEsMovil();

  // Al entrar a la bandeja sin un Asignado ya elegido, parte filtrando por el usuario firmado.
  useEffect(() => {
    if (filtro.idAsignado === null && sesion) {
      establecer({ idAsignado: sesion.idUsuario });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo al montar la pantalla
  }, []);

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });

  const bandeja = useQuery({
    queryKey: ["bandeja", filtro],
    queryFn: () => obtenerBandeja(filtro),
    placeholderData: (anterior) => anterior,
  });

  return (
    <Box sx={{
      p: { xs: 1.5, sm: 2 },
      // En escritorio la pantalla ocupa el alto de la ventana y la tabla scrollea por
      // dentro con su encabezado fijo. En movil son tarjetas y lo natural es que
      // scrollee la pagina completa, sin un area interna con su propia barra.
      height: esMovil ? "auto" : "calc(100vh - 48px)",
      display: "flex", flexDirection: "column", overflow: esMovil ? "visible" : "hidden",
    }}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}
        sx={{
          justifyContent: "space-between", alignItems: { xs: "stretch", sm: "center" },
          mb: 2, flexShrink: 0,
        }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          Bandeja de trabajo
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
          Nuevo
        </Button>
      </Stack>

      <Box sx={{ flexShrink: 0 }}>
        <BarraFiltros catalogos={catalogos.data} />
      </Box>

      {bandeja.isError && (
        <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>
          No se pudo consultar la bandeja: {(bandeja.error as Error).message}
        </Alert>
      )}

      <Box sx={{ flex: 1, minHeight: 0 }}>
        <TablaBandeja
          datos={bandeja.data}
          cargando={bandeja.isLoading}
          catalogos={catalogos.data}
          alExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
          alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
        />
      </Box>

      <NuevoItemModal
        abierto={modalNuevo}
        catalogos={catalogos.data}
        alCerrar={() => setModalNuevo(false)}
        alExito={(mensaje) => setAviso({ tipo: "success", mensaje })}
        alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
      />

      <Snackbar
        open={aviso !== null}
        autoHideDuration={5000}
        onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert
          severity={aviso?.tipo ?? "success"}
          variant="filled"
          onClose={() => setAviso(null)}
          sx={{ minWidth: 320 }}
        >
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
