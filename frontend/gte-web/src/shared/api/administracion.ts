import { enviar, obtener } from "./http";

/* ---------- Catalogos ---------- */

export interface CatalogoItem {
  id: number;
  nombre: string;
}

export interface CatalogosAdministracion {
  categoriasProyecto: CatalogoItem[];
  estatusProyecto: CatalogoItem[];
  niveles: CatalogoItem[];
  areas: CatalogoItem[];
  puestos: CatalogoItem[];
  usuarios: CatalogoItem[];
  equipos: CatalogoItem[];
  roles: CatalogoItem[];
  horarios: CatalogoItem[];
}

export async function obtenerCatalogosAdministracion() {
  return obtener<CatalogosAdministracion>("/api/v1/catalogos/administracion");
}

/* ---------- Proyectos ---------- */

export interface Proyecto {
  idProyecto: number;
  folio: string | null;
  clave: string;
  nombre: string;
  idPrograma: number | null;
  programa: string | null;
  idCategoriaProyecto: number;
  categoriaProyecto: string;
  idEstatus: number;
  estatus: string;
  idResponsable: number | null;
  responsable: string | null;
  idEquipo: number | null;
  equipo: string | null;
  fechaInicioPlan: string | null;
  fechaFinPlan: string | null;
  fechaInicioReal: string | null;
  fechaFinReal: string | null;
  esMantenimiento: boolean;
}

export interface AccionDisponible {
  accion: string;
  etiqueta: string;
  requiereMotivo: boolean;
  esAccionPrincipal: boolean;
}

export async function obtenerProyectos(soloActivos = true) {
  const params = new URLSearchParams({ soloActivos: String(soloActivos) });
  return obtener<Proyecto[]>("/api/v1/proyectos", params);
}

export async function crearProyecto(datos: {
  clave: string;
  nombre: string;
  idPrograma: number | null;
  idCategoriaProyecto: number;
  idResponsable: number | null;
  idEquipo: number | null;
  fechaInicioPlan: string | null;
  fechaFinPlan: string | null;
  esMantenimiento: boolean;
}) {
  return enviar<Proyecto>("post", "/api/v1/proyectos", datos);
}

export async function actualizarProyecto(idProyecto: number, datos: {
  nombre: string;
  idCategoriaProyecto: number;
  idResponsable: number | null;
  idEquipo: number | null;
  fechaInicioPlan: string | null;
  fechaFinPlan: string | null;
  esMantenimiento: boolean;
}) {
  return enviar<Proyecto>("put", `/api/v1/proyectos/${idProyecto}`, datos);
}

export async function obtenerAccionesProyecto(idProyecto: number) {
  return obtener<AccionDisponible[]>(`/api/v1/proyectos/${idProyecto}/acciones`);
}

export async function cambiarEstatusProyecto(idProyecto: number, accion: string) {
  return enviar<Proyecto>("put", `/api/v1/proyectos/${idProyecto}/estatus`, { accion });
}

/* ---------- Equipos ---------- */

export interface Equipo {
  idEquipo: number;
  nombre: string;
  descripcion: string | null;
  idLider: number | null;
  lider: string | null;
  totalMiembros: number;
}

export interface MiembroEquipo {
  idEquipoMiembro: number;
  idUsuario: number;
  usuario: string;
  rolEquipo: string | null;
  porcentajeDedicacion: number;
}

export interface EquipoDetalle {
  idEquipo: number;
  nombre: string;
  descripcion: string | null;
  idLider: number | null;
  lider: string | null;
  miembros: MiembroEquipo[];
}

export async function obtenerEquipos() {
  return obtener<Equipo[]>("/api/v1/equipos");
}

export async function obtenerEquipo(idEquipo: number) {
  return obtener<EquipoDetalle>(`/api/v1/equipos/${idEquipo}`);
}

export async function crearEquipo(datos: { nombre: string; descripcion: string | null; idLider: number | null }) {
  return enviar<EquipoDetalle>("post", "/api/v1/equipos", datos);
}

export async function actualizarEquipo(
  idEquipo: number,
  datos: { nombre: string; descripcion: string | null; idLider: number | null },
) {
  return enviar<EquipoDetalle>("put", `/api/v1/equipos/${idEquipo}`, datos);
}

export async function agregarMiembroEquipo(
  idEquipo: number,
  datos: { idUsuario: number; rolEquipo: string | null; porcentajeDedicacion: number },
) {
  return enviar<EquipoDetalle>("post", `/api/v1/equipos/${idEquipo}/miembros`, datos);
}

export async function actualizarMiembroEquipo(
  idEquipo: number,
  idEquipoMiembro: number,
  datos: { rolEquipo: string | null; porcentajeDedicacion: number },
) {
  return enviar<EquipoDetalle>("put", `/api/v1/equipos/${idEquipo}/miembros/${idEquipoMiembro}`, datos);
}

export async function retirarMiembroEquipo(idEquipo: number, idEquipoMiembro: number) {
  return enviar<EquipoDetalle>("put", `/api/v1/equipos/${idEquipo}/miembros/${idEquipoMiembro}/retirar`, {});
}

/* ---------- Usuarios ---------- */

export interface Usuario {
  idUsuario: number;
  dominio: string;
  nombre: string;
  correo: string | null;
  idPuesto: number | null;
  puesto: string | null;
  idNivel: number | null;
  nivel: string | null;
  idHorario: number | null;
  horario: string | null;
  idJefe: number | null;
  jefe: string | null;
  esExterno: boolean;
  fechaAlta: string | null;
  fechaBaja: string | null;
  activo: boolean;
}

export async function obtenerUsuarios(texto?: string, soloActivos = true) {
  const params = new URLSearchParams({ soloActivos: String(soloActivos) });
  if (texto) params.set("texto", texto);
  return obtener<Usuario[]>("/api/v1/usuarios", params);
}

export async function obtenerUsuario(idUsuario: number) {
  return obtener<Usuario>(`/api/v1/usuarios/${idUsuario}`);
}

export async function crearUsuario(datos: {
  dominio: string;
  nombre: string;
  correo: string | null;
  idPuesto: number | null;
  idNivel: number | null;
  idHorario: number | null;
  idJefe: number | null;
}) {
  return enviar<Usuario>("post", "/api/v1/usuarios", datos);
}

export async function actualizarUsuario(idUsuario: number, datos: {
  nombre: string;
  correo: string | null;
  idPuesto: number | null;
  idNivel: number | null;
  idHorario: number | null;
  idJefe: number | null;
}) {
  return enviar<Usuario>("put", `/api/v1/usuarios/${idUsuario}`, datos);
}

export async function darBajaUsuario(idUsuario: number) {
  return enviar<Usuario>("put", `/api/v1/usuarios/${idUsuario}/baja`, {});
}

/* ---------- Roles ---------- */

export interface Rol {
  idRol: number;
  nombre: string;
  descripcion: string | null;
  esSistema: boolean;
  totalPermisos: number;
}

export interface PermisoMatrizItem {
  idPermiso: number;
  clave: string;
  modulo: string;
  descripcion: string | null;
  asignado: boolean;
}

export interface MatrizPermisos {
  idRol: number;
  rol: string;
  permisos: PermisoMatrizItem[];
}

export interface RolUsuario {
  idUsuarioRol: number;
  idRol: number;
  rol: string;
  idProyecto: number | null;
  proyecto: string | null;
  idEquipo: number | null;
  equipo: string | null;
}

export async function obtenerRoles() {
  return obtener<Rol[]>("/api/v1/roles");
}

export async function obtenerMatrizPermisos(idRol: number) {
  return obtener<MatrizPermisos>(`/api/v1/roles/${idRol}/permisos`);
}

export async function guardarMatrizPermisos(idRol: number, idsPermiso: number[]) {
  return enviar<MatrizPermisos>("put", `/api/v1/roles/${idRol}/permisos`, { idsPermiso });
}

export async function obtenerRolesUsuario(idUsuario: number) {
  return obtener<RolUsuario[]>(`/api/v1/usuarios/${idUsuario}/roles`);
}

export async function asignarRol(idUsuario: number, datos: { idRol: number; idProyecto: number | null }) {
  return enviar<RolUsuario[]>("post", `/api/v1/usuarios/${idUsuario}/roles`, datos);
}

export async function retirarRol(idUsuario: number, idUsuarioRol: number) {
  return enviar<RolUsuario[]>("put", `/api/v1/usuarios/${idUsuario}/roles/${idUsuarioRol}/retirar`, {});
}

/* ---------- Horarios ---------- */

export interface Horario {
  idHorario: number;
  nombre: string;
}

export interface TramoHorario {
  idHorarioTramo: number;
  diaSemana: number;
  horaInicio: string;
  horaFin: string;
}

export interface DiaFestivo {
  idDiaFestivo: number;
  fecha: string;
  descripcion: string;
  idHorario: number | null;
  horario: string | null;
}

export interface HorarioDetalle {
  idHorario: number;
  nombre: string;
  tramos: TramoHorario[];
  festivos: DiaFestivo[];
}

export async function obtenerHorarios() {
  return obtener<Horario[]>("/api/v1/horarios");
}

export async function obtenerHorario(idHorario: number) {
  return obtener<HorarioDetalle>(`/api/v1/horarios/${idHorario}`);
}

export async function crearHorario(datos: { nombre: string }) {
  return enviar<Horario>("post", "/api/v1/horarios", datos);
}

export async function guardarTramosHorario(
  idHorario: number,
  tramos: { diaSemana: number; horaInicio: string; horaFin: string }[],
) {
  return enviar<HorarioDetalle>("put", `/api/v1/horarios/${idHorario}/tramos`, { tramos });
}

export async function obtenerFestivos(idHorario?: number) {
  const params = new URLSearchParams();
  if (idHorario) params.set("idHorario", String(idHorario));
  return obtener<DiaFestivo[]>("/api/v1/festivos", params);
}

export async function crearFestivo(datos: { fecha: string; descripcion: string; idHorario: number | null }) {
  return enviar<DiaFestivo>("post", "/api/v1/festivos", datos);
}

export async function retirarFestivo(idDiaFestivo: number) {
  return enviar<object>("put", `/api/v1/festivos/${idDiaFestivo}/retirar`, {});
}

/* ---------- Ambientes ---------- */

export interface Ambiente {
  idAmbiente: number;
  idProyecto: number | null;
  proyecto: string | null;
  nombre: string;
  url: string | null;
  servidor: string | null;
  baseDatos: string | null;
  idResponsable: number | null;
  responsable: string | null;
}

export async function obtenerAmbientes(idProyecto?: number) {
  const params = new URLSearchParams();
  if (idProyecto) params.set("idProyecto", String(idProyecto));
  return obtener<Ambiente[]>("/api/v1/ambientes", params);
}

export async function crearAmbiente(datos: {
  idProyecto: number | null;
  nombre: string;
  url: string | null;
  servidor: string | null;
  baseDatos: string | null;
  idResponsable: number | null;
}) {
  return enviar<Ambiente>("post", "/api/v1/ambientes", datos);
}

export async function actualizarAmbiente(idAmbiente: number, datos: {
  nombre: string;
  url: string | null;
  servidor: string | null;
  baseDatos: string | null;
  idResponsable: number | null;
}) {
  return enviar<Ambiente>("put", `/api/v1/ambientes/${idAmbiente}`, datos);
}

export async function retirarAmbiente(idAmbiente: number) {
  return enviar<object>("put", `/api/v1/ambientes/${idAmbiente}/retirar`, {});
}
