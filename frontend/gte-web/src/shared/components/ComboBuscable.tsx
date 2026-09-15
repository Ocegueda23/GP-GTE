import { Autocomplete, TextField, Typography } from "@mui/material";
import type { SxProps, Theme } from "@mui/material";

export interface OpcionComboBuscable {
  valor: number | string;
  etiqueta: string;
  /** Encabezado bajo el que se agrupa la opcion; solo lo usan los catalogos largos. */
  grupo?: string;
}

interface PropsComboBuscable {
  label: string;
  opciones: OpcionComboBuscable[];
  value: number | string;
  onChange: (valor: number | string) => void;
  required?: boolean;
  disabled?: boolean;
  sx?: SxProps<Theme>;
}

/**
 * Reemplazo de Select+MenuItem con buscador tipo LIKE (filtro "contains" insensible a
 * mayusculas, nativo de Autocomplete). El catalogo se recibe normalizado a
 * {valor, etiqueta} para desacoplar el componente de la forma real de cada catalogo
 * (id/nombre, idRelease/version, ambiente, folio/titulo, etc). Las opciones tipo
 * "Todos"/"Sin asignar" se pasan como una entrada mas del arreglo con valor "".
 *
 * Si las opciones traen `grupo`, la lista se agrupa con ese texto como encabezado
 * (catalogos largos, ej. las categorias de incidente por nivel de atencion). El
 * arreglo tiene que llegar ya ordenado por grupo: Autocomplete no reordena, solo
 * inserta el encabezado cuando cambia el valor de una opcion a la siguiente.
 */
export function ComboBuscable({ label, opciones, value, onChange, required, disabled, sx }: PropsComboBuscable) {
  const seleccion = opciones.find((o) => o.valor === value) ?? null;
  const agrupado = opciones.some((o) => o.grupo !== undefined);

  return (
    <Autocomplete
      size="small"
      disabled={disabled}
      options={opciones}
      getOptionLabel={(o) => o.etiqueta}
      groupBy={agrupado ? (o) => o.grupo ?? "" : undefined}
      isOptionEqualToValue={(o, v) => o.valor === v.valor}
      value={seleccion}
      onChange={(_, nuevo) => onChange(nuevo ? nuevo.valor : "")}
      sx={sx}
      renderInput={(params) => <TextField {...params} label={label} required={required} />}
    />
  );
}

interface PropsComboBuscableMultiple {
  label: string;
  opciones: OpcionComboBuscable[];
  value: (number | string)[];
  onChange: (valores: (number | string)[]) => void;
  /** Muestra "N seleccionado(s)" en vez de un chip por opcion (catalogos donde la seleccion puede ser larga). */
  resumenSimple?: boolean;
  sx?: SxProps<Theme>;
}

/** Variante multiple de ComboBuscable. Por defecto renderiza un chip por opcion seleccionada. */
export function ComboBuscableMultiple({
  label, opciones, value, onChange, resumenSimple, sx,
}: PropsComboBuscableMultiple) {
  const seleccion = opciones.filter((o) => value.includes(o.valor));

  return (
    <Autocomplete
      multiple
      size="small"
      options={opciones}
      getOptionLabel={(o) => o.etiqueta}
      isOptionEqualToValue={(o, v) => o.valor === v.valor}
      value={seleccion}
      onChange={(_, nuevo) => onChange(nuevo.map((o) => o.valor))}
      sx={sx}
      renderValue={
        resumenSimple
          ? (valorSeleccionado) => (
              <Typography variant="body2" sx={{ ml: 1 }}>
                {`${valorSeleccionado.length} seleccionado(s)`}
              </Typography>
            )
          : undefined
      }
      renderInput={(params) => <TextField {...params} label={label} />}
    />
  );
}
