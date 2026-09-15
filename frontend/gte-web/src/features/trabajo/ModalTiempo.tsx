import { useState } from "react";
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { formatearMinutos, obtenerTiempos, registrarTiempo } from "../../shared/api/workitems";
import { useEsMovil } from "../../shared/hooks/useEsMovil";

interface Props {
  abierto: boolean;
  item: { idWorkItem: number; folio: string };
  alCerrar: () => void;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ModalTiempo({ abierto, item, alCerrar, alExito, alError }: Props) {
  const [fecha, setFecha] = useState(hoyIso());
  const [minutos, setMinutos] = useState("60");
  const [descripcion, setDescripcion] = useState("");
  const [enviando, setEnviando] = useState(false);
  const clienteQuery = useQueryClient();
  const esMovil = useEsMovil();

  const tiempos = useQuery({
    queryKey: ["tiempos", item.idWorkItem],
    queryFn: () => obtenerTiempos(item.idWorkItem),
    enabled: abierto,
  });
  const totalRegistrado = tiempos.data?.reduce((total, r) => total + r.minutos, 0) ?? 0;

  const guardar = async () => {
    setEnviando(true);
    try {
      const { mensaje } = await registrarTiempo(item.idWorkItem, {
        fecha,
        minutos: Number(minutos),
        descripcion: descripcion.trim(),
      });
      alExito(mensaje);
      alCerrar();
      setDescripcion("");
      setMinutos("60");
      await clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
      await clienteQuery.invalidateQueries({ queryKey: ["tiempos", item.idWorkItem] });
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "Error al registrar el tiempo.");
    } finally {
      setEnviando(false);
    }
  };

  const minutosValidos = Number(minutos) >= 1 && Number(minutos) <= 1440;

  return (
    <Dialog open={abierto} onClose={alCerrar} fullWidth maxWidth="xs" fullScreen={esMovil}>
      <DialogTitle>Registrar tiempo - {item.folio}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        {tiempos.data && tiempos.data.length > 0 && (
          <Typography variant="caption" color="text.secondary">
            Total ya registrado: {formatearMinutos(totalRegistrado)}
          </Typography>
        )}
        <TextField
          type="date"
          label="Fecha"
          value={fecha}
          onChange={(e) => setFecha(e.target.value)}
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: hoyIso() } }}
        />
        <TextField
          type="number"
          label="Minutos (1 a 1440)"
          value={minutos}
          onChange={(e) => setMinutos(e.target.value)}
          error={!minutosValidos}
          slotProps={{ htmlInput: { min: 1, max: 1440 } }}
        />
        <TextField
          label="Descripcion del avance"
          multiline
          minRows={2}
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <Button color="error" onClick={alCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !minutosValidos} onClick={() => void guardar()}>
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
