# GTE — Estado y pendientes

> Documento de continuidad. Sirve para retomar el proyecto en otra sesión sin
> contexto previo. Actualizar al cerrar cada bloque de trabajo.
>
> **Última actualización:** 2026-08-01 (edición de WorkItem en la UI, cierre de A2)
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
| **Solicitudes y triage** | Portal del solicitante, bandeja de triage, aprobar/rechazar/devolver, conversión a WorkItems trazados | Solicitudes, Triage |
| **Planeación** | Backlog priorizable, sprints (activar/cerrar con reubicación), capacidad con calendario real, burndown, kanban con WIP | Backlog, Tablero |
| **Calidad (QA)** | Planes, casos con pasos, ciclos, ejecuciones, bug desde falla, matriz de trazabilidad | QA |
| **Entregas** | Releases con contenido validado, artefactos con rollback pareado, cadena de firmas, despliegues, rollback, notas de versión | Releases |
| **Motor de estatus** | 11 procesos por datos en `tblProceso`/`tblTransicion` + `spCambiarEstatus` con guard de concurrencia | — |
| **Calendario laboral** | `fnMinutosLaborales` con turnos partidos y festivos; motor único de tiempo | — |
| **Administracion** | Proyectos (alta/edicion + cambio de estatus por el motor, folio al autorizar, RN-PRY-01 bloquea el cierre con WorkItems abiertos), equipos con miembros y % dedicacion, usuarios (alta/edicion/baja logica, RN-ADM-01 valida ciclos de jerarquia con CTE recursivo), roles (asignar/retirar con alcance global o por proyecto, matriz rol-permiso guardada en lote), horarios (tramos con turnos partidos, dias festivos) y ambientes (por proyecto o globales) | Administracion (6 pestañas) |
| **Comentarios y adjuntos** | Hilos de comentarios sobre WorkItem con formato basico (negritas, listas, etc.), @menciones con autocompletado (TipTap + catalogo de usuarios) y pegado de imagenes desde el portapapeles; adjuntos con subida/descarga por streaming autenticado (`IAlmacenArchivos` en disco, GUID + SHA-256), validacion de extension/tamano, baja logica solo por el propio autor. HTML sanitizado en el backend (`HtmlSanitizer`) antes de guardarse | franja de Comentarios bajo el detalle + pestaña Adjuntos, en Detalle de WorkItem |

**Inventario:** 104 endpoints en 13 controladores · 11 pantallas · 13 scripts SQL ·
~100 tablas (+1, `tblRefreshToken`) · 47 pruebas.

---

## 3. Pendientes

### 3.1 Bloqueantes para usar GTE con datos reales

Sin esto no se puede operar en producción, aunque el resto funcione.

| # | Pendiente | Detalle |
|---|---|---|
| ~~B1~~ | ~~**Módulo de Administración (CRUD)**~~ | **Resuelto 2026-07-31.** Proyectos, equipos+miembros, usuarios, roles (asignación+matriz en lote), horarios (tramos+festivos) y ambientes, con API completa (`AdministracionController`, 35 endpoints nuevos) y pantallas bajo `/admin` (6 pestañas). Ver detalle en la fila "Administracion" de la sección 2 y en la §3.4 lo que quedó deliberadamente fuera de alcance |
| ~~B2~~ | ~~**Autenticación en el SPA**~~ | **Resuelto 2026-08-01, cambio de alcance.** No habrá tenant de Entra ID (decisión del equipo: GTE maneja autenticación, accesos y roles totalmente dentro de sí mismo). Se construyó login propio: usuario+contraseña (BCrypt), bloqueo temporal, JWT + refresh rotativo en cookie HttpOnly, cambio de contraseña propio y reset por administrador. Ver fila "Autenticación" de la sección 2, ADR nuevo en la sección 4, y lo que queda fuera de alcance en la §3.4 (recuperar contraseña por correo, MFA, bootstrap del primer admin en un ambiente sin atajo de desarrollo) |
| B3 | **Migración de datos del GT** | Mapeo definido en el Documento Maestro §15.4: `tblTareas`→`tblWorkItem`, subtareas→hijos + registro de tiempo, historial de estatus, revisiones, usuarios/permisos, catálogos, glosario. Falta escribir y ensayar los scripts, con reportes de excepciones y checksums |
| B4 | **Despliegue** | No hay pipeline CI/CD ni guía de publicación (IIS/Kestrel, certificados, usuario de BD con permisos mínimos, variables de entorno) |

### 3.2 Alto valor, sin bloquear

| # | Pendiente | Detalle |
|---|---|---|
| ~~A1~~ | ~~**Comentarios y adjuntos**~~ | **Resuelto 2026-08-01.** Hilos de comentarios (formato básico + @menciones + imágenes pegadas) y adjuntos (subida/descarga por streaming autenticado) sobre WorkItem, API completa (`ComentariosController`, `ArchivosController`) y UI integrada en el Detalle. Ver fila "Comentarios y adjuntos" de la sección 2 y lo que quedó deliberadamente fuera de alcance en la §3.4 |
| ~~A2~~ | ~~**Edición de WorkItem en la UI**~~ | **Resuelto 2026-08-01.** Modal de edición (`ModalEditarWorkItem.tsx`) sobre el endpoint `PUT /workitems/{id}` ya existente: titulo, descripcion, criterios, prioridad, complejidad, asignado, compromiso y puntos. El boton "Editar" se oculta si el elemento esta Terminado o asignado a otra persona y el usuario no tiene el permiso correspondiente; las reglas campo-por-campo (compromiso al pasado, cambio de complejidad) las sigue validando el backend, su 403 se ve tal cual en el Snackbar. Se agrego el catalogo de Complejidades (`CatalogosBandejaResponse`) que no existia en ningun endpoint |
| A3 | **Notificaciones** | `tblNotificacion`, `tblPlantillaNotificacion` y `ICanalNotificacion` listos, sin implementación. Sin esto el solicitante no sabe que su petición avanzó (rechazo, liberación) |
| A4 | **Hangfire (trabajos en segundo plano)** | Vigilancia de SLA, snapshot de KPIs (`spSnapshotKpi` ya existe), recordatorios de compromiso, despacho del outbox `tblEventoDominio`, cierre automático de tickets |
| A5 | **Portafolio** | `tblPortafolio`, `tblPrograma`, `tblRiesgo`, `tblHito`, `tblObjetivoOkr`, `tblTarifaNivel`, `tblPresupuestoProyecto` sin módulo. Incluye la matriz de riesgos y el costo real por proyecto (horas × tarifa por nivel) |
| A6 | **SignalR** | Notificaciones en vivo y refresco de tableros; el diseño ya lo contempla (ADR-08) |

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
- El **arrastre de tarjetas del kanban no se pudo verificar con ratón real** en el entorno
  de desarrollo usado (el navegador headless no genera los eventos de puntero que dnd-kit
  necesita). La lógica del movimiento sí está verificada por su endpoint. **Conviene
  probarlo a mano.**
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

---

## 4. Decisiones firmes (no cambiar sin acuerdo del equipo)

| ADR | Decisión |
|---|---|
| 02 | .NET 9 + React. **Contradice** el estándar del Frente B (.NET 8 + Angular): pendiente ratificar y actualizar `InterfloClaude.md` |
| 03 | **`bdsGTE` es la única base.** Motor de estatus y folios propios; cero dependencia de `bdsCentral` u otra base |
| 04 | El workflow vive en datos (`tblProceso`/`tblTransicion`). Alta de procesos = filas, nunca tocar `spCambiarEstatus` |
| 06 | Integración Git tras `IProveedorGit` (conviven Gitea y GitHub) |
| 09 | El código de GTE vive en **GitHub**, de forma definitiva. Excepción deliberada al estándar del ecosistema; no proponer migración a Gitea |
| — | El frontend **nunca decide transiciones**: pide las acciones válidas al motor y envía acciones, jamás estatus destino |
| — | El esquema lo gobiernan los scripts de `DataBase/Scripts` (idempotentes). **No usar migraciones de EF**; tras cambiar el esquema, re-scaffold |
| — | MediatR 12.5.0 y AutoMapper 14.0.0 fijados por licencia libre; no subir de major sin decisión |
| — | **GTE no usa Entra ID ni ningún proveedor de identidad externo** (decisión del equipo, 2026-08-01): reemplaza la intención original de B2. Autenticación 100% propia dentro de `bdsGTE` (usuario+contraseña BCrypt, JWT propio, refresh rotativo). Un solo JWT HMAC para todo el sistema: el atajo de desarrollo y el login real emiten el mismo tipo de token (`IEmisorTokenSesion`), nunca dos mecanismos distintos |

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
- **Verificacion real de paste de imagen en el Browser pane**: a diferencia del drag del
  kanban (necesita eventos de puntero reales que el entorno headless no genera), un
  `ClipboardEvent` sintetico con `DataTransfer` SI se puede construir y despachar por script
  contra el editor -- permitio probar de punta a punta la subida por pegado sin depender
  del portapapeles real del sistema operativo.

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
