import { useEffect } from "react";
import { Box, IconButton, Stack, Typography } from "@mui/material";
import FormatBoldIcon from "@mui/icons-material/FormatBold";
import FormatItalicIcon from "@mui/icons-material/FormatItalic";
import FormatListBulletedIcon from "@mui/icons-material/FormatListBulleted";
import { EditorContent, useEditor, useEditorState } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import { useQueryClient } from "@tanstack/react-query";
import { subirArchivo } from "../api/archivos";
import { ImagenProtegida } from "./ImagenProtegida";
import { normalizarHtmlLegado } from "./textoPlano";

interface Props {
  value: string;
  onChange: (html: string) => void;
  label?: string;
  placeholder?: string;
  minHeight?: number;
  /** Si no se da, pegar una imagen se rechaza (no hay a que WorkItem adjuntarla todavia). */
  idWorkItemParaAdjuntos?: number;
  onError?: (mensaje: string) => void;
  /** Util para deshabilitar un boton de envio: un editor "vacio" sigue siendo HTML no-vacio (ej. "<p></p>"). */
  onVacioChange?: (vacio: boolean) => void;
}

/**
 * Editor enriquecido generico para campos de formulario (Descripcion de
 * WorkItem, captura de Hallazgos): formato basico + pegado de imagenes del
 * portapapeles, mismo patron que EditorComentario pero sin @menciones ni
 * boton de enviar (es un input controlado, no un formulario de comentario).
 */
export function EditorEnriquecido({
  value, onChange, label, placeholder, minHeight = 80, idWorkItemParaAdjuntos, onError, onVacioChange,
}: Props) {
  const clienteQuery = useQueryClient();

  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({ placeholder: placeholder ?? "" }),
      ImagenProtegida,
    ],
    content: normalizarHtmlLegado(value),
    editorProps: {
      attributes: { class: "editor-enriquecido" },
      handlePaste: (view, event) => {
        const items = Array.from(event.clipboardData?.items ?? []);
        const imagen = items.find((item) => item.type.startsWith("image/"));
        const archivo = imagen?.getAsFile();
        if (!archivo) return false;

        event.preventDefault();
        if (!idWorkItemParaAdjuntos) {
          onError?.("Guarda el elemento antes de poder pegar imagenes aqui.");
          return true;
        }
        subirArchivo(idWorkItemParaAdjuntos, archivo)
          .then((resultado) => {
            if (!resultado) return;
            const nodo = view.state.schema.nodes.imagenProtegida.create({ guid: resultado.dato.guidArchivo });
            view.dispatch(view.state.tr.replaceSelectionWith(nodo));
            void clienteQuery.invalidateQueries({ queryKey: ["archivos", idWorkItemParaAdjuntos] });
          })
          .catch((error: unknown) => {
            onError?.(error instanceof Error ? error.message : "No se pudo subir la imagen pegada.");
          });
        return true;
      },
    },
    onUpdate: ({ editor: instancia }) => onChange(instancia.getHTML()),
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [idWorkItemParaAdjuntos]);

  // Sincroniza resets externos (ej. reabrir el modal con otro item); las
  // ediciones propias no disparan esto porque `value` ya coincide con
  // editor.getHTML() cuando el cambio vino de onUpdate.
  useEffect(() => {
    if (!editor || editor.isDestroyed) return;
    const normalizado = normalizarHtmlLegado(value);
    if (normalizado !== editor.getHTML()) {
      editor.commands.setContent(normalizado, { emitUpdate: false });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value, editor]);

  const activo = useEditorState({
    editor,
    selector: ({ editor: instancia }) => ({
      negrita: instancia.isActive("bold"),
      cursiva: instancia.isActive("italic"),
      lista: instancia.isActive("bulletList"),
    }),
  });
  const vacio = useEditorState({ editor, selector: ({ editor: instancia }) => instancia.isEmpty });

  useEffect(() => {
    onVacioChange?.(vacio);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vacio]);

  return (
    <Box>
      {label && (
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
          {label}
        </Typography>
      )}
      <Stack direction="row" spacing={0.5} sx={{ mb: 0.5 }}>
        <IconButton size="small" color={activo.negrita ? "primary" : "default"}
          onClick={() => editor?.chain().focus().toggleBold().run()}>
          <FormatBoldIcon fontSize="small" />
        </IconButton>
        <IconButton size="small" color={activo.cursiva ? "primary" : "default"}
          onClick={() => editor?.chain().focus().toggleItalic().run()}>
          <FormatItalicIcon fontSize="small" />
        </IconButton>
        <IconButton size="small" color={activo.lista ? "primary" : "default"}
          onClick={() => editor?.chain().focus().toggleBulletList().run()}>
          <FormatListBulletedIcon fontSize="small" />
        </IconButton>
      </Stack>
      <Box sx={{
        border: "1px solid", borderColor: "divider", borderRadius: 1, p: 1, minHeight,
        "& .editor-enriquecido": { outline: "none" },
        "& .editor-enriquecido p.is-editor-empty:first-of-type::before": {
          content: "attr(data-placeholder)", color: "text.disabled", float: "left", height: 0, pointerEvents: "none",
        },
      }}>
        <EditorContent editor={editor} />
      </Box>
    </Box>
  );
}
