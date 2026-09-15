import { useEffect, useState } from "react";
import {
  Box, Divider, FormControl, IconButton, Menu, MenuItem, Select, Stack, Tooltip, Typography,
  type SelectChangeEvent,
} from "@mui/material";
import FormatBoldIcon from "@mui/icons-material/FormatBold";
import FormatItalicIcon from "@mui/icons-material/FormatItalic";
import FormatListBulletedIcon from "@mui/icons-material/FormatListBulleted";
import GridOnIcon from "@mui/icons-material/GridOn";
import { EditorContent, useEditor, useEditorState } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import { TableKit } from "@tiptap/extension-table";
import { useQueryClient } from "@tanstack/react-query";
import { subirArchivo, subirArchivoBorrador, type Archivo } from "../api/archivos";
import { ImagenProtegida } from "./ImagenProtegida";
import { FontSize } from "./FontSize";
import { ESTILOS_TABLA } from "./estilosTabla";
import { normalizarHtmlLegado } from "./textoPlano";

const TAMANOS_LETRA = ["12px", "14px", "16px", "18px", "24px"];

interface Props {
  value: string;
  onChange: (html: string) => void;
  label?: string;
  placeholder?: string;
  minHeight?: number;
  /** WorkItem al que adjuntar las imagenes pegadas cuando el item ya existe. */
  idWorkItemParaAdjuntos?: number;
  /**
   * Subida de imagen generica para entidades distintas a WorkItem (ej. Solicitud): recibe el
   * archivo pegado y devuelve el GUID ya adjuntado. Tiene prioridad sobre idWorkItemParaAdjuntos
   * si ambos se pasan.
   */
  onSubirImagen?: (archivo: File) => Promise<{ dato: Archivo; mensaje: string } | undefined>;
  onError?: (mensaje: string) => void;
  /** Util para deshabilitar un boton de envio: un editor "vacio" sigue siendo HTML no-vacio (ej. "<p></p>"). */
  onVacioChange?: (vacio: boolean) => void;
  /**
   * Habilita tablas: se pueden crear desde la barra y se conservan las que el usuario
   * pega del portapapeles (Excel, Word u otra pagina). Es opcional porque en campos
   * cortos -- un comentario, un hallazgo -- una tabla estorba mas de lo que ayuda; se
   * enciende donde el contenido de verdad es tabular, como las instrucciones de
   * implementacion de un release.
   */
  soportaTablas?: boolean;
}


/**
 * Editor enriquecido generico para campos de formulario (Descripcion de
 * WorkItem, captura de Hallazgos): formato basico + pegado de imagenes del
 * portapapeles, mismo patron que EditorComentario pero sin @menciones ni
 * boton de enviar (es un input controlado, no un formulario de comentario).
 */
export function EditorEnriquecido({
  value, onChange, label, placeholder, minHeight = 80, idWorkItemParaAdjuntos, onSubirImagen, onError,
  onVacioChange, soportaTablas = false,
}: Props) {
  const clienteQuery = useQueryClient();
  // Sin entidad destino (formulario de alta) la imagen se sube en borrador: queda sin vinculo
  // y el comando de alta la adjunta al guardar, leyendo el GUID del contenido. Antes aqui se
  // rechazaba el pegado, lo que obligaba a guardar, reabrir y volver a guardar -- y en la base
  // de conocimiento eso dejaba el articulo en version 2 recien creado.
  const subir = onSubirImagen
    ?? (idWorkItemParaAdjuntos
      ? (archivo: File) => subirArchivo(idWorkItemParaAdjuntos, archivo)
      : (archivo: File) => subirArchivoBorrador(archivo));

  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({ placeholder: placeholder ?? "" }),
      ImagenProtegida,
      FontSize,
      // Sin las extensiones de tabla registradas, ProseMirror descarta el <table> al pegar
      // y solo deja el texto suelto de las celdas: por eso el pegado desde Excel o Word
      // depende de esto, no de un manejador propio de portapapeles.
      ...(soportaTablas ? [TableKit.configure({ table: { resizable: true } })] : []),
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
        subir(archivo)
          .then((resultado) => {
            if (!resultado) return;
            const nodo = view.state.schema.nodes.imagenProtegida.create({ guid: resultado.dato.guidArchivo });
            view.dispatch(view.state.tr.replaceSelectionWith(nodo));
            if (idWorkItemParaAdjuntos) {
              void clienteQuery.invalidateQueries({ queryKey: ["archivos", idWorkItemParaAdjuntos] });
            }
          })
          .catch((error: unknown) => {
            onError?.(error instanceof Error ? error.message : "No se pudo subir la imagen pegada.");
          });
        return true;
      },
    },
    onUpdate: ({ editor: instancia }) => onChange(instancia.getHTML()),
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [idWorkItemParaAdjuntos, onSubirImagen, soportaTablas]);

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
      tabla: instancia.isActive("table"),
      tamanoLetra: (instancia.getAttributes("textStyle").fontSize as string | undefined) ?? "",
    }),
  });

  const [menuTabla, setMenuTabla] = useState<HTMLElement | null>(null);
  const accionTabla = (accion: () => void) => () => { accion(); setMenuTabla(null); };

  const cambiarTamano = (evento: SelectChangeEvent<string>) => {
    const tamano = evento.target.value;
    if (!tamano) editor?.chain().focus().unsetFontSize().run();
    else editor?.chain().focus().setFontSize(tamano).run();
  };
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
        <FormControl size="small" variant="standard" sx={{ minWidth: 72, ml: 0.5 }}>
          <Select displayEmpty value={activo.tamanoLetra} onChange={cambiarTamano}
            sx={{ fontSize: 13 }}>
            <MenuItem value="">Normal</MenuItem>
            {TAMANOS_LETRA.map((tamano) => (
              <MenuItem key={tamano} value={tamano}>{tamano}</MenuItem>
            ))}
          </Select>
        </FormControl>
        {soportaTablas && (
          <Tooltip title="Tabla">
            <IconButton size="small" color={activo.tabla ? "primary" : "default"}
              onClick={(e) => setMenuTabla(e.currentTarget)}>
              <GridOnIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
      </Stack>
      <Menu anchorEl={menuTabla} open={!!menuTabla} onClose={() => setMenuTabla(null)}>
        <MenuItem onClick={accionTabla(() => editor?.chain().focus()
          .insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run())}>
          Insertar tabla 3 x 3
        </MenuItem>
        <Divider />
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().addRowAfter().run())}>
          Agregar renglon
        </MenuItem>
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().addColumnAfter().run())}>
          Agregar columna
        </MenuItem>
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().deleteRow().run())}>
          Quitar renglon
        </MenuItem>
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().deleteColumn().run())}>
          Quitar columna
        </MenuItem>
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().mergeOrSplit().run())}>
          Combinar o dividir celdas
        </MenuItem>
        <Divider />
        <MenuItem disabled={!activo.tabla}
          onClick={accionTabla(() => editor?.chain().focus().deleteTable().run())}>
          Eliminar tabla
        </MenuItem>
      </Menu>
      <Box sx={{
        border: "1px solid", borderColor: "divider", borderRadius: 1, p: 1, minHeight,
        "& .editor-enriquecido": { outline: "none" },
        "& .editor-enriquecido p.is-editor-empty:first-of-type::before": {
          content: "attr(data-placeholder)", color: "text.disabled", float: "left", height: 0, pointerEvents: "none",
        },
        ...(soportaTablas ? ESTILOS_TABLA : {}),
      }}>
        <EditorContent editor={editor} />
      </Box>
    </Box>
  );
}
