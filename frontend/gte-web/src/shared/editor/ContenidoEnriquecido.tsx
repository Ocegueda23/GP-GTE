import { useEffect, useMemo, useRef } from "react";
import { Box } from "@mui/material";
import { descargarArchivoBlob } from "../api/archivos";
import { ESTILOS_TABLA } from "./estilosTabla";
import { normalizarHtmlLegado } from "./textoPlano";

interface Props {
  html: string;
  /**
   * Modo PUBLICO (paginas anonimas de la base de conocimiento): resuelve cada imagen
   * a una URL directa en vez de bajar el blob autenticado. Se puede usar como
   * `<img src>` sin riesgo porque la ruta publica es anonima -- no hay token que
   * exponer en la URL, que es la razon de ser del blob en el modo autenticado.
   * Sin esta prop, el componente se comporta igual que siempre.
   */
  urlPublicaImagen?: (guid: string) => string;
}

/**
 * Renderiza HTML enriquecido guardado por EditorEnriquecido/EditorComentario.
 * El HTML persistido solo trae `data-guid` en las imagenes pegadas (nunca una
 * URL, ver ImagenProtegida): aqui se resuelve el blob autenticado para
 * mostrarlas, igual que hace el NodeView dentro del editor.
 * `dangerouslySetInnerHTML` no ejecuta NodeViews de React, por eso la
 * resolucion se hace a mano contra el DOM ya renderizado.
 */
export function ContenidoEnriquecido({ html, urlPublicaImagen }: Props) {
  const contenedorRef = useRef<HTMLDivElement>(null);
  const htmlNormalizado = useMemo(() => normalizarHtmlLegado(html), [html]);

  useEffect(() => {
    const contenedor = contenedorRef.current;
    if (!contenedor) return;
    const urls: string[] = [];
    const guids = Array.from(contenedor.querySelectorAll<HTMLImageElement>("img[data-guid]"))
      .map((img) => img.getAttribute("data-guid"))
      .filter((guid): guid is string => !!guid);

    // Re-consulta el nodo actual por GUID en vez de confiar en la referencia
    // capturada al inicio: bajo StrictMode (dev) el contenedor puede haberse
    // vuelto a montar antes de que esta promesa resuelva, dejando el <img>
    // original desconectado del documento (la asignacion no truena, pero no
    // se ve nada porque ese nodo ya no esta en pantalla).
    const asignar = (guid: string, url: string) => {
      const imagenActual = contenedorRef.current?.querySelector<HTMLImageElement>(
        `img[data-guid="${guid}"]`,
      );
      if (imagenActual) {
        imagenActual.src = url;
      }
    };

    if (urlPublicaImagen) {
      guids.forEach((guid) => asignar(guid, urlPublicaImagen(guid)));
      return;
    }

    guids.forEach((guid) => {
      descargarArchivoBlob(guid)
        .then((blob) => {
          const url = URL.createObjectURL(blob);
          urls.push(url);
          asignar(guid, url);
        })
        .catch(() => {});
    });
    return () => urls.forEach((url) => URL.revokeObjectURL(url));
  }, [htmlNormalizado, urlPublicaImagen]);

  return (
    <Box
      ref={contenedorRef}
      sx={{
        "& p": { m: 0 },
        "& p + p": { mt: 1 },
        "& .mencion": { color: "primary.main", fontWeight: 600 },
        "& img[data-guid]": { maxWidth: "100%", maxHeight: 320, borderRadius: 1, display: "block", mt: 0.5 },
        ...ESTILOS_TABLA,
      }}
      dangerouslySetInnerHTML={{ __html: htmlNormalizado }}
    />
  );
}
