import { useEffect, useState } from "react";
import {
  Box, Button, Collapse, FormControlLabel, Switch, TextField,
} from "@mui/material";
import TuneIcon from "@mui/icons-material/Tune";
import { ComboBuscable, ComboBuscableMultiple } from "../../shared/components/ComboBuscable";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import type { CatalogosBandeja } from "../../shared/api/workitems";
import { useFiltrosBandeja } from "./storeFiltros";

interface Props {
  catalogos: CatalogosBandeja | undefined;
}

/**
 * Filtros con la semantica heredada del GT: sin estatus seleccionado = abiertos
 * (Pendiente a Suspendido); la UI arranca con Todos (-1), que quita el filtro.
 */
export function BarraFiltros({ catalogos }: Props) {
  const { filtro, establecer, limpiar } = useFiltrosBandeja();
  const [textoLocal, setTextoLocal] = useState(filtro.texto);
  const [panelAbierto, setPanelAbierto] = useState(false);
  const esMovil = useEsMovil();

  // Busqueda con debounce: una sola consulta al dejar de teclear
  useEffect(() => {
    const temporizador = setTimeout(() => {
      if (textoLocal !== filtro.texto) {
        establecer({ texto: textoLocal });
      }
    }, 400);
    return () => clearTimeout(temporizador);
  }, [textoLocal, filtro.texto, establecer]);

  // Cuenta los filtros puestos ademas del texto. En movil el panel arranca cerrado y
  // este numero es lo unico que avisa que la bandeja viene recortada.
  const filtrosActivos = [
    !filtro.estatus.includes(-1),
    filtro.idProyecto !== null,
    filtro.idAsignado !== null,
    filtro.idSprint !== null,
    filtro.idTipo !== null,
    filtro.soloVencidas,
  ].filter(Boolean).length;

  // Se dibujan en la misma fila que la busqueda en escritorio, o dentro del panel
  // plegable en movil: nunca los dos a la vez, por eso el mismo bloque sirve para ambos.
  const controles = (
    <>
      <ComboBuscableMultiple
        label="Estatus"
        value={filtro.estatus}
        onChange={(valores) => establecer({ estatus: valores as number[] })}
        opciones={[
          { valor: -1, etiqueta: "Todos" },
          ...(catalogos?.estatus ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre })),
        ]}
        sx={{ minWidth: { xs: "100%", sm: 180 } }}
      />

      <ComboBuscable
        label="Proyecto"
        value={filtro.idProyecto ?? ""}
        onChange={(v) => establecer({ idProyecto: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` })),
        ]}
        sx={{ minWidth: { xs: "100%", sm: 180 } }}
      />

      <ComboBuscable
        label="Asignado"
        value={filtro.idAsignado ?? ""}
        onChange={(v) => establecer({ idAsignado: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
        ]}
        sx={{ minWidth: { xs: "100%", sm: 160 } }}
      />

      <ComboBuscable
        label="Sprint"
        value={filtro.idSprint ?? ""}
        onChange={(v) => establecer({ idSprint: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          { valor: -1, etiqueta: "Backlog (sin sprint)" },
          ...(catalogos?.sprints ?? []).map((s) => ({ valor: s.id, etiqueta: s.nombre })),
        ]}
        sx={{ minWidth: { xs: "100%", sm: 180 } }}
      />

      <ComboBuscable
        label="Tipo"
        value={filtro.idTipo ?? ""}
        onChange={(v) => establecer({ idTipo: v === "" ? null : Number(v) })}
        opciones={[
          { valor: "", etiqueta: "Todos" },
          ...(catalogos?.tipos ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre })),
        ]}
        sx={{ minWidth: { xs: "100%", sm: 140 } }}
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
    </>
  );

  return (
    <Box sx={{ mb: 2 }}>
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, alignItems: "center" }}>
        <TextField
          size="small"
          label="Buscar folio, titulo o proyecto"
          value={textoLocal}
          onChange={(e) => setTextoLocal(e.target.value)}
          sx={{ minWidth: { xs: "100%", sm: 260 } }}
        />

        {/* En movil los seis filtros se guardan tras un boton: puestos uno debajo de otro
            empujan la bandeja fuera de la pantalla antes de ver un solo elemento. */}
        {esMovil && (
          <Button size="small" startIcon={<TuneIcon />} onClick={() => setPanelAbierto((abierto) => !abierto)}>
            Filtros{filtrosActivos > 0 ? ` (${filtrosActivos})` : ""}
          </Button>
        )}
        {!esMovil && controles}

        <Button size="small" onClick={limpiar}>Limpiar</Button>
      </Box>

      {esMovil && (
        <Collapse in={panelAbierto}>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5, mt: 1.5 }}>
            {controles}
          </Box>
        </Collapse>
      )}
    </Box>
  );
}
