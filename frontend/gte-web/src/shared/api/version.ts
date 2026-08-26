import { obtener } from "./http";

export interface VersionApi {
  sistema: string;
  version: string;
  ambiente: string;
  fechaServidor: string;
}

/**
 * Version del API desplegada. Anonimo en el backend a proposito (es el smoke test de
 * despliegue), asi que se puede consultar incluso antes de iniciar sesion.
 */
export async function obtenerVersionApi(): Promise<VersionApi> {
  return obtener<VersionApi>("/api/v1/version");
}

/**
 * Sello con el que se construyo este bundle. `publicar.bat` lo inyecta como VITE_VERSION;
 * en `npm run dev` no existe y devolvemos null para no mostrar un numero inventado.
 */
export const VERSION_FRONTEND: string | null = import.meta.env.VITE_VERSION ?? null;
