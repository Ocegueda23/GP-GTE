import { Box } from "@mui/material";

export function ManualUsuarioPage() {
  return (
    <Box sx={{ height: "calc(100vh - 48px)" }}>
      <iframe
        src="/manual-usuario.html"
        title="Manual de usuario de GTE"
        style={{ width: "100%", height: "100%", border: "none", display: "block" }}
      />
    </Box>
  );
}
