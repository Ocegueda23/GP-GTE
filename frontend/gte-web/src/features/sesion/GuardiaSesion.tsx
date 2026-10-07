import { useEffect, useState } from "react";
import { Alert, Box, Container, LinearProgress } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { registrarManejadorSesionInvalida } from "../../shared/api/http";
import { hayToken, obtenerSesion, recuperarSesionDeOtraPestana, useSesion } from "../../shared/api/sesion";
import { LoginPage } from "./LoginPage";

/**
 * Deja pasar solo con sesion valida. Si el token existe pero la API lo rechaza,
 * el interceptor lo descarta y aqui se vuelve al inicio de sesion.
 */
export function GuardiaSesion({ children }: { children: React.ReactNode }) {
  const { sesion, establecer } = useSesion();
  // Pestana abierta con Ctrl+Click: nace sin token (sessionStorage es por pestana) y antes
  // de mostrar el login se intenta recuperar la sesion de otra pestana o de la cookie.
  const [recuperando, setRecuperando] = useState(() => !hayToken());

  useEffect(() => {
    registrarManejadorSesionInvalida(() => establecer(null));
  }, [establecer]);

  useEffect(() => {
    if (!recuperando) return;
    let vigente = true;
    void recuperarSesionDeOtraPestana().finally(() => {
      if (vigente) setRecuperando(false);
    });
    return () => { vigente = false; };
  }, [recuperando]);

  const consulta = useQuery({
    queryKey: ["sesion"],
    queryFn: obtenerSesion,
    enabled: sesion === null && !recuperando && hayToken(),
    retry: false,
  });

  useEffect(() => {
    if (consulta.data) {
      establecer(consulta.data);
    }
  }, [consulta.data, establecer]);

  if (sesion === null && (recuperando || (hayToken() && consulta.isLoading))) {
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
