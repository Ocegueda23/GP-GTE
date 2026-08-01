import { useState } from "react";
import {
  Alert, Box, Button, Container, Divider, Paper, TextField, Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  cambiarPassword, iniciarSesion, iniciarSesionDesarrollo, obtenerConfiguracionAuth, useSesion,
  type Sesion,
} from "../../shared/api/sesion";

/** P01 - Inicio de sesion propio de GTE (usuario + contraseña, sin proveedor externo). */
export function LoginPage() {
  const [dominio, setDominio] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const establecer = useSesion((e) => e.establecer);

  const [dominioDesarrollo, setDominioDesarrollo] = useState("");
  const [errorDesarrollo, setErrorDesarrollo] = useState<string | null>(null);
  const [enviandoDesarrollo, setEnviandoDesarrollo] = useState(false);

  // Cambio de contraseña forzado: primer login o reset de administrador
  const [sesionPendiente, setSesionPendiente] = useState<Sesion | null>(null);
  const [passwordNueva, setPasswordNueva] = useState("");
  const [passwordConfirmacion, setPasswordConfirmacion] = useState("");
  const [errorCambio, setErrorCambio] = useState<string | null>(null);
  const [enviandoCambio, setEnviandoCambio] = useState(false);

  const configuracion = useQuery({
    queryKey: ["auth-configuracion"],
    queryFn: obtenerConfiguracionAuth,
  });

  const entrar = async () => {
    setEnviando(true);
    setError(null);
    try {
      const { sesion, requiereCambioPassword } = await iniciarSesion(dominio.trim(), password);
      if (requiereCambioPassword) {
        setSesionPendiente(sesion);
      } else {
        establecer(sesion);
      }
    } catch (e) {
      setError(e instanceof ErrorApi ? e.message : "No se pudo iniciar sesion.");
    } finally {
      setEnviando(false);
    }
  };

  const entrarDesarrollo = async () => {
    setEnviandoDesarrollo(true);
    setErrorDesarrollo(null);
    try {
      const { sesion } = await iniciarSesionDesarrollo(dominioDesarrollo.trim());
      establecer(sesion);
    } catch (e) {
      setErrorDesarrollo(e instanceof ErrorApi ? e.message : "No se pudo iniciar sesion.");
    } finally {
      setEnviandoDesarrollo(false);
    }
  };

  const confirmarCambioPassword = async () => {
    if (!sesionPendiente) return;
    setEnviandoCambio(true);
    setErrorCambio(null);
    try {
      await cambiarPassword(password, passwordNueva);
      establecer(sesionPendiente);
    } catch (e) {
      setErrorCambio(e instanceof ErrorApi ? e.message : "No se pudo cambiar la contraseña.");
    } finally {
      setEnviandoCambio(false);
    }
  };

  if (sesionPendiente) {
    const valido = passwordNueva.length >= 8 && passwordNueva === passwordConfirmacion;
    return (
      <Container maxWidth="sm" sx={{ pt: 10 }}>
        <Paper variant="outlined" sx={{ p: 4 }}>
          <Typography variant="h5" sx={{ fontWeight: 700, mb: 1 }}>Cambia tu contraseña</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Es tu primer ingreso o un administrador restablecio tu contraseña. Define una nueva
            antes de continuar.
          </Typography>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <TextField autoFocus size="small" type="password" label="Contraseña nueva"
              value={passwordNueva} onChange={(e) => setPasswordNueva(e.target.value)}
              helperText="Minimo 8 caracteres" />
            <TextField size="small" type="password" label="Confirmar contraseña"
              value={passwordConfirmacion} onChange={(e) => setPasswordConfirmacion(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter" && valido) void confirmarCambioPassword(); }} />
            {errorCambio !== null && <Alert severity="error">{errorCambio}</Alert>}
            <Button variant="contained" disabled={enviandoCambio || !valido}
              onClick={() => void confirmarCambioPassword()}>
              Guardar y entrar
            </Button>
          </Box>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ pt: 10 }}>
      <Paper variant="outlined" sx={{ p: 4 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 1 }}>GTE</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Gestor Tecnologico Empresarial
        </Typography>

        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <TextField autoFocus size="small" label="Cuenta de dominio" placeholder="aviramontes"
            value={dominio} onChange={(e) => setDominio(e.target.value)} />
          <TextField size="small" type="password" label="Contraseña" value={password}
            onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => { if (e.key === "Enter" && dominio.trim() && password) void entrar(); }} />
          {error !== null && <Alert severity="error">{error}</Alert>}
          <Button variant="contained" disabled={enviando || !dominio.trim() || !password}
            onClick={() => void entrar()}>
            Entrar
          </Button>
        </Box>

        {configuracion.data?.emisorDesarrollo && (
          <>
            <Divider sx={{ my: 3 }} />
            <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
              Ambiente de desarrollo: atajo sin contraseña, por cuenta de dominio.
            </Typography>
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField size="small" placeholder="aviramontes" value={dominioDesarrollo}
                onChange={(e) => setDominioDesarrollo(e.target.value)}
                onKeyDown={(e) => { if (e.key === "Enter" && dominioDesarrollo.trim()) void entrarDesarrollo(); }} />
              <Button variant="outlined" disabled={enviandoDesarrollo || !dominioDesarrollo.trim()}
                onClick={() => void entrarDesarrollo()}>
                Entrar (dev)
              </Button>
            </Box>
            {errorDesarrollo !== null && <Alert severity="error" sx={{ mt: 1 }}>{errorDesarrollo}</Alert>}
          </>
        )}

        {configuracion.isError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            No se pudo contactar la API. Verifica que este arriba y vuelve a intentar.
          </Alert>
        )}
      </Paper>
    </Container>
  );
}
