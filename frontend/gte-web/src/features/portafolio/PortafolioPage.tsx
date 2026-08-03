import { useState } from "react";
import { Alert, Box, Tab, Tabs, Typography } from "@mui/material";
import { useSesion } from "../../shared/api/sesion";
import { CosteoTab } from "./CosteoTab";
import { OkrTab } from "./OkrTab";

const PESTANAS = [
  // RPT.Costos: dato sensible (costo/hora cercano a banda salarial) -- ver tambien lo
  // habilita, ademas de POR.GestionarCosteo (quien administra el catalogo).
  { clave: "costeo", etiqueta: "Costeo", permiso: ["POR.GestionarCosteo", "RPT.Costos"] },
  { clave: "okr", etiqueta: "OKR", permiso: ["POR.GestionarOkr"] },
] as const;

/** Portafolio (A5): costeo real por proyecto y objetivos trimestrales (OKR). */
export function PortafolioPage() {
  const { puede } = useSesion();
  const disponibles = PESTANAS.filter((p) => p.permiso.some(puede));
  const [pestanaElegida, setPestanaElegida] = useState<string | null>(null);
  // Si la pestaña elegida ya no esta disponible (o nunca se eligio), cae en la primera
  // disponible -- nunca en una pestaña oculta por permiso, ni siquiera navegando directo
  // a la URL.
  const pestana = disponibles.some((p) => p.clave === pestanaElegida) ? pestanaElegida : disponibles[0]?.clave;

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Portafolio</Typography>
      {disponibles.length === 0 ? (
        <Alert severity="warning">No tienes permiso para ver esta sección.</Alert>
      ) : (
        <>
          <Tabs value={pestana} onChange={(_, valor: string) => setPestanaElegida(valor)} sx={{ mb: 2 }}>
            {disponibles.map((p) => <Tab key={p.clave} value={p.clave} label={p.etiqueta} />)}
          </Tabs>
          {pestana === "costeo" && <CosteoTab />}
          {pestana === "okr" && <OkrTab />}
        </>
      )}
    </Box>
  );
}
