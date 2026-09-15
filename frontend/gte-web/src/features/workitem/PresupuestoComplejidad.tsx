import { Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { formatearMinutos, obtenerPresupuestoEstimado } from "../../shared/api/workitems";

interface Props {
  idComplejidad: number | "";
  idAsignado: number | "";
}

/**
 * Muestra, debajo del combo de complejidad, el presupuesto que quedaria congelado con esa
 * complejidad (RN-GTE-015). El renglon exacto de la matriz depende del nivel del asignado:
 * sin asignado (o sin nivel capturado) se listan los minutos por nivel como referencia.
 */
export function PresupuestoComplejidad({ idComplejidad, idAsignado }: Props) {
  const asignado = idAsignado === "" ? null : (idAsignado as number);
  const presupuesto = useQuery({
    queryKey: ["presupuesto-complejidad", idComplejidad, asignado],
    queryFn: () => obtenerPresupuestoEstimado(idComplejidad as number, asignado),
    enabled: idComplejidad !== "",
    staleTime: 5 * 60_000,
  });

  const texto = () => {
    if (idComplejidad === "") {
      return "Define automaticamente el presupuesto de horas y puntos de historia (segun el nivel del asignado).";
    }
    const datos = presupuesto.data;
    if (!datos) {
      return "Calculando el presupuesto de esta complejidad...";
    }
    if (datos.minutos !== null) {
      const puntos = datos.puntos !== null ? ` - ${datos.puntos} pts` : "";
      return `Presupuesto: ${formatearMinutos(datos.minutos)}${puntos} (nivel ${datos.nivel}).`;
    }
    if (datos.niveles.length === 0) {
      return `La complejidad ${datos.complejidad} no tiene presupuesto capturado en la matriz.`;
    }
    const porNivel = datos.niveles
      .map((n) => `${n.nivel} ${formatearMinutos(n.minutos)}`)
      .join(" - ");
    return `Presupuesto por nivel: ${porNivel}. Se congela al asignar responsable.`;
  };

  return (
    <Typography variant="caption" color="text.secondary" sx={{ mt: -1.5 }}>
      {texto()}
    </Typography>
  );
}
