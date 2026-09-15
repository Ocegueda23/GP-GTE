import { useNavigate, useParams } from "react-router-dom";
import {
  Alert, Box, Button, GlobalStyles, LinearProgress, Paper, Stack, Table, TableBody, TableCell,
  TableHead, TableRow, Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import PrintIcon from "@mui/icons-material/Print";
import { useQuery } from "@tanstack/react-query";
import { ContenidoEnriquecido } from "../../shared/editor/ContenidoEnriquecido";
import { obtenerRelease } from "../../shared/api/entregas";

/**
 * Al imprimir, el navegador se lleva TODO el documento: barra superior, menu lateral y
 * botones incluidos. En vez de duplicar la pagina en un iframe, se apaga la visibilidad de
 * todo y se vuelve a encender solo este bloque, que ademas se saca del flujo para que
 * arranque en la esquina de la hoja.
 */
const ESTILOS_IMPRESION = (
  <GlobalStyles styles={{
    "@media print": {
      "body *": { visibility: "hidden" },
      "#solicitud-despliegue, #solicitud-despliegue *": { visibility: "visible" },
      // Sin esto el navegador imprime el folio en gris: por default recorta color de
      // texto y fondos para ahorrar tinta, y el folio se identifica justamente por rojo.
      "#solicitud-despliegue": {
        position: "absolute", left: 0, top: 0, width: "100%", padding: 0,
        WebkitPrintColorAdjust: "exact", printColorAdjust: "exact",
        fontSize: "10pt", lineHeight: 1.25,
      },
      ".no-imprimir": { display: "none !important" },
      // Las tablas del instructivo no se deben partir a la mitad de un renglon.
      "tr, img": { pageBreakInside: "avoid" },
      // El documento se imprime en papel, no se lee en pantalla: en papel el cuerpo puede
      // ser mas chico y los renglones mas apretados sin perder legibilidad, y asi la
      // solicitud deja de consumir hojas de mas. Los renglones de firma llevan alto fijo
      // (ver la tabla de Firmas) y quedan fuera de este apretado a proposito: ahi el
      // espacio es para la pluma.
      "#solicitud-despliegue .MuiTableCell-root": {
        paddingTop: "2px", paddingBottom: "2px", paddingLeft: "6px", paddingRight: "6px",
        lineHeight: 1.25,
      },
      "#solicitud-despliegue p": { margin: "0 0 2px" },
      // Un titulo de seccion solo en el pie de una hoja, con su tabla en la siguiente,
      // desperdicia media pagina; se mantiene pegado a lo que encabeza.
      "#solicitud-despliegue .titulo-seccion": { pageBreakAfter: "avoid" },
    },
  }} />
);

function formatearFecha(iso: string | null | undefined): string {
  if (!iso) return "";
  const fecha = iso.length === 10 ? new Date(iso + "T00:00:00") : new Date(iso);
  return fecha.toLocaleDateString("es-MX", { day: "2-digit", month: "long", year: "numeric" });
}

/** Renglon del bloque de datos generales: etiqueta a la izquierda, valor con subrayado tipo formato. */
function Dato({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: "baseline", py: 0.15 }}>
      <Typography variant="body2" sx={{ fontWeight: 700, minWidth: 190 }}>{etiqueta}</Typography>
      <Typography variant="body2" sx={{ flex: 1, borderBottom: "1px solid", borderColor: "divider" }}>
        {valor || " "}
      </Typography>
    </Stack>
  );
}

function Seccion({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <Box sx={{ mt: 1.5 }}>
      <Typography variant="subtitle2" className="titulo-seccion" sx={{
        fontWeight: 700, bgcolor: "action.hover", px: 1, py: 0.25,
        border: "1px solid", borderColor: "divider",
      }}>
        {titulo}
      </Typography>
      <Box sx={{ px: 1, pt: 0.5 }}>{children}</Box>
    </Box>
  );
}

/**
 * Solicitud de despliegue imprimible del release (formato equivalente al Excel que se
 * llenaba a mano, Doctos/Solicitud de despliegue.xlsx). Se genera a partir de lo que el
 * release ya tiene registrado -- contenido, artefactos, instructivo y cadena de firmas --
 * y solo se ofrece a partir de En Aprobacion, que es cuando esa informacion ya esta
 * congelada y tiene sentido mandarla a firmar o a Infraestructura.
 */
export function SolicitudDesplieguePage() {
  const { id } = useParams<{ id: string }>();
  const navegar = useNavigate();
  const idRelease = Number(id);

  const detalle = useQuery({
    queryKey: ["release", idRelease],
    queryFn: () => obtenerRelease(idRelease),
    enabled: Number.isFinite(idRelease) && idRelease > 0,
  });

  if (detalle.isLoading) return <LinearProgress />;
  const r = detalle.data;
  if (!r) {
    return <Alert severity="error" sx={{ m: 2 }}>No se encontro el release solicitado.</Alert>;
  }

  const despliegue = (patron: RegExp) =>
    r.despliegues.find((d) => !d.esRollback && patron.test(d.ambiente));
  const firmas = r.aprobaciones;
  const artefactosConInstructivo = r.artefactos.filter((a) => a.instruccionesImplementacion);

  return (
    <Box sx={{ p: 2 }}>
      {ESTILOS_IMPRESION}

      <Stack direction="row" spacing={1} className="no-imprimir" sx={{ mb: 2 }}>
        <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navegar("/releases")}>
          Volver a releases
        </Button>
        <Box sx={{ flex: 1 }} />
        <Button size="small" variant="contained" startIcon={<PrintIcon />}
          onClick={() => window.print()}>
          Imprimir
        </Button>
      </Stack>

      {r.idEstatus === 1 && (
        <Alert severity="warning" className="no-imprimir" sx={{ mb: 2 }}>
          El release sigue En Preparacion: lo que se imprima aqui todavia puede cambiar.
        </Alert>
      )}

      <Paper id="solicitud-despliegue" variant="outlined" sx={{ p: 2, maxWidth: 900, mx: "auto" }}>
        {/* El folio identifica el documento: va en el encabezado y en rojo para que se
            localice de un vistazo entre las solicitudes impresas, no perdido como un
            renglon mas de datos generales. */}
        <Stack direction="row" spacing={2} sx={{ alignItems: "baseline", mb: 1 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, flex: 1, textAlign: "center" }}>
            Solicitud de despliegue
          </Typography>
          <Typography variant="h6" sx={{ fontWeight: 700, color: "#c62828", whiteSpace: "nowrap" }}>
            {r.folio ?? ""}
          </Typography>
        </Stack>

        {/* El lider encabeza los datos generales: es a quien se le pregunta por la entrega
            cuando alguien tiene el documento impreso en la mano. El estatus no se imprime:
            en papel siempre queda desactualizado respecto al sistema. */}
        <Seccion titulo="Datos generales">
          <Dato etiqueta="Lider asignado:" valor={r.liderAsignado ?? ""} />
          <Dato etiqueta="Proyecto:" valor={`${r.claveProyecto} - ${r.proyecto}`} />
          <Dato etiqueta="Version:" valor={r.version} />
          <Dato etiqueta="Fecha requerimiento:" valor={formatearFecha(r.fechaPlan)} />
          <Dato etiqueta="Fecha PREPROD:" valor={formatearFecha(despliegue(/pre/i)?.fechaInicio)} />
          <Dato etiqueta="Fecha PRODUCCION:"
            valor={formatearFecha(despliegue(/prod/i)?.fechaInicio ?? r.fechaLiberacion)} />
        </Seccion>

        {r.notasVersion && (
          <Seccion titulo="Descripcion">
            <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{r.notasVersion}</Typography>
          </Seccion>
        )}

        <Seccion titulo={`Contenido de la version (${r.items.length})`}>
          {r.items.length === 0
            ? <Typography variant="body2">Sin elementos registrados.</Typography>
            : (
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                    <TableCell sx={{ width: 120 }}>Folio</TableCell>
                    <TableCell sx={{ width: 110 }}>Tipo</TableCell>
                    <TableCell>Descripcion</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {r.items.map((item) => (
                    <TableRow key={item.idWorkItem}>
                      <TableCell>{item.folio}</TableCell>
                      <TableCell>{item.tipo}</TableCell>
                      <TableCell>{item.titulo}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
        </Seccion>

        <Seccion titulo={`Objetos a desplegar (${r.artefactos.length})`}>
          {r.artefactos.length === 0
            ? <Typography variant="body2">Sin artefactos registrados.</Typography>
            : (
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                    <TableCell sx={{ width: 50 }}>Orden</TableCell>
                    <TableCell sx={{ width: 220 }}>Objeto</TableCell>
                    <TableCell sx={{ width: 80 }}>Version</TableCell>
                    <TableCell>Tipo</TableCell>
                    <TableCell sx={{ width: 200 }}>Reversa</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {r.artefactos.map((a) => (
                    <TableRow key={a.idArtefacto}>
                      <TableCell>{a.ordenEjecucion ?? ""}</TableCell>
                      <TableCell sx={{ wordBreak: "break-all" }}>{a.nombre}</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>{a.versionArtefacto ?? ""}</TableCell>
                      <TableCell>{a.tipo}</TableCell>
                      <TableCell>
                        {a.nombreRollback ?? a.justificacionIrreversible ?? "N/A"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
        </Seccion>

        {/* Respaldos previos: que se respalda antes de tocar produccion. El release no
            llega a firmarse sin al menos uno (validado al solicitar la aprobacion), asi
            que este apartado nunca deberia imprimirse vacio. */}
        <Seccion titulo={`Respaldos previos al despliegue (${r.respaldos.length})`}>
          {r.respaldos.length === 0
            ? <Typography variant="body2">Sin respaldos registrados.</Typography>
            : (
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                    <TableCell sx={{ width: 160 }}>Tipo</TableCell>
                    <TableCell>Nombre o ubicacion</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {r.respaldos.map((rp) => (
                    <TableRow key={rp.idReleaseRespaldo}>
                      <TableCell>{rp.tipo}</TableCell>
                      <TableCell sx={{ wordBreak: "break-word" }}>{rp.descripcion}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
        </Seccion>

        {r.instruccionesImplementacion && (
          <Seccion titulo="Instrucciones de implementacion">
            <ContenidoEnriquecido html={r.instruccionesImplementacion} />
          </Seccion>
        )}

        {artefactosConInstructivo.map((a) => (
          <Seccion key={a.idArtefacto} titulo={`Instrucciones de ejecucion - ${a.nombre}`}>
            <ContenidoEnriquecido html={a.instruccionesImplementacion!} />
          </Seccion>
        ))}

        <Seccion titulo="Firmas">
          {firmas.length === 0
            ? <Typography variant="body2">La cadena de firmas se crea al solicitar la aprobacion.</Typography>
            : (
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ "& th": { fontWeight: 700 } }}>
                    <TableCell sx={{ width: 170 }}>Rol</TableCell>
                    <TableCell>Nombre</TableCell>
                    <TableCell sx={{ width: 260 }}>Firma</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {/* Renglon alto a proposito: este bloque se imprime para firmarse a mano,
                      asi que las celdas de nombre y firma se dejan vacias -- las llenan los
                      interesados con pluma, sin datos del sistema (ni aprobador ni hash). */}
                  {firmas.map((ap) => (
                    <TableRow key={ap.idAprobacion} sx={{ "& td": { height: 64, verticalAlign: "top", pt: 1 } }}>
                      <TableCell>{ap.rolAprobacion}</TableCell>
                      <TableCell />
                      <TableCell />
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
        </Seccion>

        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 3 }}>
          Documento generado por GTE el {new Date().toLocaleString("es-MX")}.
        </Typography>
      </Paper>
    </Box>
  );
}
