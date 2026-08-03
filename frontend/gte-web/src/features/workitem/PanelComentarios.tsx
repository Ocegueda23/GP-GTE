import { useMemo, useState } from "react";
import { Box, Button, IconButton, Stack, Typography } from "@mui/material";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import ReplyIcon from "@mui/icons-material/Reply";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  crearComentario, eliminarComentario, obtenerComentarios, type Comentario,
} from "../../shared/api/comentarios";
import { obtenerCatalogosBandeja } from "../../shared/api/workitems";
import { ErrorApi } from "../../shared/api/http";
import { useSesion } from "../../shared/api/sesion";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { EditorComentario } from "./EditorComentario";

interface Props {
  idWorkItem: number;
  alError: (mensaje: string) => void;
}

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleString("es-MX", {
    day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit",
  });
}

/** Franja fija bajo el detalle (no una pestana mas), como lo dibuja el mockup del Documento Maestro. */
export function PanelComentarios({ idWorkItem, alError }: Props) {
  const [respondiendoA, setRespondiendoA] = useState<number | null>(null);
  const dominioActual = useSesion((estado) => estado.sesion?.dominio);
  const clienteQuery = useQueryClient();

  const comentarios = useQuery({
    queryKey: ["comentarios", idWorkItem],
    queryFn: () => obtenerComentarios(idWorkItem),
  });

  const catalogos = useQuery({
    queryKey: ["catalogos-bandeja"], queryFn: obtenerCatalogosBandeja, staleTime: 5 * 60_000,
  });

  const hilos = useMemo(() => {
    const items = comentarios.data ?? [];
    const raiz = items.filter((c) => c.idComentarioPadre === null);
    const respuestasPorPadre = new Map<number, Comentario[]>();
    for (const c of items) {
      if (c.idComentarioPadre === null) continue;
      const lista = respuestasPorPadre.get(c.idComentarioPadre) ?? [];
      lista.push(c);
      respuestasPorPadre.set(c.idComentarioPadre, lista);
    }
    return raiz.map((padre) => ({ padre, respuestas: respuestasPorPadre.get(padre.idComentario) ?? [] }));
  }, [comentarios.data]);

  const manejarError = (error: unknown, respaldo: string) => {
    alError(error instanceof ErrorApi ? error.message : respaldo);
  };

  const publicar = async (contenido: string, idComentarioPadre?: number) => {
    try {
      await crearComentario(idWorkItem, contenido, idComentarioPadre);
      setRespondiendoA(null);
      await clienteQuery.invalidateQueries({ queryKey: ["comentarios", idWorkItem] });
    } catch (error) {
      manejarError(error, "No se pudo publicar el comentario.");
    }
  };

  const borrar = async (idComentario: number) => {
    try {
      await eliminarComentario(idComentario);
      await clienteQuery.invalidateQueries({ queryKey: ["comentarios", idWorkItem] });
    } catch (error) {
      manejarError(error, "No se pudo eliminar el comentario.");
    }
  };

  const usuarios = catalogos.data?.usuarios ?? [];

  function Fila({ comentario, esRespuesta }: { comentario: Comentario; esRespuesta: boolean }) {
    const esAutor = dominioActual !== undefined
      && dominioActual.toLowerCase() === comentario.usuarioRegistro.toLowerCase();

    return (
      <Box sx={{ ml: esRespuesta ? 3 : 0, py: 1, borderTop: "1px solid", borderColor: "divider" }}>
        <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start" }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="caption" sx={{ fontWeight: 700 }}>{comentario.autor}</Typography>
            <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
              {formatearFecha(comentario.fechaRegistro)}
            </Typography>
            <Box sx={{ mt: 0.5 }}>
              {/* El HTML ya viene sanitizado por el backend (ISanitizadorHtml); nunca se confia en HTML sin sanear. */}
              <ContenidoEnriquecido html={comentario.contenido} />
            </Box>
          </Box>
          <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
            {!esRespuesta && (
              <IconButton size="small" onClick={() => setRespondiendoA(comentario.idComentario)}>
                <ReplyIcon fontSize="small" />
              </IconButton>
            )}
            {esAutor && (
              <IconButton size="small" onClick={() => void borrar(comentario.idComentario)}>
                <DeleteOutlineOutlinedIcon fontSize="small" />
              </IconButton>
            )}
          </Stack>
        </Stack>

        {respondiendoA === comentario.idComentario && (
          <Box sx={{ mt: 1 }}>
            <EditorComentario
              idWorkItem={idWorkItem}
              usuarios={usuarios}
              enviando={false}
              placeholder="Responder..."
              onEnviar={(html) => void publicar(html, comentario.idComentario)}
              onError={(mensaje) => alError(mensaje)}
            />
            <Button size="small" sx={{ mt: 0.5 }} onClick={() => setRespondiendoA(null)}>Cancelar</Button>
          </Box>
        )}
      </Box>
    );
  }

  return (
    <Box sx={{ mt: 2 }}>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>Comentarios</Typography>

      {hilos.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
          Sin comentarios todavia.
        </Typography>
      )}

      {hilos.map(({ padre, respuestas }) => (
        <Box key={padre.idComentario}>
          <Fila comentario={padre} esRespuesta={false} />
          {respuestas.map((respuesta) => (
            <Fila key={respuesta.idComentario} comentario={respuesta} esRespuesta />
          ))}
        </Box>
      ))}

      <Box sx={{ mt: 2 }}>
        <EditorComentario
          idWorkItem={idWorkItem}
          usuarios={usuarios}
          enviando={false}
          onEnviar={(html) => void publicar(html)}
          onError={(mensaje) => alError(mensaje)}
        />
      </Box>
    </Box>
  );
}
