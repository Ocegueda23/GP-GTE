import { useEffect, useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, InputLabel, MenuItem, Select, TextField,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  actualizarWorkItem, type CatalogosBandeja, type WorkItemDetalle,
} from "../../shared/api/workitems";

interface Props {
  abierto: boolean;
  item: WorkItemDetalle;
  catalogos: CatalogosBandeja | undefined;
  alCerrar: () => void;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Edicion de un WorkItem existente. Las reglas de negocio (compromiso al pasado, cambio de
 * complejidad, item terminado/ajeno) las valida el backend; su 403 se muestra tal cual. */
export function ModalEditarWorkItem({ abierto, item, catalogos, alCerrar, alExito, alError }: Props) {
  const [titulo, setTitulo] = useState(item.titulo);
  const [descripcion, setDescripcion] = useState(item.descripcion ?? "");
  const [criterios, setCriterios] = useState(item.criteriosAceptacion ?? "");
  const [idPrioridad, setIdPrioridad] = useState<number | "">(item.idPrioridad);
  const [idComplejidad, setIdComplejidad] = useState<number | "">(item.idComplejidad ?? "");
  const [idAsignado, setIdAsignado] = useState<number | "">(item.idAsignado ?? "");
  const [compromiso, setCompromiso] = useState(item.fechaCompromiso?.slice(0, 10) ?? "");
  const [puntos, setPuntos] = useState(item.puntosHistoria?.toString() ?? "");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  useEffect(() => {
    if (!abierto) return;
    setTitulo(item.titulo);
    setDescripcion(item.descripcion ?? "");
    setCriterios(item.criteriosAceptacion ?? "");
    setIdPrioridad(item.idPrioridad);
    setIdComplejidad(item.idComplejidad ?? "");
    setIdAsignado(item.idAsignado ?? "");
    setCompromiso(item.fechaCompromiso?.slice(0, 10) ?? "");
    setPuntos(item.puntosHistoria?.toString() ?? "");
  }, [abierto, item]);

  const valido = titulo.trim().length > 0 && idPrioridad !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await actualizarWorkItem(item.idWorkItem, {
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        criteriosAceptacion: criterios.trim() || null,
        idPrioridad: idPrioridad as number,
        idComplejidad: idComplejidad === "" ? null : (idComplejidad as number),
        idAsignado: idAsignado === "" ? null : (idAsignado as number),
        fechaCompromiso: compromiso || null,
        puntosHistoria: puntos === "" ? null : Number(puntos),
      });
      alExito(mensaje);
      alCerrar();
      await Promise.all([
        clienteQuery.invalidateQueries({ queryKey: ["workitem", item.folio] }),
        clienteQuery.invalidateQueries({ queryKey: ["acciones", item.idWorkItem] }),
        clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
      ]);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo guardar el elemento.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Dialog open={abierto} onClose={alCerrar} fullWidth maxWidth="sm">
      <DialogTitle>Editar {item.folio}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <TextField
          size="small" required label="Titulo" value={titulo}
          onChange={(e) => setTitulo(e.target.value)}
          slotProps={{ htmlInput: { maxLength: 200 } }}
        />
        <TextField
          size="small" label="Descripcion" multiline minRows={3}
          value={descripcion} onChange={(e) => setDescripcion(e.target.value)}
        />
        <TextField
          size="small" label="Criterios de aceptacion" multiline minRows={2}
          value={criterios} onChange={(e) => setCriterios(e.target.value)}
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
          <InputLabel>Complejidad</InputLabel>
          <Select label="Complejidad" value={idComplejidad}
            onChange={(e) => setIdComplejidad(e.target.value as number | "")}>
            <MenuItem value="">Sin definir</MenuItem>
            {catalogos?.complejidades.map((c) => (
              <MenuItem key={c.id} value={c.id}>{c.nombre}</MenuItem>
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
          size="small" type="date" label="Fecha compromiso"
          value={compromiso} onChange={(e) => setCompromiso(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small" type="number" label="Puntos de historia"
          value={puntos} onChange={(e) => setPuntos(e.target.value)}
          slotProps={{ htmlInput: { min: 0 } }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={alCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
