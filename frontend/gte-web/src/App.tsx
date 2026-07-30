import { AppBar, Box, Button, Container, CssBaseline, ThemeProvider, Toolbar, Typography, createTheme } from "@mui/material";
import { BrowserRouter, Link as RouterLink, Navigate, Route, Routes } from "react-router-dom";
import { BandejaPage } from "./features/trabajo/BandejaPage";
import { DetallePage } from "./features/workitem/DetallePage";
import { MiDiaPage } from "./features/midia/MiDiaPage";
import { PortalPage } from "./features/solicitudes/PortalPage";
import { TriagePage } from "./features/triage/TriagePage";

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

export default function App() {
  return (
    <ThemeProvider theme={tema}>
      <CssBaseline />
      <BrowserRouter>
        <AppBar position="static">
          <Toolbar variant="dense">
            <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: 1 }}>
              GTE
            </Typography>
            <Box sx={{ ml: 3, display: "flex", gap: 1 }}>
              <Button color="inherit" component={RouterLink} to="/mi-dia">Mi dia</Button>
              <Button color="inherit" component={RouterLink} to="/trabajo">Trabajo</Button>
              <Button color="inherit" component={RouterLink} to="/solicitudes">Solicitudes</Button>
              <Button color="inherit" component={RouterLink} to="/triage">Triage</Button>
            </Box>
          </Toolbar>
        </AppBar>
        <Container maxWidth={false} sx={{ px: 0 }}>
          <Box>
            <Routes>
              <Route path="/" element={<Navigate to="/mi-dia" replace />} />
              <Route path="/mi-dia" element={<MiDiaPage />} />
              <Route path="/trabajo" element={<BandejaPage />} />
              <Route path="/wi/:folio" element={<DetallePage />} />
              <Route path="/solicitudes" element={<PortalPage />} />
              <Route path="/triage" element={<TriagePage />} />
            </Routes>
          </Box>
        </Container>
      </BrowserRouter>
    </ThemeProvider>
  );
}
