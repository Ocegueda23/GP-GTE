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
}

export function hayToken(): boolean {
  return sessionStorage.getItem(CLAVE_TOKEN) !== null;
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
