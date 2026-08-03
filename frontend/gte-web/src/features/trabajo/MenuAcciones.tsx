import { useState } from "react";
import {
  Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, Menu, MenuItem, TextField,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";
import {
  cambiarEstatus, obtenerAcciones, type AccionDisponible, type BandejaItem, type CatalogosBandeja,
} from "../../shared/api/workitems";
import { NuevoItemModal } from "./NuevoItemModal";

interface Props {
  item: BandejaItem;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/**
 * Acciones de la fila: se consultan al motor de workflow al abrir el menu,
 * de modo que solo se ofrecen transiciones validas para el usuario.
 */
export function MenuAcciones({ item, catalogos, alExito, alError }: Props) {
  const [ancla, setAncla] = useState<HTMLElement | null>(null);
  const [acciones, setAcciones] = useState<AccionDisponible[] | null>(null);
  const [cargando, setCargando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [modalSubtarea, setModalSubtarea] = useState(false);
  const clienteQuery = useQueryClient();
  const sesion = useSesion((estado) => estado.sesion);
  const puede = useSesion((estado) => estado.puede);

  // RN-REQ-05: solo el propio asignado modifica un elemento; sin asignar tambien
  // cuenta como ajeno (decision del equipo 2026-08-02). El backend revalida igual.
  const esAjeno = item.idAsignado !== sesion?.idUsuario && !puede("WI.ModificarAjeno");

  const abrirMenu = async (evento: React.MouseEvent<HTMLElement>) => {
    setAncla(evento.currentTarget);
    setCargando(true);
    try {
      setAcciones(await obtenerAcciones(item.idWorkItem));
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudieron consultar las acciones.");
      setAncla(null);
    } finally {
      setCargando(false);
    }
  };

  const cerrarMenu = () => {
    setAncla(null);
    setAcciones(null);
  };

  const ejecutar = async (accion: AccionDisponible, motivoCapturado?: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatus(item.idWorkItem, accion.accion, motivoCapturado);
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      await clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
    } catch (error) {
      if (error instanceof ErrorApi) {
        const detalle = error.detalle as { revisionesPendientes?: unknown[] } | undefined;
        const extra = detalle?.revisionesPendientes
          ? ` (${detalle.revisionesPendientes.length} revision(es) pendiente(s))`
          : "";
        alError(error.message + extra);
      } else {
        alError("Error inesperado al cambiar el estatus.");
      }
    } finally {
      setEnviando(false);
    }
  };

  const seleccionar = (accion: AccionDisponible) => {
    cerrarMenu();
    if (accion.requiereMotivo) {
      setAccionConMotivo(accion);
    } else {
      void ejecutar(accion);
    }
  };

  return (
    <>
      <IconButton size="small" onClick={abrirMenu} aria-label={`Acciones de ${item.folio}`}>
        {cargando ? <CircularProgress size={18} /> : <MoreVertIcon fontSize="small" />}
      </IconButton>

      <Menu anchorEl={ancla} open={ancla !== null && acciones !== null} onClose={cerrarMenu}>
        {acciones?.map((accion) => (
          <MenuItem key={accion.accion} onClick={() => seleccionar(accion)}>
            {accion.etiqueta}
          </MenuItem>
        ))}
        {!esAjeno && (
          <MenuItem
            onClick={() => {
              cerrarMenu();
              setModalSubtarea(true);
            }}
          >
            Registrar Subtarea
          </MenuItem>
        )}
      </Menu>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth>
        <DialogTitle>{accionConMotivo?.etiqueta} - {item.folio}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={2}
            margin="dense"
            label="Motivo (obligatorio)"
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAccionConMotivo(null)}>Cancelar</Button>
          <Button
            variant="contained"
            disabled={enviando || motivo.trim().length === 0}
            onClick={() => accionConMotivo && void ejecutar(accionConMotivo, motivo.trim())}
          >
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>

      <NuevoItemModal
        abierto={modalSubtarea}
        catalogos={catalogos}
        padre={{ idWorkItem: item.idWorkItem, folio: item.folio, idProyecto: item.idProyecto }}
        alCerrar={() => setModalSubtarea(false)}
        alExito={alExito}
        alError={alError}
      />
    </>
  );
}
