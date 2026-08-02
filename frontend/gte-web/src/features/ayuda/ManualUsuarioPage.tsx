import { useState } from "react";
import {
  Accordion, AccordionDetails, AccordionSummary, Box, Chip, Divider, List, ListItem,
  ListItemText, Paper, Stack, Typography,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { DiagramaFlujoSolicitud } from "./DiagramaFlujoSolicitud";

interface Seccion {
  id: string;
  titulo: string;
  contenido: React.ReactNode;
}

const SECCIONES: Seccion[] = [
  {
    id: "inicio-sesion",
    titulo: "Iniciar sesion",
    contenido: (
      <Stack spacing={1.5}>
        <Typography variant="body2">
          Entra con tu usuario y contrasena en la pantalla de inicio. Si todavia no tienes
          una contrasena, o la olvidaste, un administrador de GTE puede restablecerla por ti
          -- por ahora no hay un flujo de "olvide mi contrasena" automatico por correo.
        </Typography>
        <Typography variant="body2">
          La primera vez que entres con una contrasena temporal, el sistema te va a pedir
          que la cambies antes de dejarte continuar.
        </Typography>
      </Stack>
    ),
  },
  {
    id: "menu",
    titulo: "El menu principal",
    contenido: (
      <Stack spacing={1.5}>
        <Typography variant="body2">
          En la parte de arriba de cualquier pantalla encuentras:
        </Typography>
        <List dense disablePadding sx={{ pl: 1 }}>
          <ListItem disableGutters>
            <ListItemText primary="Los botones de navegacion" secondary="Solo se ven las secciones a las que tienes acceso -- si te falta alguna que crees que deberias ver, pregunta a un administrador." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="La campana de notificaciones" secondary="Te avisa cuando alguien te menciona en un comentario, o cuando una solicitud tuya se aprueba, rechaza o devuelve. Al hacer click en una, te lleva directo a lo que la genero." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Tu nombre, a la derecha" secondary="Muestra tu usuario y tus roles. Ahi mismo esta la opcion para cerrar sesion." />
          </ListItem>
        </List>
      </Stack>
    ),
  },
  {
    id: "mi-dia",
    titulo: "Mi Dia",
    contenido: (
      <Stack spacing={1.5}>
        <Typography variant="body2">
          Es la pantalla que ves al entrar. Te muestra de un vistazo:
        </Typography>
        <List dense disablePadding sx={{ pl: 1 }}>
          <ListItem disableGutters>
            <ListItemText primary="Lo que tienes en proceso ahora mismo" secondary="Solo puedes tener UN elemento en proceso a la vez -- si inicias uno nuevo, el anterior se suspende solo." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Vencidas, para hoy, y proximos 7 dias" secondary="Listas rapidas de lo que se te viene encima." />
          </ListItem>
        </List>
        <Typography variant="body2">
          Desde aqui puedes darle "Iniciar" a cualquier elemento de la lista, o registrar
          tiempo invertido en el que ya tienes en proceso.
        </Typography>
      </Stack>
    ),
  },
  {
    id: "trabajo",
    titulo: "Trabajo (tu bandeja)",
    contenido: (
      <Typography variant="body2">
        Aqui ves todos los elementos de trabajo asignados a ti (o a tu equipo, segun los
        filtros), con su estatus, prioridad y fecha compromiso. Haz click en el folio
        (por ejemplo <strong>GTE-0042</strong>) para abrir el detalle completo.
      </Typography>
    ),
  },
  {
    id: "detalle",
    titulo: "El detalle de un elemento de trabajo",
    contenido: (
      <Stack spacing={1.5}>
        <Typography variant="body2">
          Al abrir un folio encuentras toda su informacion, y ademas:
        </Typography>
        <List dense disablePadding sx={{ pl: 1 }}>
          <ListItem disableGutters>
            <ListItemText primary="Botones de accion" secondary='Cambian el estatus (por ejemplo "Iniciar", "Terminar", "Enviar a revision"). Las opciones que ves son siempre las validas para el estatus actual -- no puedes "saltarte" pasos.' />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Comentarios" secondary="Puedes escribir con negritas/listas, mencionar a alguien con @ (le llega una notificacion), y pegar imagenes directo desde el portapapeles." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Adjuntos" secondary="Sube o descarga archivos relacionados. Solo tu (el autor) puedes borrar tus propios comentarios o adjuntos." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Registro de tiempo" secondary="Cuanto tiempo llevas invertido en total." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Editar" secondary="El boton de editar solo aparece si de verdad puedes cambiar algo (por permisos y por el estatus actual)." />
          </ListItem>
        </List>
      </Stack>
    ),
  },
  {
    id: "solicitudes",
    titulo: "Solicitudes",
    contenido: (
      <Typography variant="body2">
        Si necesitas pedir algo nuevo (un requerimiento, una mejora, reportar un bug), entra
        a <strong>Solicitudes</strong> y llena el formulario. Un responsable la revisa
        y decide si se aprueba, se rechaza, o se regresa pidiendo mas informacion -- te
        llega una notificacion en cualquiera de los tres casos. Si se aprueba, se convierte
        en uno o mas elementos de trabajo que puedes seguir desde tu bandeja.
      </Typography>
    ),
  },
  {
    id: "flujo-completo",
    titulo: "El flujo completo: de una solicitud a su cierre",
    contenido: (
      <Stack spacing={2}>
        <Typography variant="body2">
          Este es el recorrido completo que sigue una solicitud desde que la envias hasta
          que la tarea que genera queda cerrada:
        </Typography>
        <DiagramaFlujoSolicitud />
        <Stack spacing={1}>
          <Typography variant="body2">
            <strong>1. Solicitud enviada</strong> -- llenas el formulario en Solicitudes con
            lo que necesitas.
          </Typography>
          <Typography variant="body2">
            <strong>2. Revision de solicitudes</strong> -- un responsable la revisa y decide: <em>aprobarla</em>{" "}
            (sigue el flujo normal), <em>rechazarla</em> (termina ahi, no procede), o{" "}
            <em>devolverla</em> pidiendo mas informacion (vuelves a mandarla con los datos
            que faltaban). Te llega una notificacion con la decision.
          </Typography>
          <Typography variant="body2">
            <strong>3. Se crean una o mas tareas</strong> -- si se aprueba, la solicitud se
            convierte en uno o mas elementos de trabajo, ya ligados a ella para que se
            pueda dar seguimiento.
          </Typography>
          <Typography variant="body2">
            <strong>4. En trabajo</strong> -- la tarea avanza de Pendiente a En proceso
            (con su tiempo registrado) hasta quedar Terminada por quien la trabaja.
          </Typography>
          <Typography variant="body2">
            <strong>5. Revision de calidad</strong> -- en los proyectos que la usan, alguien
            revisa el resultado. Si encuentra algo, la tarea vuelve a correccion (paso 4);
            si no, sigue adelante.
          </Typography>
          <Typography variant="body2">
            <strong>6. Terminada (cierre)</strong> -- la tarea queda cerrada de verdad, y se
            le avisa al solicitante original que ya esta lista.
          </Typography>
        </Stack>
      </Stack>
    ),
  },
  {
    id: "otras-secciones",
    titulo: "Otras secciones (segun tu rol)",
    contenido: (
      <Stack spacing={1}>
        <Typography variant="body2">
          Dependiendo de tu rol, es posible que tambien veas:
        </Typography>
        <List dense disablePadding sx={{ pl: 1 }}>
          <ListItem disableGutters>
            <ListItemText primary="Tablero y Backlog" secondary="Planeacion de sprints y kanban -- para lideres/planeadores." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="QA" secondary="Planes y ejecucion de pruebas -- para el equipo de calidad." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Releases" secondary="Armado y aprobacion de versiones -- para lideres y QA." />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText primary="Administracion" secondary="Proyectos, equipos, usuarios, roles, horarios -- solo para administradores." />
          </ListItem>
        </List>
        <Typography variant="body2" color="text.secondary">
          Si crees que deberias tener acceso a alguna de estas y no la ves, pide a un
          administrador que revise tu rol.
        </Typography>
      </Stack>
    ),
  },
  {
    id: "ayuda-adicional",
    titulo: "Si algo no funciona o tienes dudas",
    contenido: (
      <Typography variant="body2">
        Contacta al equipo que administra GTE en tu organizacion -- ellos pueden
        restablecer tu contrasena, ajustar tus permisos, o revisar un problema puntual.
      </Typography>
    ),
  },
];

export function ManualUsuarioPage() {
  const [expandido, setExpandido] = useState<string | false>(SECCIONES[0].id);

  return (
    <Box sx={{ p: 2, maxWidth: 900, mx: "auto" }}>
      <Stack spacing={0.5} sx={{ mb: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>Manual de usuario</Typography>
        <Typography variant="body2" color="text.secondary">
          Guia rapida de como usar GTE. No necesitas saber nada de programacion ni de
          como esta construido el sistema para seguirla.
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Typography variant="body2">
          <strong>GTE</strong> (Gestor Tecnologico Empresarial) es donde se registran, dan
          seguimiento y cierran las tareas, solicitudes y proyectos del equipo. Esta guia
          cubre lo basico para trabajar en el dia a dia; abre cada seccion para ver el
          detalle.
        </Typography>
      </Paper>

      {SECCIONES.map((seccion, indice) => (
        <Accordion
          key={seccion.id}
          expanded={expandido === seccion.id}
          onChange={(_evento, abierta) => setExpandido(abierta ? seccion.id : false)}
          disableGutters
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
              <Chip size="small" label={indice + 1} sx={{ minWidth: 28 }} />
              <Typography sx={{ fontWeight: 600 }}>{seccion.titulo}</Typography>
            </Stack>
          </AccordionSummary>
          <AccordionDetails>{seccion.contenido}</AccordionDetails>
        </Accordion>
      ))}

      <Divider sx={{ my: 3 }} />
      <Typography variant="caption" color="text.secondary">
        Este manual cubre las funciones disponibles hoy en GTE y se ira actualizando
        conforme se agreguen mas.
      </Typography>
    </Box>
  );
}
