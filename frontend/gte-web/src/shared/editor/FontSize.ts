import { TextStyle } from "@tiptap/extension-text-style";
import type { ChainedCommands } from "@tiptap/core";

declare module "@tiptap/core" {
  interface Commands<ReturnType> {
    fontSize: {
      setFontSize: (tamano: string) => ReturnType;
      unsetFontSize: () => ReturnType;
    };
  }
}

/**
 * Tamano de letra sobre la marca textStyle (span con style="font-size:...").
 * No se usa el paquete oficial @tiptap/extension-font-size: solo tiene una
 * version 3.0.0-next.x publicada, no una version estable compatible con el
 * resto de paquetes Tiptap 3.29.2 del proyecto.
 */
export const FontSize = TextStyle.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      fontSize: {
        default: null,
        parseHTML: (elemento: HTMLElement) => elemento.style.fontSize || null,
        renderHTML: (atributos: { fontSize?: string | null }) => {
          if (!atributos.fontSize) return {};
          return { style: `font-size: ${atributos.fontSize}` };
        },
      },
    };
  },
  addCommands() {
    return {
      setFontSize: (tamano: string) => ({ chain }: { chain: () => ChainedCommands }) =>
        chain().setMark("textStyle", { fontSize: tamano }).run(),
      unsetFontSize: () => ({ chain }: { chain: () => ChainedCommands }) =>
        chain().setMark("textStyle", { fontSize: null }).run(),
    };
  },
});
