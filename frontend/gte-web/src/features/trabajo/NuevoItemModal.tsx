import { useEffect, useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { useSesion } from "../../shared/api/sesion";
import { crearWorkItem, type CatalogosBandeja } from "../../shared/api/workitems";

const ID_PRIORIDAD_MEDIA = 3;
const CATEGORIA_PROYECTO_TI = "TI";

interface Props {
  abierto: boolean;
  catalogos: CatalogosBandeja | undefined;
  alCerrar: () => void;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
  /** Al capturarse, el alta crea una subtarea: proyecto bloqueado al del padre. */
  padre?: { idWorkItem: number; folio: string; idProyecto: number };
  /** Al copiar un elemento existente, prellena sus datos (no fecha compromiso: se recalcula igual que en una alta nueva). */
  copiaDe?: {
    idProyecto: number; idTipo: number | ""; idPrioridad: number | ""; titulo: string;
    descripcion: string | null; idAsignado: number | null;
  };
}

function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Alta rapida de elementos: el folio y el estatus inicial los fija el backend. */
export function NuevoItemModal({ abierto, catalogos, alCerrar, alExito, alError, padre, copiaDe }: Props) {
  const sesion = useSesion((estado) => estado.sesion);
  const [idProyecto, setIdProyecto] = useState<number | "">(padre?.idProyecto ?? copiaDe?.idProyecto ?? "");
  const [idTipo, setIdTipo] = useState<number | "">(copiaDe?.idTipo ?? "");
  const [idPrioridad, setIdPrioridad] = useState<number | "">(copiaDe?.idPrioridad ?? ID_PRIORIDAD_MEDIA);
  const [idAsignado, setIdAsignado] = useState<number | "">(
    copiaDe ? (copiaDe.idAsignado ?? "") : (sesion?.idUsuario ?? ""),
  );
  const [idComplejidad, setIdComplejidad] = useState<number | "">("");
  const [titulo, setTitulo] = useState(copiaDe ? `Copia de ${copiaDe.titulo}` : "");
  const [descripcion, setDescripcion] = useState(copiaDe?.descripcion ?? "");
  const [compromiso, setCompromiso] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();

  const valido = idProyecto !== "" && idTipo !== "" && idPrioridad !== "" && idComplejidad !== ""
    && titulo.trim().length > 0;

  const limpiar = () => {
    setTitulo("");
    setDescripcion("");
    setCompromiso("");
    setIdAsignado(sesion?.idUsuario ?? "");
    setIdComplejidad("");
  };

  // Proyectos de categoria TI suelen iniciarse el mismo dia: precargar la fecha
  // de hoy si el usuario todavia no capturo una (no pisa una fecha ya escrita).
  useEffect(() => {
    if (compromiso !== "") return;
    const proyecto = catalogos?.proyectos.find((p) => p.id === idProyecto);
    if (proyecto?.categoriaProyecto === CATEGORIA_PROYECTO_TI) {
      setCompromiso(hoyIso());
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo reacciona al cambio de proyecto
  }, [idProyecto, catalogos?.proyectos]);

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
        idComplejidad: idComplejidad as number,
        idAsignado: idAsignado === "" ? null : (idAsignado as number),
        fechaCompromiso: compromiso || null,
        idPadre: padre?.idWorkItem,
      });
      alExito(`${mensaje} (${dato.folio})`);
      limpiar();
      alCerrar();
      await clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
      if (padre) {
        await clienteQuery.invalidateQueries({ queryKey: ["hijos", padre.idWorkItem] });
      }
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al crear el elemento.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Dialog open={abierto} onClose={alCerrar} fullWidth maxWidth="sm">
      <DialogTitle>
        {padre ? `Nueva subtarea de ${padre.folio}` : copiaDe ? "Copiar elemento de trabajo" : "Nuevo elemento de trabajo"}
      </DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        <ComboBuscable
          label="Proyecto"
          required
          disabled={padre !== undefined}
          value={idProyecto}
          onChange={(v) => setIdProyecto(v as number | "")}
          opciones={(catalogos?.proyectos ?? []).map((p) => ({ valor: p.id, etiqueta: `${p.clave} - ${p.nombre}` }))}
        />
        <ComboBuscable
          label="Tipo"
          required
          value={idTipo}
          onChange={(v) => setIdTipo(v as number | "")}
          opciones={(catalogos?.tipos ?? []).map((t) => ({ valor: t.id, etiqueta: t.nombre }))}
        />
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
        <Typography variant="caption" color="text.secondary" sx={{ mt: -1.5 }}>
          Define automaticamente el presupuesto de horas y puntos de historia (segun el nivel del asignado).
        </Typography>
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
