import { useEffect, useState } from "react";
import {
  Box, Button, FormControlLabel, Switch, TextField,
} from "@mui/material";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import type { CatalogosBandeja } from "../../shared/api/workitems";
import { useFiltrosBandeja } from "./storeFiltros";

interface Props {
  catalogos: CatalogosBandeja | undefined;
}

/**
 * Filtros con la semantica heredada del GT: sin estatus seleccionado = abiertos
 * (Pendiente a Suspendido); la opcion Todos (-1) quita el filtro.
 */
export function BarraFiltros({ catalogos }: Props) {
  const { filtro, establecer, limpiar } = useFiltrosBandeja();
  const [textoLocal, setTextoLocal] = useState(filtro.texto);

  // Busqueda con debounce: una sola consulta al dejar de teclear
  useEffect(() => {
    const temporizador = setTimeout(() => {
      if (textoLocal !== filtro.texto) {
        establecer({ texto: textoLocal });
      }
    }, 400);
    return () => clearTimeout(temporizador);
  }, [textoLocal, filtro.texto, establecer]);

  return (
    <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, alignItems: "center", mb: 2 }}>
      <TextField
        size="small"
        label="Buscar folio, titulo o proyecto"
        value={textoLocal}
        onChange={(e) => setTextoLocal(e.target.value)}
        sx={{ minWidth: 260 }}
      />

      <ComboBuscableMultiple
        label="Estatus"
        value={filtro.estatus}
        onChange={(valores) => establecer({ estatus: valores as number[] })}
        opciones={[
          { valor: -1, etiqueta: "Todos" },
          ...(catalogos?.estatus ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre })),
        ]}
        sx={{ minWidth: 180 }}
      />

      <ComboBuscable
        label="Proyecto"
        value={filtro.idProyecto ?? ""}
        onChange={(v) => establecer({ idProyecto: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` })),
        ]}
        sx={{ minWidth: 180 }}
      />

      <ComboBuscable
        label="Asignado"
        value={filtro.idAsignado ?? ""}
        onChange={(v) => establecer({ idAsignado: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
        ]}
        sx={{ minWidth: 160 }}
      />

      <ComboBuscable
        label="Tipo"
        value={filtro.idTipo ?? ""}
        onChange={(v) => establecer({ idTipo: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.tipos ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre })),
        ]}
        sx={{ minWidth: 140 }}
      />

      <FormControlLabel
        control={
          <Switch
            checked={filtro.soloVencidas}
            onChange={(e) => establecer({ soloVencidas: e.target.checked })}
          />
        }
        label="Solo vencidas"
      />

      <Button size="small" onClick={limpiar}>Limpiar</Button>
    </Box>
  );
}
