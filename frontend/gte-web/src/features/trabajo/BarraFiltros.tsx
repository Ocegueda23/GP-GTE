import { useEffect, useState } from "react";
import {
  Box, Button, Chip, FormControl, FormControlLabel, InputLabel,
  MenuItem, Select, Switch, TextField,
} from "@mui/material";
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

      <FormControl size="small" sx={{ minWidth: 180 }}>
        <InputLabel>Estatus</InputLabel>
        <Select
          multiple
          label="Estatus"
          value={filtro.estatus}
          onChange={(e) => establecer({ estatus: e.target.value as number[] })}
          renderValue={(seleccion) =>
            seleccion.includes(-1) ? (
              <Chip size="small" label="Todos" />
            ) : (
              <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>
                {seleccion.map((id) => (
                  <Chip
                    key={id}
                    size="small"
                    label={catalogos?.estatus.find((c) => c.id === id)?.nombre ?? id}
                  />
                ))}
              </Box>
            )
          }
        >
          <MenuItem value={-1}>Todos</MenuItem>
          {catalogos?.estatus.map((c) => (
            <MenuItem key={c.id} value={c.id}>{c.nombre}</MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl size="small" sx={{ minWidth: 180 }}>
        <InputLabel>Proyecto</InputLabel>
        <Select
          label="Proyecto"
          value={filtro.idProyecto ?? ""}
          onChange={(e) =>
            establecer({ idProyecto: (e.target.value as number | "") === "" ? null : Number(e.target.value) })}
        >
          <MenuItem value="">Todos</MenuItem>
          {catalogos?.proyectos.map((p) => (
            <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl size="small" sx={{ minWidth: 160 }}>
        <InputLabel>Asignado</InputLabel>
        <Select
          label="Asignado"
          value={filtro.idAsignado ?? ""}
          onChange={(e) =>
            establecer({ idAsignado: (e.target.value as number | "") === "" ? null : Number(e.target.value) })}
        >
          <MenuItem value="">Todos</MenuItem>
          {catalogos?.usuarios.map((u) => (
            <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl size="small" sx={{ minWidth: 140 }}>
        <InputLabel>Tipo</InputLabel>
        <Select
          label="Tipo"
          value={filtro.idTipo ?? ""}
          onChange={(e) =>
            establecer({ idTipo: (e.target.value as number | "") === "" ? null : Number(e.target.value) })}
        >
          <MenuItem value="">Todos</MenuItem>
          {catalogos?.tipos.map((t) => (
            <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
          ))}
        </Select>
      </FormControl>

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
