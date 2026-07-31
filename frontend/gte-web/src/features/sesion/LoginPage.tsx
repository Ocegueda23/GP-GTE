import { useState } from "react";
import {
  Alert, Box, Button, Container, Paper, TextField, Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { iniciarSesionDesarrollo, obtenerConfiguracionAuth, useSesion } from "../../shared/api/sesion";

/** P01 - Inicio de sesion. Con Entra ID configurado redirige al proveedor corporativo. */
export function LoginPage() {
  const [dominio, setDominio] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const establecer = useSesion((e) => e.establecer);

  const configuracion = useQuery({
    queryKey: ["auth-configuracion"],
    queryFn: obtenerConfiguracionAuth,
  });

  const entrar = async () => {
    setEnviando(true);
    setError(null);
    try {
      const { sesion } = await iniciarSesionDesarrollo(dominio.trim());
      establecer(sesion);
    } catch (e) {
      setError(e instanceof ErrorApi ? e.message : "No se pudo iniciar sesion.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Container maxWidth="sm" sx={{ pt: 10 }}>
      <Paper variant="outlined" sx={{ p: 4 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 1 }}>GTE</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Gestor Tecnologico Empresarial
        </Typography>

        {configuracion.isLoading && (
          <Typography variant="body2">Consultando la configuracion de acceso...</Typography>
        )}

        {configuracion.data?.identidadExterna && (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Esta instalacion usa la cuenta corporativa para entrar.
            </Alert>
            <Button variant="contained" fullWidth
              onClick={() => { window.location.href = "/api/v1/auth/entra/inicio"; }}>
              Entrar con mi cuenta de Interflo
            </Button>
            <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 1 }}>
              El flujo con el proveedor de identidad se habilita al configurar el tenant.
            </Typography>
          </Box>
        )}

        {configuracion.data?.emisorDesarrollo && !configuracion.data.identidadExterna && (
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <Alert severity="warning">
              Ambiente de desarrollo: se entra con la cuenta de dominio, sin contrasena.
              En produccion se usa la cuenta corporativa.
            </Alert>
            <TextField
              autoFocus
              size="small"
              label="Cuenta de dominio"
              placeholder="aviramontes"
              value={dominio}
              onChange={(e) => setDominio(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter" && dominio.trim()) void entrar(); }}
            />
            {error !== null && <Alert severity="error">{error}</Alert>}
            <Button variant="contained" disabled={enviando || !dominio.trim()}
              onClick={() => void entrar()}>
              Entrar
            </Button>
          </Box>
        )}

        {configuracion.isError && (
          <Alert severity="error">
            No se pudo contactar la API. Verifica que este arriba y vuelve a intentar.
          </Alert>
        )}
      </Paper>
    </Container>
  );
}
