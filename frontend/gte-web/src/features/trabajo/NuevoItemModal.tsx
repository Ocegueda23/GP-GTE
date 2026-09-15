import { useEffect, useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import { useSesion } from "../../shared/api/sesion";
import { crearWorkItem, filtrarComplejidades, type CatalogosBandeja } from "../../shared/api/workitems";
import { obtenerSprints } from "../../shared/api/planeacion";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import { PresupuestoComplejidad } from "../workitem/PresupuestoComplejidad";

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
  /**
   * Valores con los que abre el alta cuando quien la lanza ya conoce el contexto
   * (el tablero: proyecto del equipo, sprint activo y persona del filtro). No cambia
   * ninguna regla: siguen siendo campos editables antes de guardar.
   */
  inicial?: { idProyecto?: number; idSprint?: number; idAsignado?: number };
}

function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Alta rapida de elementos: el folio y el estatus inicial los fija el backend. */
export function NuevoItemModal({ abierto, catalogos, alCerrar, alExito, alError, padre, copiaDe, inicial }: Props) {
  const sesion = useSesion((estado) => estado.sesion);
  const puede = useSesion((estado) => estado.puede);
  const [idProyecto, setIdProyecto] = useState<number | "">(
    padre?.idProyecto ?? copiaDe?.idProyecto ?? inicial?.idProyecto ?? "");
  const [idTipo, setIdTipo] = useState<number | "">(copiaDe?.idTipo ?? "");
  const [idPrioridad, setIdPrioridad] = useState<number | "">(copiaDe?.idPrioridad ?? ID_PRIORIDAD_MEDIA);
  const [idAsignado, setIdAsignado] = useState<number | "">(
    copiaDe ? (copiaDe.idAsignado ?? "") : (inicial?.idAsignado ?? sesion?.idUsuario ?? ""),
  );
  const [idComplejidad, setIdComplejidad] = useState<number | "">("");
  const [idSprint, setIdSprint] = useState<number | "">(inicial?.idSprint ?? "");
  const [titulo, setTitulo] = useState(copiaDe ? `Copia de ${copiaDe.titulo}` : "");
  const [descripcion, setDescripcion] = useState(copiaDe?.descripcion ?? "");
  const [descripcionVacia, setDescripcionVacia] = useState(true);
  const [compromiso, setCompromiso] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();
  const esMovil = useEsMovil();

  const valido = idProyecto !== "" && idTipo !== "" && idPrioridad !== "" && idComplejidad !== ""
    && titulo.trim().length > 0;

  // Comprometer al sprint desde el alta exige el permiso de planeacion (el backend lo valida
  // igual); sin el, el elemento nace en el backlog y se mueve desde el detalle del sprint.
  const puedeAsignarSprint = puede("PLA.GestionarSprints");
  const sprints = useQuery({
    queryKey: ["sprints", "abiertos"],
    queryFn: () => obtenerSprints({ soloAbiertos: true }),
    enabled: abierto && puedeAsignarSprint,
    staleTime: 60_000,
  });

  const limpiar = () => {
    setTitulo("");
    setDescripcion("");
    setDescripcionVacia(true);
    setCompromiso("");
    setIdAsignado(inicial?.idAsignado ?? sesion?.idUsuario ?? "");
    setIdComplejidad("");
    setIdSprint(inicial?.idSprint ?? "");
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

  // La complejidad depende de la categoria del proyecto: al cambiar de proyecto se
  // descarta la seleccion previa si ya no pertenece a la categoria nueva.
  const proyectoSeleccionado = catalogos?.proyectos.find((p) => p.id === idProyecto);
  const complejidades = filtrarComplejidades(
    catalogos?.complejidades, proyectoSeleccionado?.idCategoriaProyecto);

  useEffect(() => {
    if (idComplejidad === "") return;
    if (!complejidades.some((c) => c.id === idComplejidad)) {
      setIdComplejidad("");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo reacciona al cambio de opciones
  }, [complejidades, idComplejidad]);

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    try {
      const { dato, mensaje } = await crearWorkItem({
        idProyecto: idProyecto as number,
        idTipoWorkItem: idTipo as number,
        titulo: titulo.trim(),
        descripcion: descripcionVacia ? null : descripcion,
        idPrioridad: idPrioridad as number,
        idComplejidad: idComplejidad as number,
        idAsignado: idAsignado === "" ? null : (idAsignado as number),
        fechaCompromiso: compromiso || null,
        idSprint: idSprint === "" ? null : (idSprint as number),
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
    <Dialog open={abierto} onClose={alCerrar} fullWidth maxWidth="sm" fullScreen={esMovil}>
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
        <EditorEnriquecido
          label="Descripcion"
          value={descripcion}
          onChange={setDescripcion}
          onVacioChange={setDescripcionVacia}
          onError={alError}
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
          disabled={idProyecto === ""}
          value={idComplejidad}
          onChange={(v) => setIdComplejidad(v as number | "")}
          opciones={complejidades.map((c) => ({ valor: c.id, etiqueta: c.nombre }))}
        />
        <PresupuestoComplejidad idComplejidad={idComplejidad} idAsignado={idAsignado} />
        <ComboBuscable
          label="Asignado"
          value={idAsignado}
          onChange={(v) => setIdAsignado(v as number | "")}
          opciones={[
            { valor: "", etiqueta: "Sin asignar" },
            ...(catalogos?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
          ]}
        />
        {puedeAsignarSprint && (
          <ComboBuscable
            label="Sprint"
            value={idSprint}
            onChange={(v) => setIdSprint(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Backlog (sin sprint)" },
              ...(sprints.data ?? []).map((s) => ({
                valor: s.idSprint,
                etiqueta: `${s.folio ?? s.nombre} - ${s.nombre} (${s.estatus})`,
              })),
            ]}
          />
        )}
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
        <Button color="error" onClick={alCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
          Crear
        </Button>
      </DialogActions>
    </Dialog>
  );
}
