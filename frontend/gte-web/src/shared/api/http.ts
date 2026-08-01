import axios, { type InternalAxiosRequestConfig } from "axios";

/**
 * Envelope estandar de toda respuesta de la API GTE.
 * El front lee `response` para el dato y `code`/`success` para el flujo.
 */
export interface ApiResponse<T> {
  code:
    | "OK"
    | "NOT_FOUND"
    | "VALIDATION_ERROR"
    | "CONFLICT"
    | "FORBIDDEN"
    | "INTERNAL_ERROR";
  success: boolean;
  userMessage: string;
  message: string | null;
  response: T | null;
}

export interface ResultadoPaginado<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

/** Error de negocio con el mensaje legible del backend y el detalle estructurado (409). */
export class ErrorApi extends Error {
  readonly code: string;
  readonly detalle?: unknown;

  constructor(mensaje: string, code: string, detalle?: unknown) {
    super(mensaje);
    this.name = "ErrorApi";
    this.code = code;
    this.detalle = detalle;
  }
}

const CLAVE_TOKEN = "gte.token";

/** Rutas de auth que nunca se reintentan tras un refresh (evitan bucles sin sentido). */
const RUTAS_SIN_REINTENTO = [
  "/api/v1/auth/refresh",
  "/api/v1/auth/login",
  "/api/v1/auth/desarrollo/token",
  "/api/v1/auth/logout",
];

export const URL_BASE_API = import.meta.env.VITE_API_URL ?? "http://localhost:5088";

export const http = axios.create({
  baseURL: URL_BASE_API,
  headers: { "Content-Type": "application/json" },
  // El refresh token viaja en una cookie HttpOnly (no en el body ni en localStorage).
  withCredentials: true,
});

http.interceptors.request.use((config) => {
  const token = sessionStorage.getItem(CLAVE_TOKEN);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Handler que la aplicacion registra para reaccionar a una sesion invalida. */
let alPerderSesion: (() => void) | null = null;

export function registrarManejadorSesionInvalida(handler: () => void) {
  alPerderSesion = handler;
}

/** Un solo refresh en vuelo aunque varias peticiones truenen con 401 al mismo tiempo. */
let refrescoEnCurso: Promise<string | null> | null = null;

async function intentarRefrescar(): Promise<string | null> {
  refrescoEnCurso ??= (async () => {
    try {
      const { data } = await http.post<ApiResponse<{ token: string }>>("/api/v1/auth/refresh");
      if (!data.success || !data.response) return null;
      sessionStorage.setItem(CLAVE_TOKEN, data.response.token);
      return data.response.token;
    } catch {
      return null;
    } finally {
      refrescoEnCurso = null;
    }
  })();
  return refrescoEnCurso;
}

interface SolicitudConReintento extends InternalAxiosRequestConfig {
  _reintentada?: boolean;
}

// Access token vencido: se intenta un refresh silencioso (la cookie viaja sola) y se
// reintenta la peticion original una sola vez; si tambien falla, se cierra la sesion.
http.interceptors.response.use(
  (respuesta) => respuesta,
  async (error) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    const solicitudOriginal = error.config as SolicitudConReintento | undefined;
    const esRutaSinReintento = RUTAS_SIN_REINTENTO.some((ruta) => solicitudOriginal?.url?.startsWith(ruta));

    if (!solicitudOriginal || solicitudOriginal._reintentada || esRutaSinReintento) {
      sessionStorage.removeItem(CLAVE_TOKEN);
      alPerderSesion?.();
      return Promise.reject(error);
    }

    const nuevoToken = await intentarRefrescar();
    if (!nuevoToken) {
      sessionStorage.removeItem(CLAVE_TOKEN);
      alPerderSesion?.();
      return Promise.reject(error);
    }

    solicitudOriginal._reintentada = true;
    solicitudOriginal.headers.Authorization = `Bearer ${nuevoToken}`;
    return http(solicitudOriginal);
  },
);

export function lanzarErrorApi(error: unknown): never {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as ApiResponse<unknown>;
    if (data.userMessage) {
      const detalle = (data.response as { detalle?: unknown } | null)?.detalle;
      throw new ErrorApi(data.userMessage, data.code, detalle);
    }
  }
  throw new ErrorApi("No se pudo conectar con el servidor.", "INTERNAL_ERROR");
}

/** GET tipado que desempaqueta el envelope. */
export async function obtener<T>(url: string, params?: URLSearchParams): Promise<T> {
  try {
    const { data } = await http.get<ApiResponse<T>>(url, { params });
    if (!data.success || data.response === null) {
      throw new ErrorApi(data.userMessage, data.code);
    }
    return data.response;
  } catch (error) {
    if (error instanceof ErrorApi) throw error;
    lanzarErrorApi(error);
  }
}

/** POST/PUT/DELETE tipado: devuelve el dato y el userMessage para el toast. */
export async function enviar<T>(
  metodo: "post" | "put" | "delete",
  url: string,
  body?: unknown,
): Promise<{ dato: T; mensaje: string }> {
  try {
    const { data } = metodo === "delete"
      ? await http.delete<ApiResponse<T>>(url)
      : await http[metodo]<ApiResponse<T>>(url, body);
    if (!data.success) {
      throw new ErrorApi(data.userMessage, data.code);
    }
    return { dato: data.response as T, mensaje: data.userMessage };
  } catch (error) {
    if (error instanceof ErrorApi) throw error;
    lanzarErrorApi(error);
  }
}
