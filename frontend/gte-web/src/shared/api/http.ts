import axios from "axios";

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

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5088",
  headers: { "Content-Type": "application/json" },
});

http.interceptors.request.use((config) => {
  const token = sessionStorage.getItem("gte.token");
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

// Token vencido o invalido: se descarta y la aplicacion vuelve al inicio de sesion
http.interceptors.response.use(
  (respuesta) => respuesta,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      sessionStorage.removeItem("gte.token");
      alPerderSesion?.();
    }
    return Promise.reject(error);
  },
);

function lanzarErrorApi(error: unknown): never {
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

/** POST/PUT tipado: devuelve el dato y el userMessage para el toast. */
export async function enviar<T>(
  metodo: "post" | "put",
  url: string,
  body?: unknown,
): Promise<{ dato: T; mensaje: string }> {
  try {
    const { data } = await http[metodo]<ApiResponse<T>>(url, body);
    if (!data.success) {
      throw new ErrorApi(data.userMessage, data.code);
    }
    return { dato: data.response as T, mensaje: data.userMessage };
  } catch (error) {
    if (error instanceof ErrorApi) throw error;
    lanzarErrorApi(error);
  }
}
