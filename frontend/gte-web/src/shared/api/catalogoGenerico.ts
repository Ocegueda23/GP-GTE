import { enviar, obtener, type ResultadoPaginado } from "./http";

export interface CatalogoResumen {
  idCatalogo: number;
  clave: string;
  nombreTabla: string;
  titulo: string;
}

export interface ColumnaConfig {
  nombreColumna: string;
  tipoSql: string;
  esNulable: boolean;
  longitudMaxima: number | null;
  esPk: boolean;
  esIdentity: boolean;
  displayName: string;
  esVisible: boolean;
  esSoloLectura: boolean;
  esRequerido: boolean;
  ordinalPos: number;
  tablaFk: string | null;
  columnaClaveFk: string | null;
  columnaMostrarFk: string | null;
  esCifrado: boolean;
  autoFechaAlta: boolean;
  autoFechaEdicion: boolean;
  autoUsuarioAlta: boolean;
  autoUsuarioEdicion: boolean;
}

export interface ConfiguracionCatalogo {
  idCatalogo: number;
  clave: string;
  nombreTabla: string;
  titulo: string;
  columnas: ColumnaConfig[];
}

export interface ColumnaEsquema {
  nombreColumna: string;
  tipoSql: string;
  esNulable: boolean;
  longitudMaxima: number | null;
  esPk: boolean;
  esIdentity: boolean;
}

export interface FiltroColumnaDiscreto {
  nombreColumna: string;
  valores: string[];
}

export interface FiltroFecha {
  nombreColumna: string;
  desde: string | null;
  hasta: string | null;
}

export interface FiltrosListado {
  texto?: string;
  filtrosColumna: FiltroColumnaDiscreto[];
  fecha: FiltroFecha | null;
  ordenarPor?: string;
  ordenDescendente: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface OpcionFk {
  valor: string;
  etiqueta: string;
}

export type Registro = Record<string, unknown>;

/* ---------- Consulta ---------- */

export async function obtenerCatalogos() {
  return obtener<CatalogoResumen[]>("/api/v1/catalogo-generico");
}

export async function obtenerConfigCatalogo(clave: string) {
  return obtener<ConfiguracionCatalogo>(`/api/v1/catalogo-generico/${clave}/config`);
}

export async function listarRegistros(clave: string, filtros: FiltrosListado) {
  const { dato } = await enviar<ResultadoPaginado<Registro>>(
    "post", `/api/v1/catalogo-generico/${clave}/registros/consulta`, filtros,
  );
  return dato;
}

export async function obtenerValoresDistintos(clave: string, nombreColumna: string) {
  return obtener<string[]>(
    `/api/v1/catalogo-generico/${clave}/registros/valores-distintos/${encodeURIComponent(nombreColumna)}`,
  );
}

export async function descifrarValor(clave: string, clavesPk: Registro, nombreColumna: string) {
  const { dato } = await enviar<string | null>(
    "post", `/api/v1/catalogo-generico/${clave}/registros/descifrar`, { clavesPk, nombreColumna },
  );
  return dato;
}

export async function obtenerOpcionesFk(clave: string, nombreColumna: string) {
  return obtener<OpcionFk[]>(
    `/api/v1/catalogo-generico/${clave}/registros/opciones-fk/${encodeURIComponent(nombreColumna)}`,
  );
}

export async function crearRegistro(clave: string, valores: Registro) {
  const { dato, mensaje } = await enviar<Registro>(
    "post", `/api/v1/catalogo-generico/${clave}/registros`, { valores },
  );
  return { dato, mensaje };
}

export async function actualizarRegistro(clave: string, clavesPk: Registro, valores: Registro) {
  const { mensaje } = await enviar<object>(
    "put", `/api/v1/catalogo-generico/${clave}/registros`, { clavesPk, valores },
  );
  return mensaje;
}

export async function eliminarRegistro(clave: string, clavesPk: Registro) {
  const { mensaje } = await enviar<object>(
    "delete", `/api/v1/catalogo-generico/${clave}/registros`, { clavesPk },
  );
  return mensaje;
}

/* ---------- Administracion ---------- */

export async function obtenerTablasDisponibles() {
  return obtener<string[]>("/api/v1/catalogo-generico/admin/tablas-disponibles");
}

export async function obtenerColumnasEsquema(nombreTabla: string) {
  return obtener<ColumnaEsquema[]>(
    `/api/v1/catalogo-generico/admin/tablas/${encodeURIComponent(nombreTabla)}/columnas`,
  );
}

export async function crearCatalogo(clave: string, nombreTabla: string, titulo: string) {
  const { dato, mensaje } = await enviar<ConfiguracionCatalogo>(
    "post", "/api/v1/catalogo-generico/admin", { clave, nombreTabla, titulo },
  );
  return { dato, mensaje };
}

export async function actualizarConfigColumnas(clave: string, columnas: ColumnaConfig[]) {
  const { dato, mensaje } = await enviar<ConfiguracionCatalogo>(
    "put", `/api/v1/catalogo-generico/admin/${clave}/columnas`, { columnas },
  );
  return { dato, mensaje };
}
