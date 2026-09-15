/**
 * Formato de fecha compartido por las tres pantallas de entregas (listado, detalle y
 * Solicitud de despliegue). Estaba copiado en cada archivo y las tres iban divergiendo.
 */
export function formatearFecha(iso: string | null): string {
  if (!iso) return "-";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "short", year: "numeric" });
}
