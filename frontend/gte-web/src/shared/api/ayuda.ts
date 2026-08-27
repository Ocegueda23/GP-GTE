import { http } from "./http";

/**
 * Centro de Mando TI. Descarga autenticada (no <iframe src> directo al endpoint: eso
 * expondria el JWT o simplemente devolveria 401, el iframe no manda el header). El
 * backend exige el permiso AYU.CentroMando en el handler.
 */
export async function descargarCentroMando(): Promise<Blob> {
  const { data } = await http.get<Blob>("/api/v1/ayuda/centro-mando-ti", { responseType: "blob" });
  return data;
}

export const PERMISO_CENTRO_MANDO = "AYU.CentroMando";
