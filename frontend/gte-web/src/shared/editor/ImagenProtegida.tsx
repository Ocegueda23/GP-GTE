import { useEffect, useState } from "react";
import { Box } from "@mui/material";
import { Node, NodeViewWrapper, ReactNodeViewRenderer, mergeAttributes, type NodeViewProps } from "@tiptap/react";
import { descargarArchivoBlob } from "../api/archivos";

/**
 * Nodo TipTap inline que solo guarda el GUID del adjunto; nunca una URL directa
 * (evita exponer el JWT). Compartido por todos los editores enriquecidos del
 * sistema (comentarios, descripcion de WorkItem, hallazgos) -- una sola
 * implementacion, ver InterfloClaude.md seccion 5 sobre no duplicar patrones.
 */
export const ImagenProtegida = Node.create({
  name: "imagenProtegida",
  group: "inline",
  inline: true,
  atom: true,
  addAttributes() {
    return {
      guid: {
        default: null,
        parseHTML: (elemento: HTMLElement) => elemento.getAttribute("data-guid"),
        renderHTML: (atributos: Record<string, unknown>) => ({ "data-guid": atributos.guid }),
      },
    };
  },
  parseHTML() {
    return [{ tag: "img[data-guid]" }];
  },
  renderHTML({ HTMLAttributes }) {
    return ["img", mergeAttributes(HTMLAttributes)];
  },
  addNodeView() {
    return ReactNodeViewRenderer(ImagenProtegidaView);
  },
});

/** Arma el blob autenticado a partir del GUID; nunca <img src> directo al endpoint. */
function ImagenProtegidaView({ node }: NodeViewProps) {
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    const guid = node.attrs.guid as string | null;
    if (!guid) return;
    let cancelado = false;
    let objectUrl: string | null = null;
    descargarArchivoBlob(guid)
      .then((blob) => {
        if (cancelado) return;
        objectUrl = URL.createObjectURL(blob);
        setSrc(objectUrl);
      })
      .catch(() => {});
    return () => {
      cancelado = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [node.attrs.guid]);

  return (
    <NodeViewWrapper as="span" style={{ display: "inline-block", verticalAlign: "middle" }}>
      {src ? (
        <img
          src={src} alt=""
          style={{ maxWidth: "100%", maxHeight: 320, borderRadius: 4, display: "block" }}
        />
      ) : (
        <Box sx={{ width: 120, height: 80, bgcolor: "grey.200", borderRadius: 1 }} />
      )}
    </NodeViewWrapper>
  );
}
