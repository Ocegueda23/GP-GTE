import { useState } from "react";
import {
  Alert, Box, Button, IconButton, LinearProgress, Stack, Table, TableBody, TableCell,
  TableHead, TableRow, Tooltip, Typography,
} from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import {
  asignarAccesoProyecto, obtenerAccesosProyecto, obtenerCatalogosAdministracion,
  retirarAccesoProyecto,
} from "../../shared/api/administracion";

interface Props {
  idProyecto: number;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

/**
 * Accesos del proyecto: quien tiene que rol AQUI. Es la misma tblUsuarioRol que administra
 * la pestana de usuarios -- un rol dado aqui aparece alla como "(nombre del proyecto)" en
 * vez de "(global)" --, por eso no hay un catalogo aparte de permisos por proyecto.
 * El rol sigue siendo lo que agrupa permisos: aqui se elige a quien y con que rol.
 */
export function AccesosProyectoTab({ idProyecto, alExito, alError }: Props) {
  const [idUsuario, setIdUsuario] = useState<number | "">("");
  const [idRol, setIdRol] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const accesos = useQuery({
    queryKey: ["accesos-proyecto", idProyecto],
    queryFn: () => obtenerAccesosProyecto(idProyecto),
  });

  const agregar = async () => {
    if (idUsuario === "" || idRol === "") return;
    setEnviando(true);
    try {
      const { mensaje } = await asignarAccesoProyecto(idProyecto, {
        idUsuario: idUsuario as number, idRol: idRol as number,
      });
      setIdUsuario("");
      setIdRol("");
      alExito(mensaje);
      await accesos.refetch();
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo asignar el acceso.");
    } finally {
      setEnviando(false);
    }
  };

  const retirar = async (idUsuarioRol: number) => {
    try {
      const { mensaje } = await retirarAccesoProyecto(idProyecto, idUsuarioRol);
      alExito(mensaje);
      await accesos.refetch();
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo retirar el acceso.");
    }
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <Typography variant="body2" color="text.secondary">
        El rol asignado aqui solo aplica en este proyecto. Los roles globales se administran
        en la pestana de Usuarios y no aparecen en esta lista.
      </Typography>

      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
        <ComboBuscable
          label="Persona"
          value={idUsuario}
          onChange={(v) => setIdUsuario(v as number | "")}
          opciones={(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre }))}
          sx={{ flex: 1 }}
        />
        <ComboBuscable
          label="Rol"
          value={idRol}
          onChange={(v) => setIdRol(v as number | "")}
          opciones={(catalogos.data?.roles ?? []).map((r) => ({ valor: r.id, etiqueta: r.nombre }))}
          sx={{ flex: 1 }}
        />
        <Button
          variant="outlined"
          disabled={idUsuario === "" || idRol === "" || enviando}
          onClick={() => void agregar()}>
          Agregar
        </Button>
      </Stack>

      {accesos.isLoading && <LinearProgress />}
      {accesos.isError && (
        <Alert severity="error">{(accesos.error as Error).message}</Alert>
      )}

      {accesos.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          Nadie tiene accesos propios de este proyecto todavia.
        </Typography>
      )}

      {accesos.data !== undefined && accesos.data.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Persona</TableCell>
              <TableCell>Rol</TableCell>
              <TableCell>Desde</TableCell>
              <TableCell>Lo dio</TableCell>
              <TableCell align="right">Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {accesos.data.map((a) => (
              <TableRow key={a.idUsuarioRol} hover>
                <TableCell>
                  {a.usuario}
                  <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
                    {a.dominio}
                  </Typography>
                </TableCell>
                <TableCell>{a.rol}</TableCell>
                <TableCell>{new Date(a.fechaRegistro).toLocaleDateString()}</TableCell>
                <TableCell>{a.usuarioRegistro}</TableCell>
                <TableCell align="right">
                  <Tooltip title="Retirar acceso">
                    <IconButton
                      size="small"
                      aria-label="Retirar acceso"
                      onClick={() => void retirar(a.idUsuarioRol)}>
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  );
}
