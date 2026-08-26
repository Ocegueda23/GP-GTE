import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, IconButton,
  MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  crearAmbito, eliminarAmbito, TIPO_AMBITO,
  type AmbitoRegla, type CatalogosReglasNegocio,
} from "../../shared/api/reglasNegocio";

interface Props {
  abierto: boolean;
  idProyecto: number;
  ambitos: AmbitoRegla[];
  catalogos: CatalogosReglasNegocio | undefined;
  onCerrar: () => void;
  onCambio: (mensaje: string) => void;
}

/**
 * Administracion de los flujos de operacion y caracteristicas del sistema de UN proyecto.
 * El catalogo es propio de cada proyecto a proposito: los flujos de un sistema no son los
 * de otro, y un catalogo compartido obligaria a nombres genericos que no le sirven a nadie.
 */
export function AmbitosModal({ abierto, idProyecto, ambitos, catalogos, onCerrar, onCambio }: Props) {
  const clienteQuery = useQueryClient();
  const [nombre, setNombre] = useState("");
  const [idTipo, setIdTipo] = useState<number>(TIPO_AMBITO.flujoDeOperacion);
  const [error, setError] = useState<string | null>(null);

  const refrescar = () => clienteQuery.invalidateQueries({ queryKey: ["reglasNegocio"] });

  const agregar = useMutation({
    mutationFn: () => crearAmbito({
      idProyecto,
      idTipoAmbitoRegla: idTipo,
      nombre: nombre.trim(),
      descripcion: null,
    }),
    onSuccess: ({ mensaje }) => {
      setNombre("");
      setError(null);
      void refrescar();
      onCambio(mensaje);
    },
    onError: (e: Error) => setError(e.message),
  });

  const quitar = useMutation({
    mutationFn: (idAmbito: number) => eliminarAmbito(idAmbito),
    onSuccess: ({ mensaje }) => {
      setError(null);
      void refrescar();
      onCambio(mensaje);
    },
    onError: (e: Error) => setError(e.message),
  });

  return (
    <Dialog open={abierto} onClose={onCerrar} maxWidth="sm" fullWidth>
      <DialogTitle>Flujos y caracteristicas del proyecto</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <Typography variant="body2" color="text.secondary">
            Una regla se ubica en un flujo de operacion o en una caracteristica del sistema,
            nunca en los dos. Si aplica a ambos casos, se dan de alta dos reglas.
          </Typography>

          <Stack direction="row" spacing={1}>
            <TextField
              select
              label="Tipo"
              value={idTipo}
              onChange={(e) => setIdTipo(Number(e.target.value))}
              sx={{ minWidth: 220 }}
              size="small"
            >
              {(catalogos?.tiposAmbito ?? []).map((t) => (
                <MenuItem key={t.id} value={t.id}>{t.nombre}</MenuItem>
              ))}
            </TextField>
            <TextField
              label="Nombre"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              fullWidth
              size="small"
            />
            <Button
              variant="contained"
              disabled={nombre.trim() === "" || agregar.isPending}
              onClick={() => agregar.mutate()}
            >
              Agregar
            </Button>
          </Stack>

          {ambitos.length === 0 ? (
            <Box sx={{ py: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Este proyecto todavia no tiene flujos ni caracteristicas capturadas.
              </Typography>
            </Box>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Tipo</TableCell>
                  <TableCell>Nombre</TableCell>
                  <TableCell align="right">Reglas</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {ambitos.map((a) => (
                  <TableRow key={a.idAmbitoRegla}>
                    <TableCell>
                      <Chip size="small" label={a.nombreTipoAmbito} variant="outlined" />
                    </TableCell>
                    <TableCell>{a.nombre}</TableCell>
                    <TableCell align="right">{a.totalReglas}</TableCell>
                    <TableCell align="right">
                      <IconButton
                        size="small"
                        aria-label={`Dar de baja ${a.nombre}`}
                        disabled={quitar.isPending}
                        onClick={() => quitar.mutate(a.idAmbitoRegla)}
                      >
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCerrar}>Cerrar</Button>
      </DialogActions>
    </Dialog>
  );
}
