import { useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, FormControl,
  Grid, InputLabel, LinearProgress, MenuItem, Paper, Select, Snackbar, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { obtenerCatalogosAdministracion, obtenerProyectos } from "../../shared/api/administracion";
import {
  actualizarPresupuesto, actualizarTarifa, crearPresupuesto, crearTarifa, obtenerCostoProyecto,
  obtenerPresupuestos, obtenerTarifas, retirarPresupuesto, retirarTarifa,
  type PresupuestoProyecto, type TarifaNivel,
} from "../../shared/api/costeo";
import { useSesion } from "../../shared/api/sesion";

function formatearMoneda(valor: number): string {
  return valor.toLocaleString("es-MX", { style: "currency", currency: "MXN" });
}

const ANIO_ACTUAL = new Date().getFullYear();

/** Costeo real por proyecto: tarifas por nivel, presupuesto y reporte vs costo real. */
export function CosteoTab() {
  const { puede } = useSesion();
  const puedeGestionar = puede("POR.GestionarCosteo");
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const [idProyecto, setIdProyecto] = useState<number | "">("");
  const [anio, setAnio] = useState(ANIO_ACTUAL);
  const clienteQuery = useQueryClient();

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });
  const refrescar = (clave: string) => clienteQuery.invalidateQueries({ queryKey: [clave] });

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const proyectos = useQuery({ queryKey: ["proyectos"], queryFn: () => obtenerProyectos() });
  const tarifas = useQuery({ queryKey: ["tarifas-nivel"], queryFn: obtenerTarifas });

  const presupuestos = useQuery({
    queryKey: ["presupuestos-proyecto", idProyecto],
    queryFn: () => obtenerPresupuestos(idProyecto as number),
    enabled: idProyecto !== "",
  });
  const costo = useQuery({
    queryKey: ["costo-proyecto", idProyecto, anio],
    queryFn: () => obtenerCostoProyecto(idProyecto as number, anio),
    enabled: idProyecto !== "",
  });

  return (
    <Box>
      <SeccionTarifas
        tarifas={tarifas.data ?? []}
        niveles={catalogos.data?.niveles ?? []}
        puedeGestionar={puedeGestionar}
        alExito={(m) => { avisar(m); void refrescar("tarifas-nivel"); }}
        alError={(m) => avisar(m, true)}
      />

      <Typography variant="subtitle1" sx={{ fontWeight: 700, mt: 4, mb: 1 }}>Presupuesto y costo real</Typography>
      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <FormControl size="small" sx={{ minWidth: 250 }}>
          <InputLabel>Proyecto</InputLabel>
          <Select label="Proyecto" value={idProyecto} onChange={(e) => setIdProyecto(e.target.value as number)}>
            {proyectos.data?.map((p) => (
              <MenuItem key={p.idProyecto} value={p.idProyecto}>{p.clave} - {p.nombre}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField size="small" type="number" label="Anio" value={anio}
          onChange={(e) => setAnio(Number(e.target.value))} sx={{ width: 120 }} />
      </Stack>

      {idProyecto === "" ? (
        <Typography color="text.secondary">Selecciona un proyecto para ver su presupuesto y costo real.</Typography>
      ) : (
        <>
          <SeccionPresupuesto
            idProyecto={idProyecto}
            anio={anio}
            presupuestos={presupuestos.data ?? []}
            puedeGestionar={puedeGestionar}
            alExito={(m) => { avisar(m); void refrescar("presupuestos-proyecto"); void refrescar("costo-proyecto"); }}
            alError={(m) => avisar(m, true)}
          />

          <Typography variant="subtitle2" sx={{ fontWeight: 700, mt: 3, mb: 1 }}>Reporte de costo real</Typography>
          {costo.isLoading && <LinearProgress />}
          {costo.data && (
            <>
              <Grid container spacing={2} sx={{ mb: 2 }}>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Presupuesto</Typography>
                    <Typography variant="h6">{formatearMoneda(costo.data.montoAutorizado)}</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Costo real</Typography>
                    <Typography variant="h6"
                      sx={{ color: costo.data.costoReal > costo.data.montoAutorizado ? "error.main" : undefined }}>
                      {formatearMoneda(costo.data.costoReal)}
                    </Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Horas autorizadas</Typography>
                    <Typography variant="h6">{costo.data.horasAutorizadas}</Typography>
                  </Paper>
                </Grid>
                <Grid size={{ xs: 6, md: 3 }}>
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">Horas reales</Typography>
                    <Typography variant="h6"
                      sx={{ color: costo.data.horasReales > costo.data.horasAutorizadas ? "error.main" : undefined }}>
                      {costo.data.horasReales.toFixed(1)}
                    </Typography>
                  </Paper>
                </Grid>
              </Grid>

              <Paper variant="outlined">
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Usuario</TableCell>
                        <TableCell align="right">Horas</TableCell>
                        <TableCell align="right">Costo</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {costo.data.detallePorUsuario.length === 0 && (
                        <TableRow>
                          <TableCell colSpan={3}>
                            <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                              Sin tiempo registrado en este proyecto/anio.
                            </Typography>
                          </TableCell>
                        </TableRow>
                      )}
                      {costo.data.detallePorUsuario.map((d) => (
                        <TableRow key={d.idUsuario}>
                          <TableCell>{d.usuario}</TableCell>
                          <TableCell align="right">{d.horas.toFixed(1)}</TableCell>
                          <TableCell align="right">{formatearMoneda(d.costo)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Paper>
            </>
          )}
        </>
      )}

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}

function SeccionTarifas({ tarifas, niveles, puedeGestionar, alExito, alError }: {
  tarifas: TarifaNivel[]; niveles: { id: number; nombre: string }[]; puedeGestionar: boolean;
  alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [modal, setModal] = useState(false);
  const [editando, setEditando] = useState<TarifaNivel | null>(null);
  const [idNivel, setIdNivel] = useState<number | "">("");
  const [costoHora, setCostoHora] = useState("");
  const [vigenciaDesde, setVigenciaDesde] = useState("");

  const abrirNueva = () => { setEditando(null); setIdNivel(""); setCostoHora(""); setVigenciaDesde(""); setModal(true); };
  const abrirEditar = (t: TarifaNivel) => {
    setEditando(t); setIdNivel(t.idNivel); setCostoHora(String(t.costoHora)); setVigenciaDesde(t.vigenciaDesde); setModal(true);
  };

  const guardar = async () => {
    if (idNivel === "" || !costoHora || !vigenciaDesde) return;
    try {
      if (editando) {
        const { mensaje } = await actualizarTarifa(editando.idTarifaNivel, { costoHora: Number(costoHora), vigenciaDesde });
        alExito(mensaje);
      } else {
        const { mensaje } = await crearTarifa({ idNivel: idNivel as number, costoHora: Number(costoHora), vigenciaDesde });
        alExito(mensaje);
      }
      setModal(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo guardar la tarifa.");
    }
  };

  const retirar = async (idTarifaNivel: number) => {
    try {
      await retirarTarifa(idTarifaNivel);
      alExito("Tarifa retirada.");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo retirar la tarifa.");
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Tarifas por nivel</Typography>
        {puedeGestionar && (
          <Button size="small" variant="contained" startIcon={<AddIcon />} onClick={abrirNueva}>Nueva tarifa</Button>
        )}
      </Stack>
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Nivel</TableCell>
              <TableCell align="right">Costo/hora</TableCell>
              <TableCell>Vigente desde</TableCell>
              {puedeGestionar && <TableCell />}
            </TableRow>
          </TableHead>
          <TableBody>
            {tarifas.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    Sin tarifas registradas.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {tarifas.map((t) => (
              <TableRow key={t.idTarifaNivel}>
                <TableCell>{t.nivel}</TableCell>
                <TableCell align="right">{formatearMoneda(t.costoHora)}</TableCell>
                <TableCell>{t.vigenciaDesde}</TableCell>
                {puedeGestionar && (
                  <TableCell align="right">
                    <Button size="small" onClick={() => abrirEditar(t)}>Editar</Button>
                    <Button size="small" color="error" onClick={() => void retirar(t.idTarifaNivel)}>Retirar</Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      {puedeGestionar && (
        <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="xs">
          <DialogTitle>{editando ? "Editar tarifa" : "Nueva tarifa"}</DialogTitle>
          <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
            <FormControl size="small" required disabled={!!editando}>
              <InputLabel>Nivel</InputLabel>
              <Select label="Nivel" value={idNivel} onChange={(e) => setIdNivel(e.target.value as number)}>
                {niveles.map((n) => <MenuItem key={n.id} value={n.id}>{n.nombre}</MenuItem>)}
              </Select>
            </FormControl>
            <TextField size="small" type="number" required label="Costo por hora" value={costoHora}
              onChange={(e) => setCostoHora(e.target.value)} slotProps={{ htmlInput: { min: 0, step: 0.01 } }} />
            <TextField size="small" type="date" required label="Vigente desde" value={vigenciaDesde}
              onChange={(e) => setVigenciaDesde(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setModal(false)}>Cancelar</Button>
            <Button variant="contained" disabled={idNivel === "" || !costoHora || !vigenciaDesde} onClick={() => void guardar()}>
              Guardar
            </Button>
          </DialogActions>
        </Dialog>
      )}
    </Box>
  );
}

function SeccionPresupuesto({ idProyecto, anio, presupuestos, puedeGestionar, alExito, alError }: {
  idProyecto: number; anio: number; presupuestos: PresupuestoProyecto[]; puedeGestionar: boolean;
  alExito: (m: string) => void; alError: (m: string) => void;
}) {
  const [modal, setModal] = useState(false);
  const [editando, setEditando] = useState<PresupuestoProyecto | null>(null);
  const [anioForm, setAnioForm] = useState(anio);
  const [monto, setMonto] = useState("");
  const [horas, setHoras] = useState("");

  const abrirNuevo = () => { setEditando(null); setAnioForm(anio); setMonto(""); setHoras(""); setModal(true); };
  const abrirEditar = (p: PresupuestoProyecto) => {
    setEditando(p); setAnioForm(p.anio); setMonto(String(p.montoAutorizado)); setHoras(String(p.horasAutorizadas)); setModal(true);
  };

  const guardar = async () => {
    if (!monto || !horas) return;
    try {
      if (editando) {
        const { mensaje } = await actualizarPresupuesto(editando.idPresupuestoProyecto, {
          montoAutorizado: Number(monto), horasAutorizadas: Number(horas),
        });
        alExito(mensaje);
      } else {
        const { mensaje } = await crearPresupuesto({
          idProyecto, anio: anioForm, montoAutorizado: Number(monto), horasAutorizadas: Number(horas),
        });
        alExito(mensaje);
      }
      setModal(false);
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo guardar el presupuesto.");
    }
  };

  const retirar = async (id: number) => {
    try {
      await retirarPresupuesto(id);
      alExito("Presupuesto retirado.");
    } catch (error) {
      alError(error instanceof ErrorApi ? error.message : "No se pudo retirar el presupuesto.");
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Presupuesto por anio</Typography>
        {puedeGestionar && (
          <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={abrirNuevo}>Nuevo presupuesto</Button>
        )}
      </Stack>
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Anio</TableCell>
              <TableCell align="right">Monto autorizado</TableCell>
              <TableCell align="right">Horas autorizadas</TableCell>
              {puedeGestionar && <TableCell />}
            </TableRow>
          </TableHead>
          <TableBody>
            {presupuestos.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    Sin presupuesto registrado para este proyecto.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {presupuestos.map((p) => (
              <TableRow key={p.idPresupuestoProyecto} selected={p.anio === anio}>
                <TableCell>{p.anio} {p.anio === anio && <Chip size="small" label="seleccionado" sx={{ ml: 1 }} />}</TableCell>
                <TableCell align="right">{formatearMoneda(p.montoAutorizado)}</TableCell>
                <TableCell align="right">{p.horasAutorizadas}</TableCell>
                {puedeGestionar && (
                  <TableCell align="right">
                    <Button size="small" onClick={() => abrirEditar(p)}>Editar</Button>
                    <Button size="small" color="error" onClick={() => void retirar(p.idPresupuestoProyecto)}>Retirar</Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      {puedeGestionar && (
        <Dialog open={modal} onClose={() => setModal(false)} fullWidth maxWidth="xs">
          <DialogTitle>{editando ? "Editar presupuesto" : "Nuevo presupuesto"}</DialogTitle>
          <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
            <TextField size="small" type="number" required label="Anio" value={anioForm}
              disabled={!!editando} onChange={(e) => setAnioForm(Number(e.target.value))} />
            <TextField size="small" type="number" required label="Monto autorizado" value={monto}
              onChange={(e) => setMonto(e.target.value)} slotProps={{ htmlInput: { min: 0, step: 0.01 } }} />
            <TextField size="small" type="number" required label="Horas autorizadas" value={horas}
              onChange={(e) => setHoras(e.target.value)} slotProps={{ htmlInput: { min: 0, step: 0.5 } }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setModal(false)}>Cancelar</Button>
            <Button variant="contained" disabled={!monto || !horas} onClick={() => void guardar()}>Guardar</Button>
          </DialogActions>
        </Dialog>
      )}
    </Box>
  );
}
