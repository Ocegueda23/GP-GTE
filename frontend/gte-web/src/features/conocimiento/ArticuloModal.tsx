import { useEffect, useState } from "react";
import {
  Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel,
  Stack, Switch, TextField, Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { EditorEnriquecido } from "../../shared/editor/EditorEnriquecido";
import {
  actualizarArticulo, crearArticulo, subirArchivoArticulo, type Articulo,
} from "../../shared/api/conocimiento";
import { PanelAdjuntosArticulo } from "./PanelAdjuntosArticulo";

interface Props {
  abierto: boolean;
  /** null = alta; con articulo = edicion. */
  articulo: Articulo | null;
  onCerrar: () => void;
  onExito: (mensaje: string) => void;
}

/**
 * Alta y edicion de articulos y terminos del glosario. Reutiliza EditorEnriquecido,
 * el mismo editor de la Descripcion de WorkItem: trae formato basico y pegado de
 * imagenes del portapapeles.
 *
 * Pegar una imagen exige que el articulo YA exista (necesita un IdEntidad al que
 * adjuntar el archivo), asi que en el alta el editor rechaza el pegado con un aviso
 * y la subida queda disponible al reabrir para editar -- mismo comportamiento que
 * los demas formularios del sistema.
 */
export function ArticuloModal({ abierto, articulo, onCerrar, onExito }: Props) {
  const [titulo, setTitulo] = useState("");
  const [contenido, setContenido] = useState("");
  const [esGlosario, setEsGlosario] = useState(false);
  const [esPublico, setEsPublico] = useState(false);
  const [contenidoVacio, setContenidoVacio] = useState(true);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const clienteQuery = useQueryClient();

  const esEdicion = articulo !== null;

  useEffect(() => {
    if (!abierto) return;
    setTitulo(articulo?.titulo ?? "");
    setContenido(articulo?.contenido ?? "");
    setEsGlosario(articulo?.esGlosario ?? false);
    setEsPublico(articulo?.esPublico ?? false);
    setError(null);
  }, [abierto, articulo]);

  const valido = titulo.trim().length > 0 && !contenidoVacio;

  const guardar = async () => {
    if (!valido) return;
    setEnviando(true);
    setError(null);
    try {
      const datos = { titulo: titulo.trim(), contenido, esGlosario, esPublico };
      const { mensaje } = esEdicion
        ? await actualizarArticulo(articulo.idArticuloConocimiento, datos)
        : await crearArticulo(datos);
      await clienteQuery.invalidateQueries({ queryKey: ["conocimiento"] });
      if (esEdicion) {
        await clienteQuery.invalidateQueries({
          queryKey: ["articulo", articulo.idArticuloConocimiento],
        });
      }
      onExito(mensaje);
      onCerrar();
    } catch (err) {
      setError(err instanceof ErrorApi ? err.message : "No se pudo guardar el articulo.");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Dialog open={abierto} onClose={onCerrar} fullWidth maxWidth="md">
      <DialogTitle>{esEdicion ? "Editar articulo" : "Nuevo articulo"}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
        {error && <Alert severity="error">{error}</Alert>}

        <TextField
          size="small" required label="Titulo" value={titulo}
          onChange={(e) => setTitulo(e.target.value)}
          slotProps={{ htmlInput: { maxLength: 200 } }}
        />

        <Stack direction="row" sx={{ flexWrap: "wrap", gap: 2 }}>
          <FormControlLabel
            control={<Switch checked={esGlosario} onChange={(e) => setEsGlosario(e.target.checked)} />}
            label="Es termino del Glosario"
          />
          <FormControlLabel
            control={<Switch color="secondary" checked={esPublico}
              onChange={(e) => setEsPublico(e.target.checked)} />}
            label="Publico (visible sin iniciar sesion)"
          />
        </Stack>

        {esPublico && (
          <Alert severity="warning">
            Este articulo se podra leer desde internet sin iniciar sesion, con sus imagenes.
            Los archivos adjuntos NO se publican: solo se ven desde aqui.
          </Alert>
        )}

        <EditorEnriquecido
          label="Contenido"
          placeholder="Escribe aqui, o pega una imagen del portapapeles..."
          value={contenido}
          onChange={setContenido}
          onVacioChange={setContenidoVacio}
          minHeight={220}
          onSubirImagen={esEdicion
            ? (archivo) => subirArchivoArticulo(articulo.idArticuloConocimiento, archivo)
            : undefined}
          onError={setError}
        />

        {esEdicion ? (
          <PanelAdjuntosArticulo
            idArticulo={articulo.idArticuloConocimiento}
            alError={setError}
          />
        ) : (
          <Typography variant="caption" color="text.secondary">
            Guarda el articulo para poder adjuntar archivos y pegar imagenes.
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onCerrar}>Cancelar</Button>
        <Button variant="contained" disabled={enviando || !valido} onClick={() => void guardar()}>
          Guardar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
