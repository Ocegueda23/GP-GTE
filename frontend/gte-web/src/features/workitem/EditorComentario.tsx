import { forwardRef, useCallback, useEffect, useImperativeHandle, useState } from "react";
import { Box, Button, IconButton, List, ListItemButton, ListItemText, Paper, Stack } from "@mui/material";
import FormatBoldIcon from "@mui/icons-material/FormatBold";
import FormatItalicIcon from "@mui/icons-material/FormatItalic";
import FormatListBulletedIcon from "@mui/icons-material/FormatListBulleted";
import {
  EditorContent, Node, NodeViewWrapper, ReactNodeViewRenderer, ReactRenderer,
  mergeAttributes, useEditor, useEditorState, type NodeViewProps,
} from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import Mention from "@tiptap/extension-mention";
import type { SuggestionKeyDownProps, SuggestionProps } from "@tiptap/suggestion";
import { useQueryClient } from "@tanstack/react-query";
import { descargarArchivoBlob, subirArchivo } from "../../shared/api/archivos";
import type { CatalogoItem } from "../../shared/api/workitems";

interface ItemMencion {
  id: string;
  label: string;
}

interface ListaMencionesProps {
  items: ItemMencion[];
  command: (item: ItemMencion) => void;
}

interface ListaMencionesHandle {
  onKeyDown: (props: SuggestionKeyDownProps) => boolean;
}

/** Popup de sugerencias de @mencion; posicionado por el mount() que ya trae @tiptap/suggestion v3. */
const ListaMenciones = forwardRef<ListaMencionesHandle, ListaMencionesProps>(function ListaMenciones(
  { items, command },
  ref,
) {
  const [indice, setIndice] = useState(0);
  useEffect(() => setIndice(0), [items]);

  useImperativeHandle(ref, () => ({
    onKeyDown: ({ event }) => {
      if (items.length === 0) return false;
      if (event.key === "ArrowDown") {
        setIndice((i) => (i + 1) % items.length);
        return true;
      }
      if (event.key === "ArrowUp") {
        setIndice((i) => (i - 1 + items.length) % items.length);
        return true;
      }
      if (event.key === "Enter") {
        command(items[indice]);
        return true;
      }
      return false;
    },
  }), [items, indice, command]);

  if (items.length === 0) return null;

  return (
    <Paper variant="outlined" sx={{ maxHeight: 220, overflowY: "auto", minWidth: 180 }}>
      <List dense disablePadding>
        {items.map((item, i) => (
          <ListItemButton key={item.id} selected={i === indice} onClick={() => command(item)}>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </Paper>
  );
});

function crearSugerenciaMenciones(usuarios: CatalogoItem[]) {
  return {
    items: ({ query }: { query: string }): ItemMencion[] =>
      usuarios
        .filter((u) => u.nombre.toLowerCase().includes(query.toLowerCase()))
        .slice(0, 8)
        .map((u) => ({ id: String(u.id), label: u.nombre })),
    render: () => {
      let componente: ReactRenderer<ListaMencionesHandle, ListaMencionesProps>;
      let desmontar: (() => void) | undefined;

      return {
        onStart: (props: SuggestionProps<ItemMencion>) => {
          componente = new ReactRenderer(ListaMenciones, { props, editor: props.editor });
          desmontar = props.mount(componente.element);
        },
        onUpdate(props: SuggestionProps<ItemMencion>) {
          componente.updateProps(props);
        },
        onKeyDown(props: SuggestionKeyDownProps) {
          if (props.event.key === "Escape") {
            desmontar?.();
            return true;
          }
          return componente.ref?.onKeyDown(props) ?? false;
        },
        onExit() {
          desmontar?.();
          componente.destroy();
        },
      };
    },
  };
}

/** Nodo inline que solo guarda el GUID del adjunto; nunca una URL (evita exponer el JWT). */
const ImagenProtegida = Node.create({
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

interface Props {
  idWorkItem: number;
  usuarios: CatalogoItem[];
  enviando: boolean;
  placeholder?: string;
  onEnviar: (html: string) => void;
  onError: (mensaje: string) => void;
}

/** Editor enriquecido para comentarios: formato basico, @menciones e imagenes pegadas del portapapeles. */
export function EditorComentario({ idWorkItem, usuarios, enviando, placeholder, onEnviar, onError }: Props) {
  const clienteQuery = useQueryClient();
  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({ placeholder: placeholder ?? "Escribe un comentario..." }),
      ImagenProtegida,
      Mention.configure({
        HTMLAttributes: { class: "mencion" },
        suggestion: crearSugerenciaMenciones(usuarios),
      }),
    ],
    editorProps: {
      attributes: { class: "editor-comentario" },
      handlePaste: (view, event) => {
        const items = Array.from(event.clipboardData?.items ?? []);
        const imagen = items.find((item) => item.type.startsWith("image/"));
        const archivo = imagen?.getAsFile();
        if (!archivo) return false;

        event.preventDefault();
        subirArchivo(idWorkItem, archivo)
          .then((resultado) => {
            if (!resultado) return;
            const nodo = view.state.schema.nodes.imagenProtegida.create({ guid: resultado.dato.guidArchivo });
            view.dispatch(view.state.tr.replaceSelectionWith(nodo));
            // Tambien crea un adjunto real (tblArchivoVinculo): refleja en la pestana Adjuntos.
            void clienteQuery.invalidateQueries({ queryKey: ["archivos", idWorkItem] });
          })
          .catch((error: unknown) => {
            onError(error instanceof Error ? error.message : "No se pudo subir la imagen pegada.");
          });
        return true;
      },
    },
  }, [idWorkItem]);

  const vacio = useEditorState({ editor, selector: ({ editor: instancia }) => instancia.isEmpty });
  const activo = useEditorState({
    editor,
    selector: ({ editor: instancia }) => ({
      negrita: instancia.isActive("bold"),
      cursiva: instancia.isActive("italic"),
      lista: instancia.isActive("bulletList"),
    }),
  });

  const enviar = useCallback(() => {
    if (editor.isEmpty) return;
    onEnviar(editor.getHTML());
    editor.commands.clearContent();
  }, [editor, onEnviar]);

  return (
    <Paper variant="outlined" sx={{ p: 1 }}>
      <Stack direction="row" spacing={0.5} sx={{ mb: 0.5 }}>
        <IconButton size="small" color={activo.negrita ? "primary" : "default"}
          onClick={() => editor.chain().focus().toggleBold().run()}>
          <FormatBoldIcon fontSize="small" />
        </IconButton>
        <IconButton size="small" color={activo.cursiva ? "primary" : "default"}
          onClick={() => editor.chain().focus().toggleItalic().run()}>
          <FormatItalicIcon fontSize="small" />
        </IconButton>
        <IconButton size="small" color={activo.lista ? "primary" : "default"}
          onClick={() => editor.chain().focus().toggleBulletList().run()}>
          <FormatListBulletedIcon fontSize="small" />
        </IconButton>
      </Stack>
      <Box sx={{
        border: "1px solid", borderColor: "divider", borderRadius: 1, p: 1, minHeight: 80,
        "& .editor-comentario": { outline: "none" },
        "& .editor-comentario p.is-editor-empty:first-of-type::before": {
          content: "attr(data-placeholder)", color: "text.disabled", float: "left", height: 0, pointerEvents: "none",
        },
        "& .mencion": { color: "primary.main", fontWeight: 600 },
      }}>
        <EditorContent editor={editor} />
      </Box>
      <Stack direction="row" sx={{ justifyContent: "flex-end", mt: 1 }}>
        <Button size="small" variant="contained" disabled={enviando || vacio} onClick={enviar}>
          Comentar
        </Button>
      </Stack>
    </Paper>
  );
}
