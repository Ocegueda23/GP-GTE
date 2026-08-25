import { enviar, obtener } from "./http";

/**
 * Catalogo de reglas de negocio por proyecto.
 *
 * Modelo: toda regla tiene un proyecto DUENO y puede declararse como afectando a otros
 * proyectos (lista explicita). Al consultar un proyecto llegan sus reglas propias y las
 * heredadas de otros proyectos; las heredadas se muestran pero solo se editan en su origen.
 */

const BASE = "/api/v1/reglas-negocio";

/** IDs fijos de dbo.tblTipoAmbitoRegla (contrato del script 43). */
export const TIPO_AMBITO = {
  flujoDeOperacion: 1,
  caracteristicaDelSistema: 2,
} as const;

/** IDs fijos de dbo.tblEstadoReglaNegocio (contrato del script 43). */
export const ESTADO_REGLA = {
  vigente: 1,
  implementadaParcialmente: 2,
  documentadaSinImplementar: 3,
  derogada: 4,
} as const;

export type OrigenRegla = "Propia" | "Heredada";

export interface ProyectoConReglas {
  idProyecto: number;
  clave: string;
  nombre: string;
  totalReglas: number;
}

export interface OpcionCatalogo {
  id: number;
  nombre: string;
}

export interface CatalogosReglasNegocio {
  tiposAmbito: OpcionCatalogo[];
  estados: OpcionCatalogo[];
  tiposRelacion: OpcionCatalogo[];
}

export interface ReglaNegocioResumen {
  idReglaNegocio: number;
  clave: string;
  nombre: string;
  idProyectoDueno: number;
  claveProyectoDueno: string;
  nombreProyectoDueno: string;
  origen: OrigenRegla;
  idAmbitoRegla: number | null;
  nombreAmbito: string | null;
  idTipoAmbitoRegla: number | null;
  nombreTipoAmbito: string | null;
  idEstadoReglaNegocio: number;
  nombreEstado: string;
  /** Solo en heredadas: como le pega la regla al proyecto consultado. */
  descripcionImpacto: string | null;
  activo: boolean;
}

export interface ImpactoRegla {
  idReglaNegocioImpacto: number;
  idProyectoAfectado: number;
  claveProyectoAfectado: string;
  nombreProyectoAfectado: string;
  descripcionImpacto: string | null;
  idAmbitoRegla: number | null;
  nombreAmbito: string | null;
}

export interface RelacionRegla {
  idReglaNegocioRelacion: number;
  idReglaNegocioRelacionada: number;
  claveRelacionada: string;
  nombreRelacionada: string;
  idProyectoRelacionada: number;
  idTipoRelacionRegla: number;
  nombreTipoRelacion: string;
  nota: string | null;
}

export interface ReglaNegocio {
  idReglaNegocio: number;
  idProyecto: number;
  claveProyecto: string;
  nombreProyecto: string;
  clave: string;
  nombre: string;
  enunciado: string;
  justificacion: string | null;
  idAmbitoRegla: number | null;
  nombreAmbito: string | null;
  idTipoAmbitoRegla: number | null;
  nombreTipoAmbito: string | null;
  idEstadoReglaNegocio: number;
  nombreEstado: string;
  mensajeError: string | null;
  permisoBypass: string | null;
  ubicacionCodigo: string | null;
  fechaVigenciaDesde: string | null;
  fechaVigenciaHasta: string | null;
  versionActual: number;
  impactos: ImpactoRegla[];
  relaciones: RelacionRegla[];
  fechaRegistro: string;
  usuarioRegistro: string;
  fechaMovto: string | null;
  usuarioMovto: string | null;
  activo: boolean;
}

export interface CatalogoReglasProyecto {
  idProyecto: number;
  claveProyecto: string;
  nombreProyecto: string;
  propias: ReglaNegocioResumen[];
  heredadas: ReglaNegocioResumen[];
  totalPropias: number;
  totalHeredadas: number;
}

export interface ReglaVersion {
  idReglaNegocioVersion: number;
  numeroVersion: number;
  enunciado: string;
  justificacion: string | null;
  motivoCambio: string | null;
  fechaRegistro: string;
  usuarioRegistro: string;
}

export interface AmbitoRegla {
  idAmbitoRegla: number;
  idProyecto: number;
  idTipoAmbitoRegla: number;
  nombreTipoAmbito: string;
  nombre: string;
  descripcion: string | null;
  totalReglas: number;
  activo: boolean;
}

export interface FiltroCatalogoProyecto {
  idAmbitoRegla?: number | null;
  idEstadoReglaNegocio?: number | null;
  idTipoAmbitoRegla?: number | null;
  busqueda?: string;
  incluirHeredadas?: boolean;
  incluirDerogadas?: boolean;
}

/**
 * La clave NO va aqui: la forma el backend como RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO}
 * usando el motor de folios (dbo.spGenerarFolio). Llega en la respuesta del alta.
 */
export interface ReglaNegocioCrear {
  idProyecto: number;
  nombre: string;
  enunciado: string;
  justificacion: string | null;
  idAmbitoRegla: number | null;
  idEstadoReglaNegocio: number;
  mensajeError: string | null;
  permisoBypass: string | null;
  ubicacionCodigo: string | null;
  fechaVigenciaDesde: string | null;
  fechaVigenciaHasta: string | null;
}

export type ReglaNegocioActualizar = Omit<ReglaNegocioCrear, "idProyecto"> & {
  motivoCambio: string | null;
};

export interface ImpactoReglaCrear {
  idProyectoAfectado: number;
  descripcionImpacto: string | null;
  idAmbitoRegla: number | null;
}

function armarParametros(filtro: FiltroCatalogoProyecto): URLSearchParams {
  const params = new URLSearchParams();
  if (filtro.idAmbitoRegla != null) params.set("idAmbitoRegla", String(filtro.idAmbitoRegla));
  if (filtro.idEstadoReglaNegocio != null) {
    params.set("idEstadoReglaNegocio", String(filtro.idEstadoReglaNegocio));
  }
  if (filtro.idTipoAmbitoRegla != null) params.set("idTipoAmbitoRegla", String(filtro.idTipoAmbitoRegla));
  if (filtro.busqueda) params.set("busqueda", filtro.busqueda);
  if (filtro.incluirHeredadas === false) params.set("incluirHeredadas", "false");
  if (filtro.incluirDerogadas) params.set("incluirDerogadas", "true");
  return params;
}

export const obtenerProyectosConReglas = () =>
  obtener<ProyectoConReglas[]>(`${BASE}/proyectos`);

export const obtenerCatalogosReglas = () =>
  obtener<CatalogosReglasNegocio>(`${BASE}/catalogos`);

export const obtenerCatalogoProyecto = (idProyecto: number, filtro: FiltroCatalogoProyecto = {}) =>
  obtener<CatalogoReglasProyecto>(`${BASE}/proyectos/${idProyecto}`, armarParametros(filtro));

export const obtenerAmbitos = (idProyecto: number) =>
  obtener<AmbitoRegla[]>(`${BASE}/proyectos/${idProyecto}/ambitos`);

export const buscarReglas = (busqueda: string) =>
  obtener<ReglaNegocioResumen[]>(`${BASE}/buscar`, new URLSearchParams({ busqueda }));

export const obtenerRegla = (idRegla: number) =>
  obtener<ReglaNegocio>(`${BASE}/${idRegla}`);

export const obtenerVersionesRegla = (idRegla: number) =>
  obtener<ReglaVersion[]>(`${BASE}/${idRegla}/versiones`);

export const crearRegla = (datos: ReglaNegocioCrear) =>
  enviar<ReglaNegocio>("post", BASE, datos);

export const actualizarRegla = (idRegla: number, datos: ReglaNegocioActualizar) =>
  enviar<ReglaNegocio>("put", `${BASE}/${idRegla}`, datos);

export const derogarRegla = (idRegla: number) =>
  enviar<boolean>("delete", `${BASE}/${idRegla}`);

export const reactivarRegla = (idRegla: number) =>
  enviar<boolean>("post", `${BASE}/${idRegla}/reactivar`);

export const agregarImpacto = (idRegla: number, datos: ImpactoReglaCrear) =>
  enviar<ReglaNegocio>("post", `${BASE}/${idRegla}/impactos`, datos);

export const quitarImpacto = (idImpacto: number) =>
  enviar<boolean>("delete", `${BASE}/impactos/${idImpacto}`);

export const crearAmbito = (datos: {
  idProyecto: number;
  idTipoAmbitoRegla: number;
  nombre: string;
  descripcion: string | null;
}) => enviar<number>("post", `${BASE}/ambitos`, datos);

export const actualizarAmbito = (
  idAmbito: number,
  datos: { nombre: string; descripcion: string | null },
) => enviar<boolean>("put", `${BASE}/ambitos/${idAmbito}`, datos);

export const eliminarAmbito = (idAmbito: number) =>
  enviar<boolean>("delete", `${BASE}/ambitos/${idAmbito}`);
