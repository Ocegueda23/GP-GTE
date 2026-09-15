import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Chip, LinearProgress, Link,
  Paper, Snackbar, Stack, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import {
  DndContext, DragOverlay, PointerSensor, useDraggable, useDroppable,
  useSensor, useSensors, type DragEndEvent, type DragStartEvent,
} from "@dnd-kit/core";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { moverTarjeta, obtenerTablero, type ColumnaTablero } from "../../shared/api/planeacion";
import { obtenerCatalogosBandeja, type BandejaItem } from "../../shared/api/workitems";
import { useSesion } from "../../shared/api/sesion";
import { NuevoItemModal } from "../trabajo/NuevoItemModal";

const TODOS_LOS_EQUIPOS = "todos";
const TODAS_LAS_PERSONAS = "todas";

/** dbo.tblEstatusWorkItem.Suspendido: el tablero lo pinta como apartado, no como etapa. */
const ESTATUS_SUSPENDIDO = 5;

function Tarjeta({ item, arrastrable = true, ajena = false }: {
  item: BandejaItem; arrastrable?: boolean; ajena?: boolean;
}) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: item.idWorkItem,
    disabled: !arrastrable,
  });

  return (
    <Paper
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      variant="outlined"
      sx={{
        p: 1,
        mb: 1,
        cursor: arrastrable ? "grab" : "default",
        opacity: isDragging ? 0.4 : 1,
        borderLeft: 4,
        borderLeftColor: item.esVencida ? "error.main" : "primary.light",
        touchAction: "none",
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 0.5 }}>
        <Link component={RouterLink} to={`/wi/${item.folio}`} underline="hover"
          sx={{ fontWeight: 700, fontSize: 12 }} onPointerDown={(e) => e.stopPropagation()}>
          {item.folio}
        </Link>
        <Chip size="small" label={item.tipo} variant="outlined" sx={{ height: 18, fontSize: 10 }} />
        {item.puntosHistoria !== null && (
          <Chip size="small" label={`${item.puntosHistoria} pts`} sx={{ height: 18, fontSize: 10 }} />
        )}
      </Stack>
      <Typography variant="body2" sx={{ fontSize: 13, lineHeight: 1.3 }}>{item.titulo}</Typography>
      <Typography variant="caption" color="text.secondary">
        {item.asignado ?? "Sin asignar"}
        {item.revisionesPendientes > 0 && ` - ${item.revisionesPendientes} hallazgo(s)`}
        {ajena && " - no es tuya"}
      </Typography>
    </Paper>
  );
}

function Columna({ columna, esPropia }: { columna: ColumnaTablero; esPropia: (item: BandejaItem) => boolean }) {
  const { setNodeRef, isOver } = useDroppable({ id: `col-${columna.idEstatusWorkItem}` });
  const excedeWip = columna.limiteWip !== null && columna.items.length >= columna.limiteWip;
  const detenida = columna.idEstatusWorkItem === ESTATUS_SUSPENDIDO;

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{
        p: 1,
        minWidth: 240,
        flex: 1,
        backgroundColor: isOver ? "action.hover" : "background.paper",
        borderColor: isOver ? "primary.main" : detenida ? "warning.light" : undefined,
        borderStyle: detenida ? "dashed" : undefined,
      }}
    >
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2"
          sx={{ fontWeight: 700, color: detenida ? "warning.dark" : undefined }}>
          {columna.nombre}
        </Typography>
        <Chip
          size="small"
          color={excedeWip ? "warning" : "default"}
          label={columna.limiteWip !== null
            ? `${columna.items.length}/${columna.limiteWip}`
            : columna.items.length}
        />
      </Stack>
      {columna.items.length === 0 && (
        <Typography variant="caption" color="text.secondary">
          {detenida ? "Nada detenido." : "Sin elementos."}
        </Typography>
      )}
      {columna.items.map((item) => {
        const propia = esPropia(item);
        return <Tarjeta key={item.idWorkItem} item={item} arrastrable={propia} ajena={!propia} />;
      })}
    </Paper>
  );
}

/** P05 - Tablero Kanban: soltar una tarjeta ejecuta la accion de workflow correspondiente. */
export function TableroPage() {
  const [idEquipo, setIdEquipo] = useState<number | typeof TODOS_LOS_EQUIPOS>(TODOS_LOS_EQUIPOS);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [arrastrando, setArrastrando] = useState<BandejaItem | null>(null);
  const clienteQuery = useQueryClient();
  const sensores = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));
  const sesion = useSesion((estado) => estado.sesion);
  const puede = useSesion((estado) => estado.puede);
  // El tablero abre en "solo lo mio", que es como se usa a diario; el combo permite ver
  // el tablero completo del equipo eligiendo "Todas las personas".
  const [idAsignado, setIdAsignado] = useState<number | typeof TODAS_LAS_PERSONAS>(
    sesion?.idUsuario ?? TODAS_LAS_PERSONAS);
  const [modalNuevo, setModalNuevo] = useState(false);

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });

  const equipos = catalogos.data?.equipos ?? [];
  const equipoActual = idEquipo === TODOS_LOS_EQUIPOS ? undefined : idEquipo;
  const asignadoActual = idAsignado === TODAS_LAS_PERSONAS ? undefined : idAsignado;

  const tablero = useQuery({
    queryKey: ["tablero", equipoActual ?? TODOS_LOS_EQUIPOS, asignadoActual ?? TODAS_LAS_PERSONAS],
    queryFn: () => obtenerTablero(equipoActual, asignadoActual),
  });

  // RN-GTE-021: un usuario no puede arrastrar (y por lo tanto mover de columna) una
  // tarjeta que no le pertenece, salvo que tenga el permiso de modificar ajenos --
  // mismo criterio que MenuAcciones.tsx/DetallePage.tsx. El backend ya lo rechaza
  // (RN-GTE-012), esto evita el intento fallido y avisa por que en la propia tarjeta.
  const esPropia = (item: BandejaItem) =>
    item.idAsignado === sesion?.idUsuario || puede("WI.ModificarAjeno");

  // El alta desde el tablero abre con el contexto que el tablero ya conoce, y todo sigue
  // editable en el modal: el proyecto solo si el equipo filtrado tiene uno solo (con varios
  // no hay default honesto) y el sprint activo solo si la persona puede comprometer sprints,
  // porque el backend lo exige de todas formas (RN-GTE-019).
  const proyectosDelEquipo = (catalogos.data?.proyectos ?? [])
    .filter((p) => equipoActual !== undefined && p.idEquipo === equipoActual);
  const inicialAlta = {
    idProyecto: proyectosDelEquipo.length === 1 ? proyectosDelEquipo[0].id : undefined,
    idSprint: puede("PLA.GestionarSprints") ? (tablero.data?.idSprintActivo ?? undefined) : undefined,
    idAsignado: asignadoActual,
  };

  const alIniciarArrastre = (evento: DragStartEvent) => {
    const id = Number(evento.active.id);
    const item = tablero.data?.columnas.flatMap((c) => c.items).find((i) => i.idWorkItem === id);
    setArrastrando(item ?? null);
  };

  const alTerminarArrastre = async (evento: DragEndEvent) => {
    setArrastrando(null);
    const idWorkItem = Number(evento.active.id);
    const destino = evento.over?.id?.toString();
    if (!destino?.startsWith("col-")) return;

    const idEstatusDestino = Number(destino.replace("col-", ""));
    const item = tablero.data?.columnas.flatMap((c) => c.items).find((i) => i.idWorkItem === idWorkItem);
    if (!item || item.idEstatus === idEstatusDestino || !esPropia(item)) return;

    try {
      const { mensaje } = await moverTarjeta(idWorkItem, idEstatusDestino);
      setAviso({ tipo: "success", mensaje });
    } catch (error) {
      // El backend manda: si rechaza el movimiento, el tablero se recarga como estaba
      setAviso({
        tipo: "error",
        mensaje: error instanceof ErrorApi ? error.message : "No se pudo mover la tarjeta.",
      });
    } finally {
      await clienteQuery.invalidateQueries({ queryKey: ["tablero"] });
      await clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
    }
  };

  return (
    <Box sx={{ p: 2 }}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Tablero</Typography>
        <Stack direction="row" spacing={2}
          sx={{ alignItems: "center", flexWrap: "wrap", rowGap: 1, justifyContent: "flex-end" }}>
          {tablero.data?.sprintActivo && (
            <Chip color="success" label={`Sprint activo: ${tablero.data.sprintActivo}`} />
          )}
          <ComboBuscable
            label="Asignado"
            value={idAsignado}
            onChange={(v) => setIdAsignado(v === TODAS_LAS_PERSONAS ? TODAS_LAS_PERSONAS : (v as number))}
            opciones={[
              { valor: TODAS_LAS_PERSONAS, etiqueta: "Todas las personas" },
              ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
            sx={{ minWidth: 200 }}
          />
          <ComboBuscable
            label="Equipo"
            value={idEquipo}
            onChange={(v) => setIdEquipo(v === TODOS_LOS_EQUIPOS ? TODOS_LOS_EQUIPOS : (v as number))}
            opciones={[
              { valor: TODOS_LOS_EQUIPOS, etiqueta: "Todos los equipos" },
              ...equipos.map((eq) => ({ valor: eq.id, etiqueta: eq.nombre })),
            ]}
            sx={{ minWidth: 200 }}
          />
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
            Nuevo
          </Button>
        </Stack>
      </Stack>

      {equipos.length === 0 && !catalogos.isLoading && (
        <Alert severity="info" sx={{ mb: 2 }}>
          No hay equipos registrados. Crea un equipo y asignale proyectos para poder filtrar el tablero por equipo.
        </Alert>
      )}

      {tablero.isLoading && <LinearProgress />}
      {tablero.isError && (
        <Alert severity="error">{(tablero.error as Error).message}</Alert>
      )}

      {tablero.data && (
        <DndContext sensors={sensores} onDragStart={alIniciarArrastre} onDragEnd={alTerminarArrastre}>
          <Box sx={{ display: "flex", gap: 1.5, overflowX: "auto", alignItems: "flex-start", pb: 1 }}>
            {tablero.data.columnas.map((columna) => (
              <Columna key={columna.idTableroColumna} columna={columna} esPropia={esPropia} />
            ))}
          </Box>
          <DragOverlay>
            {arrastrando && <Tarjeta item={arrastrando} arrastrable={false} ajena={!esPropia(arrastrando)} />}
          </DragOverlay>
        </DndContext>
      )}

      {modalNuevo && (
        <NuevoItemModal
          abierto
          catalogos={catalogos.data}
          inicial={inicialAlta}
          alCerrar={() => setModalNuevo(false)}
          alExito={(mensaje) => {
            setAviso({ tipo: "success", mensaje });
            void clienteQuery.invalidateQueries({ queryKey: ["tablero"] });
          }}
          alError={(mensaje) => setAviso({ tipo: "error", mensaje })}
        />
      )}

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
