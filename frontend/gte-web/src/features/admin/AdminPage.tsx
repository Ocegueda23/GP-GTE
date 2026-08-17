import { useState } from "react";
import { Box, Tab, Tabs, Typography } from "@mui/material";
import { useSesion } from "../../shared/api/sesion";
import { ProyectosTab } from "./ProyectosTab";
import { EquiposTab } from "./EquiposTab";
import { UsuariosTab } from "./UsuariosTab";
import { RolesTab } from "./RolesTab";
import { HorariosTab } from "./HorariosTab";
import { AmbientesTab } from "./AmbientesTab";
import { AreasTab } from "./AreasTab";
import { PuestosTab } from "./PuestosTab";

const PESTANAS = [
  { clave: "proyectos", etiqueta: "Proyectos", permiso: "ADM.Usuarios" },
  { clave: "equipos", etiqueta: "Equipos", permiso: "ADM.Usuarios" },
  { clave: "usuarios", etiqueta: "Usuarios", permiso: "ADM.Usuarios" },
  { clave: "roles", etiqueta: "Roles", permiso: "ADM.Roles" },
  { clave: "horarios", etiqueta: "Horarios", permiso: "ADM.Usuarios" },
  { clave: "ambientes", etiqueta: "Ambientes", permiso: "ADM.Usuarios" },
  { clave: "areas", etiqueta: "Areas", permiso: "ADM.Usuarios" },
  { clave: "puestos", etiqueta: "Puestos", permiso: "ADM.Usuarios" },
] as const;

/** P20-P22 - Administracion: proyectos, equipos, usuarios, roles, horarios y ambientes. */
export function AdminPage() {
  const { puede } = useSesion();
  const disponibles = PESTANAS.filter((p) => puede(p.permiso));
  const [pestana, setPestana] = useState<string>(disponibles[0]?.clave ?? "proyectos");

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>Administracion</Typography>
      <Tabs value={pestana} onChange={(_, valor: string) => setPestana(valor)} sx={{ mb: 2 }}>
        {disponibles.map((p) => <Tab key={p.clave} value={p.clave} label={p.etiqueta} />)}
      </Tabs>
      {pestana === "proyectos" && <ProyectosTab />}
      {pestana === "equipos" && <EquiposTab />}
      {pestana === "usuarios" && <UsuariosTab />}
      {pestana === "roles" && <RolesTab />}
      {pestana === "horarios" && <HorariosTab />}
      {pestana === "ambientes" && <AmbientesTab />}
      {pestana === "areas" && <AreasTab />}
      {pestana === "puestos" && <PuestosTab />}
    </Box>
  );
}
