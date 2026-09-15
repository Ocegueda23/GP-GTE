import { useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { useEsMovil } from "../../shared/hooks/useEsMovil";
import {
  cambiarEstatus, obtenerAcciones, type AccionDisponible,
} from "../../shared/api/workitems";

interface Props {
  idWorkItem: number;
  folio: string;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/** Botones de transicion del detalle: solo las acciones validas del motor. */
export function BotonesAcciones({ idWorkItem, folio, alExito, alError }: Props) {
  const [accionConMotivo, setAccionConMotivo] = useState<AccionDisponible | null>(null);
  const [motivo, setMotivo] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();
  const esMovil = useEsMovil();

  const acciones = useQuery({
    queryKey: ["acciones", idWorkItem],
    queryFn: () => obtenerAcciones(idWorkItem),
  });

  const ejecutar = async (accion: AccionDisponible, motivoCapturado?: string) => {
    setEnviando(true);
    try {
      const { mensaje } = await cambiarEstatus(idWorkItem, accion.accion, motivoCapturado);
      alExito(mensaje);
      setAccionConMotivo(null);
      setMotivo("");
      await Promise.all([
        clienteQuery.invalidateQueries({ queryKey: ["workitem", folio] }),
        clienteQuery.invalidateQueries({ queryKey: ["acciones", idWorkItem] }),
        clienteQuery.invalidateQueries({ queryKey: ["bandeja"] }),
      ]);
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

  return (
    <>
      {/* En movil los botones se reparten el ancho: con pocas acciones quedan grandes
          y faciles de tocar, y con muchas se acomodan en varios renglones. */}
      <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
        {acciones.data?.map((accion) => (
          <Button
            key={accion.accion}
            size={esMovil ? "medium" : "small"}
            variant={accion.esAccionPrincipal ? "contained" : "outlined"}
            disabled={enviando}
            sx={{ flexGrow: { xs: 1, sm: 0 } }}
            onClick={() =>
              accion.requiereMotivo ? setAccionConMotivo(accion) : void ejecutar(accion)}
          >
            {accion.etiqueta}
          </Button>
        ))}
      </Stack>

      <Dialog open={accionConMotivo !== null} onClose={() => setAccionConMotivo(null)} fullWidth fullScreen={esMovil}>
        <DialogTitle>{accionConMotivo?.etiqueta} - {folio}</DialogTitle>
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
          <Button color="error" onClick={() => setAccionConMotivo(null)}>Cancelar</Button>
          <Button
            variant="contained"
            disabled={enviando || motivo.trim().length === 0}
            onClick={() => accionConMotivo && void ejecutar(accionConMotivo, motivo.trim())}
          >
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
