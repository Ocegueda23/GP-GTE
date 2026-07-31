import { useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, FormControl,
  IconButton, InputLabel, LinearProgress, MenuItem, Paper, Select, Snackbar, Stack, Table,
  TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import {
  actualizarUsuario, asignarRol, crearUsuario, darBajaUsuario, obtenerCatalogosAdministracion,
  obtenerRolesUsuario, obtenerUsuarios, retirarRol, type Usuario,
} from "../../shared/api/administracion";

/** P20 - Usuarios: alta manual, baja logica, nivel, horario, jefe y asignacion de roles. */
export function UsuariosTab() {
  const [texto, setTexto] = useState("");
  const [modalNuevo, setModalNuevo] = useState(false);
  const [usuarioEditar, setUsuarioEditar] = useState<Usuario | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const [dominio, setDominio] = useState("");
  const [nombre, setNombre] = useState("");
  const [correo, setCorreo] = useState("");
  const [idPuesto, setIdPuesto] = useState<number | "">("");
  const [idNivel, setIdNivel] = useState<number | "">("");
  const [idHorario, setIdHorario] = useState<number | "">("");
  const [idJefe, setIdJefe] = useState<number | "">("");

  const [nombreEditar, setNombreEditar] = useState("");
  const [correoEditar, setCorreoEditar] = useState("");
  const [idPuestoEditar, setIdPuestoEditar] = useState<number | "">("");
  const [idNivelEditar, setIdNivelEditar] = useState<number | "">("");
  const [idHorarioEditar, setIdHorarioEditar] = useState<number | "">("");
  const [idJefeEditar, setIdJefeEditar] = useState<number | "">("");
  const [idRolNuevo, setIdRolNuevo] = useState<number | "">("");

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const usuarios = useQuery({
    queryKey: ["usuarios-admin", texto], queryFn: () => obtenerUsuarios(texto || undefined),
  });
  const rolesUsuario = useQuery({
    queryKey: ["roles-usuario", usuarioEditar?.idUsuario],
    queryFn: () => obtenerRolesUsuario(usuarioEditar!.idUsuario),
    enabled: usuarioEditar !== null,
  });

  const avisar = (mensaje: string, error = false) => setAviso({ tipo: error ? "error" : "success", mensaje });
  const refrescar = () => Promise.all([
    clienteQuery.invalidateQueries({ queryKey: ["usuarios-admin"] }),
    clienteQuery.invalidateQueries({ queryKey: ["roles-usuario"] }),
  ]);

  const limpiarAlta = () => {
    setDominio(""); setNombre(""); setCorreo("");
    setIdPuesto(""); setIdNivel(""); setIdHorario(""); setIdJefe("");
  };

  const crear = async () => {
    try {
      const { mensaje } = await crearUsuario({
        dominio: dominio.trim(), nombre: nombre.trim(), correo: correo.trim() || null,
        idPuesto: idPuesto === "" ? null : (idPuesto as number),
        idNivel: idNivel === "" ? null : (idNivel as number),
        idHorario: idHorario === "" ? null : (idHorario as number),
        idJefe: idJefe === "" ? null : (idJefe as number),
      });
      avisar(mensaje);
      setModalNuevo(false);
      limpiarAlta();
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el usuario.", true);
    }
  };

  const abrirEditar = (usuario: Usuario) => {
    setUsuarioEditar(usuario);
    setNombreEditar(usuario.nombre);
    setCorreoEditar(usuario.correo ?? "");
    setIdPuestoEditar(usuario.idPuesto ?? "");
    setIdNivelEditar(usuario.idNivel ?? "");
    setIdHorarioEditar(usuario.idHorario ?? "");
    setIdJefeEditar(usuario.idJefe ?? "");
  };

  const guardarEdicion = async () => {
    try {
      const { mensaje } = await actualizarUsuario(usuarioEditar!.idUsuario, {
        nombre: nombreEditar.trim(), correo: correoEditar.trim() || null,
        idPuesto: idPuestoEditar === "" ? null : (idPuestoEditar as number),
        idNivel: idNivelEditar === "" ? null : (idNivelEditar as number),
        idHorario: idHorarioEditar === "" ? null : (idHorarioEditar as number),
        idJefe: idJefeEditar === "" ? null : (idJefeEditar as number),
      });
      avisar(mensaje);
      setUsuarioEditar(null);
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo actualizar el usuario.", true);
    }
  };

  const darBaja = async () => {
    try {
      const { mensaje } = await darBajaUsuario(usuarioEditar!.idUsuario);
      avisar(mensaje);
      setUsuarioEditar(null);
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo dar de baja al usuario.", true);
    }
  };

  const asignar = async () => {
    try {
      await asignarRol(usuarioEditar!.idUsuario, { idRol: idRolNuevo as number, idProyecto: null });
      avisar("Rol asignado.");
      setIdRolNuevo("");
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo asignar el rol.", true);
    }
  };

  const retirar = async (idUsuarioRol: number) => {
    try {
      await retirarRol(usuarioEditar!.idUsuario, idUsuarioRol);
      avisar("Rol retirado.");
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo retirar el rol.", true);
    }
  };

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2, flexWrap: "wrap", gap: 1 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Usuarios</Typography>
        <Stack direction="row" spacing={1}>
          <TextField size="small" placeholder="Buscar..." value={texto} onChange={(e) => setTexto(e.target.value)} />
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setModalNuevo(true)}>
            Nuevo usuario
          </Button>
        </Stack>
      </Stack>

      <Paper variant="outlined">
        {usuarios.isLoading && <LinearProgress />}
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Dominio</TableCell>
              <TableCell>Nombre</TableCell>
              <TableCell>Puesto</TableCell>
              <TableCell>Nivel</TableCell>
              <TableCell>Jefe</TableCell>
              <TableCell>Horario</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {usuarios.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay usuarios con este filtro.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {usuarios.data?.map((u) => (
              <TableRow key={u.idUsuario} hover sx={{ cursor: "pointer" }} onClick={() => abrirEditar(u)}>
                <TableCell>{u.dominio}</TableCell>
                <TableCell>{u.nombre}</TableCell>
                <TableCell>{u.puesto ?? "-"}</TableCell>
                <TableCell>{u.nivel ?? "-"}</TableCell>
                <TableCell>{u.jefe ?? "-"}</TableCell>
                <TableCell>{u.horario ?? "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={modalNuevo} onClose={() => setModalNuevo(false)} fullWidth maxWidth="sm">
        <DialogTitle>Nuevo usuario</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Cuenta de dominio" value={dominio}
            onChange={(e) => setDominio(e.target.value)} />
          <TextField size="small" required label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <TextField size="small" label="Correo" value={correo} onChange={(e) => setCorreo(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Puesto</InputLabel>
            <Select label="Puesto" value={idPuesto} onChange={(e) => setIdPuesto(e.target.value as number)}>
              <MenuItem value="">Sin puesto</MenuItem>
              {catalogos.data?.puestos.map((p) => <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Nivel</InputLabel>
            <Select label="Nivel" value={idNivel} onChange={(e) => setIdNivel(e.target.value as number)}>
              <MenuItem value="">Sin nivel</MenuItem>
              {catalogos.data?.niveles.map((n) => <MenuItem key={n.id} value={n.id}>{n.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Horario</InputLabel>
            <Select label="Horario" value={idHorario} onChange={(e) => setIdHorario(e.target.value as number)}>
              <MenuItem value="">Sin horario</MenuItem>
              {catalogos.data?.horarios.map((h) => <MenuItem key={h.id} value={h.id}>{h.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Jefe</InputLabel>
            <Select label="Jefe" value={idJefe} onChange={(e) => setIdJefe(e.target.value as number)}>
              <MenuItem value="">Sin jefe</MenuItem>
              {catalogos.data?.usuarios.map((u) => <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>)}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalNuevo(false)}>Cancelar</Button>
          <Button variant="contained" disabled={dominio.trim().length === 0 || nombre.trim().length === 0}
            onClick={() => void crear()}>
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={usuarioEditar !== null} onClose={() => setUsuarioEditar(null)} fullWidth maxWidth="sm">
        <DialogTitle>{usuarioEditar?.nombre}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "12px !important" }}>
          <TextField size="small" required label="Nombre" value={nombreEditar}
            onChange={(e) => setNombreEditar(e.target.value)} />
          <TextField size="small" label="Correo" value={correoEditar} onChange={(e) => setCorreoEditar(e.target.value)} />
          <FormControl size="small">
            <InputLabel>Puesto</InputLabel>
            <Select label="Puesto" value={idPuestoEditar} onChange={(e) => setIdPuestoEditar(e.target.value as number)}>
              <MenuItem value="">Sin puesto</MenuItem>
              {catalogos.data?.puestos.map((p) => <MenuItem key={p.id} value={p.id}>{p.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Nivel</InputLabel>
            <Select label="Nivel" value={idNivelEditar} onChange={(e) => setIdNivelEditar(e.target.value as number)}>
              <MenuItem value="">Sin nivel</MenuItem>
              {catalogos.data?.niveles.map((n) => <MenuItem key={n.id} value={n.id}>{n.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Horario</InputLabel>
            <Select label="Horario" value={idHorarioEditar} onChange={(e) => setIdHorarioEditar(e.target.value as number)}>
              <MenuItem value="">Sin horario</MenuItem>
              {catalogos.data?.horarios.map((h) => <MenuItem key={h.id} value={h.id}>{h.nombre}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl size="small">
            <InputLabel>Jefe</InputLabel>
            <Select label="Jefe" value={idJefeEditar} onChange={(e) => setIdJefeEditar(e.target.value as number)}>
              <MenuItem value="">Sin jefe</MenuItem>
              {catalogos.data?.usuarios.filter((u) => u.id !== usuarioEditar?.idUsuario).map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.nombre}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <Divider />
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Roles asignados</Typography>
          {rolesUsuario.data?.length === 0 && (
            <Typography variant="body2" color="text.secondary">Sin roles asignados.</Typography>
          )}
          {rolesUsuario.data?.map((r) => (
            <Stack key={r.idUsuarioRol} direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Typography variant="body2" sx={{ flex: 1 }}>
                {r.rol}{r.proyecto ? ` (${r.proyecto})` : " (global)"}
              </Typography>
              <IconButton size="small" aria-label="Retirar rol" onClick={() => void retirar(r.idUsuarioRol)}>
                <DeleteOutlineIcon fontSize="small" />
              </IconButton>
            </Stack>
          ))}
          <Stack direction="row" spacing={1}>
            <FormControl size="small" sx={{ flex: 1 }}>
              <InputLabel>Agregar rol</InputLabel>
              <Select label="Agregar rol" value={idRolNuevo} onChange={(e) => setIdRolNuevo(e.target.value as number)}>
                {catalogos.data?.roles.map((r) => <MenuItem key={r.id} value={r.id}>{r.nombre}</MenuItem>)}
              </Select>
            </FormControl>
            <Button variant="outlined" disabled={idRolNuevo === ""} onClick={() => void asignar()}>Asignar</Button>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ justifyContent: "space-between", px: 3 }}>
          <Button color="error" onClick={() => void darBaja()}>Dar de baja</Button>
          <Stack direction="row" spacing={1}>
            <Button onClick={() => setUsuarioEditar(null)}>Cancelar</Button>
            <Button variant="contained" onClick={() => void guardarEdicion()}>Guardar</Button>
          </Stack>
        </DialogActions>
      </Dialog>

      <Snackbar open={aviso !== null} autoHideDuration={6000} onClose={() => setAviso(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        <Alert severity={aviso?.tipo ?? "success"} variant="filled" onClose={() => setAviso(null)}>
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </Box>
  );
}
