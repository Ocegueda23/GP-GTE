import { useRef, useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider,
  IconButton, LinearProgress, Paper, Snackbar, Stack, Table,
  TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import PhotoCameraIcon from "@mui/icons-material/PhotoCamera";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ErrorApi } from "../../shared/api/http";
import { AvatarUsuario } from "../../shared/components/AvatarUsuario";
import { ComboBuscable } from "../../shared/components/ComboBuscable";
import { EncabezadoOrdenable } from "../../shared/components/EncabezadoOrdenable";
import { useOrdenTabla } from "../../shared/hooks/useOrdenTabla";
import {
  actualizarUsuario, asignarRol, crearUsuario, darBajaUsuario, obtenerAreas,
  obtenerCatalogosAdministracion, obtenerPuestos, obtenerRolesUsuario, obtenerUsuarios,
  restablecerPasswordUsuario, retirarRol, subirFotoUsuario, type Usuario,
} from "../../shared/api/administracion";

/** P20 - Usuarios: alta manual, baja logica, nivel, horario, jefe y asignacion de roles. */
export function UsuariosTab() {
  const [texto, setTexto] = useState("");
  const [modalNuevo, setModalNuevo] = useState(false);
  const [usuarioEditar, setUsuarioEditar] = useState<Usuario | null>(null);
  const [passwordAMostrar, setPasswordAMostrar] = useState<{ nombre: string; password: string } | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "success" | "error"; mensaje: string } | null>(null);
  const clienteQuery = useQueryClient();

  const [dominio, setDominio] = useState("");
  const [nombre, setNombre] = useState("");
  const [correo, setCorreo] = useState("");
  const [idArea, setIdArea] = useState<number | "">("");
  const [idPuesto, setIdPuesto] = useState<number | "">("");
  const [idNivel, setIdNivel] = useState<number | "">("");
  const [idHorario, setIdHorario] = useState<number | "">("");
  const [idJefe, setIdJefe] = useState<number | "">("");

  const [nombreEditar, setNombreEditar] = useState("");
  const [correoEditar, setCorreoEditar] = useState("");
  const [idAreaEditar, setIdAreaEditar] = useState<number | "">("");
  const [idPuestoEditar, setIdPuestoEditar] = useState<number | "">("");
  const [idNivelEditar, setIdNivelEditar] = useState<number | "">("");
  const [idHorarioEditar, setIdHorarioEditar] = useState<number | "">("");
  const [idJefeEditar, setIdJefeEditar] = useState<number | "">("");
  const [idRolNuevo, setIdRolNuevo] = useState<number | "">("");
  const inputFoto = useRef<HTMLInputElement>(null);
  const [subiendoFoto, setSubiendoFoto] = useState(false);

  const catalogos = useQuery({
    queryKey: ["catalogos-admin"], queryFn: obtenerCatalogosAdministracion, staleTime: 5 * 60_000,
  });
  const areas = useQuery({ queryKey: ["areas-admin"], queryFn: obtenerAreas, staleTime: 5 * 60_000 });
  const puestos = useQuery({ queryKey: ["puestos-admin"], queryFn: obtenerPuestos, staleTime: 5 * 60_000 });
  const usuarios = useQuery({
    queryKey: ["usuarios-admin", texto], queryFn: () => obtenerUsuarios(texto || undefined),
  });
  const { datosOrdenados: usuariosOrdenados, ordenarPor, descendente, ordenar } = useOrdenTabla(usuarios.data);
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
    setIdArea(""); setIdPuesto(""); setIdNivel(""); setIdHorario(""); setIdJefe("");
  };

  const crear = async () => {
    try {
      const { dato, mensaje } = await crearUsuario({
        dominio: dominio.trim(), nombre: nombre.trim(), correo: correo.trim() || null,
        idPuesto: idPuesto === "" ? null : (idPuesto as number),
        idNivel: idNivel === "" ? null : (idNivel as number),
        idHorario: idHorario === "" ? null : (idHorario as number),
        idJefe: idJefe === "" ? null : (idJefe as number),
      });
      avisar(mensaje);
      setModalNuevo(false);
      limpiarAlta();
      setPasswordAMostrar({ nombre: dato.nombre, password: dato.passwordTemporal });
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo crear el usuario.", true);
    }
  };

  const restablecerPassword = async () => {
    if (!usuarioEditar) return;
    try {
      const { dato } = await restablecerPasswordUsuario(usuarioEditar.idUsuario);
      setPasswordAMostrar({ nombre: usuarioEditar.nombre, password: dato.passwordTemporal });
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo restablecer la contraseña.", true);
    }
  };

  const abrirEditar = (usuario: Usuario) => {
    setUsuarioEditar(usuario);
    setNombreEditar(usuario.nombre);
    setCorreoEditar(usuario.correo ?? "");
    const puestoActual = (puestos.data ?? []).find((p) => p.idPuesto === usuario.idPuesto);
    setIdAreaEditar(puestoActual?.idArea ?? "");
    setIdPuestoEditar(usuario.idPuesto ?? "");
    setIdNivelEditar(usuario.idNivel ?? "");
    setIdHorarioEditar(usuario.idHorario ?? "");
    setIdJefeEditar(usuario.idJefe ?? "");
  };

  const subirFoto = async (archivo: File) => {
    if (!usuarioEditar) return;
    setSubiendoFoto(true);
    try {
      const { dato, mensaje } = await subirFotoUsuario(usuarioEditar.idUsuario, archivo);
      avisar(mensaje);
      setUsuarioEditar({ ...usuarioEditar, urlFoto: `/api/v1/archivos/${dato.guidArchivo}` });
      await refrescar();
    } catch (error) {
      avisar(error instanceof ErrorApi ? error.message : "No se pudo subir la foto.", true);
    } finally {
      setSubiendoFoto(false);
      if (inputFoto.current) inputFoto.current.value = "";
    }
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
              <EncabezadoOrdenable clave="dominio" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Dominio</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="nombre" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nombre</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="puesto" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Puesto</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="nivel" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Nivel</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="jefe" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Jefe</EncabezadoOrdenable>
              <EncabezadoOrdenable clave="horario" ordenActual={ordenarPor} descendente={descendente} onOrdenar={ordenar}>Horario</EncabezadoOrdenable>
            </TableRow>
          </TableHead>
          <TableBody>
            {usuariosOrdenados.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
                    No hay usuarios con este filtro.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {usuariosOrdenados.map((u) => (
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
          <ComboBuscable
            label="Area"
            value={idArea}
            onChange={(v) => { setIdArea(v as number | ""); setIdPuesto(""); }}
            opciones={[
              { valor: "", etiqueta: "Todas" },
              ...(areas.data ?? []).map((a) => ({ valor: a.idArea, etiqueta: a.nombre })),
            ]}
          />
          <ComboBuscable
            label="Puesto"
            value={idPuesto}
            onChange={(v) => setIdPuesto(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin puesto" },
              ...(puestos.data ?? [])
                .filter((p) => idArea === "" || p.idArea === idArea)
                .map((p) => ({ valor: p.idPuesto, etiqueta: p.nombre })),
            ]}
          />
          <ComboBuscable
            label="Nivel"
            value={idNivel}
            onChange={(v) => setIdNivel(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin nivel" },
              ...(catalogos.data?.niveles ?? []).map((n) => ({ valor: n.id, etiqueta: n.nombre })),
            ]}
          />
          <ComboBuscable
            label="Horario"
            value={idHorario}
            onChange={(v) => setIdHorario(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin horario" },
              ...(catalogos.data?.horarios ?? []).map((h) => ({ valor: h.id, etiqueta: h.nombre })),
            ]}
          />
          <ComboBuscable
            label="Jefe"
            value={idJefe}
            onChange={(v) => setIdJefe(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin jefe" },
              ...(catalogos.data?.usuarios ?? []).map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />
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

          <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
            {usuarioEditar && <AvatarUsuario urlFoto={usuarioEditar.urlFoto} nombre={usuarioEditar.nombre} sx={{ width: 56, height: 56 }} />}
            <Button
              size="small"
              variant="outlined"
              startIcon={<PhotoCameraIcon />}
              disabled={subiendoFoto}
              onClick={() => inputFoto.current?.click()}
            >
              {subiendoFoto ? "Subiendo..." : "Cambiar foto"}
            </Button>
            <input
              ref={inputFoto}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              style={{ display: "none" }}
              onChange={(e) => { const archivo = e.target.files?.[0]; if (archivo) void subirFoto(archivo); }}
            />
          </Stack>

          <ComboBuscable
            label="Area"
            value={idAreaEditar}
            onChange={(v) => { setIdAreaEditar(v as number | ""); setIdPuestoEditar(""); }}
            opciones={[
              { valor: "", etiqueta: "Todas" },
              ...(areas.data ?? []).map((a) => ({ valor: a.idArea, etiqueta: a.nombre })),
            ]}
          />
          <ComboBuscable
            label="Puesto"
            value={idPuestoEditar}
            onChange={(v) => setIdPuestoEditar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin puesto" },
              ...(puestos.data ?? [])
                .filter((p) => idAreaEditar === "" || p.idArea === idAreaEditar)
                .map((p) => ({ valor: p.idPuesto, etiqueta: p.nombre })),
            ]}
          />
          <ComboBuscable
            label="Nivel"
            value={idNivelEditar}
            onChange={(v) => setIdNivelEditar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin nivel" },
              ...(catalogos.data?.niveles ?? []).map((n) => ({ valor: n.id, etiqueta: n.nombre })),
            ]}
          />
          <ComboBuscable
            label="Horario"
            value={idHorarioEditar}
            onChange={(v) => setIdHorarioEditar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin horario" },
              ...(catalogos.data?.horarios ?? []).map((h) => ({ valor: h.id, etiqueta: h.nombre })),
            ]}
          />
          <ComboBuscable
            label="Jefe"
            value={idJefeEditar}
            onChange={(v) => setIdJefeEditar(v as number | "")}
            opciones={[
              { valor: "", etiqueta: "Sin jefe" },
              ...(catalogos.data?.usuarios ?? [])
                .filter((u) => u.id !== usuarioEditar?.idUsuario)
                .map((u) => ({ valor: u.id, etiqueta: u.nombre })),
            ]}
          />

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
            <ComboBuscable
              label="Agregar rol"
              value={idRolNuevo}
              onChange={(v) => setIdRolNuevo(v as number | "")}
              opciones={(catalogos.data?.roles ?? []).map((r) => ({ valor: r.id, etiqueta: r.nombre }))}
              sx={{ flex: 1 }}
            />
            <Button variant="outlined" disabled={idRolNuevo === ""} onClick={() => void asignar()}>Asignar</Button>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ justifyContent: "space-between", px: 3 }}>
          <Stack direction="row" spacing={1}>
            <Button color="error" onClick={() => void darBaja()}>Dar de baja</Button>
            <Button onClick={() => void restablecerPassword()}>Restablecer contraseña</Button>
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button onClick={() => setUsuarioEditar(null)}>Cancelar</Button>
            <Button variant="contained" onClick={() => void guardarEdicion()}>Guardar</Button>
          </Stack>
        </DialogActions>
      </Dialog>

      <Dialog open={passwordAMostrar !== null} onClose={() => setPasswordAMostrar(null)} fullWidth maxWidth="xs">
        <DialogTitle>Contraseña temporal</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ mb: 2 }}>
            Compartela con <strong>{passwordAMostrar?.nombre}</strong>. No se va a volver a mostrar.
          </Typography>
          <TextField size="small" fullWidth value={passwordAMostrar?.password ?? ""}
            slotProps={{ input: { readOnly: true } }}
            onFocus={(e) => e.target.select()} />
        </DialogContent>
        <DialogActions>
          <Button variant="contained" onClick={() => setPasswordAMostrar(null)}>Listo</Button>
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
