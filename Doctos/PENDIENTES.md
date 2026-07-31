# GTE — Estado y pendientes

> Documento de continuidad. Sirve para retomar el proyecto en otra sesión sin
> contexto previo. Actualizar al cerrar cada bloque de trabajo.
>
> **Última actualización:** 2026-07-31 (cierre del modulo de Administracion)
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

**Iniciar sesión en desarrollo:** la pantalla de login pide una cuenta de dominio, sin
contraseña (el emisor local de tokens solo existe en Development). Datos sembrados a mano
durante el desarrollo: `aviramontes` (rol Administrador) y `lgarcia` (rol Desarrollador).
Si la base está recién creada no hay usuarios: cualquier cuenta que se escriba se
aprovisiona sola pero **nace sin roles**, así que hay que asignarle uno por SQL
(`tblUsuarioRol`) para poder operar.

```bash
dotnet test GTE.sln    # 34 pruebas; las de integración se omiten si no hay LocalDB
```

---

## 2. Qué está funcionando (verificado extremo a extremo)

| Módulo | Alcance | Pantalla |
|---|---|---|
| **Autenticación** | Toda la API exige token (401 sin él). Entra ID por configuración; emisor local solo en Development. Aprovisionamiento JIT sin roles. Menú filtrado por permisos | Login |
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

**Inventario:** 92 endpoints en 11 controladores · 11 pantallas · 12 scripts SQL ·
~100 tablas · 38 pruebas.

---

## 3. Pendientes

### 3.1 Bloqueantes para usar GTE con datos reales

Sin esto no se puede operar en producción, aunque el resto funcione.

| # | Pendiente | Detalle |
|---|---|---|
| ~~B1~~ | ~~**Módulo de Administración (CRUD)**~~ | **Resuelto 2026-07-31.** Proyectos, equipos+miembros, usuarios, roles (asignación+matriz en lote), horarios (tramos+festivos) y ambientes, con API completa (`AdministracionController`, 35 endpoints nuevos) y pantallas bajo `/admin` (6 pestañas). Ver detalle en la fila "Administracion" de la sección 2 y en la §3.4 lo que quedó deliberadamente fuera de alcance |
| B2 | **Flujo de Entra ID en el SPA** | El backend ya valida tokens de Entra; falta la redirección Authorization Code + PKCE en el frontend. Necesita el tenant real (client id, tenant id, redirect URI). Hoy solo se entra con el emisor local de desarrollo |
| B3 | **Migración de datos del GT** | Mapeo definido en el Documento Maestro §15.4: `tblTareas`→`tblWorkItem`, subtareas→hijos + registro de tiempo, historial de estatus, revisiones, usuarios/permisos, catálogos, glosario. Falta escribir y ensayar los scripts, con reportes de excepciones y checksums |
| B4 | **Despliegue** | No hay pipeline CI/CD ni guía de publicación (IIS/Kestrel, certificados, usuario de BD con permisos mínimos, variables de entorno) |

### 3.2 Alto valor, sin bloquear

| # | Pendiente | Detalle |
|---|---|---|
| A1 | **Comentarios y adjuntos** | `tblComentario`, `tblArchivo`, `tblArchivoVinculo` y el contrato `IAlmacenArchivos` existen sin implementación. Es lo que más se usa a diario en un gestor de tareas (incluye menciones `@usuario`) |
| A2 | **Edición de WorkItem en la UI** | El endpoint `PUT /workitems/{id}` ya existe con todas sus reglas; falta la pantalla. Hoy no se puede cambiar asignado, compromiso ni complejidad desde la interfaz |
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
