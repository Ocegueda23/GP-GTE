import { create } from "zustand";
import { filtroInicial, type FiltroBandeja } from "../../shared/api/workitems";

interface EstadoFiltros {
  filtro: FiltroBandeja;
  establecer: (cambios: Partial<FiltroBandeja>) => void;
  cambiarPagina: (page: number) => void;
  limpiar: () => void;
}

/** Store del feature Trabajo: cambiar un filtro regresa a la pagina 1. */
export const useFiltrosBandeja = create<EstadoFiltros>((set) => ({
  filtro: filtroInicial,
  establecer: (cambios) =>
    set((estado) => ({ filtro: { ...estado.filtro, ...cambios, page: 1 } })),
  cambiarPagina: (page) =>
    set((estado) => ({ filtro: { ...estado.filtro, page } })),
  limpiar: () => set({ filtro: filtroInicial }),
}));
