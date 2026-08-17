import { useEffect, useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
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
  }, [abierto, item]);

  const valido = titulo.trim().length > 0 && idPrioridad !== "" && idComplejidad !== "";

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { mensaje } = await actualizarWorkItem(item.idWorkItem, {
        titulo: titulo.trim(),
        descripcion: descripcion.trim() || null,
        criteriosAceptacion: criterios.trim() || null,
        idPrioridad: idPrioridad as number,
        idComplejidad: idComplejidad as number,
        idAsignado: idAsignado === "" ? null : (idAsignado as number),
        fechaCompromiso: compromiso || null,
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
        <EditorEnriquecido
          label="Descripcion"
          value={descripcion}
          onChange={setDescripcion}
          idWorkItemParaAdjuntos={item.idWorkItem}
          onError={alError}
        />
        <TextField
          size="small" label="Criterios de aceptacion" multiline minRows={2}
          value={criterios} onChange={(e) => setCriterios(e.target.value)}
        />
        <ComboBuscable
          label="Prioridad"
          required
          value={idPrioridad}
          onChange={(v) => setIdPrioridad(v as number | "")}
          opciones={(catalogos?.prioridades ?? []).map((p) => ({ valor: p.id, etiqueta: p.nombre }))}
        />
        <ComboBuscable
          label="Complejidad"
          required
          value={idComplejidad}
          onChange={(v) => setIdComplejidad(v as number | "")}
          opciones={(catalogos?.complejidades ?? []).map((c) => ({ valor: c.id, etiqueta: c.nombre }))}
        />
        <ComboBuscable
          label="Asignado"
          value={idAsignado}
          onChange={(v) => setIdAsignado(v as number | "")}
          opciones={[
            { valor: "", etiqueta: "Sin asignar" },
            ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
          ]}
        />
        <TextField
          size="small" type="date" label="Fecha compromiso"
          value={compromiso} onChange={(e) => setCompromiso(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small" label="Puntos de historia (automatico)"
          value={item.puntosHistoria ?? "Sin calcular"}
          disabled
          helperText="Se calcula solo de la matriz Complejidad x Nivel al asignar o cambiar complejidad."
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
