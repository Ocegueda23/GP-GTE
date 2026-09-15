/**
 * Clave del permiso de escritura del modulo (GTE.Domain.Conocimiento.PermisosConocimiento).
 * La lectura no exige permiso: P23 esta marcada como "Todos" en el Documento Maestro.
 */
export const PERMISO_ADMINISTRAR_CONOCIMIENTO = "CON.Administrar";

/**
 * Baja de articulos, separada de la escritura: un articulo es memoria acumulada del
 * equipo y su baja no se deshace desde la interfaz.
 */
export const PERMISO_ELIMINAR_CONOCIMIENTO = "CON.Eliminar";
