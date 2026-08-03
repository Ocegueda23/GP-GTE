import { useState } from "react";
import {
  Box, Button, Chip, Link, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { formatearMinutos, obtenerHijos, type CatalogosBandeja } from "../../shared/api/workitems";
import { NuevoItemModal } from "../trabajo/NuevoItemModal";

interface Props {
  idWorkItem: number;
  folio: string;
  idProyecto: number;
  catalogos: CatalogosBandeja | undefined;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function colorEstatus(idEstatus: number): "default" | "success" | "info" | "warning" | "error" {
  switch (idEstatus) {
    case 2: return "success";   // En Proceso
    case 3: return "info";      // En Pruebas
    case 4: return "warning";   // Correccion
    case 7: return "error";     // Cancelado
    default: return "default";
  }
}

/** Subtareas (WorkItems hijos): sin esto no hay forma de llegar al tiempo registrado en cada una. */
export function PanelSubtareas({ idWorkItem, folio, idProyecto, catalogos, alExito, alError }: Props) {
  const [modalNueva, setModalNueva] = useState(false);

  const hijos = useQuery({
    queryKey: ["hijos", idWorkItem],
    queryFn: () => obtenerHijos(idWorkItem),
  });

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Subtareas</Typography>
        <Button size="small" variant="contained" onClick={() => setModalNueva(true)}>
          Agregar subtarea
        </Button>
      </Stack>

      {hijos.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
          Sin subtareas registradas.
        </Typography>
      )}

      {hijos.data && hijos.data.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow sx={{ "& th": { fontWeight: 700 } }}>
              <TableCell>Folio</TableCell>
              <TableCell>Titulo</TableCell>
              <TableCell>Estatus</TableCell>
              <TableCell>Asignado</TableCell>
              <TableCell align="right">Tiempo registrado</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {hijos.data.map((hijo) => (
              <TableRow key={hijo.idWorkItem} hover>
                <TableCell sx={{ whiteSpace: "nowrap" }}>
                  <Link component={RouterLink} to={`/wi/${hijo.folio}`} underline="hover">
                    {hijo.folio}
                  </Link>
                </TableCell>
                <TableCell sx={{ maxWidth: 320 }}>{hijo.titulo}</TableCell>
                <TableCell>
                  <Chip size="small" label={hijo.estatus} color={colorEstatus(hijo.idEstatus)}
                    variant={hijo.idEstatus === 6 ? "outlined" : "filled"} />
                </TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{hijo.asignado ?? "-"}</TableCell>
                <TableCell align="right">{formatearMinutos(hijo.minutosRegistrados)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <NuevoItemModal
        abierto={modalNueva}
        catalogos={catalogos}
        padre={{ idWorkItem, folio, idProyecto }}
        alCerrar={() => setModalNueva(false)}
        alExito={alExito}
        alError={alError}
      />
    </Box>
  );
}
