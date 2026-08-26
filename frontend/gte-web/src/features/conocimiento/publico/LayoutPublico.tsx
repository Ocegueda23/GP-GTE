import type { ReactNode } from "react";
import { Box, Divider, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

/**
 * Shell de las paginas ANONIMAS de la base de conocimiento. Deliberadamente NO reutiliza
 * el layout de la aplicacion: el AppBar y el Drawer internos listan los 20 modulos de GTE
 * y eso le revelaria la estructura interna del sistema a cualquier visitante sin sesion.
 * Aqui solo hay marca, un enlace de acceso para el equipo y el contenido.
 */
export function LayoutPublico({ children }: { children: ReactNode }) {
  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default", display: "flex", flexDirection: "column" }}>
      <Box
        component="header"
        sx={{
          bgcolor: "background.paper", borderBottom: 1, borderColor: "divider",
          px: { xs: 2, md: 5 }, py: 1.5,
        }}
      >
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", gap: 2 }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", minWidth: 0 }}>
            <Typography
              component={RouterLink} to="/publico/conocimiento"
              sx={{ fontWeight: 700, letterSpacing: 0.5, color: "primary.main", textDecoration: "none" }}
            >
              GTE
            </Typography>
            <Divider orientation="vertical" flexItem />
            <Typography variant="body2" color="text.secondary" noWrap>
              Base de Conocimiento
            </Typography>
          </Stack>
          <Typography
            component={RouterLink} to="/"
            variant="body2"
            sx={{ fontWeight: 600, color: "text.secondary", textDecoration: "none", whiteSpace: "nowrap" }}
          >
            ¿Eres del equipo? Acceder
          </Typography>
        </Stack>
      </Box>

      <Box component="main" sx={{ flex: 1 }}>
        {children}
      </Box>

      <Box
        component="footer"
        sx={{ borderTop: 1, borderColor: "divider", py: 2.5, px: 2, textAlign: "center" }}
      >
        <Typography variant="caption" color="text.disabled">
          Interflo - Departamento de Desarrollo
        </Typography>
      </Box>
    </Box>
  );
}
