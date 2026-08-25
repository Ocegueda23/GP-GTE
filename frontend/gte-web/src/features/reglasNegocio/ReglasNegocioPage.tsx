import { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, MenuItem, Paper, Snackbar, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { useSesion } from "../../shared/api/sesion";
import {
  ESTADO_REGLA, obtenerAmbitos, obtenerCatalogoProyecto, obtenerCatalogosReglas,
  obtenerProyectosConReglas, type ReglaNegocioResumen,
} from "../../shared/api/reglasNegocio";
import { obtenerProyectos } from "../../shared/api/administracion";
import { PERMISO_ADMINISTRAR_REGLAS } from "./permisos";
import { ReglaModal } from "./ReglaModal";
import { AmbitosModal } from "./AmbitosModal";

function colorEstado(idEstado: number): "success" | "warning" | "info" | "default" {
  if (idEstado === ESTADO_REGLA.vigente) return "success";
  if (idEstado === ESTADO_REGLA.implementadaParcialmente) return "warning";
  if (idEstado === ESTADO_REGLA.documentadaSinImplementar) return "info";
  return "default";
}

/**
 * Catalogo de reglas de negocio, entrado por proyecto.
 *
 * Se muestran dos bloques: las reglas PROPIAS del proyecto (que se editan aqui) y las
 * HEREDADAS, que son reglas de otros proyectos que declararon afectar a este. Las heredadas
 * se leen pero no se editan: se cambian en su proyecto de origen, y por eso llevan el chip
 * con la clave del dueno y un enlace a su ficha.
 */
export function ReglasNegocioPage() {
  const puede = useSesion((estado) => estado.puede);
  const puedeAdministrar = puede(PERMISO_ADMINISTRAR_REGLAS);

  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [busqueda, setBusqueda] = useState("");
  const [idEstado, setIdEstado] = useState<number | "">("");
  const [incluirDerogadas, setIncluirDerogadas] = useState(false);
  const [modalRegla, setModalRegla] = useState(false);
  const [modalAmbitos, setModalAmbitos] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);

  const proyectos = useQuery({
    queryKey: ["reglasNegocio", "proyectos"],
    queryFn: obtenerProyectosConReglas,
  });

  // Para dar de alta una regla hace falta poder elegir CUALQUIER proyecto activo, no solo
  // los que ya tienen reglas: si no, un proyecto nuevo nunca podria estrenar catalogo.
  const todosProyectos = useQuery({
    queryKey: ["reglasNegocio", "todosProyectos"],
    queryFn: () => obtenerProyectos(true),
    enabled: puedeAdministrar,
  });

  const catalogos = useQuery({
    queryKey: ["reglasNegocio", "catalogos"],
    queryFn: obtenerCatalogosReglas,
  });

  const opcionesProyecto = useMemo(() => {
    const conReglas = proyectos.data ?? [];
    if (!puedeAdministrar) return conReglas.map((p) => ({ id: p.idProyecto, etiqueta: `${p.clave} - ${p.nombre}` }));

    const mapa = new Map<number, string>();
    for (const p of conReglas) mapa.set(p.idProyecto, `${p.clave} - ${p.nombre}`);
    for (const p of todosProyectos.data ?? []) mapa.set(p.idProyecto, `${p.clave} - ${p.nombre}`);
    return [...mapa.entries()]
      .map(([id, etiqueta]) => ({ id, etiqueta }))
      .sort((a, b) => a.etiqueta.localeCompare(b.etiqueta));
  }, [proyectos.data, todosProyectos.data, puedeAdministrar]);

  useEffect(() => {
    if (idProyecto === "" && opcionesProyecto.length > 0) {
      setIdProyecto(opcionesProyecto[0].id);
    }
  }, [opcionesProyecto, idProyecto]);

  const catalogo = useQuery({
    queryKey: ["reglasNegocio", "catalogoProyecto", idProyecto, busqueda, idEstado, incluirDerogadas],
    queryFn: () => obtenerCatalogoProyecto(idProyecto as number, {
      busqueda: busqueda || undefined,
      idEstadoReglaNegocio: idEstado === "" ? null : idEstado,
      incluirDerogadas,
    }),
    enabled: idProyecto !== "",
  });

  const ambitos = useQuery({
    queryKey: ["reglasNegocio", "ambitos", idProyecto],
    queryFn: () => obtenerAmbitos(idProyecto as number),
    enabled: idProyecto !== "",
  });

  const renderTabla = (reglas: ReglaNegocioResumen[], heredadas: boolean) => (
    <TableContainer component={Paper} variant="outlined" sx={{ overflowX: "auto" }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell sx={{ width: 130 }}>Clave</TableCell>
            <TableCell>Nombre</TableCell>
            <TableCell sx={{ width: 220 }}>Flujo / caracteristica</TableCell>
            <TableCell sx={{ width: 190 }}>Estado</TableCell>
            {heredadas && <TableCell sx={{ width: 190 }}>Origen</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {reglas.map((r) => (
            <TableRow key={`${r.idReglaNegocio}-${r.origen}`} hover>
              <TableCell>
                <RouterLink to={`/reglas-negocio/${r.idReglaNegocio}`}>{r.clave}</RouterLink>
              </TableCell>
              <TableCell>
                {r.nombre}
                {heredadas && r.descripcionImpacto && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
                    {r.descripcionImpacto}
                  </Typography>
                )}
              </TableCell>
              <TableCell>
                {r.nombreAmbito
                  ? <Chip size="small" variant="outlined" label={`${r.nombreTipoAmbito}: ${r.nombreAmbito}`} />
                  : <Typography variant="caption" color="text.secondary">General del proyecto</Typography>}
              </TableCell>
              <TableCell>
                <Chip size="small" color={colorEstado(r.idEstadoReglaNegocio)} label={r.nombreEstado} />
              </TableCell>
              {heredadas && (
                <TableCell>
                  <Tooltip title="Esta regla se edita en su proyecto de origen">
                    <Chip size="small" color="warning" variant="outlined" label={r.claveProyectoDueno} />
                  </Tooltip>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );

  return (
    <Box sx={{ p: 3 }}>
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 2 }}>
        <Typography variant="h5">Reglas de negocio</Typography>
        {puedeAdministrar && idProyecto !== "" && (
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" onClick={() => setModalAmbitos(true)}>
              Flujos y caracteristicas
            </Button>
            <Button variant="contained" onClick={() => setModalRegla(true)}>
              Nueva regla
            </Button>
          </Stack>
        )}
      </Stack>

      <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: "wrap", gap: 1 }}>
        <TextField
          select
          label="Proyecto"
          value={idProyecto}
          onChange={(e) => setIdProyecto(Number(e.target.value))}
          size="small"
          sx={{ minWidth: 320 }}
        >
          {opcionesProyecto.map((p) => (
            <MenuItem key={p.id} value={p.id}>{p.etiqueta}</MenuItem>
          ))}
        </TextField>

        <TextField
          label="Buscar"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          size="small"
          sx={{ minWidth: 260 }}
          placeholder="Clave, nombre o texto del enunciado"
        />

        <TextField
          select
          label="Estado"
          value={idEstado}
          onChange={(e) => setIdEstado(e.target.value === "" ? "" : Number(e.target.value))}
          size="small"
          sx={{ minWidth: 240 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {(catalogos.data?.estados ?? []).map((e) => (
            <MenuItem key={e.id} value={e.id}>{e.nombre}</MenuItem>
          ))}
        </TextField>

        <Button
          size="small"
          onClick={() => setIncluirDerogadas((v) => !v)}
        >
          {incluirDerogadas ? "Ocultar derogadas" : "Ver derogadas"}
        </Button>
      </Stack>

      {catalogo.isLoading && <CircularProgress size={24} />}
      {catalogo.isError && <Alert severity="error">{(catalogo.error as Error).message}</Alert>}

      {catalogo.data && (
        <Stack spacing={4}>
          <Box>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Reglas propias ({catalogo.data.totalPropias})
            </Typography>
            {catalogo.data.propias.length === 0 ? (
              <Alert severity="info">
                Este proyecto todavia no tiene reglas propias capturadas.
              </Alert>
            ) : renderTabla(catalogo.data.propias, false)}
          </Box>

          <Box>
            <Typography variant="subtitle1" sx={{ mb: 1 }}>
              Reglas de otros proyectos que afectan a este ({catalogo.data.totalHeredadas})
            </Typography>
            {catalogo.data.heredadas.length === 0 ? (
              <Alert severity="info">
                Ningun otro proyecto declaro que alguna de sus reglas afecte a este.
              </Alert>
            ) : renderTabla(catalogo.data.heredadas, true)}
          </Box>
        </Stack>
      )}

      {idProyecto !== "" && (
        <>
          <ReglaModal
            abierto={modalRegla}
            idProyecto={idProyecto}
            regla={null}
            ambitos={ambitos.data ?? []}
            catalogos={catalogos.data}
            onCerrar={() => setModalRegla(false)}
            onGuardado={setAviso}
          />
          <AmbitosModal
            abierto={modalAmbitos}
            idProyecto={idProyecto}
            ambitos={ambitos.data ?? []}
            catalogos={catalogos.data}
            onCerrar={() => setModalAmbitos(false)}
            onCambio={setAviso}
          />
        </>
      )}

      <Snackbar
        open={aviso !== null}
        autoHideDuration={4000}
        onClose={() => setAviso(null)}
        message={aviso ?? ""}
      />
    </Box>
  );
}
