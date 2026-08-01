import { useRef, useState } from "react";
import {
  Box, Button, Chip, IconButton, List, ListItem, ListItemIcon, ListItemText, Stack, Typography,
} from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import DownloadIcon from "@mui/icons-material/Download";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  descargarArchivoBlob, eliminarArchivoVinculo, formatearTamano, obtenerArchivos, subirArchivo,
} from "../../shared/api/archivos";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";

interface Props {
  idWorkItem: number;
  alExito: (mensaje: string) => void;
  alError: (mensaje: string) => void;
}

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

export function PanelAdjuntos({ idWorkItem, alExito, alError }: Props) {
  const [subiendo, setSubiendo] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const dominioActual = useSesion((estado) => estado.sesion?.dominio);
  const clienteQuery = useQueryClient();

  const archivos = useQuery({
    queryKey: ["archivos", idWorkItem],
    queryFn: () => obtenerArchivos(idWorkItem),
  });

  const manejarError = (error: unknown, respaldo: string) => {
    alError(error instanceof ErrorApi ? error.message : respaldo);
  };

  const subir = async (archivo: File) => {
    setSubiendo(true);
    try {
      const resultado = await subirArchivo(idWorkItem, archivo);
      if (resultado) alExito(resultado.mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["archivos", idWorkItem] });
    } catch (error) {
      manejarError(error, "No se pudo subir el archivo.");
    } finally {
      setSubiendo(false);
    }
  };

  const descargar = async (guidArchivo: string, nombreArchivo: string) => {
    try {
      const blob = await descargarArchivoBlob(guidArchivo);
      const url = URL.createObjectURL(blob);
      const enlace = document.createElement("a");
      enlace.href = url;
      enlace.download = nombreArchivo;
      enlace.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      manejarError(error, "No se pudo descargar el archivo.");
    }
  };

  const eliminar = async (idArchivoVinculo: number) => {
    try {
      const mensaje = await eliminarArchivoVinculo(idArchivoVinculo);
      alExito(mensaje);
      await clienteQuery.invalidateQueries({ queryKey: ["archivos", idWorkItem] });
    } catch (error) {
      manejarError(error, "No se pudo eliminar el adjunto.");
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Adjuntos</Typography>
        <Button size="small" variant="contained" disabled={subiendo} onClick={() => inputRef.current?.click()}>
          Adjuntar archivo
        </Button>
        <input
          ref={inputRef} type="file" hidden
          onChange={(evento) => {
            const archivo = evento.target.files?.[0];
            evento.target.value = "";
            if (archivo) void subir(archivo);
          }}
        />
      </Stack>

      {archivos.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
          Sin adjuntos todavia.
        </Typography>
      )}

      <List dense disablePadding>
        {archivos.data?.map((archivo) => {
          const esAutor = dominioActual !== undefined
            && dominioActual.toLowerCase() === archivo.usuarioRegistro.toLowerCase();
          return (
            <ListItem key={archivo.idArchivoVinculo} disableGutters divider sx={{ gap: 1 }}>
              <ListItemIcon sx={{ minWidth: 32 }}>
                <AttachFileIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText
                sx={{ minWidth: 0 }}
                primary={archivo.nombreArchivo}
                secondary={
                  `${formatearTamano(archivo.tamanoBytes)} - ${archivo.autor}`
                  + ` - ${formatearFecha(archivo.fechaRegistro)}`
                }
              />
              {archivo.extension && (
                <Chip size="small" label={archivo.extension.replace(".", "").toUpperCase()} variant="outlined" />
              )}
              <IconButton size="small" onClick={() => void descargar(archivo.guidArchivo, archivo.nombreArchivo)}>
                <DownloadIcon fontSize="small" />
              </IconButton>
              {esAutor && (
                <IconButton size="small" onClick={() => void eliminar(archivo.idArchivoVinculo)}>
                  <DeleteOutlineOutlinedIcon fontSize="small" />
                </IconButton>
              )}
            </ListItem>
          );
        })}
      </List>
    </Box>
  );
}
