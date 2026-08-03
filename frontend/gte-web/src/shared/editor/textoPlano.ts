/**
 * Compatibilidad con contenido legado: Descripcion de WorkItem y Comentarios
 * de Hallazgo eran texto plano antes de tener editor enriquecido (TipTap). Si
 * el valor guardado no parece HTML (sin ninguna etiqueta), se escapa y los
 * saltos de linea se convierten a <br> para que se seguan viendo igual en el
 * editor/vista nuevos. Una vez que se guarda desde el editor enriquecido el
 * valor ya es HTML real y esta funcion lo deja intacto.
 */
export function normalizarHtmlLegado(valor: string): string {
  if (!valor) {
    return valor;
  }
  if (/<[a-z][\s\S]*>/i.test(valor)) {
    return valor;
  }
  const escapado = valor
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
  return escapado.split(/\r?\n/).join("<br>");
}
