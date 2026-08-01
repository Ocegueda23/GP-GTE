import { enviar, obtener } from "./http";

export interface Notificacion {
  idNotificacion: number;
  titulo: string;
  mensaje: string | null;
  entidad: string | null;
  idEntidad: number | null;
  url: string | null;
  leida: boolean;
  fechaLeida: string | null;
  fechaRegistro: string;
}

export async function obtenerNotificaciones(soloNoLeidas: boolean) {
  return obtener<Notificacion[]>("/api/v1/me/notificaciones", new URLSearchParams({
    soloNoLeidas: String(soloNoLeidas),
  }));
}

export async function marcarNotificacionLeida(idNotificacion: number) {
  const { mensaje } = await enviar<object>("put", `/api/v1/me/notificaciones/${idNotificacion}/leer`);
  return mensaje;
}

export async function marcarTodasNotificacionesLeidas() {
  const { mensaje } = await enviar<object>("put", "/api/v1/me/notificaciones/leer-todas");
  return mensaje;
}
