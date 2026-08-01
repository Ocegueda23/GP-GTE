import {
  AppBar, Badge, Box, Button, Chip, Container, CssBaseline, Divider, IconButton, ListItemText,
  Menu, MenuItem, ThemeProvider, Toolbar, Tooltip, Typography, createTheme,
} from "@mui/material";
import NotificationsIcon from "@mui/icons-material/Notifications";
import { useState } from "react";
import { BrowserRouter, Link as RouterLink, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  marcarNotificacionLeida, marcarTodasNotificacionesLeidas, obtenerNotificaciones,
} from "./shared/api/notificaciones";
import { useConexionTiempoReal } from "./shared/tiempoReal/useConexionTiempoReal";
import { BandejaPage } from "./features/trabajo/BandejaPage";
import { DetallePage } from "./features/workitem/DetallePage";
import { MiDiaPage } from "./features/midia/MiDiaPage";
import { PortalPage } from "./features/solicitudes/PortalPage";
import { TriagePage } from "./features/triage/TriagePage";
import { BacklogPage } from "./features/planeacion/BacklogPage";
import { TableroPage } from "./features/planeacion/TableroPage";
import { QaPage } from "./features/calidad/QaPage";
import { ReleasesPage } from "./features/entregas/ReleasesPage";
import { GuardiaSesion } from "./features/sesion/GuardiaSesion";
import { AdminPage } from "./features/admin/AdminPage";
import { cerrarSesion, cerrarSesionServidor, useSesion } from "./shared/api/sesion";

const tema = createTheme({
  palette: {
    primary: { main: "#334155" },   // slate 700
    secondary: { main: "#0f766e" }, // teal 700
    background: { default: "#f6f7f9" },
  },
  typography: {
    fontSize: 13.5,
    h5: { fontSize: "1.25rem" },
  },
  components: {
    MuiPaper: { defaultProps: { elevation: 0 } },
  },
});

/** Opciones del menu con el/los permisos que las habilita (null = disponible para todos). */
const NAVEGACION: { ruta: string; etiqueta: string; permiso: string | string[] | null }[] = [
  { ruta: "/mi-dia", etiqueta: "Mi dia", permiso: null },
  { ruta: "/trabajo", etiqueta: "Trabajo", permiso: null },
  { ruta: "/tablero", etiqueta: "Tablero", permiso: null },
  { ruta: "/backlog", etiqueta: "Backlog", permiso: "PLA.GestionarSprints" },
  { ruta: "/qa", etiqueta: "QA", permiso: "QA.Ejecutar" },
  { ruta: "/releases", etiqueta: "Releases", permiso: "REL.Crear" },
  { ruta: "/solicitudes", etiqueta: "Solicitudes", permiso: null },
  { ruta: "/triage", etiqueta: "Triage", permiso: "SOL.Triage" },
  { ruta: "/admin", etiqueta: "Administracion", permiso: ["ADM.Usuarios", "ADM.Roles"] },
];

function CampanaNotificaciones() {
  const navegar = useNavigate();
  const clienteQuery = useQueryClient();
  const [anclaNotificaciones, setAnclaNotificaciones] = useState<HTMLElement | null>(null);

  const notificaciones = useQuery({
    queryKey: ["notificaciones"],
    queryFn: () => obtenerNotificaciones(true),
    refetchOnWindowFocus: true,
  });

  const abrirNotificacion = async (idNotificacion: number, url: string | null) => {
    setAnclaNotificaciones(null);
    try {
      await marcarNotificacionLeida(idNotificacion);
    } finally {
      await clienteQuery.invalidateQueries({ queryKey: ["notificaciones"] });
      if (url) navegar(url);
    }
  };

  const marcarTodas = async () => {
    try {
      await marcarTodasNotificacionesLeidas();
    } finally {
      await clienteQuery.invalidateQueries({ queryKey: ["notificaciones"] });
    }
  };

  const pendientes = notificaciones.data ?? [];

  return (
    <>
      <IconButton color="inherit" onClick={(e) => setAnclaNotificaciones(e.currentTarget)}>
        <Badge badgeContent={pendientes.length} color="error">
          <NotificationsIcon />
        </Badge>
      </IconButton>
      <Menu
        anchorEl={anclaNotificaciones}
        open={anclaNotificaciones !== null}
        onClose={() => setAnclaNotificaciones(null)}
        slotProps={{ paper: { sx: { minWidth: 320, maxWidth: 400 } } }}
      >
        {pendientes.length === 0 && (
          <MenuItem disabled>Sin notificaciones pendientes.</MenuItem>
        )}
        {pendientes.map((notificacion) => (
          <MenuItem
            key={notificacion.idNotificacion}
            onClick={() => void abrirNotificacion(notificacion.idNotificacion, notificacion.url)}
            sx={{ whiteSpace: "normal" }}
          >
            <ListItemText primary={notificacion.titulo} secondary={notificacion.mensaje} />
          </MenuItem>
        ))}
        {pendientes.length > 0 && [
          <Divider key="divisor" />,
          <MenuItem key="marcar-todas" onClick={() => void marcarTodas()}>
            Marcar todas como leidas
          </MenuItem>,
        ]}
      </Menu>
    </>
  );
}

function BarraSuperior() {
  const { sesion, establecer, puede } = useSesion();
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  useConexionTiempoReal();

  const salir = () => {
    void cerrarSesionServidor();
    cerrarSesion();
    establecer(null);
    setAncla(null);
  };

  return (
    <AppBar position="static">
      <Toolbar variant="dense">
        <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: 1 }}>GTE</Typography>
        <Box sx={{ ml: 3, display: "flex", gap: 1, flexWrap: "wrap", flex: 1 }}>
          {NAVEGACION
            .filter((opcion) => opcion.permiso === null
              || (Array.isArray(opcion.permiso) ? opcion.permiso.some(puede) : puede(opcion.permiso)))
            .map((opcion) => (
              <Button key={opcion.ruta} color="inherit" component={RouterLink} to={opcion.ruta}>
                {opcion.etiqueta}
              </Button>
            ))}
        </Box>
        <CampanaNotificaciones />
        <Tooltip title={`${sesion?.dominio} - ${sesion?.roles.join(", ")}`}>
          <Chip
            label={sesion?.nombre ?? ""}
            onClick={(e) => setAncla(e.currentTarget)}
            sx={{ color: "inherit", borderColor: "rgba(255,255,255,0.5)", ml: 1 }}
            variant="outlined"
          />
        </Tooltip>
        <Menu anchorEl={ancla} open={ancla !== null} onClose={() => setAncla(null)}>
          <MenuItem disabled>{sesion?.correo ?? sesion?.dominio}</MenuItem>
          <MenuItem onClick={salir}>Cerrar sesion</MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}

export default function App() {
  return (
    <ThemeProvider theme={tema}>
      <CssBaseline />
      <BrowserRouter>
        <GuardiaSesion>
          <BarraSuperior />
          <Container maxWidth={false} sx={{ px: 0 }}>
            <Box>
              <Routes>
                <Route path="/" element={<Navigate to="/mi-dia" replace />} />
                <Route path="/mi-dia" element={<MiDiaPage />} />
                <Route path="/trabajo" element={<BandejaPage />} />
                <Route path="/wi/:folio" element={<DetallePage />} />
                <Route path="/tablero" element={<TableroPage />} />
                <Route path="/backlog" element={<BacklogPage />} />
                <Route path="/qa" element={<QaPage />} />
                <Route path="/releases" element={<ReleasesPage />} />
                <Route path="/solicitudes" element={<PortalPage />} />
                <Route path="/triage" element={<TriagePage />} />
                <Route path="/admin" element={<AdminPage />} />
              </Routes>
            </Box>
          </Container>
        </GuardiaSesion>
      </BrowserRouter>
    </ThemeProvider>
  );
}
