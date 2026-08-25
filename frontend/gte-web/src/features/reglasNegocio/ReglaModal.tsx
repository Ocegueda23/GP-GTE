import { useEffect, useState } from "react";
import {
  Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField,
} from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  actualizarRegla, crearRegla, ESTADO_REGLA,
  type AmbitoRegla, type CatalogosReglasNegocio, type ReglaNegocio,
} from "../../shared/api/reglasNegocio";

interface Props {
  abierto: boolean;
  idProyecto: number;
  /** null = alta; con valor = edicion de esa regla. */
  regla: ReglaNegocio | null;
  ambitos: AmbitoRegla[];
  catalogos: CatalogosReglasNegocio | undefined;
  onCerrar: () => void;
  onGuardado: (mensaje: string) => void;
}

/**
 * Alta y edicion de una regla. El proyecto dueno y la clave no se editan despues del alta:
 * cambiar el dueno cambiaria la identidad de la regla y dejaria sus impactos apuntando a
 * un proyecto que ya no es el suyo.
 */
export function ReglaModal({
  abierto, idProyecto, regla, ambitos, catalogos, onCerrar, onGuardado,
}: Props) {
  const clienteQuery = useQueryClient();
  const esEdicion = regla !== null;

  const [clave, setClave] = useState("");
  const [nombre, setNombre] = useState("");
  const [enunciado, setEnunciado] = useState("");
  const [justificacion, setJustificacion] = useState("");
  const [idAmbito, setIdAmbito] = useState<number | "">("");
  const [idEstado, setIdEstado] = useState<number>(ESTADO_REGLA.vigente);
  const [mensajeError, setMensajeError] = useState("");
  const [permisoBypass, setPermisoBypass] = useState("");
  const [ubicacionCodigo, setUbicacionCodigo] = useState("");
  const [motivoCambio, setMotivoCambio] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!abierto) return;
    setClave(regla?.clave ?? "");
    setNombre(regla?.nombre ?? "");
    setEnunciado(regla?.enunciado ?? "");
    setJustificacion(regla?.justificacion ?? "");
    setIdAmbito(regla?.idAmbitoRegla ?? "");
    setIdEstado(regla?.idEstadoReglaNegocio ?? ESTADO_REGLA.vigente);
    setMensajeError(regla?.mensajeError ?? "");
    setPermisoBypass(regla?.permisoBypass ?? "");
    setUbicacionCodigo(regla?.ubicacionCodigo ?? "");
    setMotivoCambio("");
    setError(null);
  }, [abierto, regla]);

  const guardar = useMutation({
    mutationFn: async () => {
      const comun = {
        nombre: nombre.trim(),
        enunciado: enunciado.trim(),
        justificacion: justificacion.trim() || null,
        idAmbitoRegla: idAmbito === "" ? null : idAmbito,
        idEstadoReglaNegocio: idEstado,
        mensajeError: mensajeError.trim() || null,
        permisoBypass: permisoBypass.trim() || null,
        ubicacionCodigo: ubicacionCodigo.trim() || null,
        fechaVigenciaDesde: null,
        fechaVigenciaHasta: null,
      };

      return esEdicion
        ? actualizarRegla(regla.idReglaNegocio, { ...comun, motivoCambio: motivoCambio.trim() || null })
        : crearRegla({ ...comun, idProyecto });
    },
    onSuccess: ({ mensaje }) => {
      void clienteQuery.invalidateQueries({ queryKey: ["reglasNegocio"] });
      onGuardado(mensaje);
      onCerrar();
    },
    onError: (e: Error) => setError(e.message),
  });

  const puedeGuardar = nombre.trim() !== "" && enunciado.trim() !== "";

  return (
    <Dialog open={abierto} onClose={onCerrar} maxWidth="md" fullWidth>
      <DialogTitle>{esEdicion ? `Editar ${regla.clave}` : "Nueva regla de negocio"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <Stack direction="row" spacing={2}>
            {/* La clave no se captura: la forma el backend al guardar como
                RN-{clave del proyecto}-{consecutivo}. En edicion se muestra ya
                asignada, solo como referencia. */}
            <TextField
              label="Clave"
              value={esEdicion ? clave : "Se asigna al guardar"}
              disabled
              helperText={esEdicion ? "No se puede cambiar." : "RN-<proyecto>-<consecutivo>"}
              sx={{ width: 220 }}
            />
            <TextField
              label="Nombre"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              fullWidth
            />
          </Stack>

          <TextField
            label="Enunciado"
            value={enunciado}
            onChange={(e) => setEnunciado(e.target.value)}
            multiline
            minRows={4}
            fullWidth
            helperText="Que exige la regla, en lenguaje de negocio."
          />

          <TextField
            label="Justificacion"
            value={justificacion}
            onChange={(e) => setJustificacion(e.target.value)}
            multiline
            minRows={3}
            fullWidth
            helperText="Por que existe. Es lo que normalmente se pierde y lo que mas cuesta reconstruir."
          />

          <Stack direction="row" spacing={2}>
            <TextField
              select
              label="Flujo o caracteristica"
              value={idAmbito}
              onChange={(e) => setIdAmbito(e.target.value === "" ? "" : Number(e.target.value))}
              fullWidth
              helperText="Opcional: una regla puede ser general al proyecto."
            >
              <MenuItem value="">(sin ubicar)</MenuItem>
              {ambitos.map((a) => (
                <MenuItem key={a.idAmbitoRegla} value={a.idAmbitoRegla}>
                  {a.nombreTipoAmbito}: {a.nombre}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Estado"
              value={idEstado}
              onChange={(e) => setIdEstado(Number(e.target.value))}
              sx={{ minWidth: 260 }}
            >
              {(catalogos?.estados ?? []).map((e) => (
                <MenuItem key={e.id} value={e.id}>{e.nombre}</MenuItem>
              ))}
            </TextField>
          </Stack>

          <Stack direction="row" spacing={2}>
            <TextField
              label="Permiso que la omite"
              value={permisoBypass}
              onChange={(e) => setPermisoBypass(e.target.value)}
              fullWidth
              helperText="Opcional. Ejemplo: WI.OmitirValidacionCierre"
            />
            <TextField
              label="Donde se aplica en el codigo"
              value={ubicacionCodigo}
              onChange={(e) => setUbicacionCodigo(e.target.value)}
              fullWidth
              helperText="Opcional: ruta del archivo que la implementa."
            />
          </Stack>

          <TextField
            label="Mensaje al usuario cuando la regla bloquea"
            value={mensajeError}
            onChange={(e) => setMensajeError(e.target.value)}
            fullWidth
          />

          {esEdicion && (
            <TextField
              label="Motivo del cambio"
              value={motivoCambio}
              onChange={(e) => setMotivoCambio(e.target.value)}
              fullWidth
              helperText="Se guarda en el historial si el enunciado o la justificacion cambian."
            />
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCerrar}>Cancelar</Button>
        <Button
          variant="contained"
          disabled={!puedeGuardar || guardar.isPending}
          onClick={() => guardar.mutate()}
        >
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
