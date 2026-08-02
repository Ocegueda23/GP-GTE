# GTE — Estado y pendientes

> Documento de continuidad. Sirve para retomar el proyecto en otra sesión sin
> contexto previo. Actualizar al cerrar cada bloque de trabajo.
>
> **Última actualización:** 2026-08-01 (despliegue: Kestrel directo como Windows Service, cierre de B4)
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
| **Autenticación** | Propia de GTE, sin proveedor externo: usuario+contraseña (BCrypt), bloqueo temporal tras 5 intentos fallidos, JWT de acceso (15 min) + refresh token rotativo en cookie HttpOnly (8h, con detección de reuso que revoca toda la cadena), cambio de contraseña propio y reset por administrador. Toda la API exige token (401 sin él); atajo local sin contraseña solo en Development. Menú filtrado por permisos | Login |
| **WorkItems** | Bandeja con filtros heredados del GT, detalle, alta, cambio de estatus por acción, registro de tiempo | Trabajo, Detalle |
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
| **Notificaciones y tiempo real** | Campana con notificaciones In-App (`tblNotificacion`) que llegan en vivo por SignalR (`NotificacionesHub`, un solo hub para notificaciones + refresco de tablero); disparadores: Solicitud aprobada/rechazada/devuelta (notifica al solicitante) y @mencion en un comentario (notifica al mencionado). El tablero Kanban se refresca solo cuando cualquier WorkItem cambia de estatus, sin importar quien lo haya movido | campana en la barra superior (todas las pantallas) |
| **Manual de usuario (Ayuda)** | Pagina de ayuda estatica dentro de la SPA, en espanol simple para usuarios sin conocimiento tecnico: secciones en acordeon (login, menu, Mi Dia, bandeja, detalle de WorkItem, Solicitudes, el flujo completo de una solicitud hasta su cierre con diagrama SVG -- incluye las ramas de Rechazada/Devuelta y Hallazgos de QA --, otras secciones segun rol, contacto de soporte). Sin permiso (disponible para cualquier usuario autenticado); contenido fijo en el codigo, no hay editor -- actualizarlo requiere tocar `ManualUsuarioPage.tsx` | Ayuda (visible para todos en el menu) |
| **Menu lateral (2026-08-02)** | La navegacion se movio de una barra horizontal arriba a un panel lateral fijo del lado derecho (`Drawer` de MUI, `variant="permanent"`), con la opcion activa resaltada segun la ruta actual. En pantallas chicas se colapsa a un boton de menu en la barra superior que abre un cajon deslizable (`variant="temporary"`) que se cierra solo al navegar. La barra superior conservo el logo, la campana de notificaciones y el chip de usuario (con el nombre truncado en pantallas chicas para no empujar el boton de menu fuera de la vista) | Panel lateral derecho (todas las pantallas) |

**Inventario:** 108 endpoints en 14 controladores + 1 hub de SignalR · 12 pantallas ·
13 scripts SQL · ~100 tablas (+1, `tblRefreshToken`) · 49 pruebas.

---

## 3. Pendientes

### 3.1 Bloqueantes para usar GTE con datos reales

Sin esto no se puede operar en producción, aunque el resto funcione.

| # | Pendiente | Detalle |
|---|---|---|
| ~~B1~~ | ~~**Módulo de Administración (CRUD)**~~ | **Resuelto 2026-07-31.** Proyectos, equipos+miembros, usuarios, roles (asignación+matriz en lote), horarios (tramos+festivos) y ambientes, con API completa (`AdministracionController`, 35 endpoints nuevos) y pantallas bajo `/admin` (6 pestañas). Ver detalle en la fila "Administracion" de la sección 2 y en la §3.4 lo que quedó deliberadamente fuera de alcance |
| ~~B2~~ | ~~**Autenticación en el SPA**~~ | **Resuelto 2026-08-01, cambio de alcance.** No habrá tenant de Entra ID (decisión del equipo: GTE maneja autenticación, accesos y roles totalmente dentro de sí mismo). Se construyó login propio: usuario+contraseña (BCrypt), bloqueo temporal, JWT + refresh rotativo en cookie HttpOnly, cambio de contraseña propio y reset por administrador. Ver fila "Autenticación" de la sección 2, ADR nuevo en la sección 4, y lo que queda fuera de alcance en la §3.4 (recuperar contraseña por correo, MFA, bootstrap del primer admin en un ambiente sin atajo de desarrollo) |
| B3 | **Migración de datos del GT** | Mapeo definido en el Documento Maestro §15.4: `tblTareas`→`tblWorkItem`, subtareas→hijos + registro de tiempo, historial de estatus, revisiones, usuarios/permisos, catálogos, glosario. Falta escribir y ensayar los scripts, con reportes de excepciones y checksums |
| ~~B4~~ | ~~**Despliegue**~~ | **Resuelto y CONFIRMADO 2026-08-01 contra un servidor real (`SRVPROD\NASA`): el Windows Service quedo corriendo, el primer login funciono y la contrasena de arranque ya se cambio.** Kestrel directo como Windows Service (sin IIS, sin Docker, sin CI/CD -- mismo patrón real que ya usa `Interflo.ServiceHealth`, ver decisión en la sección 4). La API sirve la SPA compilada en el mismo proceso (`wwwroot` + fallback). `publicar.bat` hace `npm run build` + `dotnet publish` + limpia la carpeta de destino + copia el build a `wwwroot`, todo en un solo paso. Tres herramientas para las variables de entorno del servicio, todas con merge seguro (nunca borran las demás): `generar-clave-jwt.bat` (genera `Jwt__ClaveFirma` sola), `configurar-variable-servicio.bat` (una variable cualquiera) y `configurar-servicio-completo.bat` (las tres de un jalón + reinicia el servicio). Login de BD de mínimo privilegio con **autenticación de SQL Server** (no de Windows) via `DataBase/Scripts/01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql`, y manual completo (`Doctos/MANUAL_INSTALACION_GTE.md`, calcado del formato real de ServiceHealth). `dotnet test` en 49/49 durante todo el proceso.<br><br>**Problemas reales encontrados y resueltos en la primera instalación real** (todos con lección correspondiente en la sección 5): (1) `sc start` daba error 1053 -- faltaba `UseWindowsService()`; (2) el `.exe` no arrancaba, el servidor no tenía el runtime 9.0 ("You must install or update .NET") -- se decidió retargetear todo el backend a **.NET 8** (ver ADR-02 actualizado en la sección 4) en vez de instalar el 9.0; (3) tras el retargeteo, un segundo intento de instalación trueno con `FileNotFoundException` de `System.Runtime` version 9.0.0.0 -- archivos del publish viejo mezclados con el nuevo en la carpeta instalada (`dotnet publish` no borra lo que ya no necesita); (4) `Falta Jwt:ClaveFirma` -- variable de entorno todavía no configurada; (5) al `bdsGTE` real le faltaba correr `01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql` (las columnas `PasswordHash`/`RequiereCambioPassword` no existían, `INSERT` fallaba con "Invalid column name"); (6) `tblUsuario` estaba vacía (base nueva, sin datos migrados del GT todavía) -- se creó el primer Administrador a mano con un `INSERT` + hash BCrypt generado con la misma librería de la API (ver receta en el manual, sección "Primer login"); (7) `Login failed for user 'NT AUTHORITY\SYSTEM'` -- el servicio no se había reiniciado después de configurar `ConnectionStrings__bdsGTE`, así que seguía usando el `Trusted_Connection=True` por default de `appsettings.json` contra `localhost` en vez de la cadena real. Fuera de alcance deliberado: sin pipeline de CI (se decidió mantener todo manual, igual que el resto del ecosistema) |

### 3.2 Alto valor, sin bloquear

| # | Pendiente | Detalle |
|---|---|---|
| ~~A1~~ | ~~**Comentarios y adjuntos**~~ | **Resuelto 2026-08-01.** Hilos de comentarios (formato básico + @menciones + imágenes pegadas) y adjuntos (subida/descarga por streaming autenticado) sobre WorkItem, API completa (`ComentariosController`, `ArchivosController`) y UI integrada en el Detalle. Ver fila "Comentarios y adjuntos" de la sección 2 y lo que quedó deliberadamente fuera de alcance en la §3.4 |
| ~~A2~~ | ~~**Edición de WorkItem en la UI**~~ | **Resuelto 2026-08-01.** Modal de edición (`ModalEditarWorkItem.tsx`) sobre el endpoint `PUT /workitems/{id}` ya existente: titulo, descripcion, criterios, prioridad, complejidad, asignado, compromiso y puntos. El boton "Editar" se oculta si el elemento esta Terminado o asignado a otra persona y el usuario no tiene el permiso correspondiente; las reglas campo-por-campo (compromiso al pasado, cambio de complejidad) las sigue validando el backend, su 403 se ve tal cual en el Snackbar. Se agrego el catalogo de Complejidades (`CatalogosBandejaResponse`) que no existia en ningun endpoint |
| ~~A3~~ | ~~**Notificaciones**~~ | **Resuelto 2026-08-01.** Alta In-App (`tblNotificacion`) + listar/marcar leida(s), disparada desde Solicitud aprobar/rechazar/devolver y @mencion en comentarios. `ICanalNotificacion`/`tblPlantillaNotificacion` quedan sin implementar (ver §3.4): solo canal InApp, mensajes inline |
| A4 | **Hangfire (trabajos en segundo plano)** | Vigilancia de SLA, snapshot de KPIs (`spSnapshotKpi` ya existe), recordatorios de compromiso, despacho del outbox `tblEventoDominio`, cierre automático de tickets |
| A5 | **Portafolio** | `tblPortafolio`, `tblPrograma`, `tblRiesgo`, `tblHito`, `tblObjetivoOkr`, `tblTarifaNivel`, `tblPresupuestoProyecto` sin módulo. Incluye la matriz de riesgos y el costo real por proyecto (horas × tarifa por nivel) |
| ~~A6~~ | ~~**SignalR**~~ | **Resuelto 2026-08-01.** `NotificacionesHub` unico (no dos como preveia el diseño original) para notificaciones en vivo (`Clients.User`) y refresco de tableros (`Clients.All` en `workItemActualizado`, sin grupo por equipo). Verificado con dos sesiones reales simultaneas en el navegador |

### 3.3 Fases del roadmap que faltan completas

**Resto de Fase 3 — integración Git**
Diseñada tras la abstracción `IProveedorGit` (ADR-06) porque conviven Gitea (proyectos
internos) y GitHub (repositorio de GTE, ADR-09). Tablas listas: `tblRepositorio`,
`tblCommit`, `tblCommitWorkItem`, `tblPullRequest`, `tblPipelineEjecucion`, `tblArtefacto`.
Alcance: webhook entrante autenticado por secreto de repositorio, vinculación de commits al
WorkItem por folio en el mensaje, estado de PR, botón "crear rama", registro de pipelines y
transiciones automáticas configurables.

**Fase 4 — Operación y Soporte**
- Incidentes (`tblIncidente`): severidad, causa raíz obligatoria en S1/S2, vínculo al
  correctivo y al release causante, disponibilidad mensual.
- Mesa de ayuda (`tblTicket`, `tblSla`): SLA en minutos laborales —el cálculo inverso ya
  está resuelto en `ICalendarioLaboral.SumarMinutosLaboralesAsync`—, pausa en "Esperando
  usuario", escalamiento a desarrollo, encuestas de satisfacción.
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
