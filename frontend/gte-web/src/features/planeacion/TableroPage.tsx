import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Chip, FormControl, InputLabel, LinearProgress, Link, MenuItem,
  Paper, Select, Snackbar, Stack, Typography,
} from "@mui/material";
import {
  DndContext, DragOverlay, PointerSensor, useDraggable, useDroppable,
  useSensor, useSensors, type DragEndEvent, type DragStartEvent,
} from "@dnd-kit/core";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { moverTarjeta, obtenerTablero, type ColumnaTablero } from "../../shared/api/planeacion";
import { obtenerCatalogosBandeja, type BandejaItem } from "../../shared/api/workitems";

function Tarjeta({ item, arrastrable = true }: { item: BandejaItem; arrastrable?: boolean }) {
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
      </Typography>
    </Paper>
  );
}

function Columna({ columna }: { columna: ColumnaTablero }) {
  const { setNodeRef, isOver } = useDroppable({ id: `col-${columna.idEstatusWorkItem}` });
  const excedeWip = columna.limiteWip !== null && columna.items.length >= columna.limiteWip;

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{
        p: 1,
        minWidth: 240,
        flex: 1,
        backgroundColor: isOver ? "action.hover" : "background.paper",
        borderColor: isOver ? "primary.main" : undefined,
      }}
    >
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>{columna.nombre}</Typography>
        <Chip
          size="small"
          color={excedeWip ? "warning" : "default"}
          label={columna.limiteWip !== null
            ? `${columna.items.length}/${columna.limiteWip}`
            : columna.items.length}
        />
      </Stack>
      {columna.items.length === 0 && (
        <Typography variant="caption" color="text.secondary">Sin elementos.</Typography>
      )}
      {columna.items.map((item) => <Tarjeta key={item.idWorkItem} item={item} />)}
    </Paper>
  );
}

/** P05 - Tablero Kanban: soltar una tarjeta ejecuta la accion de workflow correspondiente. */
export function TableroPage() {
  const [idEquipo, setIdEquipo] = useState<number | "">("");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [arrastrando, setArrastrando] = useState<BandejaItem | null>(null);
  const clienteQuery = useQueryClient();
  const sensores = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"],
    queryFn: obtenerCatalogosBandeja,
    staleTime: 5 * 60_000,
  });

  const equipos = catalogos.data?.equipos ?? [];
  const equipoActual = idEquipo === "" ? equipos[0]?.id : (idEquipo as number);

  const tablero = useQuery({
    queryKey: ["tablero", equipoActual],
    queryFn: () => obtenerTablero(equipoActual!),
    enabled: equipoActual !== undefined,
  });

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
    if (!item || item.idEstatus === idEstatusDestino) return;

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
        <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
          {tablero.data?.sprintActivo && (
            <Chip color="success" label={`Sprint activo: ${tablero.data.sprintActivo}`} />
          )}
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Equipo</InputLabel>
            <Select label="Equipo" value={equipoActual ?? ""}
              onChange={(e) => setIdEquipo(e.target.value as number)}>
              {equipos.map((eq) => (
                <MenuItem key={eq.id} value={eq.id}>{eq.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>
      </Stack>

      {equipos.length === 0 && !catalogos.isLoading && (
        <Alert severity="info">
          No hay equipos registrados. Crea un equipo y asignale proyectos para usar el tablero.
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
              <Columna key={columna.idTableroColumna} columna={columna} />
            ))}
          </Box>
          <DragOverlay>
            {arrastrando && <Tarjeta item={arrastrando} arrastrable={false} />}
          </DragOverlay>
        </DndContext>
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
