import { useEffect, useState } from "react";
import { Avatar } from "@mui/material";
import type { SxProps, Theme } from "@mui/material";
import { descargarArchivoBlob } from "../api/archivos";

function iniciales(nombre: string): string {
  return nombre.split(" ").filter(Boolean).slice(0, 2).map((p) => p[0]).join("").toUpperCase();
}

/**
 * Avatar con foto de perfil autenticada: `urlFoto` (ej. "/api/v1/archivos/{guid}") no se puede
 * usar directo como `<img src>` -- el navegador no manda el Authorization Bearer en una carga
 * de imagen y el endpoint respondería 401 (mismo motivo por el que `archivos.ts` ya evita
 * `<img src>`/`<a href>` directos). Se descarga como blob autenticado y se usa un object URL.
 */
export function AvatarUsuario({ urlFoto, nombre, sx }: { urlFoto: string | null; nombre: string; sx?: SxProps<Theme> }) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    let url: string | null = null;

    if (urlFoto) {
      const guid = urlFoto.split("/").pop();
      if (guid) {
        void descargarArchivoBlob(guid).then((blob) => {
          if (cancelado) return;
          url = URL.createObjectURL(blob);
          setObjectUrl(url);
        }).catch(() => setObjectUrl(null));
      }
    } else {
      setObjectUrl(null);
    }

    return () => {
      cancelado = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [urlFoto]);

  return <Avatar src={objectUrl ?? undefined} sx={sx}>{iniciales(nombre)}</Avatar>;
}
