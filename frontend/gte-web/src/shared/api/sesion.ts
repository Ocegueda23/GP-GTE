import { create } from "zustand";
import { enviar, obtener } from "./http";

export interface Sesion {
  idUsuario: number;
  dominio: string;
  nombre: string;
  correo: string | null;
  puesto: string | null;
  nivel: string | null;
  roles: string[];
  permisos: string[];
  equipos: number[];
  sinRoles: boolean;
}

export interface ConfiguracionAuth {
  emisorDesarrollo: boolean;
}

export interface ResultadoLogin {
  token: string;
  expira: string;
  sesion: Sesion;
  requiereCambioPassword: boolean;
}

const CLAVE_TOKEN = "gte.token";
const CLAVE_TOKEN_REAL = "gte.token.real";
const CLAVE_NOMBRE_REAL = "gte.suplantador.nombre";

export async function obtenerConfiguracionAuth() {
  return obtener<ConfiguracionAuth>("/api/v1/auth/configuracion");
}

export async function obtenerSesion() {
  return obtener<Sesion>("/api/v1/auth/sesion");
}

/** Login propio de GTE: cuenta de dominio + contraseña. */
export async function iniciarSesion(dominio: string, password: string) {
  const { dato, mensaje } = await enviar<ResultadoLogin>(
    "post", "/api/v1/auth/login", { dominio, password },
  );
  sessionStorage.setItem(CLAVE_TOKEN, dato.token);
  return { sesion: dato.sesion, mensaje, requiereCambioPassword: dato.requiereCambioPassword };
}

/** Atajo de desarrollo: sin contraseña, solo disponible si la API lo habilita. */
export async function iniciarSesionDesarrollo(dominio: string) {
  const { dato, mensaje } = await enviar<{ token: string; expira: string; sesion: Sesion }>(
    "post", "/api/v1/auth/desarrollo/token", { dominio },
  );
  sessionStorage.setItem(CLAVE_TOKEN, dato.token);
  return { sesion: dato.sesion, mensaje };
}

/** Rota el refresh token (cookie HttpOnly, viaja sola) y emite un access token nuevo. */
export async function refrescarSesion() {
  const { dato } = await enviar<ResultadoLogin>("post", "/api/v1/auth/refresh");
  sessionStorage.setItem(CLAVE_TOKEN, dato.token);
  return dato;
}

/** Revoca la sesion en el servidor (refresh token). Silencioso si ya no habia sesion valida. */
export async function cerrarSesionServidor() {
  try {
    await enviar<object>("post", "/api/v1/auth/logout");
  } catch {
    // No importa si el servidor ya no tenia nada que revocar.
  }
}

export async function cambiarPassword(passwordActual: string, passwordNueva: string) {
  const { mensaje } = await enviar<object>("post", "/api/v1/auth/cambiar-password", {
    passwordActual, passwordNueva,
  });
  return mensaje;
}

/** Limpia la sesion local (sessionStorage). Llamar despues de cerrarSesionServidor(). */
export function cerrarSesion() {
  sessionStorage.removeItem(CLAVE_TOKEN);
  sessionStorage.removeItem(CLAVE_TOKEN_REAL);
  sessionStorage.removeItem(CLAVE_NOMBRE_REAL);
}

export function hayToken(): boolean {
  return sessionStorage.getItem(CLAVE_TOKEN) !== null;
}

/**
 * "Iniciar sesion como" (soporte), auditado: exige el permiso ADM.Suplantar y la PROPIA
 * contraseña de quien suplanta. Guarda el token real aparte (para poder salir de la
 * suplantacion sin volver a iniciar sesion) y activa el token del suplantado.
 */
export async function iniciarSuplantacion(idUsuarioSuplantado: number, password: string) {
  const tokenReal = sessionStorage.getItem(CLAVE_TOKEN);
  const { dato, mensaje } = await enviar<{ token: string; expira: string; sesion: Sesion }>(
    "post", "/api/v1/auth/suplantacion/iniciar", { idUsuarioSuplantado, password },
  );
  if (tokenReal) sessionStorage.setItem(CLAVE_TOKEN_REAL, tokenReal);
  sessionStorage.setItem(CLAVE_NOMBRE_REAL, useSesion.getState().sesion?.nombre ?? "");
  sessionStorage.setItem(CLAVE_TOKEN, dato.token);
  return { sesion: dato.sesion, mensaje };
}

/** True mientras hay una suplantacion activa en este navegador. */
export function estaSuplantando(): boolean {
  return sessionStorage.getItem(CLAVE_TOKEN_REAL) !== null;
}

export function obtenerNombreSuplantador(): string | null {
  return sessionStorage.getItem(CLAVE_NOMBRE_REAL);
}

/** Cierra la suplantacion activa y vuelve a dejar el token real como el activo. */
export async function terminarSuplantacion() {
  try {
    await enviar<object>("post", "/api/v1/auth/suplantacion/terminar");
  } catch {
    // Aun si el aviso al servidor falla, el navegador debe poder volver al usuario real.
  }
  const tokenReal = sessionStorage.getItem(CLAVE_TOKEN_REAL);
  if (tokenReal) sessionStorage.setItem(CLAVE_TOKEN, tokenReal);
  sessionStorage.removeItem(CLAVE_TOKEN_REAL);
  sessionStorage.removeItem(CLAVE_NOMBRE_REAL);
}

interface EstadoSesion {
  sesion: Sesion | null;
  establecer: (sesion: Sesion | null) => void;
  /** La UI oculta opciones con esto; el backend siempre revalida. */
  puede: (clave: string) => boolean;
}

export const useSesion = create<EstadoSesion>((set, get) => ({
  sesion: null,
  establecer: (sesion) => set({ sesion }),
  puede: (clave) => get().sesion?.permisos.includes(clave) ?? false,
}));
