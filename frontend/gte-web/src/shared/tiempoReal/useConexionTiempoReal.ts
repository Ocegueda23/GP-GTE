import { useEffect } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { URL_BASE_API } from "../api/http";

const CLAVE_TOKEN = "gte.token";

/**
 * Conecta al hub de SignalR mientras el componente este montado (BarraSuperior, que solo
 * renderiza con sesion activa gracias a GuardiaSesion). Un solo hub para dos eventos:
 * "notificacion" (por usuario, via Clients.User) y "workItemActualizado" (broadcast, para
 * refrescar tableros abiertos).
 */
export function useConexionTiempoReal() {
  const clienteQuery = useQueryClient();

  useEffect(() => {
    const conexion = new HubConnectionBuilder()
      .withUrl(`${URL_BASE_API}/hubs/notificaciones`, {
        accessTokenFactory: () => sessionStorage.getItem(CLAVE_TOKEN) ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conexion.on("notificacion", () => {
      void clienteQuery.invalidateQueries({ queryKey: ["notificaciones"] });
    });

    conexion.on("workItemActualizado", () => {
      void clienteQuery.invalidateQueries({ queryKey: ["tablero"] });
      void clienteQuery.invalidateQueries({ queryKey: ["bandeja"] });
    });

    void conexion.start().catch(() => {
      // Sin conexion en vivo, la app sigue funcionando por REST; no es fatal.
    });

    return () => {
      void conexion.stop();
    };
  }, [clienteQuery]);
}
