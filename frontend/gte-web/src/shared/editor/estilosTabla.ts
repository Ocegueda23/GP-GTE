import type { SystemStyleObject, Theme } from "@mui/system";

/**
 * Estilos de las tablas del contenido enriquecido. Vive aparte del editor porque lo
 * comparten los dos lados -- el que captura (EditorEnriquecido) y el que solo muestra
 * (ContenidoEnriquecido, la Solicitud de despliegue imprimible) -- y ese segundo lado
 * tambien se usa en las paginas publicas, que no deben arrastrar TipTap solo por unas
 * reglas de CSS.
 */
export const ESTILOS_TABLA: SystemStyleObject<Theme> = {
  "& table": {
    borderCollapse: "collapse",
    tableLayout: "fixed",
    width: "100%",
    my: 1,
  },
  "& table td, & table th": {
    border: "1px solid",
    borderColor: "divider",
    px: 1,
    py: 0.5,
    verticalAlign: "top",
    position: "relative",
  },
  "& table th": { bgcolor: "action.hover", fontWeight: 700, textAlign: "left" },
  "& table p": { m: 0 },
  "& .tableWrapper": { overflowX: "auto" },
  // Celdas seleccionadas dentro del editor (clase que pone ProseMirror).
  "& .selectedCell:after": {
    content: '""',
    position: "absolute",
    inset: 0,
    bgcolor: "primary.main",
    opacity: 0.12,
    pointerEvents: "none",
  },
  // Manija de ancho de columna del editor redimensionable.
  "& .column-resize-handle": {
    position: "absolute",
    right: -2,
    top: 0,
    bottom: 0,
    width: "4px",
    bgcolor: "primary.main",
    pointerEvents: "none",
  },
};
