import { useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, MenuItem, Select, TextField,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { crearWorkItem, type CatalogosBandeja } from "../../shared/api/workitems";

interface Props {
  abierto: boolean;
  catalogos: CatalogosBandeja | undefined;
  alCerrar: () => void;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Alta rapida de elementos: el folio y el estatus inicial los fija el backend. */
export function NuevoItemModal({ abierto, catalogos, alCerrar, alExito, alError }: Props) {
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [idTipo, setIdTipo] = useState<number | "">("");
  const [idPrioridad, setIdPrioridad] = useState<number | "">("");
  const [idAsignado, setIdAsignado] = useState<number | "">("");
  const [titulo, setTitulo] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [compromiso, setCompromiso] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const valido = idProyecto !== "" && idTipo !== "" && idPrioridad !== "" && titulo.trim().length > 0;

  const limpiar = () => {
    setTitulo("");
    setDescripcion("");
    setCompromiso("");
    setIdAsignado("");
  };

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { dato, mensaje } = await crearWorkItem({
        idProyecto: idProyecto as number,
        idTipoWorkItem: idTipo as number,
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        idPrioridad: idPrioridad as number,
        idAsignado: idAsignado === "" ? null : (idAsignado as number),
        fechaCompromiso: compromiso || null,
      });
      alExito(`${mensaje} (${dato.folio})`);
      limpiar();
      alCerrar();
      await clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al crear el elemento.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Dialog open={abierto} onClose={alCerrar} fullWidth maxWidth="sm">
      <DialogTitle>Nuevo elemento de trabajo</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <FormControl size="small" required>
          <InputLabel>Proyecto</InputLabel>
          <Select label="Proyecto" value={idProyecto}
            onChange={(e) => setIdProyecto(e.target.value as number | "")}>
            {catalogos?.proyectos.map((p) => (
              <MenuItem key={p.id} value={p.id}>{p.clave} - {p.nombre}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" required>
          <InputLabel>Tipo</InputLabel>
          <Select label="Tipo" value={idTipo}
            onChange={(e) => setIdTipo(e.target.value as number | "")}>
            {catalogos?.tipos.map((t) => (
              <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          size="small"
          required
          label="Titulo"
          value={titulo}
          onChange={(e) => setTitulo(e.target.value)}
          slotProps={{ htmlInput: { maxLength: 200 } }}
        />
        <TextField
          size="small"
          label="Descripcion"
          multiline
          minRows={3}
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
        />
        <FormControl size="small" required>
          <InputLabel>Prioridad</InputLabel>
          <Select label="Prioridad" value={idPrioridad}
            onChange={(e) => setIdPrioridad(e.target.value as number | "")}>
            {catalogos?.prioridades.map((p) => (
              <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small">
          <InputLabel>Asignado</InputLabel>
          <Select label="Asignado" value={idAsignado}
            onChange={(e) => setIdAsignado(e.target.value as number | "")}>
            <MenuItem value="">Sin asignar</MenuItem>
            {catalogos?.usuarios.map((u) => (
              <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          size="small"
          type="date"
          label="Fecha compromiso"
          value={compromiso}
          onChange={(e) => setCompromiso(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          helperText="Obligatoria para poder iniciar el elemento"
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={alCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
          Crear
        </Button>
      </DialogActions>
    </Dialog>
  );
}
