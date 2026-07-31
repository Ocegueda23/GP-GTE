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
  identidadExterna: boolean;
  authority: string | null;
  audience: string;
  emisorDesarrollo: boolean;
}

const CLAVE_TOKEN = "gte.token";

export async function obtenerConfiguracionAuth() {
  return obtener<ConfiguracionAuth>("/api/v1/auth/configuracion");
}

export async function obtenerSesion() {
  return obtener<Sesion>("/api/v1/auth/sesion");
}

export async function iniciarSesionDesarrollo(dominio: string) {
  const { dato, mensaje } = await enviar<{ token: string; expira: string; sesion: Sesion }>(
    "post", "/api/v1/auth/desarrollo/token", { dominio },
  );
  sessionStorage.setItem(CLAVE_TOKEN, dato.token);
  return { sesion: dato.sesion, mensaje };
}

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
