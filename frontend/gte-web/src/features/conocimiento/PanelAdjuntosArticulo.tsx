import { useRef, useState } from "react";
import {
  Box, Button, Chip, IconButton, List, ListItem, ListItemIcon, ListItemText, Stack, Typography,
} from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import DownloadIcon from "@mui/icons-material/Download";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  descargarArchivoBlob, eliminarArchivoVinculo, formatearTamano,
} from "../../shared/api/archivos";
import { obtenerArchivosArticulo, subirArchivoArticulo } from "../../shared/api/conocimiento";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";

interface Props {
  idArticulo: number;
  alError: (mensaje: string) => void;
  /** Solo lectura: oculta subir y eliminar (para quien no tiene CON.Administrar). */
  soloLectura?: boolean;
}

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}

/**
 * Adjuntos de un articulo. Mismo patron que PanelAdjuntos (WorkItem): la tabla de
 * vinculos es generica, solo cambia la entidad. Incluye las imagenes que el editor
 * pego en el texto, porque son archivos adjuntos al articulo igual que un PDF.
 */
export function PanelAdjuntosArticulo({ idArticulo, alError, soloLectura = false }: Props) {
  const [subiendo, setSubiendo] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const dominioActual = useSesion((estado) => estado.sesion?.dominio);
  const clienteQuery = useQueryClient();

  const archivos = useQuery({
    queryKey: ["archivos-articulo", idArticulo],
    queryFn: () => obtenerArchivosArticulo(idArticulo),
  });

  const manejarError = (error: unknown, respaldo: string) => {
    alError(error instanceof ErrorApi ? error.message : respaldo);
  };

  const subir = async (archivo: File) => {
    setSubiendo(true);
    try {
      await subirArchivoArticulo(idArticulo, archivo);
      await clienteQuery.invalidateQueries({ queryKey: ["archivos-articulo", idArticulo] });
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
      await eliminarArchivoVinculo(idArchivoVinculo);
      await clienteQuery.invalidateQueries({ queryKey: ["archivos-articulo", idArticulo] });
    } catch (error) {
      manejarError(error, "No se pudo eliminar el adjunto.");
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">Adjuntos</Typography>
        {!soloLectura && (
          <>
            <Button size="small" variant="outlined" disabled={subiendo}
              onClick={() => inputRef.current?.click()}>
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
          </>
        )}
      </Stack>

      {archivos.data?.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
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
              {!soloLectura && esAutor && (
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
