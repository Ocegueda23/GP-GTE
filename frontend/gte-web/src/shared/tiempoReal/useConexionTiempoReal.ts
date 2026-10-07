import { useEffect } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { URL_BASE_API } from "../api/http";
import type { Notificacion } from "../api/notificaciones";
import { invalidarVistasDeTrabajo } from "../api/workitems";

const CLAVE_TOKEN = "gte.token";

function mostrarNotificacionEscritorio(notificacion: Notificacion, navegar: (url: string) => void) {
  if (!("Notification" in window) || Notification.permission !== "granted") return;

  const aviso = new Notification(notificacion.titulo, {
    body: notificacion.mensaje ?? undefined,
    tag: `gte-notificacion-${notificacion.idNotificacion}`,
  });
  aviso.onclick = () => {
    window.focus();
    if (notificacion.url) navegar(notificacion.url);
    aviso.close();
  };
}

/**
 * Conecta al hub de SignalR mientras el componente este montado (BarraSuperior, que solo
 * renderiza con sesion activa gracias a GuardiaSesion). Un solo hub para dos eventos:
 * "notificacion" (por usuario, via Clients.User) y "workItemActualizado" (broadcast, para
 * refrescar tablero, bandeja y Mi dia abiertos). Ademas de refrescar la campana, dispara una
 * notificacion de escritorio (Web Notifications API).
 */
export function useConexionTiempoReal() {
  const clienteQuery = useQueryClient();
  const navegar = useNavigate();

  useEffect(() => {
    const conexion = new HubConnectionBuilder()
      .withUrl(`${URL_BASE_API}/hubs/notificaciones`, {
        accessTokenFactory: () => sessionStorage.getItem(CLAVE_TOKEN) ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conexion.on("notificacion", (notificacion: Notificacion) => {
      void clienteQuery.invalidateQueries({ queryKey: ["notificaciones"] });
      mostrarNotificacionEscritorio(notificacion, navegar);
    });

    conexion.on("workItemActualizado", () => {
      void invalidarVistasDeTrabajo(clienteQuery);
    });

    void conexion.start().catch(() => {
      // Sin conexion en vivo, la app sigue funcionando por REST; no es fatal.
    });

    return () => {
      void conexion.stop();
    };
  }, [clienteQuery, navegar]);
}
