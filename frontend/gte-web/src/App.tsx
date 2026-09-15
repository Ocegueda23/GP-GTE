import {
  AppBar, Badge, Box, Button, Chip, CssBaseline, Divider, Drawer, IconButton, List,
  ListItemButton, ListItemText, Menu, MenuItem, Stack, ThemeProvider, Toolbar, Tooltip,
  Typography, createTheme, type PaletteMode,
} from "@mui/material";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import MenuIcon from "@mui/icons-material/Menu";
import NotificationsIcon from "@mui/icons-material/Notifications";
import { useEffect, useMemo, useState } from "react";
import {
  BrowserRouter, Link as RouterLink, Navigate, Route, Routes, useLocation, useNavigate,
} from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  marcarNotificacionLeida, marcarTodasNotificacionesLeidas, obtenerNotificaciones,
} from "./shared/api/notificaciones";
import { useConexionTiempoReal } from "./shared/tiempoReal/useConexionTiempoReal";
import { VERSION_FRONTEND, obtenerVersionApi } from "./shared/api/version";
import { BandejaPage } from "./features/trabajo/BandejaPage";
import { DetallePage } from "./features/workitem/DetallePage";
import { MiDiaPage } from "./features/midia/MiDiaPage";
import { PortalPage } from "./features/solicitudes/PortalPage";
import { TriagePage } from "./features/triage/TriagePage";
import { PortalTicketsPage } from "./features/soporte/PortalTicketsPage";
import { BandejaTicketsPage } from "./features/soporte/BandejaTicketsPage";
import { DetalleTicketPage } from "./features/soporte/DetalleTicketPage";
import { BandejaIncidentesPage } from "./features/operacion/BandejaIncidentesPage";
import { DetalleIncidentePage } from "./features/operacion/DetalleIncidentePage";
import { PortafolioPage } from "./features/portafolio/PortafolioPage";
import { BacklogPage } from "./features/planeacion/BacklogPage";
import { TableroPage } from "./features/planeacion/TableroPage";
import { ReleasesPage } from "./features/entregas/ReleasesPage";
import { SolicitudDesplieguePage } from "./features/entregas/SolicitudDesplieguePage";
import { GuardiaSesion } from "./features/sesion/GuardiaSesion";
import { DashboardEjecutivoPage } from "./features/dashboard/DashboardEjecutivoPage";
import { IndicadoresEjecutivosPage } from "./features/indicadoresEjecutivos/IndicadoresEjecutivosPage";
import { ActividadUsuarioPage } from "./features/reportes/ActividadUsuarioPage";
import { CatalogoReportesPage } from "./features/reportes/CatalogoReportesPage";
import { AdminPage } from "./features/admin/AdminPage";
import { WorkflowsPage } from "./features/admin/WorkflowsPage";
import { CatalogosPage } from "./features/catalogos/CatalogosPage";
import { CatalogosAdminPage } from "./features/catalogos/admin/CatalogosAdminPage";
import { ManualUsuarioPage } from "./features/ayuda/ManualUsuarioPage";
import { CentroMandoPage as AyudaCentroMandoPage } from "./features/ayuda/CentroMandoPage";
import { CentroMandoPage } from "./features/centroMando/CentroMandoPage";
import { EvaluacionResponsablePage } from "./features/centroMando/EvaluacionResponsablePage";
import { CatalogoIndicadoresPage } from "./features/centroMando/CatalogoIndicadoresPage";
import { ConocimientoPage } from "./features/conocimiento/ConocimientoPage";
import { ReglasNegocioPage } from "./features/reglasNegocio/ReglasNegocioPage";
import { DetalleReglaPage } from "./features/reglasNegocio/DetalleReglaPage";
import { DetalleArticuloPage } from "./features/conocimiento/DetalleArticuloPage";
import { ConocimientoPublicoPage } from "./features/conocimiento/publico/ConocimientoPublicoPage";
import { DetalleArticuloPublicoPage } from "./features/conocimiento/publico/DetalleArticuloPublicoPage";
import {
  cerrarSesion, cerrarSesionServidor, estaSuplantando, obtenerNombreSuplantador,
  terminarSuplantacion, useSesion,
} from "./shared/api/sesion";

const CLAVE_TEMA = "gte.tema";

/** Los tres inputs nativos de fecha/hora que dibujan su propio icono de calendario. */
const SELECTOR_ICONO_CALENDARIO = [
  "input[type=date]::-webkit-calendar-picker-indicator",
  "input[type=datetime-local]::-webkit-calendar-picker-indicator",
  "input[type=time]::-webkit-calendar-picker-indicator",
].join(", ");

/**
 * primary/secondary quedan fijos en ambos modos (MUI ya resuelve un contrastText legible
 * para cada uno); solo se fija background.default en claro para no perder el fondo actual
 * -- en oscuro se deja el default de MUI (#121212), ya pensado para contraste WCAG AA.
 */
function construirTema(modo: PaletteMode) {
  return createTheme({
    palette: {
      mode: modo,
      primary: { main: "#334155" },   // slate 700
      secondary: { main: "#0f766e" }, // teal 700
      background: modo === "light"
        ? { default: "#f6f7f9" }
        // MuiPaper fuerza elevation:0 (sin el overlay que MUI usaria para diferenciar
        // superficies en oscuro), y el default de MUI deja paper == default (#121212
        // ambos) -- sin esto, tablas/tarjetas se funden con el fondo de la pagina.
        : { default: "#0f172a", paper: "#1e293b" },   // slate 900 / slate 800
      // Colores de fuente del modo oscuro tomados del esquema Dark+ del editor: el gris
      // azulado anterior (slate 100/400) se leia lavado sobre el fondo. Solo se tocan los
      // colores de texto/divisor; fondos, primary y secondary quedan igual.
      ...(modo === "dark"
        ? {
          text: { primary: "#e6e6e6", secondary: "#9d9d9d", disabled: "#6d6d6d" },
          // Los cuatro semanticos son tonos claros del Dark+ pensados para LEERSE sobre
          // el fondo oscuro (variantes text/outlined de Button, Chip, Alert). Como son
          // claros, contrastText se fija a mano al fondo oscuro: si se deja que MUI lo
          // calcule, la variante contained termina con texto blanco sobre relleno claro
          // y el texto del boton se pierde.
          info: { main: "#4fc1ff", contrastText: "#0f172a" },    // azul
          success: { main: "#89d185", contrastText: "#0f172a" }, // verde
          warning: { main: "#ce9178", contrastText: "#0f172a" }, // naranja
          error: { main: "#f48771", contrastText: "#0f172a" },   // rojo
          // primary/secondary son oscuros: su texto va en blanco, tambien explicito.
          primary: { main: "#334155", contrastText: "#ffffff" },
          secondary: { main: "#0f766e", contrastText: "#ffffff" },
          divider: "#3e3e42",
        }
        : {}),
    },
    typography: {
      fontSize: 13.5,
      h5: { fontSize: "1.25rem" },
    },
    components: {
      MuiPaper: { defaultProps: { elevation: 0 } },
      // Los titulos en oscuro van con el azul claro de identificadores del Dark+ para
      // separarlos del cuerpo de texto sin bajar el contraste. h6 queda fuera: lo usan
      // el titulo del AppBar y los encabezados de dialogo, que van sobre primary.
      ...(modo === "dark"
        ? {
          MuiTypography: {
            styleOverrides: {
              h1: { color: "#9cdcfe" },
              h2: { color: "#9cdcfe" },
              h3: { color: "#9cdcfe" },
              h4: { color: "#9cdcfe" },
              h5: { color: "#9cdcfe" },
            },
          },
          // Botones text/outlined: MUI pinta la etiqueta con el `main` del color, y
          // primary (#334155) / secondary (#0f766e) son tonos oscuros pensados para
          // RELLENO (AppBar, contained) -- sobre el fondo #0f172a la etiqueta quedaba
          // practicamente invisible. En oscuro se sustituyen por los tonos claros del
          // Dark+: azul para las acciones normales, verde azulado para secondary. Las
          // variantes con color error/success/warning ya heredan el rojo/verde/naranja
          // claros de la paleta, asi que no necesitan override.
          // Se usa `variants` (y no los slots textPrimary/outlinedPrimary): MUI 9 ya no
          // genera esas clases compuestas, los overrides con ese nombre no aplican.
          MuiButton: {
            variants: [
              {
                props: { variant: "text", color: "primary" },
                style: {
                  color: "#4fc1ff",
                  "&:hover": { backgroundColor: "rgba(79, 193, 255, 0.10)" },
                },
              },
              {
                props: { variant: "outlined", color: "primary" },
                style: {
                  color: "#4fc1ff",
                  borderColor: "rgba(79, 193, 255, 0.5)",
                  "&:hover": {
                    borderColor: "#4fc1ff",
                    backgroundColor: "rgba(79, 193, 255, 0.10)",
                  },
                },
              },
              {
                props: { variant: "text", color: "secondary" },
                style: {
                  color: "#4ec9b0",
                  "&:hover": { backgroundColor: "rgba(78, 201, 176, 0.10)" },
                },
              },
              {
                props: { variant: "outlined", color: "secondary" },
                style: {
                  color: "#4ec9b0",
                  borderColor: "rgba(78, 201, 176, 0.5)",
                  "&:hover": {
                    borderColor: "#4ec9b0",
                    backgroundColor: "rgba(78, 201, 176, 0.10)",
                  },
                },
              },
            ],
          },
          // IconButton color="primary" tiene el mismo problema (slate sobre fondo oscuro).
          MuiIconButton: {
            variants: [
              { props: { color: "primary" }, style: { color: "#4fc1ff" } },
              { props: { color: "secondary" }, style: { color: "#4ec9b0" } },
            ],
          },
          // Chip outlined: igual que los botones, la etiqueta se pinta con el `main` del
          // color y primary/secondary son tonos de relleno, ilegibles sobre el fondo.
          MuiChip: {
            variants: [
              {
                props: { variant: "outlined", color: "primary" },
                style: { color: "#4fc1ff", borderColor: "rgba(79, 193, 255, 0.5)" },
              },
              {
                props: { variant: "outlined", color: "secondary" },
                style: { color: "#4ec9b0", borderColor: "rgba(78, 201, 176, 0.5)" },
              },
            ],
          },
          // Tabs: la pestana activa y su subrayado usan primary.main (#334155), que sobre
          // el fondo oscuro no se distingue de las inactivas. Van al azul del Dark+.
          MuiTabs: { styleOverrides: { indicator: { backgroundColor: "#4fc1ff" } } },
          MuiTab: {
            styleOverrides: {
              root: {
                color: "#9d9d9d",
                "&.Mui-selected": { color: "#4fc1ff" },
              },
            },
          },
          // Triangulo desplegable de los combos (Select y Autocomplete) en el mismo azul.
          MuiSelect: { styleOverrides: { icon: { color: "#4fc1ff" } } },
          MuiNativeSelect: { styleOverrides: { icon: { color: "#4fc1ff" } } },
          MuiAutocomplete: {
            styleOverrides: {
              popupIndicator: { color: "#4fc1ff" },
              clearIndicator: { color: "#9d9d9d" },
            },
          },
          // El icono de calendario de <input type="date"> lo dibuja el navegador y sale
          // negro (no hereda color); solo se puede recolorear con filter. La cadena
          // aproxima el azul #4fc1ff sobre el glifo negro original.
          MuiCssBaseline: {
            styleOverrides: {
              [SELECTOR_ICONO_CALENDARIO]: {
                cursor: "pointer",
                filter: "invert(72%) sepia(41%) saturate(1352%) hue-rotate(174deg) "
                  + "brightness(103%) contrast(101%)",
              },
            },
          },
        }
        : {}),
      // Los folios/titulos enlazados se pintan como texto normal en muchas pantallas
      // (Typography+RouterLink con color explicito propio); el <Link> de MUI sin color
      // usa "primary" por default, que en modo oscuro (slate 700 sobre fondo casi negro)
      // se pierde. "info" da buen contraste en ambos modos sin tocar primary/secondary,
      // que siguen usandose para AppBar/botones.
      MuiLink: { defaultProps: { color: "info" } },
    },
  });
}

function useModoTema() {
  const [modo, setModo] = useState<PaletteMode>(
    () => (localStorage.getItem(CLAVE_TEMA) === "dark" ? "dark" : "light"),
  );
  useEffect(() => {
    localStorage.setItem(CLAVE_TEMA, modo);
  }, [modo]);
  return {
    modo,
    alternar: () => setModo((previo) => (previo === "light" ? "dark" : "light")),
  };
}

const ANCHO_MENU = 220;

/** Opciones del menu con el/los permisos que las habilita (null = disponible para todos). */
const NAVEGACION: { ruta: string; etiqueta: string; permiso: string | string[] | null }[] = [
  { ruta: "/mi-dia", etiqueta: "Mi dia", permiso: null },
  { ruta: "/trabajo", etiqueta: "Trabajo", permiso: null },
  { ruta: "/tablero", etiqueta: "Tablero", permiso: null },
  { ruta: "/backlog", etiqueta: "Backlog", permiso: "PLA.GestionarSprints" },
  { ruta: "/releases", etiqueta: "Releases", permiso: "REL.Crear" },
  { ruta: "/solicitudes", etiqueta: "Solicitudes", permiso: null },
  { ruta: "/triage", etiqueta: "Revision de solicitudes", permiso: "SOL.Triage" },
  { ruta: "/tickets", etiqueta: "Mis tickets", permiso: null },
  { ruta: "/soporte", etiqueta: "Mesa de ayuda", permiso: "TKT.Atender" },
  { ruta: "/operacion/incidentes", etiqueta: "Incidentes", permiso: "INC.Gestionar" },
  { ruta: "/dashboard-ejecutivo", etiqueta: "Dashboard ejecutivo", permiso: null },
  { ruta: "/indicadores-ejecutivos", etiqueta: "Indicadores ejecutivos", permiso: ["DASH.Ejecutivo", "DASH.VerDepartamento"] },
  { ruta: "/centro-mando", etiqueta: "Centro de Mando TI", permiso: "GES.Ver" },
  { ruta: "/centro-mando/catalogo", etiqueta: "Indicadores de gestion", permiso: "GES.Administrar" },
  { ruta: "/portafolio", etiqueta: "Portafolio", permiso: ["POR.GestionarCosteo", "POR.GestionarOkr", "RPT.Costos"] },
  { ruta: "/reportes", etiqueta: "Reportes", permiso: ["RPT.Ver", "RPT.Costos", "RPT.Auditoria", "RPT.Actividad"] },
  { ruta: "/catalogos", etiqueta: "Catalogos", permiso: null },
  { ruta: "/catalogos/admin", etiqueta: "Administrar catalogos", permiso: "ADM.CatalogoGenerico" },
  { ruta: "/admin", etiqueta: "Administracion", permiso: ["ADM.Usuarios", "ADM.Roles"] },
  { ruta: "/admin/workflows", etiqueta: "Workflows", permiso: "ADM.Workflows" },
  // P23 es "Todos" en el Documento Maestro: leer no exige permiso (escribir si, CON.Administrar).
  { ruta: "/conocimiento", etiqueta: "Base de conocimiento", permiso: null },
  { ruta: "/reglas-negocio", etiqueta: "Reglas de negocio", permiso: "RGN.Ver" },
  { ruta: "/ayuda", etiqueta: "Ayuda", permiso: null },
  // El Manual de usuario es para todos; el Centro de Mando TI describe como se evalua a
  // cada responsable de area, asi que exige permiso (el backend tambien lo valida).
  { ruta: "/ayuda/centro-mando", etiqueta: "Centro de Mando TI", permiso: "AYU.CentroMando" },
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
      <IconButton
        color="inherit"
        aria-label="Notificaciones"
        onClick={(e) => {
          setAnclaNotificaciones(e.currentTarget);
          if ("Notification" in window && Notification.permission === "default") {
            void Notification.requestPermission();
          }
        }}
      >
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

function ListaNavegacion({ alNavegar }: { alNavegar?: () => void }) {
  const { puede } = useSesion();
  const ubicacion = useLocation();

  return (
    <List sx={{ pt: 1 }}>
      {NAVEGACION
        .filter((opcion) => opcion.permiso === null
          || (Array.isArray(opcion.permiso) ? opcion.permiso.some(puede) : puede(opcion.permiso)))
        .map((opcion) => (
          <ListItemButton
            key={opcion.ruta}
            component={RouterLink}
            to={opcion.ruta}
            selected={ubicacion.pathname === opcion.ruta}
            onClick={alNavegar}
          >
            <ListItemText primary={opcion.etiqueta} />
          </ListItemButton>
        ))}
    </List>
  );
}

/**
 * Sello de version debajo del nombre del sistema. Existe para responder de un golpe de
 * vista "este servidor tiene la ultima publicacion?": el numero grande es el del bundle
 * (VITE_VERSION, estampado por publicar.bat) y el tooltip trae el del API. Si no
 * coinciden, quedo a medias el despliegue (tipico: se copio wwwroot pero no los DLL, o al
 * reves) y el sello se pinta en ambar para que salte a la vista.
 */
function SelloVersion() {
  const { data: versionApi } = useQuery({
    queryKey: ["version-api"],
    queryFn: obtenerVersionApi,
    staleTime: Infinity,
    retry: false,
  });

  const sello = VERSION_FRONTEND ?? versionApi?.version ?? null;
  if (!sello) return null;

  const descuadre = VERSION_FRONTEND !== null
    && versionApi !== undefined
    && versionApi.version !== VERSION_FRONTEND;

  const detalle = versionApi
    ? `API ${versionApi.version} · ${versionApi.ambiente}`
    : "No se pudo consultar la version del API";

  return (
    <Tooltip title={descuadre ? `${detalle}. El frontend y el API no coinciden: el despliegue quedo a medias.` : detalle}>
      <Typography
        variant="caption"
        sx={{
          display: "block",
          lineHeight: 1,
          letterSpacing: 0,
          opacity: descuadre ? 1 : 0.7,
          color: descuadre ? "warning.main" : "inherit",
          cursor: "default",
        }}
      >
        v{sello}{descuadre ? " !" : ""}
      </Typography>
    </Tooltip>
  );
}

function BarraSuperior({ alAbrirMenu, modo, alternarModo }: {
  alAbrirMenu: () => void; modo: PaletteMode; alternarModo: () => void;
}) {
  const { sesion, establecer } = useSesion();
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  useConexionTiempoReal();

  const salir = () => {
    void cerrarSesionServidor();
    cerrarSesion();
    establecer(null);
    setAncla(null);
  };

  const salirDeSuplantacion = async () => {
    await terminarSuplantacion();
    window.location.href = "/";
  };

  return (
    <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
      <Toolbar variant="dense">
        <IconButton color="inherit" aria-label="Abrir menu" onClick={alAbrirMenu}
          sx={{ display: { sm: "none" }, mr: 1 }}>
          <MenuIcon />
        </IconButton>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: 1, lineHeight: 1.15 }}>GTE</Typography>
          <SelloVersion />
        </Box>
        {estaSuplantando() && (
          <Stack direction="row" spacing={1} sx={{ alignItems: "center", mr: 1 }}>
            <Chip
              color="warning"
              size="small"
              label={`Actuando como ${sesion?.nombre ?? ""}${
                obtenerNombreSuplantador() ? ` · admin: ${obtenerNombreSuplantador()}` : ""
              }`}
            />
            <Button
              size="small" color="inherit" variant="outlined"
              sx={{ borderColor: "rgba(255,255,255,0.5)" }}
              onClick={() => void salirDeSuplantacion()}
            >
              Salir
            </Button>
          </Stack>
        )}
        <Tooltip title={modo === "light" ? "Tema oscuro" : "Tema claro"}>
          <IconButton color="inherit" aria-label="Alternar tema claro/oscuro" onClick={alternarModo}>
            {modo === "light" ? <DarkModeIcon /> : <LightModeIcon />}
          </IconButton>
        </Tooltip>
        <CampanaNotificaciones />
        <Tooltip title={`${sesion?.dominio} - ${sesion?.roles.join(", ")}`}>
          <Chip
            label={sesion?.nombre ?? ""}
            onClick={(e) => setAncla(e.currentTarget)}
            sx={{
              color: "inherit", borderColor: "rgba(255,255,255,0.5)", ml: 1,
              maxWidth: { xs: 90, sm: "none" },
              "& .MuiChip-label": { overflow: "hidden", textOverflow: "ellipsis" },
            }}
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

/**
 * Shell autenticado: exige sesion y monta el AppBar, el menu de modulos y sus rutas.
 * El modo de tema llega por props (no llama a useModoTema): el hook guarda estado local,
 * asi que una segunda instancia dejaria al ThemeProvider sin enterarse del cambio.
 */
function AplicacionAutenticada({ modo, alternarModo }: { modo: PaletteMode; alternarModo: () => void }) {
  const [menuMovilAbierto, setMenuMovilAbierto] = useState(false);

  return (
        <GuardiaSesion>
          <Box sx={{ display: "flex" }}>
            <BarraSuperior alAbrirMenu={() => setMenuMovilAbierto(true)} modo={modo} alternarModo={alternarModo} />

            {/* Menu lateral fijo (pantallas medianas o mas grandes) */}
            <Drawer
              anchor="left"
              variant="permanent"
              sx={{
                display: { xs: "none", sm: "block" },
                width: ANCHO_MENU,
                flexShrink: 0,
                "& .MuiDrawer-paper": { width: ANCHO_MENU, boxSizing: "border-box" },
              }}
            >
              <Toolbar variant="dense" />
              <ListaNavegacion />
            </Drawer>

            {/* Menu lateral deslizable (celular) */}
            <Drawer
              anchor="left"
              variant="temporary"
              open={menuMovilAbierto}
              onClose={() => setMenuMovilAbierto(false)}
              ModalProps={{ keepMounted: true }}
              sx={{
                display: { xs: "block", sm: "none" },
                "& .MuiDrawer-paper": { width: ANCHO_MENU, boxSizing: "border-box" },
              }}
            >
              <Toolbar variant="dense" />
              <ListaNavegacion alNavegar={() => setMenuMovilAbierto(false)} />
            </Drawer>

            <Box component="main" sx={{ flexGrow: 1, width: { sm: `calc(100% - ${ANCHO_MENU}px)` } }}>
              <Toolbar variant="dense" />
              <Routes>
                <Route path="/" element={<Navigate to="/mi-dia" replace />} />
                <Route path="/mi-dia" element={<MiDiaPage />} />
                <Route path="/trabajo" element={<BandejaPage />} />
                <Route path="/wi/:folio" element={<DetallePage />} />
                <Route path="/tablero" element={<TableroPage />} />
                <Route path="/backlog" element={<BacklogPage />} />
                <Route path="/releases" element={<ReleasesPage />} />
                <Route path="/releases/:id/solicitud" element={<SolicitudDesplieguePage />} />
                <Route path="/solicitudes" element={<PortalPage />} />
                <Route path="/triage" element={<TriagePage />} />
                <Route path="/tickets" element={<PortalTicketsPage />} />
                <Route path="/tickets/:folio" element={<DetalleTicketPage />} />
                <Route path="/soporte" element={<BandejaTicketsPage />} />
                <Route path="/operacion/incidentes" element={<BandejaIncidentesPage />} />
                <Route path="/operacion/incidentes/:folio" element={<DetalleIncidentePage />} />
                <Route path="/dashboard-ejecutivo" element={<DashboardEjecutivoPage />} />
                <Route path="/indicadores-ejecutivos" element={<IndicadoresEjecutivosPage />} />
                <Route path="/portafolio" element={<PortafolioPage />} />
                <Route path="/reportes" element={<CatalogoReportesPage />} />
                <Route path="/reportes/actividad-usuario" element={<ActividadUsuarioPage />} />
                <Route path="/catalogos" element={<CatalogosPage />} />
                <Route path="/catalogos/admin" element={<CatalogosAdminPage />} />
                <Route path="/admin" element={<AdminPage />} />
                <Route path="/admin/workflows" element={<WorkflowsPage />} />
                <Route path="/conocimiento" element={<ConocimientoPage />} />
                <Route path="/conocimiento/:id" element={<DetalleArticuloPage />} />
                <Route path="/reglas-negocio" element={<ReglasNegocioPage />} />
                <Route path="/reglas-negocio/:id" element={<DetalleReglaPage />} />
                <Route path="/ayuda" element={<ManualUsuarioPage />} />
                <Route path="/ayuda/centro-mando" element={<AyudaCentroMandoPage />} />
                <Route path="/centro-mando" element={<CentroMandoPage />} />
                <Route path="/centro-mando/equipos/:idEquipo" element={<EvaluacionResponsablePage />} />
                <Route path="/centro-mando/catalogo" element={<CatalogoIndicadoresPage />} />
              </Routes>
            </Box>
          </Box>
        </GuardiaSesion>
  );
}

export default function App() {
  const { modo, alternar } = useModoTema();
  const tema = useMemo(() => construirTema(modo), [modo]);

  return (
    <ThemeProvider theme={tema}>
      <CssBaseline />
      <BrowserRouter>
        <Routes>
          {/*
            Base de conocimiento PUBLICA: fuera de GuardiaSesion a proposito (son las
            unicas rutas del SPA que se cargan sin sesion) y con su propio layout, que no
            expone el menu de modulos internos. El backend es el que decide que articulos
            son publicos; aqui solo se pinta lo que devuelve.
          */}
          <Route path="/publico/conocimiento" element={<ConocimientoPublicoPage />} />
          <Route path="/publico/conocimiento/:id" element={<DetalleArticuloPublicoPage />} />
          <Route path="/*" element={<AplicacionAutenticada modo={modo} alternarModo={alternar} />} />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}
