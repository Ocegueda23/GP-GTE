import { useEffect, useState } from "react";
import {
  Alert, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, Stack, TextField,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import {
  actualizarRegistro, crearRegistro, obtenerOpcionesFk,
  type ColumnaConfig, type ConfiguracionCatalogo, type Registro,
} from "../../shared/api/catalogoGenerico";

const TIPOS_NUMERICOS = new Set(["int", "smallint", "tinyint", "bigint", "decimal", "numeric", "money", "smallmoney", "float", "real"]);
const TIPOS_FECHA_HORA = new Set(["datetime", "datetime2", "smalldatetime"]);

function esColumnaEditable(columna: ColumnaConfig, esAlta: boolean): boolean {
  if (columna.esIdentity) return false;
  if (esAlta) return !columna.autoFechaAlta && !columna.autoUsuarioAlta;
  return !columna.esPk && !columna.esSoloLectura && !columna.autoFechaEdicion && !columna.autoUsuarioEdicion;
}

/** Convierte el valor crudo del formulario (siempre string/boolean) al tipo JSON que espera el backend. */
function prepararValor(tipoSql: string, crudo: string | boolean): unknown {
  if (typeof crudo === "boolean") return crudo;
  if (crudo === "") return null;
  const tipo = tipoSql.toLowerCase();
  if (TIPOS_NUMERICOS.has(tipo)) {
    const numero = Number(crudo);
    return Number.isNaN(numero) ? null : numero;
  }
  return crudo;
}

function ComboFk({ clave, columna, value, onChange }: {
  clave: string; columna: ColumnaConfig; value: string; onChange: (v: string) => void;
}) {
  const opciones = useQuery({
    queryKey: ["catalogo-opciones-fk", clave, columna.nombreColumna],
    queryFn: () => obtenerOpcionesFk(clave, columna.nombreColumna),
  });
  return (
    <ComboBuscable
      label={columna.displayName}
      required={columna.esRequerido}
      value={value}
      onChange={(v) => onChange(String(v))}
      opciones={(opciones.data ?? []).map((o) => ({ valor: o.valor, etiqueta: o.etiqueta }))}
    />
  );
}

interface Props {
  clave: string;
  config: ConfiguracionCatalogo;
  /** null = alta; con datos = edicion. */
  registro: Registro | null;
  onClose: () => void;
  onGuardado: (mensaje: string) => void;
}

/** Formulario dinamico de alta/edicion: un control por columna visible y editable, segun tipo. */
export function CatalogoFormModal({ clave, config, registro, onClose, onGuardado }: Props) {
  const esAlta = registro === null;
  const columnas = [...config.columnas]
    .filter((c) => c.esVisible && esColumnaEditable(c, esAlta))
    .sort((a, b) => a.ordinalPos - b.ordinalPos);

  const [valores, setValores] = useState<Record<string, string | boolean>>({});
  const [error, setError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    const iniciales: Record<string, string | boolean> = {};
    columnas.forEach((c) => {
      const actual = registro?.[c.nombreColumna];
      if (c.tipoSql.toLowerCase() === "bit") {
        iniciales[c.nombreColumna] = Boolean(actual);
      } else if (c.esCifrado) {
        iniciales[c.nombreColumna] = ""; // nunca se precarga un valor cifrado
      } else {
        iniciales[c.nombreColumna] = actual === null || actual === undefined ? "" : String(actual);
      }
    });
    setValores(iniciales);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- se recalcula solo al abrir el modal con otro registro
  }, [registro, config.clave]);

  const cambiar = (nombreColumna: string, valor: string | boolean) => {
    setValores((actual) => ({ ...actual, [nombreColumna]: valor }));
  };

  const guardar = async () => {
    const faltante = columnas.find((c) => c.esRequerido && !c.esCifrado && !valores[c.nombreColumna]);
    if (faltante) {
      setError(`${faltante.displayName} es obligatorio.`);
      return;
    }

    const payload: Registro = {};
    columnas.forEach((c) => {
      const crudo = valores[c.nombreColumna];
      // En edicion, un campo cifrado en blanco significa "no cambiar": se omite del payload.
      if (c.esCifrado && !esAlta && crudo === "") return;
      payload[c.nombreColumna] = prepararValor(c.tipoSql, crudo);
    });

    setGuardando(true);
    setError(null);
    try {
      if (esAlta) {
        const { mensaje } = await crearRegistro(clave, payload);
        onGuardado(mensaje);
      } else {
        const clavesPk: Registro = {};
        config.columnas.filter((c) => c.esPk).forEach((c) => { clavesPk[c.nombreColumna] = registro![c.nombreColumna]; });
        const mensaje = await actualizarRegistro(clave, clavesPk, payload);
        onGuardado(mensaje);
      }
    } catch (err) {
      setError(err instanceof ErrorApi ? err.message : "No se pudo guardar el registro.");
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{esAlta ? `Nuevo: ${config.titulo}` : `Editar: ${config.titulo}`}</DialogTitle>
      <DialogContent>
        <Stack sx={{ gap: 2, mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          {columnas.map((c) => {
            const tipo = c.tipoSql.toLowerCase();
            const valor = valores[c.nombreColumna] ?? (tipo === "bit" ? false : "");

            if (c.tablaFk && c.columnaClaveFk && c.columnaMostrarFk) {
              return (
                <ComboFk key={c.nombreColumna} clave={clave} columna={c}
                  value={String(valor)} onChange={(v) => cambiar(c.nombreColumna, v)} />
              );
            }
            if (tipo === "bit") {
              return (
                <FormControlLabel key={c.nombreColumna}
                  control={<Checkbox checked={Boolean(valor)} onChange={(e) => cambiar(c.nombreColumna, e.target.checked)} />}
                  label={c.displayName} />
              );
            }
            if (tipo === "date") {
              return (
                <TextField key={c.nombreColumna} label={c.displayName} type="date" required={c.esRequerido}
                  slotProps={{ inputLabel: { shrink: true } }}
                  value={valor} onChange={(e) => cambiar(c.nombreColumna, e.target.value)} />
              );
            }
            if (TIPOS_FECHA_HORA.has(tipo)) {
              return (
                <TextField key={c.nombreColumna} label={c.displayName} type="datetime-local" required={c.esRequerido}
                  slotProps={{ inputLabel: { shrink: true } }}
                  value={valor} onChange={(e) => cambiar(c.nombreColumna, e.target.value)} />
              );
            }
            if (TIPOS_NUMERICOS.has(tipo)) {
              return (
                <TextField key={c.nombreColumna} label={c.displayName} type="number" required={c.esRequerido}
                  value={valor} onChange={(e) => cambiar(c.nombreColumna, e.target.value)} />
              );
            }
            return (
              <TextField key={c.nombreColumna} label={c.displayName} required={c.esRequerido}
                type={c.esCifrado ? "password" : "text"}
                placeholder={c.esCifrado && !esAlta ? "Dejar en blanco para no cambiar" : undefined}
                slotProps={{ htmlInput: { maxLength: c.longitudMaxima ?? undefined } }}
                multiline={!c.esCifrado && (c.longitudMaxima ?? 0) > 200}
                value={valor} onChange={(e) => cambiar(c.nombreColumna, e.target.value)} />
            );
          })}
          {columnas.length === 0 && <Alert severity="info">Este catalogo no tiene columnas editables configuradas.</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button color="error" onClick={onClose} disabled={guardando}>Cancelar</Button>
        <Button variant="contained" onClick={() => void guardar()} disabled={guardando}>Guardar</Button>
      </DialogActions>
    </Dialog>
  );
}
