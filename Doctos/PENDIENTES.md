# GTE — Estado y pendientes

> Documento de continuidad. Sirve para retomar el proyecto en otra sesión sin
> contexto previo. Actualizar al cerrar cada bloque de trabajo.
>
> **Última actualización:** 2026-08-03 (Notificacion al convertir una Solicitud, segunda pasada: ademas de avisar al solicitante (ver abajo), `ConvertirSolicitudHandler` ahora notifica tambien al **usuario asignado** de cada item del desglose que traiga `IdAsignado` -- "Se te asigno el elemento de trabajo {folio}" con el titulo del item, ligado a la entidad `WorkItem`/ruta `/wi/{folio}` (no todos los items traen asignado, el triage puede dejarlos sin asignar). Verificado en vivo en LocalDB con dos cuentas reales: convertida una Solicitud con Asignado=Luis Garcia, la notificacion aparece de inmediato en la campana de `lgarcia`. Ver seccion 2)
> **Actualización anterior 6:** 2026-08-03 (**Falta la notificacion al convertir una Solicitud.** `CambiarEstatusSolicitudCommand` ya notifica al solicitante en Aprobar/Rechazar/Devolver, pero Convertir tiene su propio comando (`ConvertirSolicitudCommand`, por el desglose de items) y nunca quedo con su propia notificacion -- gap real, no regresion de esta sesion. Se agrego `IServicioNotificaciones` a `ConvertirSolicitudHandler`: notifica al `IdSolicitante` original con "Tu solicitud {Titulo} fue convertida en trabajo" + el/los folio(s) generados, mismo patron y ruta (`/solicitudes`) que las otras transiciones. Verificado en vivo en LocalDB: aparece en la campana de notificaciones de inmediato tras convertir. Ver seccion 2)
> **Actualización anterior 5:** 2026-08-03 (**Bug de produccion: "Convertir en trabajo" no abria el modal, `crypto.randomUUID is not a function` en consola.** Causa: `crypto.randomUUID()` (usado en `TriagePage.tsx` para generar el `uiId` de cada fila del desglose) solo existe en contexto seguro (HTTPS o `localhost`); produccion sirve por HTTP plano sobre un hostname real (no localhost, no HTTPS), asi que el navegador ni siquiera expone la funcion -- nunca fallaba en pruebas locales porque `localhost` siempre cuenta como contexto seguro. Se reemplazo por un generador simple sin dependencia de Web Crypto (`generarUiId`, timestamp+random en base36) -- el uiId es solo correlacion cliente-servidor de esta pantalla, no necesita ser criptografico. Verificado en vivo en LocalDB: Solicitud -> Triage (Tomar/Aprobar/Convertir) completa sin error. **Mismo tipo de trampa que la cookie de sesion de mas abajo: algo que solo se prueba bien en `localhost` y se rompe en el hostname real de produccion -- revisar el resto del codigo por usos de APIs de contexto seguro (`crypto.subtle`, `crypto.randomUUID`, Clipboard API, etc.) que puedan tener el mismo problema.** Ver seccion 2)
> **Actualización anterior 4:** 2026-08-03 (Solicitud/WorkItem: mismo patron de "Usuario solicitante" (catalogo tblUsuarioSolicitante) extendido de Tickets a Solicitudes -- se captura opcionalmente al crear la solicitud, SOLO si quien la registra tiene `SOL.Triage` (un Lider/analista levantandola a nombre de otra persona), y se copia automaticamente al WorkItem cuando `ConvertirSolicitudHandler` lo convierte (mismo mecanismo con que ya se copian `IdSolicitante`/`IdSolicitud` hoy). Columnas nuevas: `tblSolicitud.IdUsuarioSolicitante` y `tblWorkItem.IdUsuarioSolicitante` (script 21). **Misma trampa de nombre de FK que en Tickets** (ver Actualizacion anterior 2): `tblWorkItem` ya tenia un FK llamado `FK_tblWorkItem_tblUsuarioSolicitante` para `IdSolicitante->tblUsuario` (por el ROL, no la tabla) -- se verifico ANTES de escribir el script y el FK nuevo usa el sufijo "Catalogo" en ambas tablas. Verificado en vivo end-to-end en LocalDB con datos reales migrados: Solicitud SOL-2026-0040 creada con Usuario solicitante=Maria Garcia -> Triage (Tomar/Aprobar a proyecto GTE)-> Convertida en WorkItem GTE-0009, que muestra "Usuario solicitante: Maria Garcia" en su Detalle junto al "Solicitante" interno (Ana Viramontes); confirmado que lgarcia (sin SOL.Triage) no ve el campo al crear una solicitud. Tambien se agrego un indicador "*" con tooltip en la columna Solicitante de la bandeja de Triage cuando hay Usuario solicitante capturado. Ver seccion 2)
> **Actualización anterior 3:** 2026-08-03 (Mesa de ayuda: filtro de Estatus en la bandeja de agentes -- antes ocultaba Cerrado sin poder verlos, ahora hay selector con "Todos" igual que la Bandeja de trabajo, defecto "Todos"; autoasignacion al crear un ticket si quien lo registra ya tiene TKT.Atender -- pasa directo a Asignado via el mismo grafo de ASIGNAR, un usuario regular sigue creando en Nuevo sin asignar; RESOLVER ahora exige Solucion y MinutosSolucion -- columnas nuevas en tblTicket, script 16, validado en backend y capturado en un dialogo nuevo en Bandeja y Detalle; **corrección de la cookie de refresh** -- `EstablecerCookieRefresh` fijaba `Secure=true` sin importar el esquema real, y ni desarrollo (localhost sin TLS) ni el despliegue real (Kestrel plano, sin reverse proxy) sirven por HTTPS hoy, asi que el navegador descartaba la cookie por completo y la sesion "expiraba" a los 15 minutos del access token en vez de las 8 horas del refresh token -- reproducido en vivo (login real seguido de `POST /auth/refresh` devolvia "No hay sesion que refrescar" de inmediato); tambien se corrigio `Logout` (borraba la cookie con `Path=/` en vez de `Path=/api/v1/auth`, asi que nunca sobrescribia la real -- expuesto por la prueba `Logout_RevocaElRefreshToken_YaNoSePuedeRefrescar`, que fallaba con 403 en vez de 401 hasta corregirlo); ver seccion 2)
> **Actualización anterior 2:** 2026-08-03 (Ticket: captura de Usuario solicitante y Locacion por el ingeniero de soporte. El equipo creo dos catalogos nuevos a mano directo en produccion -- `tblUsuarioSolicitante` (gente que puede no tener cuenta de GTE) y `tblLocacion` -- sin bitacora y sin script en el repo; se les agrego la bitacora estandar (scripts 17 y 18), un script de reproducibilidad para ambientes nuevos que en produccion es no-op (19, ver nota abajo), y dos columnas nuevas en tblTicket (`IdUsuarioSolicitante`, `IdLocacion`, script 20). El "Nuevo ticket" del portal ahora muestra estos dos campos SOLO si quien lo llena tiene `TKT.Atender` -- un usuario normal sigue viendo el formulario de siempre. **Trampa encontrada probando en LocalDB antes de tocar produccion**: el FK existente `IdSolicitante->tblUsuario` ya se llamaba `FK_tblTicket_tblUsuarioSolicitante` (nombrado por el ROL, no por la tabla destino); un `IF NOT EXISTS` con ese mismo nombre para el FK nuevo chocaba y se saltaba sin avisar, dejando la columna nueva sin FK real -- el script 20 usa `FK_tblTicket_tblUsuarioSolicitanteCatalogo` para no colisionar. Verificado en vivo en LocalDB con datos sembrados (Juan Perez/Maria Garcia, Planta 1/Oficinas): el ticket se crea con ambos campos resueltos por nombre en el Detalle, y el formulario NO los muestra a un usuario sin `TKT.Atender`. Ver seccion 2)
> **Actualización anterior:** 2026-08-03 (B3: corte real de la migración del GT ejecutado contra produccion, SRVPROD\NASA -- scripts 01-05 de Migracion + 06 de backfill, ver detalle abajo; nueva funcionalidad "Subtareas" para exponer el tiempo migrado a los WorkItems hijos, ver seccion 2)
> **Repositorio:** https://github.com/Ocegueda23/GP-GTE (rama `main`)
> **Diseño completo:** `Doctos/GTE-DocumentoMaestro.md` (fuente de verdad de decisiones)
> **Reglas para escribir código aquí:** `CLAUDE.md` en la raíz

---

## 1. Cómo levantar el entorno

```bash
# 1. Base de datos (SQL Server o LocalDB). Crea bdsGTE y corre la tanda completa:
#    DataBase/Scripts, en orden, todos contra bdsGTE.
#    El script 01 crea la base si no existe; el 10 verifica el despliegue.

# 2. API (puerto 5088)
dotnet run --project src/GTE.WebApi --urls http://localhost:5088 --environment Development

# 3. SPA (puerto 5173)
cd frontend/gte-web && npm install && npm run dev
```

Cadena de conexión local por variable de entorno:
`ConnectionStrings__bdsGTE=Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True`

**GTE ya no usa Entra ID ni ningún proveedor externo: la autenticación es propia** (usuario
+ contraseña con BCrypt, JWT propio, refresh token rotativo en cookie HttpOnly). Fuera de
`Development`, `Jwt:ClaveFirma` es **obligatoria** (32+ caracteres) o la API no arranca; en
`Development` sin configurarla se genera una efímera (los tokens se invalidan al reiniciar).

**Iniciar sesión en desarrollo:** la pantalla de login tiene el formulario real (cuenta de
dominio + contraseña) y, si `Jwt:Desarrollo:Habilitado=true`, una sección aparte con el
atajo sin contraseña de siempre. Los usuarios existentes (`aviramontes` rol Administrador,
`lgarcia` rol Desarrollador) **no tienen contraseña puesta todavía** (la columna
`PasswordHash` nace en `NULL`): para probar el login real hay que entrar primero con el
atajo de desarrollo y usar "Restablecer contraseña" en Administración > Usuarios para
generarles una. Si la base está recién creada, cualquier cuenta que se escriba en el atajo
de desarrollo se aprovisiona sola pero **nace sin roles**, así que hay que asignarle uno
por SQL (`tblUsuarioRol`) para poder operar.

```bash
dotnet test GTE.sln    # 45 pruebas; las de integración se omiten si no hay LocalDB
```

---

## 2. Qué está funcionando (verificado extremo a extremo)

| Módulo | Alcance | Pantalla |
|---|---|---|
| **Autenticación** | Propia de GTE, sin proveedor externo: usuario+contraseña (BCrypt), bloqueo temporal tras 5 intentos fallidos, JWT de acceso (15 min) + refresh token rotativo en cookie HttpOnly (8h, con detección de reuso que revoca toda la cadena), cambio de contraseña propio y reset por administrador. Toda la API exige token (401 sin él); atajo local sin contraseña solo en Development (no emite cookie de refresh, es puramente de acceso). Menú filtrado por permisos. **Corrección 2026-08-03**: la cookie de refresh nunca se guardaba en el navegador (`Secure=true` fijo sobre una conexión sin TLS, tanto en desarrollo como en el despliegue real -- ver ADR de despliegue en la sección 4) y `Logout` la borraba con un `Path` que no coincidía con el de alta, así que tampoco la invalidaba de verdad; la sesión real terminaba a los 15 minutos del access token en vez de las 8 horas prometidas del refresh token. `AuthController.EstablecerCookieRefresh` ahora usa `Secure = Request.IsHttps` y `SameSite=Lax` (SPA y API comparten site), y `Logout` borra con el mismo `Path=/api/v1/auth` del alta | Login |
| **WorkItems** | Bandeja con filtros heredados del GT, detalle, alta, cambio de estatus por acción, registro de tiempo | Trabajo, Detalle |
| **Subtareas (2026-08-03)** | Pestaña nueva en el Detalle que lista los WorkItems hijos (`IdPadre`) de un elemento: folio, titulo, estatus, asignado y tiempo registrado (suma directa de `tblRegistroTiempo`, no el "Invertido" del padre), mas boton "Agregar subtarea" (reutiliza `NuevoItemModal` con el Proyecto bloqueado al del padre). Nacio del corte real de B3: la migracion del GT adjunta el tiempo migrado a los WorkItems hijo, y GTE no tenia ninguna forma de navegar de un padre a sus hijos ni de crear uno nuevo (ni pantalla ni endpoint expuesto) -- sin esto, el tiempo migrado quedaba invisible aunque la tabla si lo tuviera. Endpoint `GET /api/v1/workitems/{id}/hijos`; alta via el `POST /api/v1/workitems` existente con `idPadre`. Sin rollup hacia el "Invertido" del padre (fuera de alcance de esta pasada) | pestaña Subtareas en Detalle de WorkItem |
| **Mi Día** | Item en proceso, vencidas, para hoy, próximos 7 días, tiempo del día | Mi Día |
| **Revisiones** | Hallazgos de QA/code review que bloquean el cierre; reapertura con permiso | pestaña en Detalle |
| **Solicitudes y revisión** | Portal del solicitante, bandeja de revisión (antes llamada "triage" en el código -- la etiqueta visible se cambió a "Revisión de solicitudes" 2026-08-02 porque nadie fuera del equipo entendía el término; el nombre interno del componente/ruta/permiso sigue siendo `triage`/`SOL.Triage`, sin tocar), aprobar/rechazar/devolver, conversión a WorkItems trazados | Solicitudes, Revisión de solicitudes |
| **Planeación** | Backlog priorizable, sprints (activar/cerrar con reubicación), capacidad con calendario real, burndown, kanban con WIP | Backlog, Tablero |
| **Calidad (QA)** | Planes, casos con pasos, ciclos, ejecuciones, bug desde falla, matriz de trazabilidad | QA |
| **Entregas** | Releases con contenido validado, artefactos con rollback pareado, cadena de firmas, despliegues, rollback, notas de versión | Releases |
| **Motor de estatus** | 11 procesos por datos en `tblProceso`/`tblTransicion` + `spCambiarEstatus` con guard de concurrencia | — |
| **Calendario laboral** | `fnMinutosLaborales` con turnos partidos y festivos; motor único de tiempo | — |
| **Administracion** | Proyectos (alta/edicion + cambio de estatus por el motor, folio al autorizar, RN-PRY-01 bloquea el cierre con WorkItems abiertos), equipos con miembros y % dedicacion, usuarios (alta/edicion/baja logica, RN-ADM-01 valida ciclos de jerarquia con CTE recursivo), roles (asignar/retirar con alcance global o por proyecto, matriz rol-permiso guardada en lote), horarios (tramos con turnos partidos, dias festivos) y ambientes (por proyecto o globales) | Administracion (6 pestañas) |
| **Comentarios y adjuntos** | Hilos de comentarios sobre WorkItem con formato basico (negritas, listas, etc.), @menciones con autocompletado (TipTap + catalogo de usuarios) y pegado de imagenes desde el portapapeles; adjuntos con subida/descarga por streaming autenticado (`IAlmacenArchivos` en disco, GUID + SHA-256), validacion de extension/tamano, baja logica solo por el propio autor. HTML sanitizado en el backend (`HtmlSanitizer`) antes de guardarse | franja de Comentarios bajo el detalle + pestaña Adjuntos, en Detalle de WorkItem |
| **Rich text en Descripcion y Hallazgos (2026-08-02)** | El mismo tratamiento de Comentarios (TipTap + pegado de imagenes + sanitizado backend) se extendio a la Descripcion del WorkItem (solo en edicion/detalle, ver nota de alcance abajo) y a la captura de Hallazgos (`PanelRevisiones`, campo "Que se encontro"). Piezas compartidas extraidas a `frontend/gte-web/src/shared/editor/` (`ImagenProtegida`, `ContenidoEnriquecido`, `EditorEnriquecido`, `textoPlano.ts`) para no duplicar el patron entre Comentarios/Descripcion/Hallazgos; `EditorComentario`/`PanelComentarios` se refactorizaron para consumir las mismas piezas en vez de mantener su propia copia. Backend: `CrearWorkItemCommand`/`ActualizarWorkItemCommand`/`CrearRevisionCommand` ahora sanitizan con `ISanitizadorHtml` igual que Comentarios (`SanitizadorHtmlGanss` ya soportaba el mismo set de tags, no necesito cambios ahi). Compatibilidad con datos legado (Descripcion/Comentarios de Hallazgo eran texto plano antes de esto): `normalizarHtmlLegado` detecta si el valor YA es HTML (contiene una etiqueta) y si no, escapa y convierte saltos de linea a `<br>` antes de mostrarlo/editarlo -- transparente, no requiere migracion de datos. **Fuera de alcance deliberado**: el modal de ALTA (`NuevoItemModal.tsx`) sigue con Descripcion en texto plano -- el WorkItem no existe todavia en ese punto, no hay a que adjuntar imagenes pegadas (mismo motivo por el que no se puede comentar antes de crear); se vuelve rich text recien en la edicion. `CriteriosAceptacion` y el "Motivo" de reapertura de hallazgo quedan como texto plano (no se pidio ampliarlos) | Modal editar WorkItem + pestaña Descripcion del Detalle; modal "Reportar hallazgo" + listado de Revisiones |
| **Notificaciones y tiempo real** | Campana con notificaciones In-App (`tblNotificacion`) que llegan en vivo por SignalR (`NotificacionesHub`, un solo hub para notificaciones + refresco de tablero); disparadores: Solicitud aprobada/rechazada/devuelta/**convertida (2026-08-03, notifica al solicitante Y a cada usuario asignado de los items generados)** y @mencion en un comentario (notifica al mencionado). El tablero Kanban se refresca solo cuando cualquier WorkItem cambia de estatus, sin importar quien lo haya movido | campana en la barra superior (todas las pantallas) |
| **Mesa de ayuda (Tickets y SLA)** | Primer módulo de Fase 4, construido 2026-08-02 verificado extremo a extremo en LocalDB (no solo compilado): alta de ticket (folio TKT-año, estatus inicial Nuevo, SLA resuelto por prioridad con fechas límite de respuesta/resolución vía `ICalendarioLaboral`), bandeja de agentes (permiso `TKT.Atender`) con las 7 transiciones del proceso `Ticket` (asignar, iniciar atención, esperar usuario, reanudar, resolver, cerrar, reabrir) vía el motor de workflow existente, escalamiento a WorkItem tipo Soporte (acción de negocio fuera del motor -- no hay transición `ESCALAR` en `tblTransicion`, así que no aparece en el listado dinámico de acciones), y encuesta de satisfacción 1-5 del solicitante al Resuelto/Cerrado. El esquema de BD (`tblTicket`, `tblSla`, `tblEstatusTicket`, `tblCategoriaTicket`, `tblEncuestaSatisfaccion`, el proceso `Ticket` en `tblProceso`/`tblTransicion`, el permiso `TKT.Atender`) ya existía desde el despliegue inicial (script 06/01/02/09); esta sesión sembró `tblTransicionConfig` + categorías + SLA por defecto (script 12) y construyó las 4 capas de código + 3 pantallas nuevas. **Fuera de alcance de esta pasada** (ver también §3.4): sin pruebas automatizadas nuevas (solo verificación manual real en LocalDB); RN-SUP-02/03 (alertas de SLA al 80%/100%, cierre automático a 5 días hábiles) requieren Hangfire (A4, no construido); Base de conocimiento y "Derivar a ticket" desde Triage de Solicitudes quedan pendientes | Mis tickets (`/tickets`, portal), Mesa de ayuda (`/soporte`, bandeja), Detalle de ticket (`/tickets/:folio`) |
| **Mesa de ayuda: filtro de estatus, autoasignación y cierre por el ingeniero (2026-08-03)** | Tres ajustes pedidos por el negocio sobre el módulo anterior, verificados en vivo en LocalDB con dos cuentas reales (`aviramontes`/Administrador con `TKT.Atender`, `lgarcia`/Desarrollador sin el permiso): (1) **Filtro de Estatus en la bandeja de agentes** (`BandejaTicketsPage.tsx`) -- antes `ObtenerBandejaAsync` ocultaba `Cerrado` sin forma de verlos desde la UI aunque el backend ya soportaba `estatus=-1` (todos); se agregó el mismo selector multiple con "Todos" que ya existía en la Bandeja de trabajo de WorkItems (`BarraFiltros.tsx`), catálogo `EstatusTicket` nuevo en `CatalogosBandejaResponse`/`CatalogosQueryService`, default `[-1]` para mostrar todo de entrada. (2) **Autoasignación al registrar un ticket**: si quien lo crea ya tiene `TKT.Atender` (un ingeniero registrando su propio caso atendido en el momento, no un usuario autoreportándose), `CrearTicketHandler` ejecuta el mismo grafo de `ASIGNAR` (motor de workflow + `AsignarAsync`) para que el ticket nazca en Asignado con el propio ingeniero como responsable; un usuario sin el permiso sigue naciendo en Nuevo sin asignar, sin cambio de comportamiento. (3) **RESOLVER exige Solucion y MinutosSolucion**: dos columnas nuevas en `tblTicket` (script `16_2026-08-03_ALTER_tblTicket.sql`, corrido en LocalDB, pendiente en dev/preprod/prod), validadas en `CambiarEstatusTicketHandler` (rechaza la transición sin ambos datos, mismo patrón que RN-OPS-02 de Incidentes) y capturadas en un diálogo nuevo (Solución multilínea + minutos) tanto en la Bandeja como en el Detalle de ticket; con eso capturado, CERRAR (que ya existía) queda disponible con sentido de negocio completo. Sin pruebas automatizadas nuevas (solo verificación manual real en LocalDB, incluyendo el caso negativo de un usuario sin `TKT.Atender`) | Mesa de ayuda (`/soporte`), Detalle de ticket (`/tickets/:folio`) |
| **Ticket: Usuario solicitante y Locacion (2026-08-03)** | Dos catálogos nuevos, `tblUsuarioSolicitante` (Usuario/Nombre/Correo — gente que puede no tener cuenta de GTE) y `tblLocacion` (Locacion/Descripcion/Activo), creados a mano en producción sin script ni bitácora; se les agregó la bitácora estándar (scripts 17/18), un script de reproducibilidad para ambientes nuevos (19, no-op en producción) y dos columnas nuevas en `tblTicket` (`IdUsuarioSolicitante`, `IdLocacion`, script 20 — constraint del segundo FK renombrado a `FK_tblTicket_tblUsuarioSolicitanteCatalogo` para no chocar con el FK ya existente `IdSolicitante->tblUsuario`, que por historia se llama igual que el nombre "obvio"). El modal "Nuevo ticket" (`PortalTicketsPage.tsx`) muestra estos dos catálogos como Select opcionales SOLO si quien lo llena tiene `TKT.Atender`; se resuelven por nombre en `TicketResponse`/`TicketQueryService` y se muestran en el Detalle de ticket. Verificado en vivo en LocalDB con datos sembrados y con las dos cuentas reales (aviramontes los ve y los captura, lgarcia no los ve en el formulario). **Scripts 17, 18, 19 y 20 corridos en producción y confirmados funcionando** (2026-08-03) — nota real de despliegue: `tblLocacion.IdLocacion` en producción no tenía PRIMARY KEY (creada a mano sin ella), lo cual tronó el script 20 al crear el FK con "Could not create constraint or index" (Msg 1750); el equipo la corrigió directo en producción antes de re-correr el script | Mis tickets (`/tickets`), Detalle de ticket (`/tickets/:folio`) |
| **Solicitud/WorkItem: Usuario solicitante (2026-08-03)** | Mismo patrón que en Tickets, extendido a Solicitudes: `tblSolicitud.IdUsuarioSolicitante` (capturado opcionalmente al crear, SOLO visible para quien tiene `SOL.Triage` — un Líder registrando a nombre de otra persona) y `tblWorkItem.IdUsuarioSolicitante` (copiado automáticamente por `ConvertirSolicitudHandler` al convertir, mismo mecanismo con que ya se copian `IdSolicitante`/`IdSolicitud`) — script 21, mismo cuidado de nombrar el FK nuevo `FK_tblSolicitud_tblUsuarioSolicitanteCatalogo`/`FK_tblWorkItem_tblUsuarioSolicitanteCatalogo` para no chocar con el FK ya existente por rol (`FK_tblWorkItem_tblUsuarioSolicitante` es `IdSolicitante->tblUsuario`, verificado ANTES de escribir el script esta vez). Indicador "*" con tooltip en la bandeja de Triage cuando hay Usuario solicitante. Verificado en vivo extremo a extremo en LocalDB con datos reales migrados: Solicitud → Triage (Tomar/Aprobar a proyecto GTE) → Convertida en WorkItem, visible en su Detalle junto al Solicitante interno; confirmado que un usuario sin `SOL.Triage` no ve el campo. **Pendiente**: correr el script 21 en producción | Mis solicitudes (`/solicitudes`), Revisión de solicitudes (`/triage`), Detalle de WorkItem (`/wi/:folio`) |
| **Incidentes** | Segundo módulo de Fase 4, construido 2026-08-02, verificado extremo a extremo en LocalDB (no solo compilado): alta de incidente (folio INC-año, estatus inicial Detectado) dentro de un proyecto con severidad S1-S4, bandeja + detalle con las 5 transiciones del proceso `Incidente` (atender, mitigar, resolver, cerrar -- sin reapertura, un incidente siempre concluye en Cerrado) vía el motor de workflow existente, RN-OPS-02 (cerrar con severidad S1/S2 exige causa raíz capturada, validado y probado en vivo: el cierre se rechaza sin causa raíz y procede tras capturarla), RN-OPS-03 (cambio de severidad como acción de negocio aparte -- no es una transición de `tblTransicion` -- con motivo obligatorio), vincular WorkItem correctivo (crea un WorkItem tipo Corrección igual patrón que el escalamiento de Tickets, probado: creó `HELPDESK-3395`), y vincular un release ya existente como causante (reutiliza `GET /api/v1/releases?idProyecto=X`, insumo futuro de DORA Change Failure Rate). El esquema de BD (`tblIncidente`, `tblEstatusIncidente`, `tblSeveridad`, el proceso `Incidente` en `tblProceso`/`tblTransicion`, el permiso `INC.Gestionar`, `tblProyecto.IdResponsable`) ya existía desde el despliegue inicial; esta sesión sembró `tblTransicionConfig` (script 13) y construyó las 4 capas de código + 2 pantallas nuevas. **Fuera de alcance de esta pasada**: RN-OPS-01 completo (notificación a "todos los canales" -- solo existe InApp -- y escalamiento automático a 30 min sin atención, necesita Hangfire/A4; tampoco se notifica "al líder" por falta de una consulta usuarios-por-rol ya establecida) -- sí se implementó la notificación InApp inmediata al responsable del proyecto en incidentes S1; disponibilidad/% uptime mensual (reporte, Fase 5); monitoreo con health checks (Hangfire + catálogo de sistemas, no existe); `tblBitacoraCambio` ("qué cambió ayer") sigue sin UI, es bitácora general de PROD no específica de Incidentes; sin pruebas automatizadas nuevas | Incidentes (`/operacion/incidentes`, bandeja), Detalle de incidente (`/operacion/incidentes/:folio`) |
| **Portafolio: Costeo real y OKRs (A5, parcial)** | Construido 2026-08-02, verificado extremo a extremo en LocalDB: catálogo de tarifas por nivel con vigencia por fecha (alta/edición/baja lógica), presupuesto por proyecto/año, y reporte de costo real (`tblRegistroTiempo` × tarifa vigente del nivel del usuario a la fecha del registro, resuelta con `OUTER APPLY` en la vista `vwCostoRegistroTiempo` — nuevo patrón de vigencia, no existía uno previo en el código) comparado contra el presupuesto, con desglose por usuario. Probado en vivo contra datos históricos migrados reales del GT (proyecto PLANTILLA ANGULAR, usuario con 20h registradas × tarifa Junior $150/h = $3,000 exacto). OKRs: objetivos trimestrales por proyecto o equipo con resultados clave (meta/valor actual editado a mano, vínculo opcional a `ClaveKpi` para cuando exista el job de snapshot). Dos permisos nuevos sembrados (`POR.GestionarCosteo`, `POR.GestionarOkr`, módulo "Portafolio" en `tblPermiso`, script 14) — a diferencia de Tickets/Incidentes, este submódulo no tenía permiso previo. **Refinamiento 2026-08-02 (mismo día): ver tarifas/presupuesto/costo real ahora exige permiso aparte de administrarlos.** En vez de sembrar un tercer permiso redundante, se reutilizó `RPT.Costos` (ya sembrado en script 02, módulo "Indicadores", reservado para el futuro Dashboard Ejecutivo — su descripción "Ver reportes de costos y rentabilidad" calzaba exacto). Las 3 consultas de lectura (`ObtenerTarifasNivelQuery`, `ObtenerPresupuestosProyectoQuery`, `ObtenerCostoProyectoQuery`) exigen `RPT.Costos` **o** `POR.GestionarCosteo` (quien administra el catálogo también puede verlo); los Commands de alta/edición/baja siguen exigiendo solo `POR.GestionarCosteo` — ver no habilita editar. En el frontend, la pestaña Costeo se oculta completa si el usuario no tiene ninguno de los dos permisos (`PortafolioPage.tsx`, con mensaje "No tienes permiso para ver esta sección" en vez de dejar caer en una pestaña oculta al navegar directo a la URL — bug encontrado y corregido en el mismo repaso), y los botones de alta/edición/baja de tarifas y presupuesto se ocultan si falta específicamente `POR.GestionarCosteo` (`CosteoTab.tsx`, prop `puedeGestionar`). Verificado en vivo con dos cuentas reales: `aviramontes` (Administrador, tiene ambos permisos) ve y edita todo sin regresión; `lgarcia` (rol Desarrollador, sin ninguno de los dos) ve "No tienes permiso..." en la pantalla, el ítem "Portafolio" ni aparece en el menú lateral, y una llamada directa a `GET /api/v1/costeo/tarifas` con su token responde `403 FORBIDDEN`. **Fuera de alcance de esta pasada**: Riesgos (matriz probabilidad×impacto, ya tiene workflow sembrado en el motor) y la jerarquía Portafolio/Programa quedan para otra sesión (ver A5 en 3.2); "avance automático" de OKR ligado a KPIs depende del job nocturno de `tblKpiValor` (Hangfire/A4); sin pruebas automatizadas nuevas | Portafolio (`/portafolio`, pestañas Costeo/OKR) |
| **Manual de usuario (Ayuda)** | Pagina de ayuda estatica dentro de la SPA, en espanol simple para usuarios sin conocimiento tecnico: secciones en acordeon (login, menu, Mi Dia, bandeja, detalle de WorkItem, Solicitudes, el flujo completo de una solicitud hasta su cierre con diagrama SVG -- incluye las ramas de Rechazada/Devuelta y Hallazgos de QA --, otras secciones segun rol, contacto de soporte). Sin permiso (disponible para cualquier usuario autenticado); contenido fijo en el codigo, no hay editor -- actualizarlo requiere tocar `ManualUsuarioPage.tsx` | Ayuda (visible para todos en el menu) |
| **Menu lateral (2026-08-02)** | La navegacion se movio de una barra horizontal arriba a un panel lateral fijo del lado izquierdo (`Drawer` de MUI, `variant="permanent"`, `anchor="left"`), con la opcion activa resaltada segun la ruta actual. En pantallas chicas se colapsa a un boton de menu al inicio de la barra superior (junto al logo) que abre un cajon deslizable (`variant="temporary"`) que se cierra solo al navegar. La barra superior conservo el logo, la campana de notificaciones y el chip de usuario (con el nombre truncado en pantallas chicas para no empujar el boton de menu fuera de la vista). Se probo primero con `anchor="right"` (pedido inicial) y se corrigio a `anchor="left"` (decision final) -- ver leccion tecnica en la seccion 5 sobre por que el lado derecho encimaba el menu con el contenido | Panel lateral izquierdo (todas las pantallas) |

**Inventario:** 141 endpoints en 18 controladores + 1 hub de SignalR · 18 pantallas ·
16 scripts SQL (mínimo; recuento aproximado entre sesiones paralelas) · ~100 tablas
(+1, `tblRefreshToken`) · 53 pruebas (sin pruebas nuevas para Tickets, Incidentes ni
Portafolio, ver filas correspondientes abajo).

---

## 3. Pendientes

### 3.1 Bloqueantes para usar GTE con datos reales

Sin esto no se puede operar en producción, aunque el resto funcione.

| # | Pendiente | Detalle |
|---|---|---|
| ~~B1~~ | ~~**Módulo de Administración (CRUD)**~~ | **Resuelto 2026-07-31.** Proyectos, equipos+miembros, usuarios, roles (asignación+matriz en lote), horarios (tramos+festivos) y ambientes, con API completa (`AdministracionController`, 35 endpoints nuevos) y pantallas bajo `/admin` (6 pestañas). Ver detalle en la fila "Administracion" de la sección 2 y en la §3.4 lo que quedó deliberadamente fuera de alcance |
| ~~B2~~ | ~~**Autenticación en el SPA**~~ | **Resuelto 2026-08-01, cambio de alcance.** No habrá tenant de Entra ID (decisión del equipo: GTE maneja autenticación, accesos y roles totalmente dentro de sí mismo). Se construyó login propio: usuario+contraseña (BCrypt), bloqueo temporal, JWT + refresh rotativo en cookie HttpOnly, cambio de contraseña propio y reset por administrador. Ver fila "Autenticación" de la sección 2, ADR nuevo en la sección 4, y lo que queda fuera de alcance en la §3.4 (recuperar contraseña por correo, MFA, bootstrap del primer admin en un ambiente sin atajo de desarrollo) |
| ~~B3~~ | ~~**Migración de datos del GT (Núcleo + Usuarios/Roles), 2026-08-01**~~ | **Corte real ejecutado 2026-08-03 contra producción (`SRVPROD\NASA`)**, con el GT viejo ya congelado (sin escrituras nuevas en `bdsApollo` durante la ventana). Los 6 scripts (01-05 de `DataBase/Scripts/Migracion/` + 06 de backfill en `DataBase/Scripts/02_Libera/`) se corrieron en orden en SSMS con una cuenta de permisos suficientes (no `svc_gte`, que es de minimo privilegio); ningun script reporto `ROLLBACK`. **Hallazgo del corte real, no bug de la migracion**: el tiempo migrado se ve correcto en `tblRegistroTiempo` pero GTE no tenia forma de navegar de un WorkItem padre a sus hijos (la pestaña "Tiempo" del Detalle filtra por `IdWorkItem` exacto, sin rollup; ver `WorkItemQueryService.ObtenerTiemposAsync`) -- el script adjunta todo el tiempo migrado a los WorkItems HIJO (creados desde `tblSubtareas`), nunca al padre/raiz. Se construyo la pestaña **"Subtareas"** en el Detalle (endpoint nuevo `GET /api/v1/workitems/{id}/hijos`, `WorkItemHijoResponse` con `MinutosRegistrados` -- suma directa de `tblRegistroTiempo`, deliberadamente NO `MinutosInvertidos`/`vwTiempoInvertido`, que sale de `tblHistorialEstatus` y la migracion nunca llena para los hijos) para poder ver y entrar a cada subtarea migrada desde su padre. Verificado en vivo en LocalDB: WorkItem padre `REESTRUCTURACION TI-0003` (19 hijos) lista cada subtarea con folio/titulo/estatus/asignado/tiempo registrado, y entrar a una (`...-0032`) confirma el registro real (20m, Roberto Gonzalez) en su propia pestaña Tiempo. **Extension pedida en la misma sesion**: el usuario pregunto por que no podia AGREGAR subtareas nuevas (solo se habia construido la vista de las migradas) -- se agrego boton "Agregar subtarea" en esa pestaña, reutilizando el modal de alta ya existente (`NuevoItemModal`), con el Proyecto pre-llenado y bloqueado al del padre (prop `padre={{idWorkItem, folio, idProyecto}}`) y `idPadre` viajando al crear (el backend -- `CrearWorkItemCommand`/`WorkItemCrearRequest.IdPadre` -- ya lo soportaba, solo no estaba expuesto en ningun formulario). Requirio exponer `IdProyecto` (antes solo `ClaveProyecto`/`Proyecto` como texto) en `WorkItemResponse`. Probado en vivo: se creo `REESTRUCTURACION TI-0097` como hijo de `-0003` y aparecio de inmediato en la lista (Pendiente, sin asignar, 0m) -- queda en LocalDB a proposito, es un registro de prueba, no de produccion. Una subtarea es un WorkItem normal (misma tabla `tblWorkItem`, entidad unificada) con `IdPadre` distinto de NULL; no hay tabla `Subtarea` aparte. **Fuera de alcance de esta pasada**: sin rollup del tiempo de los hijos hacia el "Invertido" del padre (el indicador del padre sigue en `-` aunque sus hijos tengan tiempo registrado); Ausencias y Glosario/KB siguen fuera (ver abajo, sin cambios). Contexto historico de la migracion (avance previo, 2026-08-01): Corrección de nombre: la base origen documentada como `bdsInfo` es la MISMA que `bdsApollo` (renombrada con el tiempo — el header del backup confirma `bdsGP`→`bdsInfo`→`bdsApollo`; los scripts SQL viejos del GT y comentarios de código quedaron desactualizados). 4 scripts nuevos en `DataBase/Scripts` (`01_..._ALTER_tblWorkItem`, `02_..._MigracionUsuarios`, `03_..._MigracionCatalogos`, `04_..._MigracionSolicitudes`, `05_..._MigracionNucleo`), todos idempotentes (2 corridas limpias, la segunda solo `SKIP`). Alcance migrado: 4051 WorkItems raíz + 4541 hijos (de subtareas) + 4312 registros de tiempo + 12974 filas de historial de estatus + 19 Solicitudes (desde `tblEDM`, funcionalidad nueva del GT sin commitear que el negocio decidió tratar como Solicitudes) + 4 usuarios nuevos + 5 Equipos + Proyectos/Complejidad/Festivos/Horario `INTERFLO`. Gaps de esquema cerrados: columnas `Locacion` e `IdEquipo` (FK) agregadas a `tblWorkItem` (Documento Maestro preveía Locacion pero nunca se creó; `IdEquipo` nuevo porque la división/equipo resultó ser un atributo POR TAREA, no por proyecto — 45% de los proyectos reales mezclan divisiones). David Altamirano (Analista de Datos) excluido de la migración por decisión explícita del negocio. **Checksums**: conteos por estatus y suma de minutos por persona verificados contra el origen (2 de 4 usuarios exactos, 2 con variación <1% sin investigar a fondo — ver sección 5). **Fuera de esta pasada** (queda para otra sesión): Ausencias (`tblAusencia`/`tblAusencias`, tabla destino ya existe) y Glosario/KB (Glosario Interflo — GTE no tiene UI de Base de Conocimiento todavía, es Fase 4, y falta el equivalente de `tblGlosarioTag` para relaciones bidireccionales). ~~También fuera de alcance deliberado: conversión real de RTF a HTML (1264 comentarios quedan preservados íntegros sin parsear, marcados `[conversion visual pendiente]`)~~ **Resuelto 2026-08-02** (reportado por un usuario viendo el RTF crudo en la Descripcion de un WorkItem real, folio `EDM-0017`) — ver receta completa en la sección 5 (`ConversorRtf`, herramienta desechable con `RichTextBox`/Riched20 para parsear el RTF con la misma fidelidad que el control que lo generó). Las 1264 filas migradas quedaron con texto legible en LocalDB; **falta correrlo contra dev/preprod/prod** cuando se haga el corte real de cada uno. Vínculo de Release/Version histórico de tareas no-TI sigue fuera de alcance. ~~Recálculo de `MinutosLaborales` histórico vía `fnMinutosLaborales` (9041 filas de historial quedan con ese campo NULL)~~ **Resuelto 2026-08-02**, ver script `06_2026-08-02_UPDATE_tblHistorialEstatus.sql` y lección nueva en la sección 5 (causa raíz real: `vwTiempoInvertido` filtra `MinutosLaborales IS NOT NULL`, así que sin el backfill el indicador "Invertido" se veía en 0 para todo lo migrado aunque `tblRegistroTiempo` sí tuviera los 4312 registros — reportado por un usuario como "no se migraron los tiempos"). **Falta para el corte real**: repetir el ensayo contra un backup de producción fresco (no solo el que ya se usó), y decidir fecha de corte de fin de semana con el negocio (§15.4 punto 3) |
| ~~B4~~ | ~~**Despliegue**~~ | **Resuelto y CONFIRMADO 2026-08-01 contra un servidor real (`SRVPROD\NASA`): el Windows Service quedo corriendo, el primer login funciono y la contrasena de arranque ya se cambio.** Kestrel directo como Windows Service (sin IIS, sin Docker, sin CI/CD -- mismo patrón real que ya usa `Interflo.ServiceHealth`, ver decisión en la sección 4). La API sirve la SPA compilada en el mismo proceso (`wwwroot` + fallback). `publicar.bat` hace `npm run build` + `dotnet publish` + limpia la carpeta de destino + copia el build a `wwwroot`, todo en un solo paso. Tres herramientas para las variables de entorno del servicio, todas con merge seguro (nunca borran las demás): `generar-clave-jwt.bat` (genera `Jwt__ClaveFirma` sola), `configurar-variable-servicio.bat` (una variable cualquiera) y `configurar-servicio-completo.bat` (las tres de un jalón + reinicia el servicio). Login de BD de mínimo privilegio con **autenticación de SQL Server** (no de Windows) via `DataBase/Scripts/01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql`, y manual completo (`Doctos/MANUAL_INSTALACION_GTE.md`, calcado del formato real de ServiceHealth). `dotnet test` en 49/49 durante todo el proceso.<br><br>**Problemas reales encontrados y resueltos en la primera instalación real** (todos con lección correspondiente en la sección 5): (1) `sc start` daba error 1053 -- faltaba `UseWindowsService()`; (2) el `.exe` no arrancaba, el servidor no tenía el runtime 9.0 ("You must install or update .NET") -- se decidió retargetear todo el backend a **.NET 8** (ver ADR-02 actualizado en la sección 4) en vez de instalar el 9.0; (3) tras el retargeteo, un segundo intento de instalación trueno con `FileNotFoundException` de `System.Runtime` version 9.0.0.0 -- archivos del publish viejo mezclados con el nuevo en la carpeta instalada (`dotnet publish` no borra lo que ya no necesita); (4) `Falta Jwt:ClaveFirma` -- variable de entorno todavía no configurada; (5) al `bdsGTE` real le faltaba correr `01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql` (las columnas `PasswordHash`/`RequiereCambioPassword` no existían, `INSERT` fallaba con "Invalid column name"); (6) `tblUsuario` estaba vacía (base nueva, sin datos migrados del GT todavía) -- se creó el primer Administrador a mano con un `INSERT` + hash BCrypt generado con la misma librería de la API (ver receta en el manual, sección "Primer login"); (7) `Login failed for user 'NT AUTHORITY\SYSTEM'` -- el servicio no se había reiniciado después de configurar `ConnectionStrings__bdsGTE`, así que seguía usando el `Trusted_Connection=True` por default de `appsettings.json` contra `localhost` en vez de la cadena real. Fuera de alcance deliberado: sin pipeline de CI (se decidió mantener todo manual, igual que el resto del ecosistema) |

### 3.2 Alto valor, sin bloquear

| # | Pendiente | Detalle |
|---|---|---|
| ~~A1~~ | ~~**Comentarios y adjuntos**~~ | **Resuelto 2026-08-01.** Hilos de comentarios (formato básico + @menciones + imágenes pegadas) y adjuntos (subida/descarga por streaming autenticado) sobre WorkItem, API completa (`ComentariosController`, `ArchivosController`) y UI integrada en el Detalle. Ver fila "Comentarios y adjuntos" de la sección 2 y lo que quedó deliberadamente fuera de alcance en la §3.4 |
| ~~A2~~ | ~~**Edición de WorkItem en la UI**~~ | **Resuelto 2026-08-01.** Modal de edición (`ModalEditarWorkItem.tsx`) sobre el endpoint `PUT /workitems/{id}` ya existente: titulo, descripcion, criterios, prioridad, complejidad, asignado, compromiso y puntos. El boton "Editar" se oculta si el elemento esta Terminado o asignado a otra persona y el usuario no tiene el permiso correspondiente; las reglas campo-por-campo (compromiso al pasado, cambio de complejidad) las sigue validando el backend, su 403 se ve tal cual en el Snackbar. Se agrego el catalogo de Complejidades (`CatalogosBandejaResponse`) que no existia en ningun endpoint |
| ~~A3~~ | ~~**Notificaciones**~~ | **Resuelto 2026-08-01.** Alta In-App (`tblNotificacion`) + listar/marcar leida(s), disparada desde Solicitud aprobar/rechazar/devolver y @mencion en comentarios. `ICanalNotificacion`/`tblPlantillaNotificacion` quedan sin implementar (ver §3.4): solo canal InApp, mensajes inline |
| A4 | **Hangfire (trabajos en segundo plano)** | Vigilancia de SLA, snapshot de KPIs (`spSnapshotKpi` ya existe), recordatorios de compromiso, despacho del outbox `tblEventoDominio`, cierre automático de tickets |
| A5 | **Portafolio (parcial)** | ~~Costeo real (`tblTarifaNivel`, `tblPresupuestoProyecto`) y OKRs (`tblObjetivoOkr`, `tblResultadoClave`)~~ **Resueltos 2026-08-02**, ver fila en la sección 2. Pendiente: `tblPortafolio`/`tblPrograma` (jerarquía organizacional) y `tblRiesgo` (matriz de riesgos, ya tiene workflow sembrado en el motor — Identificado/En Mitigación/Materializado/Cerrado) |
| ~~A6~~ | ~~**SignalR**~~ | **Resuelto 2026-08-01.** `NotificacionesHub` unico (no dos como preveia el diseño original) para notificaciones en vivo (`Clients.User`) y refresco de tableros (`Clients.All` en `workItemActualizado`, sin grupo por equipo). Verificado con dos sesiones reales simultaneas en el navegador |
| ~~A7~~ | ~~**Flujo de aprobacion/rechazo de pruebas QA**~~ | **Resuelto 2026-08-02.** Confirmado con el usuario: las reglas se refieren al mini-flujo de QA que YA vive en el estatus del WorkItem (EnPruebas=3, TERMINAR/RECHAZAR_QA), no a una capa nueva en el modulo Calidad. Implementado en `CambiarEstatusWorkItemHandler`: (1) nuevo permiso `WI.AprobarPruebas` (seed para rol QA, script `15_2026-08-02_INSERT_bdsGTE_PermisoAprobarPruebas.sql`) exigido por datos en `tblTransicionConfig` para TERMINAR y RECHAZAR_QA con origen EnPruebas (el TERMINAR desde En Proceso, "proyectos sin fase QA", sigue libre); (2) `ValidarRevisionPruebasAsync` bloquea autoaprobacion/autorechazo (usuario == asignado) con 400; (3) rechazar sin un hallazgo (Revision) ya registrado y pendiente tambien da 400; (4) el gate de "item ajeno" (RN-REQ-05/WI.ModificarAjeno) se excluye a proposito para estas dos transiciones -- lo normal es que quien aprueba/rechaza NO sea el asignado. Bypass acotado de Administrador (`WI.OmitirValidacionCierre`) tambien cubre estos tres checks. Prueba E2E nueva contra LocalDB real: `WorkItemsApiTests.VerticalPruebasQa_AutoaprobacionPermisoYHallazgoSeValidan` (51/51 pruebas en verde, incluye tambien la regresion de `RegistrarTiempo_EnItemAjenoSeBloqueaSinPermiso`, ver seccion 5) |

### 3.3 Fases del roadmap que faltan completas

**Resto de Fase 3 — integración Git**
Diseñada tras la abstracción `IProveedorGit` (ADR-06) porque conviven Gitea (proyectos
internos) y GitHub (repositorio de GTE, ADR-09). Tablas listas: `tblRepositorio`,
`tblCommit`, `tblCommitWorkItem`, `tblPullRequest`, `tblPipelineEjecucion`, `tblArtefacto`.
Alcance: webhook entrante autenticado por secreto de repositorio, vinculación de commits al
WorkItem por folio en el mensaje, estado de PR, botón "crear rama", registro de pipelines y
transiciones automáticas configurables.

**Fase 4 — Operación y Soporte**
- ~~Incidentes (`tblIncidente`)~~ **Resuelto 2026-08-02.** Ver fila "Incidentes" en la
  sección 2. Pendiente dentro de este sub-alcance: disponibilidad mensual (es un reporte,
  Fase 5) y monitoreo con apertura automática (Hangfire/A4).
- ~~Mesa de ayuda (`tblTicket`, `tblSla`)~~ **Resuelto 2026-08-02.** Ver fila "Mesa de
  ayuda (Tickets y SLA)" en la sección 2. Pendiente dentro de este sub-alcance: pausa
  real de SLA en "Esperando Usuario" más allá del estatus (RN-SUP-01 el reloj se detiene
  conceptualmente pero no hay job que recalcule la fecha límite al reanudar), alertas
  80%/100% y cierre automático (RN-SUP-02/03, necesitan Hangfire/A4).
- Base de conocimiento (`tblArticuloConocimiento`, `tblArticuloVersion`): incluye la
  migración del Glosario Interflo del GT con sus imágenes y tags de redirección.

**Fase 5 — Ejecutivo, automatizaciones e IA**
- Dashboard ejecutivo: KPIs, OKRs, DORA metrics, costo y rentabilidad por proyecto,
  retrabajo, CSAT. `tblKpiDefinicion`/`tblKpiValor` y `spSnapshotKpi` ya existen.
- 14 reportes del catálogo (§13 del diseño) y vistas `vwBI*` para Power BI.
- 23 automatizaciones de fábrica (§7.2) sobre `tblReglaAutomatizacion` con constructor
  visual de reglas.
- 11 funciones de IA (§14), empezando por IA-01: sugerir el desglose en historias al
  aprobar una solicitud.

### 3.4 Detalles menores conocidos

- **Autenticación propia, fuera de alcance deliberado de esta entrega:** "olvidé mi
  contraseña" por correo (no hay SMTP configurado todavía), MFA (el diseño original lo
  delegaba a Entra ID; sin Entra queda pendiente, ej. TOTP si se quiere más adelante), y el
  bootstrap del primerísimo password de un Administrador en un ambiente de producción real
  sin el atajo de desarrollo disponible (por ahora: UPDATE directo a la BD, o arrancar ese
  primer login en Development). Los usuarios existentes antes de este cambio nacen con
  `PasswordHash = NULL` y `RequiereCambioPassword = 1`: no pueden usar `/auth/login` hasta
  que un administrador les restablezca la contraseña (o ellos mismos, vía el atajo de
  desarrollo + cambio propio, si el ambiente lo permite).
- **Administracion, fuera de alcance deliberado de esta entrega:** CRUD de roles nuevos (los 8
  roles semilla ya cubren los perfiles del sistema; `EsSistema` sugiere que no se crean desde
  UI), CRUD de Areas/Puestos (catalogos simples, sin pantalla propia), gestion de
  Ausencias/vacaciones (mencionada en el Documento Maestro §3.1 pero no en el alcance de esta
  sesion), Repositorios Git (tabla `tblRepositorio` lista, sin API), y "Version del sistema".
  Los catalogos de Area/Puesto/Nivel/Horario ya se leen para los selects de Usuarios, solo
  falta un alta propia si se necesita crear valores nuevos desde la UI en vez de por SQL.
- `QaPage`: el alta de caso solo captura **un paso**; falta editar casos para agregar más.
- `tblEtiqueta` sin uso (etiquetas libres para WorkItems).
- Cadena de aprobación de releases fija (`QA`, `Líder`, `Negocio`); el diseño la quiere
  configurable por proyecto.
- RN-PLA-01 (avisar si el sprint se compromete por encima de la velocidad histórica +20%)
  no implementado; la capacidad sí se compara contra horas.
- `spImportarJira` planeado, no escrito.
- Suplantación auditada (permiso `ADM.Suplantar` ya sembrado) sin implementar.
- Tema oscuro y revisión de accesibilidad (WCAG AA) pendientes.
- ~~El arrastre de tarjetas del kanban no se pudo verificar con ratón real~~ **Verificado
  2026-08-01.** Sí funciona: se simuló un arrastre real (`PointerEvent` sintético con
  `pointerdown`/`pointermove`/`pointerup`, `isPrimary: true`, `button: 0`) moviendo
  GTE-0006 de "En proceso" a "En pruebas" — `PUT /workitems/{id}/columna` respondió 200 y
  el tablero reflejó el cambio. El intento anterior fallaba por dos motivos tecnicos, no
  por un bug de la app (ver lección nueva en la sección 5): `requestAnimationFrame` no
  corre si el Browser pane no esta compositando, y sin ceder el hilo entre cada
  `pointermove` React nunca llega a recalcular la colisión antes del `pointerup`.
- **Comentarios y adjuntos, fuera de alcance deliberado de esta entrega:** sin permiso de
  admin/líder para borrar comentarios o adjuntos ajenos (solo el propio autor puede,
  `ForbiddenException` en cualquier otro caso; agregar `COM.EliminarAjeno` si se necesita,
  siguiendo el patrón de `WI.ModificarAjeno`), sin edición de un comentario ya publicado
  (solo alta + baja lógica), sin notificación real al mencionar `@usuario` (los `data-id`
  quedan marcados inline en el HTML guardado, listos para que A3 los lea el día que exista),
  y sin antivirus sobre el almacén de archivos (mencionado como opcional en el Documento
  Maestro §8.5). El almacén (`AlmacenArchivosDisco`) usa una carpeta local por defecto
  (`AlmacenArchivos:Ruta` vacío cae a una subcarpeta junto al ejecutable); para producción
  hay que apuntarlo al share de red real.
- **Catálogo de Complejidades sin semilla de datos:** `tblComplejidad` existe y ya se
  expone en `GET /catalogos/bandeja`, pero ningún script de despliegue le carga filas — en
  un ambiente nuevo el select de Complejidad en el modal de edición de WorkItem apareceria
  vacío (no bloquea nada, el campo es opcional). Si se necesita, es un script `INSERT` de
  datos, no de esquema.
- **Edición de WorkItem, fuera de alcance deliberado de esta entrega:** el modal de alta
  (`NuevoItemModal.tsx`) sigue sin captura de complejidad ni puntos de historia (no se pidió
  ampliarlo); no se introdujo deshabilitado de campos individuales por permiso (el backend
  revalida cada regla y su 403 se muestra tal cual, consistente con el resto de la app).
- **Notificaciones y SignalR, fuera de alcance deliberado de esta entrega:** sin canales
  Correo/Teams/WhatsApp (`ICanalNotificacion` sigue sin implementación, reservado para
  cuando existan credenciales externas), sin `tblPlantillaNotificacion` (mensajes armados
  inline en cada disparador), sin disparador de Solicitud convertida ni de Release
  liberado (quedan para otra sesión), sin grupo por equipo en el broadcast de tablero
  (`Clients.All`, la escala del ERP no lo justifica hoy), sin eliminar/editar
  notificaciones (solo alta + marcar leída) y sin preferencias de canal/evento por usuario
  (el Documento Maestro las menciona en el perfil pero no hay tabla para ellas).

---

## 4. Decisiones firmes (no cambiar sin acuerdo del equipo)

| ADR | Decisión |
|---|---|
| 02 | .NET 8 (retargeteado desde .NET 9 el 2026-08-01, ver detalle abajo) + React. React **sigue divergiendo** del estándar del Frente B (Angular): pendiente ratificar y actualizar `InterfloClaude.md`. El backend en .NET 8 ya **no** diverge -- se alinea con el estándar |
| 03 | **`bdsGTE` es la única base.** Motor de estatus y folios propios; cero dependencia de `bdsCentral` u otra base |
| 04 | El workflow vive en datos (`tblProceso`/`tblTransicion`). Alta de procesos = filas, nunca tocar `spCambiarEstatus` |
| 06 | Integración Git tras `IProveedorGit` (conviven Gitea y GitHub) |
| 09 | El código de GTE vive en **GitHub**, de forma definitiva. Excepción deliberada al estándar del ecosistema; no proponer migración a Gitea |
| — | El frontend **nunca decide transiciones**: pide las acciones válidas al motor y envía acciones, jamás estatus destino |
| — | El esquema lo gobiernan los scripts de `DataBase/Scripts` (idempotentes). **No usar migraciones de EF**; tras cambiar el esquema, re-scaffold |
| — | MediatR 12.5.0 y AutoMapper 14.0.0 fijados por licencia libre; no subir de major sin decisión |
| — | **GTE no usa Entra ID ni ningún proveedor de identidad externo** (decisión del equipo, 2026-08-01): reemplaza la intención original de B2. Autenticación 100% propia dentro de `bdsGTE` (usuario+contraseña BCrypt, JWT propio, refresh rotativo). Un solo JWT HMAC para todo el sistema: el atajo de desarrollo y el login real emiten el mismo tipo de token (`IEmisorTokenSesion`), nunca dos mecanismos distintos |
| — | **Despliegue de GTE: Kestrel directo como Windows Service** (decisión del equipo, 2026-08-01), sin IIS, sin reverse proxy, sin Docker y sin pipeline de CI/CD -- mismo patrón real que ya usa `Interflo.ServiceHealth` en producción (publicación manual con `.bat` + `sc create` + variables de entorno para secretos). La API sirve también la SPA compilada en el mismo proceso (`wwwroot` + `MapFallbackToFile`). **Diverge deliberadamente** del diagrama de la sección 1.1 del Documento Maestro (que preveía IIS ARR/YARP + Redis): esa arquitectura queda como visión de escalamiento a futuro (fase N, multi-instancia); la topología mínima fase 1 (1 servidor de aplicaciones) no la necesita. Revisar/actualizar ese diagrama si el equipo decide escalar |
| — | **La API se conecta a `bdsGTE` con autenticación de SQL Server (login propio `svc_gte`), NO con autenticación de Windows** (decisión del equipo, 2026-08-01). Se intentó primero un login de Windows (`FROM WINDOWS`) para la cuenta de servicio, pero una cuenta local de una máquina no es resoluble desde un SQL Server que viva en otra máquina/VM (error 15401) -- forzaría a coordinar una cuenta de dominio con un administrador de AD, justo la clase de dependencia externa que el equipo quiere evitar (coherente con "GTE no usa Entra ID ni ningún proveedor de identidad externo", misma fila de arriba, ahora extendido también a la capa de infraestructura de datos). Requiere que el SQL Server destino tenga habilitado el modo mixto ("SQL Server and Windows Authentication mode"). El login se aprovisiona con `DataBase/Scripts/01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql` |

---

## 5. Trampas técnicas ya pagadas (no repetir)

- **EF y columnas `bit`**: el `DEFAULT 1` de la base no aplica en los INSERT de EF. Toda
  alta debe fijar `Activo = true` explícitamente.
- **EF y proyecciones intermedias**: filtrar u ordenar sobre un DTO/record ya proyectado da
  error 500 (LINQ no traducible). Patrón correcto: unir entidades sin proyectar, filtrar por
  columnas reales, proyectar al final con `Expression<Func<T,TResult>>` (ver
  `PlaneacionQueryService`).
- **EF y SPs con valor de retorno**: `ExecuteSqlRaw` no sirve; usar `DbCommand` con
  `ParameterDirection.ReturnValue` (ver `MotorWorkflow`).
- **MUI 9**: `justifyContent`, `alignItems`, `flexWrap` y `display` van dentro de `sx`, no
  como props.
- **Fechas `DateOnly`**: `new Date("2026-07-31")` se interpreta como UTC y muestra el día
  anterior; agregar `T00:00:00`.
- **Centinelas de identidad**: nunca usar un texto como `"anonimo"` para "sin identidad" —
  puede coincidir con una cuenta real y confundir la auditoría. Se usa cadena vacía.
- **Transiciones**: `TERMINAR` de un hallazgo solo procede desde En Proceso; si está
  Pendiente hay que ejecutar `INICIAR` antes, no forzar el salto.
- **TypeScript del template Vite**: `erasableSyntaxOnly` prohíbe parameter properties en
  constructores.
- **CTE recursivo para validar ciclos (RN-ADM-01)**: EF no expresa CTEs recursivos; se resuelve
  con `DbCommand` crudo parametrizado (mismo patron que `MotorWorkflow`/`GeneradorFolios`), no
  con `SqlQuery<T>` de EF 8/9 (mas simple pero no se probo aqui). Ver
  `AdministracionRepository.FormariaCicloJerarquiaAsync`.
- **`await x ?? throw ...;` como sentencia suelta no compila** (CS0201): hay que asignarlo,
  aunque sea a un descarte (`_ = await consultas.ObtenerXAsync(...) ?? throw new
  NotFoundException(...);`).
- **Icono `DeleteOutline` de `@mui/icons-material`**: esta version del paquete no trae la
  variante sin sufijo; usar `@mui/icons-material/DeleteOutlineOutlined`.
- **`Typography` con `display="block"`** ya no es una prop valida en MUI 9 sin `component`;
  usar `sx={{ display: "block" }}`.
- **Automatizacion de navegador (Browser pane) y `Tabs` de MUI**: en un entorno sin
  compositing real, el click por coordenadas de `computer` no siempre dispara el `onChange`
  de un `Tab` (ripple/touch handling); si una pestaña no cambia visualmente pero tampoco hay
  error, probar `elemento.click()` via `javascript_tool` antes de asumir que el componente
  esta roto.
- **Cookie de refresh `SameSite=None; Secure` entre `localhost:5173` y `localhost:5088`
  (puertos distintos = origenes distintos) SI viaja en Chrome/Edge**, sin HTTPS: el
  navegador trata `http://localhost` como contexto seguro. Se verifico en vivo (no se
  asumio): `document.cookie` la mantiene invisible (confirma `HttpOnly`) y aun asi
  `/auth/logout`/`/auth/refresh` la reciben. Requiere `axios.create({ withCredentials:
  true })` en el cliente y `.AllowCredentials()` en la politica de CORS (incompatible con
  origenes wildcard, pero ya se usa una lista explicita).
- **Interceptor de refresh silencioso en axios**: en el 401, revisar la URL de la peticion
  que fallo antes de reintentar -- si la peticion original YA era `/auth/refresh`,
  `/auth/login`, etc., no reintentar (bucle sin sentido). Un solo refresh en vuelo
  compartido (`Promise` memoizada) si varias peticiones truenan con 401 al mismo tiempo.
- **BCrypt.Net-Next** (no `BCrypt.Net`) es el paquete correcto; y la construccion de JWT
  (`System.IdentityModel.Tokens.Jwt`/`Microsoft.IdentityModel.Tokens`) no llega gratis a
  `GTE.Infrastructure` solo por ser dependencia transitiva de `GTE.WebApi` -- cada proyecto
  necesita su propia referencia si construye/valida tokens.
- **Mensajes de error genericos en login**: "usuario o contraseña incorrectos" debe ser
  identico tanto si el usuario no existe como si la contraseña esta mal (evita enumeracion
  de cuentas validas por diferencia de mensaje/tiempo de respuesta).
- **HTML enriquecido de usuario, sanitizar siempre en el backend**: el front nunca es la
  ultima linea de defensa. `HtmlSanitizer` (Ganss.Xss) con `AllowedTags`/`AllowedAttributes`
  explicitos (sin `src` en `img`) y `AllowDataAttributes = true` para permitir `data-guid`/
  `data-id` (menciones e imagenes) sin abrir la puerta a atributos arbitrarios.
- **Imagenes pegadas en contenido enriquecido, nunca por URL directa**: el HTML persistido
  solo guarda `data-guid`; ni el editor (NodeView de TipTap) ni la vista de solo lectura
  usan `<img src="...">` contra el endpoint -- ambos piden el blob autenticado por
  `axios` (header `Authorization`, `responseType: "blob"`) y arman un `ObjectURL` en
  cliente. **`dangerouslySetInnerHTML` no ejecuta NodeViews de React**: la vista de solo
  lectura de un comentario ya guardado necesita su propio `useEffect` que busque
  `img[data-guid]` en el DOM renderizado y resuelva el blob a mano (ver
  `ContenidoComentario` en `PanelComentarios.tsx`); no basta con que el editor sepa
  mostrarlas.
- **TipTap v3 (no v2)**: `@tiptap/suggestion` cambio el patron de posicionamiento del popup
  de menciones -- ya no hace falta `tippy.js` a mano, `SuggestionProps.mount(elemento)`
  monta y reposiciona solo (Floating UI por debajo), devuelve un `unmount()` para llamar en
  `onExit`. `@tiptap/react` reexporta todo `@tiptap/core` (`Node`, `mergeAttributes`,
  `NodeViewProps`, etc.), no hace falta importarlos de `@tiptap/core` por separado.
- **Adjuntos multipart con el cliente axios compartido**: `http.ts` fija
  `Content-Type: application/json` por defecto en la instancia; una subida con `FormData`
  necesita pisarlo explicitamente a `undefined` en esa llamada puntual para que el
  navegador calcule el boundary multipart solo (ver `subirArchivo` en
  `shared/api/archivos.ts`).
- **Verificacion real de paste de imagen en el Browser pane**: un `ClipboardEvent`
  sintetico con `DataTransfer` SI se puede construir y despachar por script contra el
  editor -- permitio probar de punta a punta la subida por pegado sin depender del
  portapapeles real del sistema operativo.
- **Simular un drag de dnd-kit (`PointerSensor`) por script, en el Browser pane**: SI se
  puede, con tres detalles que no son obvios:
  1. Hay que despachar `PointerEvent` reales (`new PointerEvent('pointerdown', {isPrimary:
     true, button: 0, pointerId, clientX, clientY, bubbles: true})`), no `MouseEvent` --
     `PointerSensor` solo escucha `pointerdown`/`pointermove`/`pointerup`.
  2. Con `activationConstraint: {distance: N}`, el PRIMER `pointermove` que supera el
     umbral solo activa el arrastre (no cuenta como movimiento); hacen falta moves
     posteriores para que la deteccion de colision (`over`) se actualice.
  3. **Nunca despachar todos los eventos en una sola rafaga sincrona**: dnd-kit actualiza
     su estado interno (`over`, colisiones) via `dispatch`/render de React, que no ocurre
     entre llamadas sincronas seguidas -- hay que ceder el hilo entre cada evento
     (`await new Promise(r => setTimeout(r, 20-30))`). `requestAnimationFrame` NO sirve
     para esto en el Browser pane: no corre si la pestaña no esta compositando/visible: use
     `setTimeout`, que si corre.
- **JWT propio + SignalR**: el claim `sub` del token (`EmisorTokenSesion.cs`) se mapea por
  defecto a `ClaimTypes.NameIdentifier`, así que `Clients.User(idUsuario.ToString())`
  funciona sin grupos manuales -- no hace falta que el Hub trackee membresías el mismo.
  El WebSocket del navegador no puede mandar el header `Authorization`: hace falta
  `Events.OnMessageReceived` en `AddJwtBearer` leyendo `access_token` del query string,
  limitado por path (`/hubs`) para no aceptar tokens por query en el resto de la API.
- **`IHubContext<T>` no puede vivir en Infrastructure** sin que esa capa dependa de
  hosting de ASP.NET Core: el contrato (`INotificadorTiempoReal`) se define en
  Application como siempre, pero la implementación concreta se registra desde `GTE.WebApi`
  (única capa que conoce el Hub) -- única excepción al patrón "implementación en
  Infrastructure" del resto del proyecto, y es correcta, no un atajo.
- **`useEffect` + conexión SignalR bajo `StrictMode`**: el doble-invoke de efectos en
  desarrollo crea y detiene una primera conexión antes de crear la definitiva -- aparece
  `Error: The connection was stopped during negotiation.` en consola, es ruido esperado
  (la segunda conexión sí queda viva), no un bug real.
- **`ListItemButton`/`MenuItem` de MUI no son `<button>` nativos**: renderizan como `div`
  con `role="button"` (`MuiButtonBase-root`) -- un `querySelectorAll('button')` para
  clicar una opción de una lista/menu de MUI por script no la encuentra; hay que buscar
  por clase (`.MuiListItemButton-root`, `.MuiMenuItem-root`) o por `[role="button"]`.
- **Verificar tiempo real con dos sesiones en el Browser pane**: `sessionStorage` es por
  pestaña, así que dos pestañas (`tabs_create`) pueden loguearse como usuarios distintos
  al mismo tiempo -- suficiente para probar un push a un usuario específico o un
  broadcast sin depender de un segundo navegador real. Ojo: las herramientas que no
  reciben `tabId` explícito actúan sobre la pestaña *frontada* (`tabs_select`), no sobre
  la última usada -- hay que pasar `tabId` explícito en cada llamada cuando se alterna
  entre pestañas o se leen resultados de la pestaña equivocada.
- **`MapFallbackToFile` usa la restricción implícita `:nonfile`**: una ruta como
  `/assets/app.js` (tiene extensión) NUNCA la matchea como endpoint -- por diseño, para
  que un archivo estático faltante dé 404 en vez de servir `index.html` por error. Con un
  `FallbackPolicy` global que exige autenticación (`RequireAuthenticatedUser`), esto
  significa que una petición a un archivo real **sin endpoint** cae directo en el
  `FallbackPolicy` y se bloquea con 401 -- pasó en vivo con los `.js`/`.css` del build de
  React. La solución real no es marcar el fallback con `AllowAnonymous` (eso solo cubre
  rutas de cliente sin extensión, ej. `/proyectos/123`): hay que colocar
  `UseDefaultFiles()`/`UseStaticFiles()` **antes** de `UseCors`/`UseAuthentication`/
  `UseAuthorization` en el pipeline, para que un archivo físico se sirva y corte el
  pipeline ahí mismo, sin llegar nunca al `FallbackPolicy`. El `MapFallbackToFile(...)`
  en sí *sí* necesita `.AllowAnonymous()` explícito (para las rutas de cliente sin
  archivo), porque el shell de la SPA tiene que cargar sin sesión -- es lo que muestra la
  pantalla de login (ver `Program.cs`).
- **`UseStaticFiles()` tolera un `wwwroot` faltante en tiempo de ejecución** (solo un WARN
  en el log), **pero `WebApplicationBuilder` NO**: el paso interno
  `StaticWebAssetsLoader.UseStaticWebAssets` (parte del arranque, corre en cualquier
  ambiente, no solo Development) construye un `PhysicalFileProvider` sobre `wwwroot` y
  **truena con `DirectoryNotFoundException` si la carpeta no existe físicamente** -- rompió
  las 24 pruebas de `GTE.Api.Tests` (`WebApplicationFactory` construye la app real) hasta
  que se agregó un `wwwroot/.gitkeep` versionado (con `.gitignore` ajustado a
  `wwwroot/*` + `!wwwroot/.gitkeep`, no a la carpeta completa). Cualquier proyecto que
  sirva una SPA desde `wwwroot` necesita la carpeta trackeada de antemano, no solo
  generada al publicar.
- **`THROW;` (sin argumentos, para relanzar en un `CATCH`) exige que la sentencia
  inmediatamente anterior termine en punto y coma** -- si no, error de sintaxis
  ("Incorrect syntax near 'THROW'") que además señala una línea equivocada (la del
  `PRINT` anterior, no la del propio `THROW`), lo que hace más difícil detectar la causa
  real a simple vista. Verificado en vivo contra LocalDB con un repro mínimo. Revisar
  cualquier bloque `CATCH` nuevo que combine `PRINT` sin `;` seguido de `THROW`.
- **Crear un login de SQL Server y su usuario en una base (`CREATE USER ... FOR LOGIN`)
  son operaciones en ámbitos distintos** (`master` vs. la base de datos): un script de
  aprovisionamiento de cuenta de servicio legítimamente necesita `USE [master]` para el
  login y `USE [bdsGTE]` para el usuario/permisos -- excepción documentada al invariante
  "todos los scripts de esta carpeta corren solo contra bdsGTE" (ver
  `DataBase/Scripts/README.md`). Si se prueba esto localmente, ojo: usar la propia cuenta
  de Windows que ya es `dbo` de la base de prueba falla con "The login already has an
  account with the user name 'dbo'" al día de crear el `USER` -- no es un bug del script,
  es que esa cuenta ya tiene una asignación en esa base; para probar de verdad hace falta
  un login distinto al dueño de la base.
- **`CREATE LOGIN ... FROM WINDOWS` exige que la cuenta sea resoluble por el SQL Server
  destino** (error 15401 "Windows NT user or group ... not found" si no) -- una cuenta
  LOCAL de una máquina (`EQUIPO\usuario`) solo existe para Windows en ESA máquina; si el
  SQL Server real vive en otro servidor/VM, nunca la va a poder validar, sin importar que
  la cuenta exista y esté bien escrita. Se descubrió probando el script de aprovisionamiento
  contra un SQL Server real distinto de la máquina de desarrollo. **Se decidió pivotear a
  autenticación de SQL Server** (login propio `svc_gte` con password, ver decisión en la
  sección 4) precisamente para no depender de que servidor de aplicaciones y SQL Server
  compartan dominio/AD -- requiere que el SQL Server destino tenga habilitado el modo
  mixto ("SQL Server and Windows Authentication mode"), verificado y documentado en
  `Doctos/MANUAL_INSTALACION_GTE.md`.
- **Guard de "no dejar el valor de ejemplo" con `RAISERROR` al inicio de un script de
  aprovisionamiento**: comparar la variable sensible (`@Password`) contra el placeholder
  literal y abortar con un mensaje claro si coinciden -- barato de escribir y evita correr
  el script contra un ambiente real con una contraseña de ejemplo por descuido. Patrón
  reutilizable para cualquier script futuro con un valor que el operador DEBE cambiar.
- **`sc start` truena con ERROR 1053 ("El servicio no respondio a tiempo...") si el `.exe`
  publicado no tiene integración con el Service Control Manager**: un Kestrel/consola
  normal (lo que produce `dotnet publish` por default) arranca bien pero nunca le avisa a
  Windows que ya quedó `RUNNING` -- Windows espera la respuesta, se cansa, y mata el
  intento, sin importar que la API en sí funcione perfecto si se corriera a mano. Se
  descubrió en vivo al instalar el servicio real por primera vez. Arreglo: paquete
  `Microsoft.Extensions.Hosting.WindowsServices` + `builder.Host.UseWindowsService();` al
  inicio de `Program.cs` (antes de cualquier otro `builder.Host...`) -- es un no-op cuando
  NO se corre como servicio (`dotnet run`, `WebApplicationFactory` de las pruebas), así que
  no rompe nada en desarrollo ni en pruebas (49/49 siguen en verde). **Lección para
  cualquier API .NET nueva del ecosistema que se vaya a instalar como Windows Service**:
  agregar esto desde el principio, no hasta el primer despliegue real.
- **.NET no hace fallback entre versiones mayores del runtime**: el servidor real
  (`SRVPROD\NASA`) tenía instalados .NET 8.0.29 y 10.0.10, pero el `.exe` (compilado para
  `net9.0`) se negó a arrancar ("You must install or update .NET to run this
  application") porque el 9.0 exacto no estaba. Se descubrió en vivo al instalar el
  servicio real. **Se decidió retargetear todo el backend de `net9.0` a `net8.0`** (los 7
  `.csproj` del repo + los paquetes `Microsoft.*` pineados a `9.0.*`/`9.0.0` que van
  atados a la versión mayor del runtime: `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Microsoft.EntityFrameworkCore.Design`/`SqlServer`,
  `Microsoft.Extensions.Configuration.Abstractions`,
  `Microsoft.Extensions.Hosting.WindowsServices`, `Microsoft.AspNetCore.Mvc.Testing`) en
  vez de instalar el runtime 9.0 en el servidor -- decisión del equipo, no solo por
  conveniencia: **.NET 9 es STS (soporte corto, ~18 meses) mientras que .NET 8 y .NET 10
  son LTS (3 años)**, y .NET 8 además ya es el estándar documentado del resto del
  ecosistema (Frente B), así que esto resuelve de paso la divergencia que ADR-02 ya tenía
  marcada como "pendiente ratificar". El retargeteo compiló limpio a la primera (0
  errores) y las 49 pruebas siguieron en verde -- no se usaba ninguna sintaxis de C# 13
  específica de `net9.0`. Ver ADR-02 actualizado en la sección 4 y en
  `InterfloClaude.md`.
- **`dotnet publish -o carpeta` no borra archivos que la version nueva ya no necesita** --
  solo agrega/sobreescribe. Tras el retargeteo de `net9.0` a `net8.0`, un segundo intento
  de instalación real truenó con `FileNotFoundException: Could not load file or assembly
  'System.Runtime, Version=9.0.0.0...'` aunque el runtime instalado ya era el 8.0.29
  correcto -- quedaron ensamblados del publish viejo (compilados contra `net9.0`)
  mezclados con los nuevos en la misma carpeta, tanto en el publish local como en la
  carpeta ya instalada en el servidor. Arreglo en dos frentes: `publicar.bat` ahora
  borra su carpeta de destino ANTES de publicar (paso 1/4), y el manual instruye borrar
  el CONTENIDO COMPLETO de la carpeta instalada en el servidor antes de copiar cualquier
  publish nuevo, no solo sobreescribir. Aplica a cualquier cambio de `TargetFramework` o
  de dependencias mayores, no solo a este caso puntual.
- **Las variables de entorno de un Windows Service solo se cargan cuando el PROCESO
  arranca**, no en caliente: configurar `ConnectionStrings__bdsGTE` (o cualquier otra) sin
  reiniciar el servicio despues deja corriendo la version vieja del ambiente. Se
  manifestó como `Login failed for user 'NT AUTHORITY\SYSTEM'. Reason: Failed to open the
  explicitly specified database 'bdsGTE'` -- el servicio (corriendo como `LocalSystem`
  porque no se especificó `obj=` en `sc create`) seguía usando el `Trusted_Connection=True`
  contra `localhost` que trae `appsettings.json` por default, en vez de la cadena real con
  el login SQL `svc_gte`. **Pista de diagnostico util**: si el error de conexión muestra
  una cuenta de Windows (`NT AUTHORITY\SYSTEM`, `NT AUTHORITY\NETWORK SERVICE`, etc.) en
  vez del login de SQL Server esperado, la variable de entorno no se está aplicando --
  casi siempre falta un reinicio del servicio. `configurar-servicio-completo.bat` ya
  reinicia solo al final para evitar este error exacto; la ruta variable-por-variable
  (`configurar-variable-servicio.bat`) requiere `sc stop`/`sc start` a mano después.
- **Bootstrap del primer usuario en una `bdsGTE` real recién creada**: la tabla
  `tblUsuario` nace vacía (no hay migración de datos del GT todavía, B3 sigue pendiente),
  así que ni siquiera existe un Administrador para entrar y usar "Restablecer
  contraseña". Se resolvió con un `INSERT` manual directo (`tblUsuario` +
  `tblUsuarioRol` contra el rol `Administrador` por nombre, no por Id hardcodeado) y un
  hash BCrypt generado con la MISMA versión de `BCrypt.Net-Next` que usa la API (un
  proyecto de consola desechable en el temp, nunca commiteado), con
  `RequiereCambioPassword = 1` para forzar que la contraseña temporal se cambie en el
  primer login -- confirmado que el flujo de cambio obligatorio funciona de verdad. Antes
  de este `INSERT`, hacía falta haber corrido ya
  `01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql` (agrega `PasswordHash`/
  `RequiereCambioPassword` a `tblUsuario`) -- si no, el `INSERT` falla con "Invalid column
  name". Receta completa (genérica, sin el hash real de nadie) en
  `Doctos/MANUAL_INSTALACION_GTE.md`, sección "Primer login en un ambiente nuevo".
- **`Drawer` `variant="permanent"` de MUI: el placeholder que reserva espacio en el layout
  flex NO necesariamente coincide con el lado donde aparece el panel fijo visual**. El
  `Drawer` renderiza dos cosas por separado: su elemento raíz (que si participa del flujo
  normal del `Box` flex donde vive, reservando el `width` que se le dé por `sx`) y su
  `.MuiDrawer-paper` interno (que SIEMPRE es `position: fixed`, anclado al lado que diga
  `anchor`, independiente de dónde quedó su placeholder en el flujo). Si el `Drawer` se
  coloca ANTES del contenido principal en el JSX pero con `anchor="right"`, el
  placeholder reserva el espacio a la IZQUIERDA (por ser el primer hijo flex), mientras el
  panel visual fijo aparece a la DERECHA -- resultado: el contenido principal cree que
  tiene todo su ancho disponible empezando desde la izquierda, pero el panel fijo de la
  derecha le tapa esa misma franja por encima (z-index mas alto). Se manifestó como "la
  barra del menú se encima en los controles, no me permite ver" en TODAS las pantallas.
  Con `anchor="left"` y el `Drawer` primero en el JSX (el patrón estándar y documentado de
  MUI, "responsive drawer"), placeholder y panel visual coinciden del mismo lado y no hay
  traslape. **Lección**: si un `Drawer` permanente se necesita anclado a la derecha,
  colocarlo DESPUÉS del contenido principal en el JSX (para que su placeholder reserve
  espacio también del lado derecho), nunca antes.

- **Nombre de la base origen del GT: verificar el header del backup, no confiar en `USE [...]` de scripts viejos ni en comentarios de código.** La base documentada como `bdsInfo` (scripts SQL versionados, comentarios en `clGlosario.cs`) y la que el negocio llama hoy `bdsApollo` son la MISMA base, solo renombrada con el tiempo (`bdsGP`→`bdsInfo`→`bdsApollo`) — se confirmó leyendo el header binario del `.bak` (nombre lógico y de servidor) en vez de asumir por el nombre del archivo o el código. Los artefactos viejos del repo simplemente no se actualizaron tras el renombre.
- **Migración de datos, tabla origen `tblComplejidad` (GT): tiene 3 filas por `idComplejidad`** (una por Nivel: Senior/Master/Junior comparten el mismo Id de complejidad). Un JOIN directo `origen.idComplejidad = destino.algo` triplica cualquier fila que se una contra ella — hay que des-duplicar primero (`SELECT DISTINCT idComplejidad, Complejidad FROM ...`) antes de usarla como lookup de nombre. Se manifestó como una violación de PRIMARY KEY en una tabla temporal puente durante la migración de WorkItems (Id de tarea "duplicado" que en realidad era el mismo id repetido 3 veces por el fan-out).
- **`SELECT @variable = columna FROM tabla WHERE sin_match` NO limpia la variable a NULL** si la condición no matchea ninguna fila — dentro de un cursor/loop, esto deja el valor de la iteración ANTERIOR en la variable (bug clásico de T-SQL, silencioso, no truena). Se manifestó migrando el solicitante de una Solicitud: un valor con caracter acentuado no matcheó por una diferencia de codificación y la fila se quedó con el solicitante de la fila anterior en vez de caer al valor por default. Arreglo: `SET @variable = NULL` explícito inmediatamente antes de cada `SELECT @variable = ...` dentro de un loop, o usar `SET` en vez de `SELECT` cuando se pueda.
- **`PRINT` de SQL Server no acepta subqueries en su expresión** ("Subqueries are not allowed in this context. Only scalar expressions are allowed"), aunque un `SELECT` normal sí las acepta en el mismo contexto. Si se necesita imprimir un conteo (`SELECT COUNT(*) FROM ...`) como parte de un mensaje, hay que resolverlo primero en una variable (`SELECT @n = COUNT(*) FROM ...; PRINT '...' + CAST(@n AS NVARCHAR(10))`), nunca embeber el `SELECT` directo dentro del `PRINT`.
- **`sqlcmd` necesita `-f 65001` (o el codepage UTF-8 equivalente) para leer scripts `.sql` con acentos/Ñ correctamente** cuando el archivo se generó en UTF-8 sin BOM — sin esa bandera, un identificador o literal con caracter acentuado (ej. columna `Descripción`, valor `PAMELA.MUÑOZ`) se decodifica mal y truena con errores de sintaxis crípticos apuntando a caracteres basura (ej. `Incorrect syntax near '³'`) en una línea que a simple vista se ve bien.
- **`CREATE INDEX` (y cualquier operación sobre índices filtrados/computados) exige `SET QUOTED_IDENTIFIER ON` explícito en la sesión** — sin eso truena con el error 1934 aunque el resto del script (incluyendo `ALTER TABLE ADD COLUMN`) haya corrido bien. Agregar `SET QUOTED_IDENTIFIER ON` justo despues de `SET XACT_ABORT ON` en cualquier script nuevo que cree índices, no solo al crear la tabla.
- **Resolver un blob autenticado en un `useEffect` async: nunca confiar en la referencia al elemento DOM capturada al inicio del efecto, re-consultarla por una clave estable (GUID) dentro del `.then()`.** `ContenidoEnriquecido` (y su antecesor `ContenidoComentario`) hacian `querySelectorAll` una vez, guardaban la referencia al `<img>`, y al resolver la promesa de `descargarArchivoBlob` le asignaban `.src` a ESA referencia. Se detecto en vivo (Browser pane, verificando el rich text nuevo de Descripcion) que `imgCapturado === document.querySelector(...)` daba `false`: para cuando la promesa resolvia, el contenedor ya se habia vuelto a montar (visto con logs: el efecto y su cleanup se dispararon dos veces, consistente con como React 18 remonta bajo ciertas condiciones en desarrollo) y el `<img>` original habia quedado desconectado del documento -- la asignacion de `.src` no truena, simplemente no se ve nada, silencioso y facil de pasar por alto. Arreglo: dentro del `.then()`, volver a buscar el nodo con `contenedorRef.current?.querySelector('img[data-guid="' + guid + '"]')` (usar el ref, no la variable capturada), asi siempre apunta al DOM vigente. Aplica a cualquier patron "capturar elemento -> await -> mutar elemento" en un efecto.
- **Un campo NULL "deliberado" en una migración puede esconderse detrás de una vista que filtra `IS NOT NULL`**: `vwTiempoInvertido` (fuente del indicador "Invertido" en Bandeja/Detalle) suma `tblHistorialEstatus.MinutosLaborales` con `WHERE MinutosLaborales IS NOT NULL` — la migración del GT (05_2026-08-01_SCRIPT_bdsGTE_MigracionNucleo.sql) dejó ese campo en NULL a propósito para las ~9041 filas migradas (no reprodujo `fnMinutosLaborales` histórico en esa pasada). Resultado: el indicador mostraba 0 para TODO WorkItem migrado aunque `tblRegistroTiempo` sí tuviera los registros crudos (4312 filas) — se percibió como "no se migró el tiempo" cuando en realidad solo faltaba un backfill. Arreglo (`06_2026-08-02_UPDATE_tblHistorialEstatus.sql`): recalcular `MinutosLaborales` con la MISMA lógica que usa `spCambiarEstatus` en vivo (`fnMinutosLaborales(FechaInicio, FechaFin, IdHorario)`), usando el horario del asignado ACTUAL del WorkItem (misma simplificación que ya hace `CambiarEstatusWorkItemHandler.ObtenerHorarioAsignadoAsync` en el backend, no se reconstruye el asignado histórico por fila). **Lección**: cuando una migración deja un campo en NULL "a propósito", buscar primero qué vista/consulta filtra por ese campo antes de asumir que el gap es invisible o de bajo impacto.
- **Al agregar un gate de ownership (RN-REQ-05) a un módulo, listar TODOS los comandos que escriben sobre la entidad, no solo los "obvios" (editar, cambiar estatus).** El 2026-08-02 se agregó el gate `WI.ModificarAjeno` a `ActualizarWorkItemCommand` y `CambiarEstatusWorkItemCommand`, pero `RegistrarTiempoCommand` (que también modifica el WorkItem — agrega tiempo invertido, afecta el consumo de presupuesto) se quedó sin el gate en esa misma pasada. Se detectó porque el usuario probó en vivo con una cuenta Desarrollador real (`Antonio.Ochoa`, no `aviramontes`/Administrador ni `lgarcia`, ya varias veces usada en pruebas) y reportó que "el desarrollador todavía puede modificar tareas de otros usuarios" — reproducido con `curl` directo contra la API (200 OK registrando tiempo en una tarea ajena) antes de tocar código, confirmando que no era percepción. Arreglo: mismo patrón exacto (`esAjeno` + `ExigirPermisoAsync(WI.ModificarAjeno, ...)`) en `RegistrarTiempoCommand`, con prueba de regresión (`RegistrarTiempo_EnItemAjenoSeBloqueaSinPermiso`). De paso: `WI.ModificarTiempo` (permiso ya sembrado para Desarrollador desde antes) resultó ser un permiso **sin usar en ningún comando** — no se reutilizó para este gate (habría sido un no-op ya que Desarrollador ya lo tiene) ni se tocó; queda para una futura funcionalidad de "corregir/editar un registro de tiempo ya guardado", que hoy no existe.
- **Auditoría completa de ownership en Adjuntos/Comentarios/Revisiones (2026-08-02, a pedido del usuario tras el hueco de `RegistrarTiempoCommand`)**: se revisaron los 6 comandos restantes que escriben sobre estas 3 entidades. Resultado: `SubirArchivoCommand` (subir adjunto) y `CrearComentarioCommand` (comentar) **no tienen gate de ownership, y es intencional** — son mecanismos de colaboración donde CUALQUIERA con acceso al WorkItem participa (QA, líder, otros devs), no una modificación del registro propio del WorkItem; restringirlos a solo el asignado rompería el flujo real. `EliminarArchivoVinculoCommand`/`EliminarComentarioCommand` (borrar) ya usan el modelo correcto para ese caso: solo el propio autor del adjunto/comentario, no el asignado del WorkItem (documentado como "sin admin-override en esta entrega"). `CrearRevisionCommand` (reportar hallazgo) tampoco tiene gate, también intencional: el rol de quien reporta es justamente ser alguien más revisando el trabajo. **Sí se encontró un hueco real**: `CorregirRevisionCommand`, camino "marcar corregido" (`Corregido=true`), no validaba NADA — cualquier usuario (ni el asignado del WorkItem, ni quien reportó el hallazgo) podía cerrar el hallazgo de otra persona, lo cual permite saltarse el gate de cierre RN-REQ-03 sin haber arreglado nada. Reproducido en vivo antes de codear (`Jose.Hernandez`, un tercero sin relación con el WorkItem ni el hallazgo, marcó corregido con 200 OK). Arreglo: mismo patrón `esAjeno` + `WI.ModificarAjeno`, aplicado solo al camino de "corregido" (el camino "reabrir" ya estaba bien protegido por `REV.Reabrir`, RN-QA-02). Prueba de regresión: `CorregirRevision_MarcarCorregidoEnItemAjenoSeBloqueaSinPermiso`.
- **"Ajeno" (RN-REQ-05) también significa SIN asignar, no solo "asignado a otra persona"** — hueco encontrado en la MISMA ronda de reportes en vivo del usuario: la condición original `estado.IdAsignado.HasValue && estado.IdAsignado != yo` dejaba `esAjeno = false` para cualquier WorkItem con `IdAsignado = NULL`, así que **cualquiera** podía iniciar, registrar tiempo, editar o marcar corregido un hallazgo en una tarea del backlog sin asignar. Reproducido en vivo (`Antonio.Ochoa` iniciando y registrando tiempo en `GTE-0003`, sin asignar, ambos con `200 OK`). El equipo decidió explícitamente (no asumido) que sin asignar SE TRATA como ajeno: nadie "toma" trabajo libremente, un Líder/Admin con `WI.ModificarAjeno` debe asignarlo primero (vía editar). Arreglo: se simplificó la condición a `estado.IdAsignado != usuarioActual?.IdUsuario` (sin el `.HasValue &&`) en los 4 comandos (`ActualizarWorkItemCommand`, `CambiarEstatusWorkItemCommand`, `RegistrarTiempoCommand`, `CorregirRevisionCommand`) — comparar contra `null` ya da `true` correctamente. Prueba de regresión: `ItemSinAsignar_SeTrataComoAjenoParaIniciarYRegistrarTiempo`. **Lección**: al validar "ownership" contra un campo nullable, probar explícitamente el caso NULL además de "pertenece a alguien más" — son dos huecos distintos, no el mismo.
- **RTF crudo en `tblWorkItem.Descripcion` migrado (B3): se puede parsear con alta fidelidad usando `System.Windows.Forms.RichTextBox`, el MISMO control que genero el RTF originalmente** (el GT es WinForms, usa Riched20 vía RichTextBox — parsearlo con esa misma clase es un round-trip simetrico, no una reimplementacion aproximada del spec RTF). Herramienta: `ConversorRtf` (proyecto de consola desechable, `net8.0-windows` + `UseWindowsForms=true` + `Microsoft.Data.SqlClient`, **nunca commiteado al repo**, mismo patron que el proyecto de consola del bootstrap del primer Administrador). Logica: `SELECT Descripcion FROM tblWorkItem WHERE Descripcion LIKE '%conversion visual pendiente%'` -> extraer el RTF de dentro del `<pre>` (des-escapando `&amp;`/`&lt;`/`&gt;`) -> en un hilo STA, `richTextBox.Rtf = rtf; texto = richTextBox.Text;` -> reconstruir HTML (`<p>` + líneas unidas con `<br>`, escapando `&`/`<`/`>`) -> `UPDATE`. Corrido con `--aplicar` (sin el flag, solo hace preview sin tocar la BD). Resultado en LocalDB: **1264/1264 convertidas, 0 fallidas**, verificado contra el ejemplo reportado por el usuario (folio `EDM-0017`) tanto por SQL como en el navegador real. **Trampa encontrada**: `\line` (salto de linea suave dentro de un parrafo) se traduce a `\v` (vertical tab, char 11) en `RichTextBox.Text`, NO a `\n`/`\r\n` como `\par` (parrafo) — si no se reemplaza `\v` por salto de linea antes de partir el texto, dos lineas separadas por `\line` quedan pegadas sin separador visible (invisible en un editor de texto, se nota solo comparando el HTML resultante). **Falta correr esta misma herramienta contra dev/preprod/prod** cuando se haga el corte real de cada ambiente (el codigo fuente no vive en el repo por ser desechable; si se necesita para otro ambiente, reconstruir desde esta receta o pedir que se regenere).

---

## 6. Contexto útil para retomar

**Estructura**: `src/GTE.Domain` (reglas puras) · `src/GTE.Application` (casos de uso
MediatR, DTOs, contratos) · `src/GTE.Infrastructure` (EF, repositorios de escritura,
query services de lectura, integraciones) · `src/GTE.WebApi` (controladores, middleware,
seguridad) · `frontend/gte-web` (SPA) · `DataBase/Scripts` (esquema).

**Al agregar un módulo nuevo, el camino ya trillado es**: constantes de estatus y acciones
en Domain → contrato de repositorio en Domain → DTOs Request/Response y comandos/consultas
en Application → repositorio y query service en Infrastructure → controlador → registro en
`Program.cs` → API tipada y pantalla en el SPA → sembrar etiquetas de transición si el
módulo tiene workflow.

**Reglas heredadas del GT que ya están implementadas y no deben perderse**: una sola tarea
En Proceso por persona (con suspensión automática de la anterior), cierre que exige avance
registrado y cero hallazgos pendientes, presupuesto por matriz complejidad × nivel congelado
al asignar, reapertura de hallazgos solo por líder, reglas especiales de proyectos de
mantenimiento, y la semántica de filtros de la bandeja.

**Commits de referencia**: `4242260` estructura inicial · `e7bd72a` revisiones ·
`d507b6a` planeación · `783c7ab` Fase 3 · `a376c9e` autenticación.

**A7 resuelto**: el usuario confirmo que las reglas de aprobacion/rechazo de pruebas se
referian al mini-flujo de QA que ya vive en el estatus del WorkItem (`EnPruebas`=3,
`TERMINAR`/`RECHAZAR_QA`), no a una capa nueva en el modulo Calidad. Ver fila A7 (sección 3.2)
para el detalle de lo implementado.
