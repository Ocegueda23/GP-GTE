const COLOR_BORDE = "#334155";
const COLOR_TEXTO = "#1e293b";
const COLOR_TEXTO_SEC = "#64748b";
const COLOR_CAJA = "#f1f5f9";
const COLOR_EXITO_BORDE = "#0f766e";
const COLOR_EXITO_CAJA = "#e6f4f3";
const COLOR_FIN_BORDE = "#b91c1c";
const COLOR_FIN_CAJA = "#fee2e2";
const COLOR_RAMA = "#b45309";

interface CajaProps {
  x: number;
  y: number;
  ancho: number;
  alto: number;
  numero?: string;
  titulo: string;
  detalle: string;
  bordeColor?: string;
  cajaColor?: string;
}

function Caja({
  x, y, ancho, alto, numero, titulo, detalle,
  bordeColor = COLOR_BORDE, cajaColor = COLOR_CAJA,
}: CajaProps) {
  const cx = x + ancho / 2;
  return (
    <g>
      <rect x={x} y={y} width={ancho} height={alto} rx={10}
        fill={cajaColor} stroke={bordeColor} strokeWidth={1.5} />
      <text x={cx} y={y + 26} textAnchor="middle" fontSize={14} fontWeight={700} fill={COLOR_TEXTO}>
        {numero ? `${numero}. ${titulo}` : titulo}
      </text>
      <text x={cx} y={y + 46} textAnchor="middle" fontSize={11.5} fill={COLOR_TEXTO_SEC}>
        {detalle}
      </text>
    </g>
  );
}

/** Flecha vertical recta, de (x, y1) a (x, y2), con punta en y2. */
function FlechaVertical({ x, y1, y2, color = COLOR_BORDE }: { x: number; y1: number; y2: number; color?: string }) {
  return <line x1={x} y1={y1} x2={x} y2={y2} stroke={color} strokeWidth={1.75} markerEnd="url(#punta)" />;
}

interface EtiquetaProps { x: number; y: number; texto: string; color?: string; ancla?: "start" | "middle" | "end" }

function Etiqueta({ x, y, texto, color = COLOR_TEXTO_SEC, ancla = "middle" }: EtiquetaProps) {
  return (
    <text x={x} y={y} textAnchor={ancla} fontSize={11} fontStyle="italic" fill={color}>
      {texto}
    </text>
  );
}

/**
 * Diagrama del flujo completo de una solicitud, desde que se envia hasta que
 * la tarea que genera se cierra -- version simplificada para usuarios finales
 * (no el detalle tecnico del motor de estatus).
 */
export function DiagramaFlujoSolicitud() {
  const cx = 360;
  const anchoCaja = 380;
  const x0 = cx - anchoCaja / 2;
  const altoCaja = 64;

  const yCaja1 = 20;
  const yCaja2 = 180;
  const yCaja3 = 340;
  const yCaja4 = 500;
  const yCaja5 = 660;
  const yCaja6 = 820;
  const altoCaja6 = 70;

  return (
    <svg viewBox="0 0 720 940" width="100%" style={{ maxWidth: 640, display: "block", margin: "0 auto" }}>
      <defs>
        <marker id="punta" markerWidth={8} markerHeight={8} refX={4} refY={4} orient="auto">
          <path d="M0,0 L8,4 L0,8 Z" fill={COLOR_BORDE} />
        </marker>
        <marker id="puntaRama" markerWidth={8} markerHeight={8} refX={4} refY={4} orient="auto">
          <path d="M0,0 L8,4 L0,8 Z" fill={COLOR_RAMA} />
        </marker>
      </defs>

      <Caja x={x0} y={yCaja1} ancho={anchoCaja} alto={altoCaja} numero="1" titulo="Solicitud enviada"
        detalle='El solicitante llena el formulario en "Solicitudes"' />
      <FlechaVertical x={cx} y1={yCaja1 + altoCaja} y2={yCaja2 - 4} />

      <Caja x={x0} y={yCaja2} ancho={anchoCaja} alto={altoCaja} numero="2" titulo="Revision de solicitudes"
        detalle="Un responsable la revisa y decide que sigue" />

      {/* Rama izquierda: Rechazada (fin) */}
      <line x1={x0} y1={yCaja2 + altoCaja / 2} x2={115} y2={yCaja2 + altoCaja / 2}
        stroke={COLOR_FIN_BORDE} strokeWidth={1.5} markerEnd="url(#puntaRama)" />
      <Etiqueta x={(x0 + 115) / 2} y={yCaja2 + altoCaja / 2 - 8} texto="Rechazada" color={COLOR_FIN_BORDE} />
      <rect x={10} y={yCaja2} width={100} height={altoCaja} rx={10}
        fill={COLOR_FIN_CAJA} stroke={COLOR_FIN_BORDE} strokeWidth={1.5} />
      <text x={60} y={yCaja2 + 30} textAnchor="middle" fontSize={12} fontWeight={700} fill={COLOR_FIN_BORDE}>Fin</text>
      <text x={60} y={yCaja2 + 46} textAnchor="middle" fontSize={10} fill={COLOR_FIN_BORDE}>no procede</text>

      {/* Rama derecha: Devuelta -> vuelve a Solicitud enviada */}
      <path d={`M ${x0 + anchoCaja},${yCaja2 + altoCaja / 2} C 680,${yCaja2 + altoCaja / 2} 680,${yCaja1 + altoCaja / 2} ${x0 + anchoCaja},${yCaja1 + altoCaja / 2}`}
        fill="none" stroke={COLOR_RAMA} strokeWidth={1.5} strokeDasharray="5,3" markerEnd="url(#puntaRama)" />
      <Etiqueta x={655} y={(yCaja2 + yCaja1) / 2 + 32} texto="Devuelta:" color={COLOR_RAMA} />
      <Etiqueta x={655} y={(yCaja2 + yCaja1) / 2 + 46} texto="falta informacion" color={COLOR_RAMA} />

      <Etiqueta x={cx + 60} y={yCaja2 + altoCaja + 24} texto="Aprobada" color={COLOR_EXITO_BORDE} />
      <FlechaVertical x={cx} y1={yCaja2 + altoCaja} y2={yCaja3 - 4} />

      <Caja x={x0} y={yCaja3} ancho={anchoCaja} alto={altoCaja} numero="3" titulo="Se crean una o mas tareas"
        detalle="Quedan vinculadas a la solicitud original" />
      <FlechaVertical x={cx} y1={yCaja3 + altoCaja} y2={yCaja4 - 4} />

      <Caja x={x0} y={yCaja4} ancho={anchoCaja} alto={altoCaja} numero="4" titulo="En trabajo"
        detalle="Pendiente -> En proceso -> tiempo registrado" />
      <FlechaVertical x={cx} y1={yCaja4 + altoCaja} y2={yCaja5 - 4} />

      <Caja x={x0} y={yCaja5} ancho={anchoCaja} alto={altoCaja} numero="5" titulo="Revision de calidad"
        detalle="Si el proyecto la usa" />

      {/* Rama derecha: Hallazgos -> vuelve a En trabajo */}
      <path d={`M ${x0 + anchoCaja},${yCaja5 + altoCaja / 2} C 680,${yCaja5 + altoCaja / 2} 680,${yCaja4 + altoCaja / 2} ${x0 + anchoCaja},${yCaja4 + altoCaja / 2}`}
        fill="none" stroke={COLOR_RAMA} strokeWidth={1.5} strokeDasharray="5,3" markerEnd="url(#puntaRama)" />
      <Etiqueta x={655} y={(yCaja5 + yCaja4) / 2 + 32} texto="Hay hallazgos:" color={COLOR_RAMA} />
      <Etiqueta x={655} y={(yCaja5 + yCaja4) / 2 + 46} texto="vuelve a correccion" color={COLOR_RAMA} />

      <Etiqueta x={cx + 65} y={yCaja5 + altoCaja + 24} texto="Sin hallazgos" color={COLOR_EXITO_BORDE} />
      <FlechaVertical x={cx} y1={yCaja5 + altoCaja} y2={yCaja6 - 4} color={COLOR_EXITO_BORDE} />

      <Caja x={x0} y={yCaja6} ancho={anchoCaja} alto={altoCaja6} numero="6" titulo="Terminada (cierre)"
        detalle="Se notifica al solicitante original"
        bordeColor={COLOR_EXITO_BORDE} cajaColor={COLOR_EXITO_CAJA} />
    </svg>
  );
}
