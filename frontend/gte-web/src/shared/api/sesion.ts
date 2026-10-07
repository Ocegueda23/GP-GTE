import { create } from "zustand";
import { enviar, intentarRefrescar, obtener } from "./http";

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

// ---------- Sesion compartida entre pestanas ----------
// El token vive en sessionStorage, que es de UNA pestana: un Ctrl+Click abria la pestana
// nueva sin token y caia en el login. La pestana nueva le pide la sesion a las que ya
// estan abiertas por BroadcastChannel (mismo origen, funciona tambien sobre HTTP plano).
// Solo si ninguna contesta usa la cookie de refresh; no se refresca de entrada porque
// abrir varias pestanas a la vez dispararia refreshes simultaneos con la misma cookie.

const CLAVES_COMPARTIDAS = [CLAVE_TOKEN, CLAVE_TOKEN_REAL, CLAVE_NOMBRE_REAL];
const ESPERA_RESPUESTA_MS = 400;

type MensajeSesion =
  | { tipo: "pedir" }
  | { tipo: "sesion"; valores: Record<string, string> };

const canalSesion = typeof BroadcastChannel === "undefined" ? null : new BroadcastChannel("gte.sesion");

// Toda pestana con sesion contesta. Se copian tambien las claves de suplantacion para que
// la pestana nueva siga siendo la misma identidad que la que le dio la sesion.
canalSesion?.addEventListener("message", (evento: MessageEvent<MensajeSesion>) => {
  if (evento.data.tipo !== "pedir" || !hayToken()) return;
  const valores: Record<string, string> = {};
  for (const clave of CLAVES_COMPARTIDAS) {
    const valor = sessionStorage.getItem(clave);
    if (valor !== null) valores[clave] = valor;
  }
  canalSesion.postMessage({ tipo: "sesion", valores } satisfies MensajeSesion);
});

function pedirSesionAOtraPestana(): Promise<boolean> {
  if (!canalSesion) return Promise.resolve(false);
  return new Promise((resolver) => {
    const alRecibir = (evento: MessageEvent<MensajeSesion>) => {
      if (evento.data.tipo !== "sesion") return;
      terminar(true, evento.data.valores);
    };
    const limite = window.setTimeout(() => terminar(false), ESPERA_RESPUESTA_MS);
    function terminar(recibida: boolean, valores?: Record<string, string>) {
      window.clearTimeout(limite);
      canalSesion!.removeEventListener("message", alRecibir);
      // Contesta la primera pestana; las respuestas que lleguen despues se ignoran.
      if (recibida && valores && !hayToken()) {
        for (const [clave, valor] of Object.entries(valores)) sessionStorage.setItem(clave, valor);
      }
      resolver(recibida);
    }
    canalSesion.addEventListener("message", alRecibir);
    canalSesion.postMessage({ tipo: "pedir" } satisfies MensajeSesion);
  });
}

/**
 * Pestana sin token: intenta recuperar la sesion sin pedir contraseña. Devuelve true si
 * quedo un token en esta pestana.
 */
export async function recuperarSesionDeOtraPestana(): Promise<boolean> {
  if (hayToken()) return true;
  if (await pedirSesionAOtraPestana()) return hayToken();
  return (await intentarRefrescar()) !== null;
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
