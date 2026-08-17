import { useMemo, useState } from "react";

/**
 * Orden client-side generico para tablas que ya traen el arreglo completo
 * (sin paginacion server-side): click en un encabezado ordena asc, un
 * segundo click en el mismo alterna a desc. Comparacion numerica para
 * numeros, localeCompare (es, numeric) para el resto.
 */
export function useOrdenTabla<T>(datos: T[] | undefined, columnaInicial: keyof T | null = null) {
  const [ordenarPor, setOrdenarPor] = useState<keyof T | null>(columnaInicial);
  const [descendente, setDescendente] = useState(false);

  const ordenar = (columna: keyof T) => {
    if (ordenarPor === columna) {
      setDescendente((previo) => !previo);
    } else {
      setOrdenarPor(columna);
      setDescendente(false);
    }
  };

  const datosOrdenados = useMemo(() => {
    if (!datos) return [];
    if (!ordenarPor) return datos;
    const copia = [...datos];
    copia.sort((a, b) => {
      const va = a[ordenarPor];
      const vb = b[ordenarPor];
      if (va == null && vb == null) return 0;
      if (va == null) return -1;
      if (vb == null) return 1;
      const cmp = typeof va === "number" && typeof vb === "number"
        ? va - vb
        : String(va).localeCompare(String(vb), "es", { numeric: true, sensitivity: "base" });
      return descendente ? -cmp : cmp;
    });
    return copia;
  }, [datos, ordenarPor, descendente]);

  return { datosOrdenados, ordenarPor, descendente, ordenar };
}
