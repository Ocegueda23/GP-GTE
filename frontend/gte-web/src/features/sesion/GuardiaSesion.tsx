import { useEffect } from "react";
import { Alert, Box, Container, LinearProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { registrarManejadorSesionInvalida } from "../../shared/api/http";
import { hayToken, obtenerSesion, useSesion } from "../../shared/api/sesion";
import { LoginPage } from "./LoginPage";

/**
 * Deja pasar solo con sesion valida. Si el token existe pero la API lo rechaza,
 * el interceptor lo descarta y aqui se vuelve al inicio de sesion.
 */
export function GuardiaSesion({ children }: { children: React.ReactNode }) {
  const { sesion, establecer } = useSesion();

  useEffect(() => {
    registrarManejadorSesionInvalida(() => establecer(null));
  }, [establecer]);

  const consulta = useQuery({
    queryKey: ["sesion"],
    queryFn: obtenerSesion,
    enabled: sesion === null && hayToken(),
    retry: false,
  });

  useEffect(() => {
    if (consulta.data) {
      establecer(consulta.data);
    }
  }, [consulta.data, establecer]);

  if (sesion === null && hayToken() && consulta.isLoading) {
    return <Box sx={{ p: 4 }}><LinearProgress /></Box>;
  }

  if (sesion === null) {
    return <LoginPage />;
  }

  if (sesion.sinRoles) {
    return (
      <Container maxWidth="sm" sx={{ pt: 8 }}>
        <Alert severity="warning">
          Tu usuario quedo registrado en GTE ({sesion.dominio}), pero todavia no tiene
          roles asignados, asi que no puedes operar. Pide a administracion que te asigne
          el rol que corresponde a tu trabajo.
        </Alert>
      </Container>
    );
  }

  return <>{children}</>;
}
