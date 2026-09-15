# GTE — Estado y pendientes

> Documento de continuidad. Sirve para retomar el proyecto en otra sesión sin
> contexto previo. Actualizar al cerrar cada bloque de trabajo.
>
> **Bloque en curso (2026-09-14) — `GTE.Instalador`: WinForms en vez de scripts de consola
> para la instalación inicial.** El usuario reportó que instalar GTE (varios `.bat`/`.ps1`
> con parámetros posicionales, orden que ya había cambiado una vez, cuidado especial con
> comillas en PowerShell) se había vuelto demasiado propenso a error. Se creó
> `tools/GTE.Instalador` (WinForms, `net8.0-windows`, agregado a `GTE.sln` en una carpeta de
> solución `tools` nueva junto a `src`/`tests`) que reemplaza a
> `configurar-servicio-completo`/`configurar-variable-servicio`/`configurar-almacen-archivos`
> (`.bat`/`.ps1`) y `generar-clave-jwt.bat` — ya no viven en el repo (el usuario los movió a
> `Doctos/SetUpInstaller/`, carpeta **sin trackear**, junto con `asistente-instalacion.html`
> y dos `.bak`; no tocar esa carpeta, es su archivo local, no parte del flujo versionado).
> Una sola ventana: lee/crea/modifica las cuatro variables de entorno del servicio
> (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__bdsGTE` armada desde Servidor/Usuario/
> Password SQL con botón "Probar conexión", `Jwt__ClaveFirma` con generador de 64 bytes, y
> `AlmacenArchivos__Ruta` con crear-carpeta-y-probar-escritura), conserva cualquier otra
> variable ya puesta en el registro, y tiene botones para crear el servicio (`sc create`) e
> Iniciar/Detener/Reiniciar. Manifest con `requireAdministrator` (sin eso, escribir en
> `HKLM\SYSTEM\CurrentControlSet\Services\...` falla en silencio o con
> `UnauthorizedAccessException` poco claro). `Doctos/MANUAL_INSTALACION_GTE.md` (Paso 3) ya
> apunta a esta herramienta en vez de a los scripts viejos.
>
> **Trampa de build encontrada (ya conocida, confirmada de nuevo aquí):** compilar el
> proyecto nuevo con `dotnet build` sin más truena en `CreateAppHost` con
> `IOException: La operación solicitada no se puede realizar en un archivo con una sección
> asignada a usuario abierta` — es el problema ya documentado de tener el repo dentro de
> Google Drive (bloquea el `.exe` nativo), no un bug del proyecto. Se compila igual con
> `dotnet build -p:UseAppHost=false`; para publicar de verdad (fuera de Drive, con el `.exe`
> real) no debería hacer falta ese flag.
>
> **Deuda de verificación de este bloque:** compila limpio (`dotnet build GTE.sln`, 0
> advertencias propias) y arranca sin excepción (probado lanzando el `.dll` con `dotnet
> exec` y confirmando que el proceso se queda vivo en el message loop de WinForms). **No se
> probó de punta a punta contra un servicio de Windows real** (crear el servicio, guardar
> variables, reiniciar, confirmar que `GTE.WebApi` las toma) — la próxima vez que se haga una
> instalación real es el momento de confirmarlo con la herramienta.
>
> **Bloque en curso (2026-09-09) — Incidentes: categoría propia y el combo de proyectos que
> no mostraba nada.** Dos pedidos del mismo día sobre el módulo de Incidentes.
>
> (1) **Catálogo de categorías de incidente.** Tabla nueva `dbo.tblCategoriaIncidente`
> (`Nombre` único + `Nivel`) y columna `tblIncidente.IdCategoriaIncidente`. Es catálogo
> APARTE de `tblCategoriaTicket` a propósito: la de tickets clasifica lo que pide un usuario
> (Duda, Acceso, Mejora) y la de incidentes lo que se cayó en operación; mezclarlas ensucia
> los dos combos y los reportes de ambos módulos. Se sembraron las 41 categorías que definió
> el equipo, con el nivel que las atiende: 33 en `Soporte N1-N2` y 8 en `Desarrollo`. El
> `Nivel` no es un catálogo aparte ni tiene CHECK — la tabla se administra desde el motor de
> catálogo genérico y un CHECK impediría dar de alta un nivel nuevo sin script. El frontend
> lo usa solo para agrupar el combo (`ComboBuscable` acepta ahora un `grupo` opcional por
> opción, ver la nota de abajo); no es columna de la bandeja. La categoría es **obligatoria**
> (decisión del 2026-09-09): la exige el alta y también la edición — una vez capturada no se
> puede dejar en blanco, y los dos incidentes viejos la piden al editarse. La validación vive
> en `CrearIncidenteValidator`/`ActualizarIncidenteValidator`, NO en la BD: la columna quedó
> NULL-able para no inventarle categoría a lo ya registrado y para que el error sea un 400 de
> dominio y no un 547 de SQL Server.
>
> (2) **El combo de Proyecto ignoraba los accesos dados en la pestaña Accesos.**
> `CatalogosQueryService` listaba solo proyectos donde el usuario es Responsable o está en el
> equipo asignado, así que dar de alta un acceso en Admin > Proyectos > Accesos (que escribe
> `tblUsuarioRol` con `IdProyecto`, tercera parte del bloque del 2026-09-04) no hacía
> absolutamente nada en los 9 combos de `/catalogos/bandeja`: se otorgaba el acceso y el
> proyecto seguía sin aparecer. Ahora un proyecto aparece por las **tres** vías: responsable,
> equipo, o rol acotado a ese proyecto. Los roles **globales** (`IdProyecto` null) NO entran
> deliberadamente: abrirían todos los proyectos del sistema y estos combos son "mis
> proyectos" (verificado: el usuario con rol Administrador global sigue viendo solo el
> proyecto del que es responsable). Esto **no relaja** la decisión de negocio del 2026-08-03
> que dejó el filtro como estaba para los proyectos migrados sin Responsable ni Equipo (§3.4):
> ahí el dato falta; aquí el acceso estaba dado de forma explícita y se ignoraba.
>
> **PENDIENTE INMEDIATO de este bloque:** correr
> `DataBase/Scripts/12_Scripts/01_2026-09-09_SCRIPT_bdsGTE_CategoriaIncidente.sql`
> (carpeta nueva de esta sesión) en **cada** servidor. Ya se corrió — dos veces, para probar
> que es idempotente — en la instancia local `localhost\SQLEXPRESS01`. Hasta que se corra, la
> bandeja de incidentes truena con `Invalid object name 'dbo.tblCategoriaIncidente'` (misma
> lección de la sección 5).
>
> **Para que el equipo pueda mantener la lista sin scripts:** registrar `tblCategoriaIncidente`
> en Administración > Catálogos (motor de catálogo genérico), igual que se hizo con
> `CATEGORIA_TICKET`. Eso crea solo los permisos `CAT.<CLAVE>.*` y la configuración de
> columnas; no se sembró desde el script porque duplicaría esa lógica.
>
> **Re-scaffold hecho a mano, a propósito:** el `--force` del README habría dejado un diff
> enorme y equivocado, porque la BD local no está sincronizada con el modelo del repo en las
> dos direcciones — le sobran las 11 tablas de Hangfire y 5 vistas `VwBi*`, y le faltan
> `tblNotaVersion`, `tblNotaVersionDetalle`, `tblReleaseRespaldo`, `tblTipoCambioVersion` y
> `tblTipoRespaldo` (los scripts de la rama de respaldos no se han corrido ahí). Se
> scaffoldeó a una carpeta desechable y se copiaron solo las tres piezas del cambio:
> `TblCategoriaIncidente.cs`, el campo y la navegación en `TblIncidente.cs`, y el `DbSet` +
> las dos configuraciones en `DbContextGTE.cs`. **Ojo para la próxima sesión que toque
> esquema:** corran primero los scripts pendientes en la BD local si quieren volver a
> scaffoldear completo.
>
> **De paso, un bug de pérdida de dato:** el diálogo Editar del detalle de incidente mandaba
> `fechaDeteccion: null` en cada guardado, así que borraba la fecha de detección aunque ese
> campo no se edite ahí. Ahora reenvía la que ya trae el incidente. Se arregló porque la
> categoría obligatoria obliga a editar los incidentes viejos y el bug les habría borrado el
> dato justo ahí.
>
> **Deuda de verificación de este bloque:** `dotnet build` (0 errores), `tsc -b` y `oxlint`
> limpios, y las tres suites pasan (34 Domain + 2 Application + 37 Api). Las dos consultas
> nuevas se verificaron **contra la BD real** con su SQL equivalente, dentro de una
> transacción con ROLLBACK: se reprodujo el bug del combo (acceso dado, cero proyectos), se
> confirmó que el filtro nuevo trae el proyecto, y que el left join de la categoría no tira
> los incidentes sin categoría. **No se probó en el navegador**: la pantalla exige login y
> el API que consume el preview es el servicio de Windows en `:5090`, que corre la versión ya
> desplegada y no conoce la columna nueva. Recordatorio de `GTE.Api.Tests`: su cadena está
> clavada a `(localdb)\MSSQLLocalDB`, que no existe en esta máquina, así que sus pruebas se
> saltan solas sin marcarse como omitidas — pasan sin ejecutar nada.
>
> **Bloque en curso (2026-09-04) — Tablero: apartado de tareas suspendidas.**
> El tablero Kanban no tenía columna para el estatus Suspendido (5): un WorkItem que se
> suspendía desaparecía del tablero, y del backlog tampoco se ve (ese solo lista lo que no
> tiene sprint), así que el trabajo detenido quedaba invisible en planeación. Se agregó la
> columna "Suspendido" a `ColumnasTableroEstandar` (GTE.Domain, única fuente de verdad del
> mapeo), entre Correccion y Terminado y sin límite WIP: los tableros nuevos ya nacen con
> ella y la vista consolidada "todos los equipos" la pinta sin tocar BD. El drag & drop no
> necesitó cambios porque el grafo ya tiene `2 SUSPENDER -> 5` y `5 REANUDAR -> 2`, ambas
> sin motivo obligatorio. En el front la columna se distingue con borde punteado ámbar,
> para que se lea como apartado y no como etapa del flujo.
>
> **PENDIENTE INMEDIATO de este bloque:** correr
> `DataBase/Scripts/11_Scripts/01_2026-09-04_INSERT_tblTableroColumna_Suspendido.sql`
> (carpeta nueva de esta sesión) en LocalDB y en cada servidor. Los tableros que ya existen
> en `tblTableroColumna` siguen sin la columna hasta que se corra: el código solo aplica el
> mapeo estándar al aprovisionar un tablero nuevo, no reconcilia los existentes. El script
> inserta Suspendido en la posición que ocupaba Terminado y desplaza +1 las columnas de su
> derecha.
>
> **Segunda parte del mismo bloque (el tablero deja de ser solo lectura):** (1) botón "Nuevo"
> que abre el mismo `NuevoItemModal` de la bandeja con el contexto que el tablero ya conoce
> (prop nueva `inicial`): el proyecto solo se prellena si el equipo filtrado tiene
> exactamente uno — para eso `ProyectoItemResponse` ahora expone `IdEquipo` — y el sprint
> activo solo si la persona tiene `PLA.GestionarSprints`, porque el backend lo exige igual
> (RN-GTE-019). (2) Filtro por usuario asignado que **abre con el usuario firmado**:
> `GET /api/v1/tablero` acepta `idAsignado` y filtra por la columna real
> `tblWorkItem.IdAsignado`; "Todas las personas" lo quita.
>
> **Deuda de verificación de esta parte:** `dotnet build` (0 errores), `tsc -b` y `oxlint`
> limpios, pero **no se probó en el navegador**. El preview del front consume el API del
> servicio de Windows en `:5090`, que corre la versión YA desplegada y no conoce `idAsignado`
> ni el `IdEquipo` del catálogo: probar ahí daría un falso verde (el filtro no filtraría y el
> prellenado no aparecería, sin un solo error en consola). Para verificar de verdad hay que
> publicar el API al servicio, o levantar una instancia aparte y apuntar el front con
> `VITE_API_URL` — recordando que `localhost` desde una sesión interactiva resuelve a la
> instancia sin `bdsGTE`, hay que escribir `localhost\SQLEXPRESS01`.
>
> **Hallazgo abierto (permisos por proyecto, pregunta del 2026-09-04):** `tblUsuarioRol` ya
> tiene alcance por proyecto (`IdProyecto` NULL = rol global, con valor = acotado a ese
> proyecto) y `VerificadorPermisos` ya lo evalúa así — un rol acotado a un proyecto NO cuenta
> para las verificaciones globales, que es lo correcto. Lo que falta es UI: `UsuariosTab.tsx`
> asigna roles **siempre** con `idProyecto: null`, así que hoy no hay forma de dar un permiso
> solo sobre un proyecto desde la aplicación (la lectura ya lo muestra: "rol (global)" vs
> "rol (Proyecto X)"). Se resolvió en la tercera parte de este mismo bloque: se administra
> desde la pestaña Accesos del proyecto, sobre esa misma tabla; **no se creó una segunda**.
>
> **Tercera parte del bloque (2026-09-04) — pestaña Accesos en el proyecto.** Primer uso real
> del alcance por proyecto de `tblUsuarioRol`. Admin > Proyectos > editar ahora abre con dos
> pestañas: "Datos" (lo de siempre) y "Accesos" (`AccesosProyectoTab.tsx`), que lista quién
> tiene qué rol EN ese proyecto y permite agregar y retirar. Endpoints nuevos:
> `GET/POST /api/v1/proyectos/{id}/accesos` y `PUT .../accesos/{idUsuarioRol}/retirar`.
> **No hay tabla nueva ni script**: escribe la misma `tblUsuarioRol` que la pantalla de
> usuarios, con `IdProyecto` = el de la ruta, así que desde ahí es imposible crear un acceso
> global, y un rol dado aquí se ve allá como "(nombre del proyecto)".
>
> Decisiones de esta parte: (1) el permiso `ADM.Roles` se verifica **con el idProyecto**, así
> que lo cumple tanto un rol global como uno acotado a ese proyecto — el administrador de un
> proyecto administra los accesos del suyo, y solo del suyo; (2) `AsignarRolAsync` ahora es a
> prueba de duplicados: si la misma persona ya tuvo ese rol con el mismo alcance y se le
> retiró, **reactiva la fila** en vez de insertar otra (`tblUsuarioRol` no tiene UNIQUE que lo
> impida), y el caso global se consulta aparte porque comparar una columna nullable contra un
> parámetro null depende de la compensación de null semantics de EF; (3) la bitácora de
> ASIGNAR_ROL/RETIRAR_ROL ahora dice el alcance ("global" o "proyecto N").
>
> **Consecuencia conocida que hay que tener presente ahora que se usan roles acotados:**
> `SesionQueryService` arma `sesion.permisos` con TODOS los permisos de TODOS los roles del
> usuario **sin filtrar por alcance**. O sea que alguien con `ADM.Roles` acotado a un proyecto
> verá la pestaña Accesos en todos los proyectos (y el menú de Administración), aunque el
> backend le responda 403 en los que no son suyos. El front siempre fue optimista con los
> permisos; mientras todos los roles eran globales daba igual. Si molesta, la corrección es
> transversal: la sesión tendría que mandar el alcance junto con cada permiso, y `puede()`
> recibir el proyecto.
>
> **Bloque en curso (2026-09-02) — Releases: listado con filtros y detalle propio.**
> La pantalla de Releases se partió en dos: `ReleasesPage` es ahora solo la bandeja (filtros
> por proyecto, estatus y líder asignado, resueltos en el backend; columnas de líder, creado
> por y fecha de creación) y el detalle vive en su propia ruta `/releases/:id`
> (`DetalleReleasePage`), igual que `/wi/:folio` en trabajo. En el detalle se agregó la
> captura del líder, la edición (no solo alta y baja) de artefactos y respaldos mientras el
> release está En Preparación, la versión que se libera por artefacto, y una columna derecha
> con cadena de aprobación, versión viva por ambiente y despliegues. La Solicitud de
> despliegue se compactó para gastar menos hojas (sin tocar el alto de los renglones de
> firma), ahora encabeza con el líder y ya no imprime el estatus.
>
> **PENDIENTE INMEDIATO de este bloque:** los dos scripts de `DataBase/Scripts/08_Scripts`
> (`01_..._ALTER_tblRelease.sql` — columna `IdLiderAsignado` + FK a `tblUsuario` — y
> `02_..._ALTER_tblReleaseArtefacto.sql` — `VersionArtefacto` y columnas de movimiento en
> artefactos y respaldos) **todavía no se han corrido en ningún ambiente**, LocalDB incluida.
> Hasta que se corran, el listado y el detalle truenan con `Invalid column name` (ver la
> lección de la sección 5 sobre este mismo síntoma). Backend y frontend compilan y las
> pruebas de Domain (34) y Application (2) pasan; `GTE.Api.Tests` sigue fallando por el
> problema de entorno ya documentado (manifiesto de static web assets apuntando a la ruta
> de otra máquina), no por este cambio.
>
> **Última actualización:** 2026-08-28 (**R15 Detalle de actividades terminadas** — con sus
> tres secciones, los dos relojes de tiempo y el fix de "A tiempo" — y **Notas de versión
> visibles para el usuario**. Liberado como **1.22.0.0** y **verificado en producción**.
> Detalle de ambos bloques en la tabla de la sección 2.)
>
> **Pendientes de este bloque:** (1) la nota de versión de la 1.22 **no está capturada** —
> el script `06_Scripts/02` sembró hasta la 1.21, y esa nota además no menciona el propio
> módulo de notas de versión, que se agregó después de escribirla; (2) las cuatro notas
> (1.18–1.21) están como **borrador**, hay que revisar la redacción y publicarlas desde la
> pantalla, y **confirmar las 3 fechas** de 1.18–1.20, que son tentativas porque esas
> versiones nunca se commitearon y su fecha real no existe en el repositorio.
>
> **Deuda de verificación que quedó al descubierto:** el R15 falló DOS veces en producción
> después de darlo por bueno con `dotnet build` y `oxlint` en verde (ver sección 5). La causa
> de fondo es que las pruebas de integración no corren aquí: `GTE.Api.Tests` **falla 28 de 36
> con 500 al pedir el token** en lugar de omitirse solas como dice el `CLAUDE.md` que deberían
> cuando no hay BD alcanzable. Vale la pena arreglar esa detección: hoy no hay forma de
> distinguir "se omitió por falta de BD" de "se rompió de verdad".
>
> **Aviso que salió de aquí:** `Directory.Build.props` es la única fuente de la versión y
> `publicar.bat` lo lee como XML. **No escribir notas ni texto suelto en ese archivo**: un
> comentario XML no admite dos guiones seguidos (que es como se escriben las viñetas) y
> cualquier contenido fuera de `</Project>` lo invalida. Ya tumbó una publicación con un
> error que no apunta al archivo (`"+" no es una cadena de versión válida`, en el restore de
> NuGet). Las notas ahora viven en `tblNotaVersion`.
>
> **Bloque anterior:** 2026-08-27 (**Centro de Mando TI** — módulo nuevo: evaluación
> mensual de los responsables de área con diagnóstico de causa raíz y alertas gerenciales.
> Verificado end-to-end contra la BD de desarrollo; **falta probar en un ambiente real y
> escribir pruebas automatizadas**.)
>
> **Qué es y por qué no duplica lo que ya había.** GTE ya tenía dos dashboards ejecutivos:
> el P18 (`IndicadoresEjecutivosQueryService`, equipo/proyecto: DORA, costo, OKR) y el de
> colaborador individual (`DashboardQueryService`, 6 dimensiones por persona). Ninguno de
> los dos evalúa **áreas**. Este módulo mide al **responsable de un área** (Desarrollo,
> Infraestructura, Soporte) y, sobre todo, responde *por qué* un score está bajo. La unidad
> de evaluación es `tblEquipo` y el responsable evaluado es su `IdLider` (decisión del
> equipo el 2026-08-27; se descartó `tblArea` porque no tiene responsable).
>
> **Lo que lo hace distinto de un contador de tickets** (y lo que no hay que romper al
> tocarlo):
> - **Diagnóstico de causa raíz** (`DiagnosticoCausaRaiz` en Domain): antes de atribuir un
>   score bajo a la persona se revisan 6 índices (espera, cambio de prioridad, alcance
>   inestable, carga, trabajo manual, dependencia única). `Persona` es el diagnóstico **por
>   descarte**, nunca el primero. Un índice en `null` (no medido) NO es lo mismo que dentro
>   de umbral: sin ningún índice con dato, el hallazgo es "falta instrumentación", que es
>   problema de gerencia, no del evaluado.
> - **Piso de cobertura** (`CalculadoraCentroMando.CoberturaMinima`, 40%): con menos del 40%
>   de indicadores con dato NO se publica score, se publica `SinDatosSuficientes` y se
>   nombra qué falta capturar. **Esto se agregó porque la primera verificación real dio
>   "100/100 Excelente" con 2 indicadores de 27** — exactamente el número engañoso que el
>   modelo existe para evitar. No quitarlo.
> - **Regla de piso del IT Health Score** (`CalculadoraSaludTi.TopeConRojo`, 79): si
>   Seguridad/continuidad o el score de cualquier responsable está en rojo, el score global
>   se topa en 79 y dice por qué. Impide que un área excelente disfrace una crítica.
> - **Origen `Manual` vs `Automatico`** en el catálogo: los indicadores sin fuente en GTE
>   (respaldos, cobertura de monitoreo, deuda técnica…) se reportan "sin datos" y quedan
>   fuera del score, nunca se les inventa un valor. Misma disciplina que el P18 con DORA
>   "Lead Time for Changes".
>
> **Esquema** (`DataBase/Scripts/03_Scripts/`, carpeta nueva de esta sesión): `01` permiso
> `AYU.CentroMando`; `02` las 5 tablas (`tblIndicadorGestion`, `tblEvaluacionEquipo`,
> `tblEvaluacionEquipoDetalle`, `tblDiagnosticoCausa`, `tblAlertaGestion`) + permisos
> `GES.Ver`/`GES.Administrar`; `03` siembra los **50 indicadores** (16 comunes + 11
> Desarrollo + 13 Infraestructura + 10 Soporte, pesos verificados en 100 por ámbito); `04`
> `tblEquipo.AmbitoCentroMando`. Se crearon tablas nuevas y NO se extendió
> `tblKpiDefinicion`/`tblKpiValor` a propósito: esas ya tienen dueño (widget de KPIs
> personalizados del P18, alimentado por `spSnapshotKpi`) y meterles categoría/umbral/peso
> ensuciaría ese widget.
>
> **El score de un responsable** = 60% bloque común + 40% bloque técnico de su área, con los
> pesos re-normalizados sobre los indicadores que sí tienen dato. Normalización: 100 en la
> meta, 50 en el umbral, lineal y recortada a [0,100] — funciona igual para indicadores que
> suben y que bajan, y permite promediar unidades incompatibles. El **semáforo se decide
> contra el valor crudo**, no contra el normalizado, para que "verde" signifique literalmente
> "cumplió la meta".
>
> **Ayuda:** el documento "Centro de Mando TI" (modelo completo, 16 secciones) se sirve por
> `GET /api/v1/ayuda/centro-mando-ti`, que **exige `AYU.CentroMando`**. Vive en
> `src/GTE.WebApi/Contenido/Ayuda/`, NO en `wwwroot`, justamente para que no sea legible por
> quien adivine la URL (el Manual de usuario sí es estático y para todos). El front lo baja
> autenticado y lo muestra desde un blob. Verificado: 401 sin token, 403 sin el permiso, 200
> con admin.
>
> **Verificado end-to-end** contra `ALIEN\SQLEXPRESS01`/`bdsGTE` con datos de prueba
> sembrados (20 tickets, 12 WorkItems, 2 incidentes de julio 2026): recálculo de los 3
> equipos, score de Soporte 71.7 con SLA 70% en rojo y retrabajo 16.7% en rojo, diagnóstico
> `Dependencia` por índice de espera 23.3%, 3 alertas (2 críticas + 1 con
> `RequiereGerencia`), y las tres pantallas renderizando. **Hueco encontrado y corregido en
> la verificación**: el recálculo manual no regeneraba alertas (solo el job de Hangfire lo
> hacía), así que el tablero se quedaba con alertas de una evaluación ya sobrescrita.
>
> **Pendiente real de este módulo:**
> - **Pruebas automatizadas** (no se escribió ninguna): `CalculadoraCentroMando.Normalizar`
>   y `DiagnosticoCausaRaiz.Diagnosticar` son lógica pura y son el candidato obvio para
>   `GTE.Domain.Tests`.
> - **`com.productividad` NO sirve hasta calibrarlo**: "puntos por hora de capacidad" depende
>   de cómo estime puntos cada equipo, no hay meta universal. Los valores sembrados (0.10 /
>   0.05) son un arranque conservador, no una meta con respaldo.
> - Los 4 índices de diagnóstico sin fuente (`AlcanceInestable`, `TrabajoManual`,
>   `DependenciaUnica`, y `CambioPrioridad` que hoy es un proxy por `tblHistorialCampo`)
>   necesitan captura nueva para que el modelo descarte causas con evidencia real.
> - Los 3 equipos de la BD de desarrollo quedaron con líder y ámbito **de prueba**
>   (`Desarrollador`→Desarrollo, `Soporte`→Soporte, `QA`→Infraestructura), y hay usuarios y
>   datos `prueba-claude`/`TST-*`/`WIT-*`/`INT-*` sembrados. Revisar antes de usar esa base
>   para otra cosa.
>
> **TRAMPA DE ENTORNO (esta máquina)**: el repo vive en una carpeta sincronizada de Google
> Drive (`G:\Otros ordenadores\...`) y su driver mantiene mapeados en memoria los `.exe` y
> DLL de `bin\`. Se manifiesta como `CreateAppHost` fallando con "sección asignada a usuario
> abierta" o `MSB3021 Access denied` al copiar dependencias, **sin que haya nada mal en el
> código**. Rodeos que funcionaron: `dotnet build -p:UseAppHost=false` para verificar
> compilación, y `dotnet publish -o <carpeta fuera de Drive>` para poder ejecutar. Ojo
> también: un `git checkout` sobre `Modelos/bdsGTE` puede pisar trabajo sin commitear y el
> sync puede restaurar archivos por su cuenta — verificar `git status` después.
>
> **OJO con el re-scaffold**: `dotnet ef dbcontext scaffold` contra la BD de desarrollo trae
> las tablas de Hangfire (`Job`, `Server`, `State`…) y las vistas `vwBI*`, que **no** estaban
> en el modelo commiteado, y además reporta diferencias en ~29 modelos no relacionados (el
> scaffold commiteado está desincronizado con esa base). En esta sesión se revirtió el
> scaffold completo y se aplicaron a mano solo las 5 entidades nuevas, la columna de
> `tblEquipo` y sus bloques de `OnModelCreating`. **Conviene reconciliar ese desfase en una
> sesión dedicada**, no de pasada.
>
> **Actualización anterior:** 2026-08-26 (**Versionado manual estándar Interflo, diagnóstico
> del almacén de archivos y ajustes de Releases** — fusionado a `main` en el merge commit
> `5108700`, PR #1, y **ya desplegado y probado en producción por el usuario**. El PR
> arrastró además 6 commits de sesiones anteriores que nunca habían llegado a `main`.)
>
> **Bug de producción resuelto — TODA subida de archivos fallaba con `INTERNAL_ERROR`**
> (imágenes pegadas en descripciones y comentarios, adjuntos de WorkItem, base de
> conocimiento, fotos de perfil, todas a la vez). No era de cada módulo: `appsettings.json`
> clava `AlmacenArchivos:Ruta = D:\GTE\Archivos` — la ruta de la máquina de desarrollo — y
> se publica tal cual, así que en un servidor sin unidad `D:` `Directory.CreateDirectory`
> truena en cada subida. **La causa de fondo estaba en `asistente-instalacion.html`**: tenía
> la ruta detrás de un checkbox opcional ("Los archivos adjuntos van a un share de red")
> cuya pista afirmaba que sin marcarlo se usaba una carpeta local junto al ejecutable, lo
> cual es **falso** — no escribía la variable y ganaba el `D:` de `appsettings.json`. Quien
> instalara con carpeta local dejaba la casilla sin marcar creyendo la pista y se llevaba la
> ruta de desarrollo al servidor. Ahora la ruta es obligatoria siempre (default local), y el
> script generado crea la carpeta y prueba escribir en ella durante la instalación.
>
> **El sistema ahora se autodiagnostica** en vez de dar un `INTERNAL_ERROR` ciego: el
> arranque comprueba la ruta y deja `ALMACEN DE ARCHIVOS NO DISPONIBLE` en el log (no aborta
> a propósito: un share caído puede volver y el resto de la API sirve igual);
> `GET /api/v1/version/almacen` (con identidad, la ruta del share no es dato público)
> reporta ruta resuelta, existencia y escritura; `AlmacenArchivosNoDisponibleException` da un
> mensaje útil con la ruta al log; y un binario ausente es 404 con explicación, no 500.
> Herramientas: `configurar-almacen-archivos.bat`/`.ps1` (nuevo, corrige solo esa variable en
> una instalación ya hecha) y `configurar-servicio-completo` pasa a registrar las **cuatro**
> variables (`RutaAlmacen` obligatoria en TERCERA posición, con guard si llega algo que no
> parece ruta, porque antes el tercero era el nombre del servicio) y ya no imprime la cadena
> de conexión ni la clave JWT completas.
>
> **Versionado (nuevo estándar en `CLAUDE.md`)**: 4 dígitos
> `Proyecto.Mejora.Defecto.Reenvío` para aplicación, sitios web e instaladores; 3 dígitos
> `Proyecto.Mejora.Defecto` para procedimientos almacenados (renglón `Version:` en el
> encabezado del script que crea el objeto). Al subir un dígito se resetean los de su
> derecha; si en una liberación van varios defectos y una mejora se versiona como mejora; si
> va un proyecto, se versiona solo como proyecto. **El número se sube A MANO al liberar,
> contra el informe de liberación** — no se genera solo ni se estampa con la fecha (el primer
> intento de esta sesión fue un sello `aaaa.MM.dd.HHmm`, descartado por no cumplir la regla).
> `Directory.Build.props` es la ÚNICA fuente; `publicar.bat` lo lee y estampa el mismo número
> en el ensamblado (`-p:Version`) y en el bundle (`VITE_VERSION`). La barra superior lo
> muestra debajo de "GTE" y **lo pinta en ámbar si bundle y API no coinciden**, que es la
> señal de un despliegue a medias (se copió `wwwroot` sin los DLL, o al revés). Versión en
> producción al cerrar esta sesión: `1.16.0.0`.
>
> **Releases**: el combo de tipo de artefacto tenía los cuatro tipos escritos a mano en la
> pantalla, así que editar `dbo.tblTipoArtefacto` en Catálogos no cambiaba nada — nuevo
> `GET /api/v1/catalogos/entregas`, que además devuelve el id de "Script SQL" para que el
> front no clave el `2` al decidir cuándo pedir la justificación de irreversibilidad
> (RN-GTE-032). Baja de contenido (el endpoint ya existía con su guarda, solo no estaba
> expuesto) y baja de artefactos (nueva de punta a punta, con bloqueo si el artefacto es la
> reversa de otro). El selector de contenido usaba la bandeja general, que no sabe nada de
> releases, y ofrecía elementos ya entregados en otra versión: nuevo
> `GET releases/{id}/candidatos` que filtra `IdRelease IS NULL`, ordena por folio y trae el
> conteo de hallazgos; la ventana suma filtros de folio/título, sprint, tipo y ocultar
> bloqueados, con contador y selección masiva (se elige el sprint y entra su contenido
> completo de un clic).
>
> **Decisión del equipo en esta sesión**: la bandera de "exige reversa" de los tipos de
> artefacto **se queda como constante en código** (`TipoArtefacto.ScriptSql`), NO se movió a
> una columna de `tblTipoArtefacto`. Se propuso hacerla configurable desde Catálogos (donde
> `TIPO_ARTEFACTO` ya está registrado como catálogo genérico) y el usuario decidió dejarla
> así. Consecuencia a tener presente: un tipo de artefacto nuevo creado desde Catálogos NO
> exigirá reversa; para eso hay que editar la constante y republicar.
>
> `publicado/` se agregó al `.gitignore` (eran ~100 MB de artefactos de despliegue sin
> versionar en la raíz). `DataBase/bdsGTE.sql` y `DataBase/script.sql` siguen deliberadamente
> fuera del control de versiones: son dumps UTF-16 de SSMS "Generate Scripts", no
> idempotentes, y no son la fuente de verdad del esquema (lo es `DataBase/Scripts`).
>
> **Bloque anterior — 2026-08-13** (**Catalogo de Reportes R01-R14 + vistas vwBI*** --
> segundo bloque de la "Fase 5 completa" (Dashboard P18 -> Reportes/PowerBI -> Automatizaciones
> -> IA). Extiende el modulo `Reportes` que ya existia con un solo reporte (Actividad de
> usuario, permiso `RPT.Actividad`) en vez de duplicarlo: mismo `IReportesQueryService`/
> `ReportesController`/namespace `GTE.Application.Reportes`, 13 metodos nuevos + el ya
> existente. Permisos nuevos `RPT.Ver` (general) y `RPT.Auditoria` (R14); `RPT.Costos`
> (ya sembrado desde el script 02 sin consumidor) ahora lo usan R08/R09 -- ver script
> `31_2026-08-13_INSERT_bdsGTE_PermisosReportes.sql`.
>
> **Los 14 reportes reusan fuentes ya existentes en vez de duplicar calculo** (misma
> disciplina que el Dashboard P18): `vwBandejaTrabajo`/`vwTiempoInvertido` (R01/R03),
> `vwCostoRegistroTiempo` (R08/R09, igual que `ICosteoQueryService`), `tblRiesgo` (R06, mismo
> gap que P18 -- sin CRUD todavia via A5, normalmente vacio), `tblKpiDefinicion`/`tblKpiValor`
> (R11, mismo snapshot de Hangfire del Dashboard P18). Piezas nuevas construidas desde cero
> (sin apoyo previo en el repo): R13 Flujo/CFD (reconstruye el estatus de cada WorkItem dia
> por dia a partir de `tblHistorialEstatus`, con una entrada sintetica de creacion en
> Pendiente para no depender de que el trigger haya logueado el estado inicial) y R14
> Auditoria (primer consumidor de `tblBitacora`, que ya se llenaba via
> `RepositoryBase.RegistrarBitacoraAsync` pero nadie la leia). Constantes nuevas en Domain
> para dejar de usar numeros magicos: `GTE.Domain.WorkItems.TiposWorkItem` (antes solo
> `EstatusIncidente.IdTipoWorkItemCorreccion`, mismo valor 9).
>
> **Exportacion a Excel**: `ClosedXML` (MIT, gratis) instalado -- decision ya confirmada con
> el usuario en el bloque anterior sobre EPPlus (licencia comercial de paga desde v5).
> `IExportadorExcel`/`ExportadorExcelClosedXml` generico (encabezados + filas de
> `object?`), cada reporte tiene un endpoint gemelo `.../exportar` que re-corre la misma
> query y aplana el DTO a filas -- sin libreria de PDF nueva, "Exportar PDF" sigue sin
> construirse en Reportes (a diferencia del Dashboard, aqui el Documento Maestro no lo pedia
> explicito por pantalla, solo Excel).
>
> **Power BI**: vistas `vwBIWorkItems`, `vwBICostos`, `vwBIReleases`, `vwBIRiesgos`,
> `vwBISla` (script `33_2026-08-13_SCRIPT_bdsGTE_VistasBI.sql`) -- tablas de hechos planas
> con nombres ya resueltos, para que el analista arme sus propios pivotes. R13 (necesita
> reconstruir estatus dia por dia, no es una fila-por-hecho) y R14 (sensible/paginada) se
> quedan fuera a proposito; R02/R07/R11 ya son planas de origen y no necesitaban vista
> nueva. Usuario SQL de solo lectura `bi_gte_sololectura` (`db_datareader` unicamente, sin
> datawriter ni EXECUTE) en script `32_2026-08-13_SCRIPT_bdsGTE_UsuarioSoloLecturaBI.sql` --
> mismo patron de blindaje que `svc_gte`, requiere editar `@Password` antes de correr (no se
> corrio en esta sesion, es un script de despliegue).
>
> **Frontend**: `features/reportes/CatalogoReportesPage.tsx` (nueva, sidebar con los 14
> reportes + tabla/grafica por reporte, filtros Desde/Hasta/Proyecto/Equipo segun aplique,
> boton Exportar Excel por reporte) en la ruta `/reportes` -- el menu "Reportes" ahora
> apunta aqui (antes iba directo a `/reportes/actividad-usuario`, que se dejo intacto como
> ruta valida sin quitar del router, solo ya no es el destino del menu). R11 usa `recharts`
> `LineChart` (serie del anio vs anio comparativo); R13 usa `AreaChart` apilado (`stackId`)
> para el CFD -- `Line` de recharts no soporta `stackId` en la tipificacion actual, hubo que
> usar `Area`/`AreaChart` en su lugar.
>
> **Bug real encontrado y corregido en la misma sesion antes de llegar a produccion**: R07
> (tiempos de triage por area) calculaba mal el promedio -- usaba `g.First(s => true).FechaRegistro`
> (la fecha de la PRIMERA solicitud del grupo) como base para TODAS las solicitudes del area
> en vez de la fecha de registro de cada una. Corregido antes de la verificacion en vivo.
>
> **Verificado**: `dotnet build`/`dotnet test` (53/53) y `tsc -b`/`vite build` limpios.
> Prueba manual real en el navegador (LocalDB real, login `aviramontes`) via el mismo
> `.claude/launch.json` temporal de la sesion anterior (revertido al terminar): R01 con
> datos reales de Julio 2026 (Lead/items/puntos/% a tiempo coherentes con lo ya visto en el
> Dashboard P18); R08 Costos con montos reales por proyecto/mes y por desarrollador
> (confirma que `vwCostoRegistroTiempo` resuelve tarifas correctamente); R06 Riesgos vacio
> como se esperaba; R11 KPIs vacio hasta correr manualmente `spSnapshotKpi` contra LocalDB
> (se corrio a mano para la prueba, mismo criterio que otros datos de prueba dejados adrede
> en LocalDB -- confirma el pipeline completo SP -> tblKpiValor -> reporte -> grafica); R13
> Flujo con un proyecto real mostro el area apilada por estatus sin errores; R14 Auditoria
> mostro 11 paginas de bitacora real (transiciones de workflow, altas JIT, etc.) -- primer
> consumidor real de esa tabla; exportar a Excel (R08) confirmado 200 OK por red. **No
> verificado en profundidad**: R02, R04, R05, R09, R10, R12 (mismos patrones ya probados en
> R01/R03/R08 y en el Dashboard P18, no se click-through uno por uno por tiempo). **Pendiente
> real**: pruebas automatizadas del modulo nuevo; decidir si vale la pena una pantalla de
> administracion de riesgos (R06/A5) ya que ahora hay tres consumidores esperando datos
> (Dashboard P18, este reporte, y la vista BI) sin ninguna forma de capturarlos.
>
> **Siguiente en la secuencia acordada**: bloque 3 (motor de automatizaciones A01-A23,
> Hangfire ya instalado desde el bloque 1 -- reusar esa infraestructura), luego IA cuando
> haya API key de Anthropic.
>
> **Actualización anterior 15:** 2026-08-13 (**Dashboard Ejecutivo P18** -- primer bloque de la
> "Fase 5 completa" pedida por el usuario (Dashboard P18 -> Reportes/PowerBI -> Automatizaciones ->
> IA, en ese orden; IA excluida de esta ronda por falta de API key de Anthropic disponible;
> Reportes usara ClosedXML en vez de EPPlus; Automatizaciones solo InApp+Correo reales, Teams/
> WhatsApp/Slack quedan como interfaz sin implementar -- decisiones confirmadas con el usuario
> antes de codear). Implementa la seccion 3.10/5.10 del Documento Maestro: vista de
> equipo/proyecto (DORA, costo, rentabilidad, OKR), **distinta del Dashboard de colaborador
> individual** ya existente (`/dashboard-ejecutivo`, GET /api/v1/dashboard) -- para evitar
> confundir ambos conceptos se le puso nombre y ruta propios: **"Indicadores ejecutivos"**
> (`/indicadores-ejecutivos`, `GET/PUT api/v1/indicadores-ejecutivos`). Requiere permiso
> `DASH.Ejecutivo` o `DASH.VerDepartamento` (ambos ya sembrados) -- a diferencia del dashboard
> individual, aqui SI bloquea con 403 sin ninguno de los dos: no hay "alcance personal" razonable
> para cifras de costo/DORA de proyecto.
>
> **Indicadores calculados en tiempo real** (mismo criterio que el dashboard de colaborador, sin
> job para lo que se puede calcular al vuelo): Lead Time (percentil 50/85 en horas laborales via
> `ICalendarioLaboral`, horario del asignado o un default resuelto por el mas antiguo activo),
> Cycle Time (reusa `vwBandejaTrabajo.MinutosInvertidos`, ya materializado), Entrega a tiempo
> (semaforo heredado 90/80), Eficiencia y Retrabajo (tipo Correccion=9), Productividad (puntos
> entre personas distintas), SLA+CSAT (global, `tblTicket` no tiene FK de proyecto en el modelo
> asi que no se puede acotar por alcance -- limitacion real, no bug), Semaforo de proyectos
> (entrega a tiempo por proyecto + costo real/presupuesto reusando `ICosteoQueryService` tal
> cual, sin duplicar logica de costeo), OKR (reusa `IOkrQueryService` tal cual), Top riesgos
> (query real contra `tblRiesgo`, que ya existe con workflow sembrado pero sigue sin CRUD/UI --
> A5 -- asi que normalmente aparecera vacio hasta que exista forma de capturar riesgos), y
> Burndown del sprint activo por equipo (ideal lineal vs real dia por dia, requiere seleccionar
> equipo en el filtro).
>
> **DORA**: Deployment Frequency y Change Failure Rate se calculan reales (`tblDespliegue`
> exitoso a un ambiente con "PROD" en el nombre, `tblRelease`/`tblIncidente.IdReleaseCausante`
> en ventana de 7 dias) y MTTR real (`tblIncidente.FechaResolucion - FechaOcurrencia`). **Lead
> Time for Changes queda explicitamente "sin datos"** -- necesita integracion Git (PR merge ->
> deploy), que sigue pendiente (resto de Fase 3, tblPullRequest sin consumidor). Nota de la
> prueba en vivo: MTTR salio negativo (-5.9h) en LocalDB porque un incidente migrado/semilla
> (`INC-2026-0001`) tiene `FechaResolucion` anterior a `FechaOcurrencia` -- es un dato de prueba
> inconsistente, no un bug de la formula (se decidio no forzar un piso en 0 para no esconder el
> problema de datos).
>
> **KPIs personalizados** (`tblKpiDefinicion`/`tblKpiValor`, sin consumidor desde que se crearon
> el 2026-07-30): esta es la pieza que si necesitaba un job nocturno, asi que se instalo
> **Hangfire** (`Hangfire.AspNetCore`/`Hangfire.SqlServer`, LGPLv3 gratis) -- primer consumidor
> real de A4 (adelantado desde el bloque 3 de automatizaciones porque el propio P18 lo
> necesitaba). `SnapshotKpiJob` llama `dbo.spSnapshotKpi` (ya existia, con `@Mensaje OUTPUT`)
> via ADO igual que `GeneradorFolios`/`spGenerarFolio`; recurrente diario a la 1am con
> `IRecurringJobManager` (API por servicio, no la estatica `RecurringJob` -- la estatica depende
> de `JobStorage.Current` y truena en pruebas de integracion, donde el proceso hospeda varios
> hosts). Storage propio en `bdsGTE` (schema `[HangFire]` que la libreria crea sola). **Sin
> dashboard web de Hangfire expuesto** (no entiende el JWT propio de GTE, exigiria un filtro de
> autorizacion dedicado -- pendiente si se necesita mas adelante). El registro del job recurrente
> al arrancar esta en try/catch: si `bdsGTE` no esta disponible en ese instante, el resto de la
> API igual arranca (encontrado real con `VersionEndpointTests`, que arranca la API sin BD real).
> **Deshabilitado explicitamente en pruebas de integracion** (`Hangfire:Deshabilitado=true` en
> `FabricaApiAutenticada`): cada prueba levanta su propio `WebApplicationFactory`, y reinstalar/
> consultar el storage SQL de Hangfire en cada una saturaria LocalDB (mismo tipo de congestion ya
> documentado en este archivo) ademas de que `RecurringJob` estatico no se reinicializa entre
> hosts sucesivos del mismo proceso -- encontrado real que rompia 27 de 28 pruebas de
> `GTE.Api.Tests` antes de aislarlo.
>
> **Tabla nueva**: `tblDashboardLayoutUsuario` (script 30, IdUsuario PK/FK 1:1, LayoutJson
> NVARCHAR(MAX)) para persistir el layout de widgets (orden + ocultos) por usuario -- upsert por
> EF en `IndicadoresEjecutivosRepository`, sin SP (mismo criterio del resto de GTE, sin stored
> procedures para CRUD simple). Sin permiso nuevo: reusa `DASH.Ejecutivo`/`DASH.VerDepartamento`.
>
> **Frontend**: `features/indicadoresEjecutivos/IndicadoresEjecutivosPage.tsx`, widgets
> arrastrables via `@dnd-kit/sortable` (ya era dependencia del repo, sin agregar libreria nueva)
> con boton de ocultar/mostrar por widget, persistidos de inmediato al arrastrar/ocultar. Reusa
> `obtenerCatalogosBandeja()` para los combos de Proyecto/Equipo (no se construyo un endpoint de
> filtros propio, ese catalogo ya trae ambos). "Exportar Excel" es CSV client-side y "Exportar
> PDF" es `window.print()`, mismas simplificaciones ya establecidas en el dashboard de
> colaborador. Menu nuevo "Indicadores ejecutivos" gateado por permiso (a diferencia de
> "Dashboard ejecutivo" que es publico).
>
> **Verificado**: `dotnet build`/`dotnet test` (53/53, incluye la correccion de las 27 pruebas
> rotas por Hangfire) y `tsc -b`/`vite build` limpios. Prueba manual real en el navegador
> (API en LocalDB real + SPA, login `aviramontes`/Administrador) via `.claude/launch.json`
> temporal para esta sesion (revertido al terminar, no se toco el `launch.json` compartido del
> equipo): pantalla carga con datos reales (Julio 2026 con datos migrados dio Lead Time
> P50=9.0h/P85=17.4h, Cycle Time=2.7h, Entrega a tiempo=14.9%; Agosto 2026, sin cierres aun, dio
> ceros/100% consistentes); filtro de mes recalcula todo en vivo; semaforo de los ~47 proyectos
> activos renderiza (todos "Verde" por falta de compromiso/presupuesto capturado, esperado);
> OKR real ("Mejorar tiempo de entrega", Plantilla Angular, 2/3) se ve con barra de avance;
> ocultar un widget y recargar la pagina confirma que el layout persiste (GET/PUT reales). **No
> verificado end-to-end**: el arrastre real de widgets con mouse -- un `left_click_drag` simple y
> tambien una secuencia sintetica de PointerEvent (el mismo patron que ya funciono para el
> kanban) no dispararon el sensor de `@dnd-kit/sortable` en este entorno de automatizacion; el
> mecanismo de guardado (misma funcion que ya se probo con ocultar/mostrar) esta verificado, solo
> falta el drag en si con mouse real. Pendiente real: pruebas automatizadas (Domain.Tests para
> `CalculadoraIndicadoresEjecutivos.Percentil`, Api.Tests para el endpoint nuevo).
>
> **Siguiente en la secuencia acordada**: bloque 2 (Reportes R01-R14 + vistas `vwBI*` con
> ClosedXML), luego bloque 3 (motor de automatizaciones A01-A23 + Hangfire ya instalado, solo
> canales InApp/Correo reales), luego IA cuando haya API key de Anthropic.
>
> **Actualización anterior 14:** 2026-08-07 (**Dashboard Ejecutivo de Metricas** -- modulo nuevo pedido
> directo por el usuario a partir de una especificacion funcional amplia (empleado del mes,
> resumen ejecutivo, carga de trabajo, indicadores por empleado, comparativos, rankings,
> tendencias, filtros globales, permisos por rol, exportacion). Antes de codear se reconcilio
> contra lo ya documentado en la seccion 3.10/5.10 de este Documento Maestro (P18 Dashboard
> Ejecutivo, enfocado a DORA/costo/OKR de equipo) -- son cosas distintas: este modulo nuevo mide
> **colaboradores individuales**, no reemplaza el P18 original. Decisiones de diseno acordadas
> con el usuario: el puntaje/evaluacion mensual se calcula 100% automatico (sin captura manual),
> el Empleado del mes se elige automatico por mayor puntaje, y la jerarquia "colaboradores de un
> lider" usa `tblUsuario.IdJefe` (ya existia en el modelo, no fue necesario agregarlo) mas
> `tblEquipo.IdLider`/`tblEquipoMiembro`.
>
> **Sin tablas nuevas**: todo se calcula en tiempo real contra datos existentes (WorkItem,
> Ticket, Incidente, Release, RegistroTiempo, Comentario) -- no hay job nocturno ni snapshot
> persistido (a diferencia de lo que el Documento Maestro dejaba previsto via
> `tblKpiDefinicion`/`tblKpiValor` para KPIs de equipo/proyecto, que siguen sin consumidor). Si
> el volumen de datos crece mucho, ese job queda como mejora futura natural sin cambiar el
> contrato de la API. Foto de "Empleado del mes": se reutiliza el mecanismo generico de archivos
> ya existente (`tblArchivo`/`tblArchivoVinculo`, `Entidad='Usuario'`) en modo **solo lectura**
> -- no se construyo pantalla de carga de foto en esta pasada (no hay hoy ningun lugar en
> Administracion > Usuarios para subir una foto de perfil; si no hay foto se muestra un avatar
> con iniciales).
>
> **Formulas de puntaje (heuristicas v1, documentadas y aisladas en
> `GTE.Domain.Dashboard.CalculadoraPuntaje` para poder ajustarlas despues sin tocar el resto)**:
> de las 6 dimensiones de evaluacion mensual (Calidad, Productividad, Puntualidad, Trabajo en
> equipo, Cumplimiento, Comunicacion), 4 tienen una fuente de datos solida (Puntualidad =
> % de entregas a tiempo; Cumplimiento = % de asignados vigentes sin vencer; Productividad =
> terminados vs promedio del equipo; Calidad = 100 menos % de reaperturas via incidente ligado
> a un WorkItem ya cerrado) y 2 son proxies mas debiles por falta de un dato mejor en el modelo
> actual (Trabajo en equipo = comentarios en items ajenos vs promedio del equipo; Comunicacion =
> total de comentarios vs promedio del equipo) -- senalado explicitamente al usuario, no
> silenciado. Una dimension sin datos suficientes se reporta como "sin datos" y no participa en
> el promedio general.
>
> **Alcance/visibilidad por rol** (sin tabla de roles nueva tipo Director/Gerente/Empleado que
> no existen en el seed de `tblRol`): permiso `DASH.Ejecutivo` (ya sembrado desde el script 02,
> sin consumidor hasta ahora) = alcance Global; permiso nuevo `DASH.VerDepartamento` (script 26)
> = alcance del area propia (via `tblUsuario.IdPuesto -> tblPuesto.IdArea`); sin ninguno de los
> dos pero liderando un equipo o con subordinados directos = alcance "Equipo" (por datos, sin
> permiso adicional); caso base = alcance "Personal" (solo su propia informacion). La ruta
> `/dashboard-ejecutivo` es visible para **cualquier usuario autenticado** (`permiso: null` en
> `NAVEGACION`, igual que "Mi dia") -- el permiso no gatea la pantalla, solo escala cuanto ve.
>
> **Simplificaciones deliberadas frente al prompt original** (por alcance/tiempo, senaladas
> directo al usuario): sin tema claro/oscuro (la app entera no tiene hoy infraestructura de
> dark mode; agregarlo solo para esta pantalla habria sido inconsistente); filtros globales
> implementados solo para Anio/Mes/Proyecto/Area/Empleado (Lider es redundante con el alcance
> automatico; Estado/Prioridad/Tipo de trabajo no tienen un catalogo unico coherente entre
> WorkItem/Ticket/Incidente/Release a la vez, se hubiera visto roto); "Exportar Excel" es un
> CSV client-side (sin libreria nueva) y "Exportar PDF" es `window.print()` del navegador (sin
> hoja de estilos de impresion dedicada); Incidencias/Releases no tienen asignacion directa a un
> usuario en el modelo (gap ya documentado en sesiones previas para "solo mis proyectos") --
> se acotan por **proyectos visibles** (responsable/equipo del alcance), no por persona.
>
> **Backend**: `GTE.Domain.Dashboard` (formulas), `IDashboardQueryService`/`DashboardQueryService`
> (Infrastructure, un solo `DbContextGTE` por consulta via `FabricaContexto`, reusa
> `ICalendarioLaboral` para horas disponibles), `DashboardController` con 4 endpoints
> (`GET /api/v1/dashboard`, `.../empleados/{id}`, `.../tendencias`, `.../filtros`) --
> patron de payload agregado como `/api/v1/mi-dia`, no fragmentado en llamadas sueltas.
> Script SQL nuevo `26_2026-08-07_INSERT_bdsGTE_PermisoDashboardDepartamento.sql`
> (`DASH.VerDepartamento`), aplicado contra LocalDB. **Frontend**: libreria `recharts` agregada
> (no habia ninguna grafica en el repo todavia); `features/dashboard/DashboardEjecutivoPage.tsx`
> + `EmpleadoDrillDownDialog.tsx`; `shared/api/dashboard.ts`; ruta y entrada de menu nuevas en
> `App.tsx`.
>
> **Verificado**: `dotnet build`/`dotnet test` (53/53 en verde, sin pruebas nuevas -- no se
> agregaron pruebas automatizadas para el modulo nuevo en esta pasada, queda como pendiente
> real); `tsc -b`/`vite build` limpios. Prueba manual real en el navegador contra la API+SPA
> levantadas con datos migrados reales (login `aviramontes`): los 4 endpoints responden 200 con
> datos coherentes; filtro Empleado narrows correctamente todo el payload (empleado del mes,
> carga de trabajo, rankings, y hace aparecer el comparativo "Empleado vs promedio del equipo");
> seccion Tendencias (lazy, bajo demanda) carga y cambia de metrica; dialogo de indicadores
> individuales (Eficiencia/Entregas/Evaluacion mensual/Evolucion anual) abre al hacer click en
> una fila y trae datos reales; exportar CSV no truena. **Nota real encontrada en vivo**: al
> filtrar el dashboard a un solo Empleado, las dimensiones relativas-al-equipo (Productividad,
> Trabajo en equipo, Comunicacion) degeneran a compararse contra si mismas (el "equipo" del
> filtro es un conjunto de 1) -- el dialogo de detalle individual no tiene este problema porque
> siempre calcula sus colegas reales (equipo/area) sin importar el filtro superior; es course
> intencional, no bug, pero vale la pena que el equipo lo sepa. **Pendiente real**: pruebas
> automatizadas del modulo nuevo (Domain.Tests para `CalculadoraPuntaje`, Api.Tests para los 4
> endpoints); revisar con el negocio si las formulas heuristicas de Trabajo en equipo/
> Comunicacion son aceptables o si conviene moverlas a captura manual como hibrido.)
>
> **Correccion misma sesion, mismo dia** (feedback del usuario probando el dashboard en vivo):
> (1) **Empleado del mes cambiaba con los filtros** -- estaba calculado sobre el mismo
> `usuarios` (alcance+filtros) que el resto del dashboard; ahora se calcula SIEMPRE sobre todos
> los empleados elegibles del sistema completo, sin importar el alcance de quien consulta ni
> los filtros de Area/Proyecto/Empleado (actual e historico). (2) **Exclusion de cuentas que no
> son colaboradores medibles**: nuevo helper `ObtenerIdsExcluidosAsync` en `DashboardQueryService`
> -- excluye cuentas con rol Administrador (cuentas de operacion del sistema, no colaboradores)
> y cuentas `EsExterno=true` (ej. "Solicitante Externo (migracion GT)") de TODAS las metricas
> (empleado del mes, carga de trabajo, rankings, comparativos, catalogo de filtro Empleado) --
> siguen pudiendo USAR el dashboard para ver a otros, solo no aparecen como sujeto medido.
> (3) **Bug real encontrado y corregido de la sesion anterior**: el `UrlFoto` (tanto de
> Empleado del mes como del drill-down individual) apuntaba directo a
> `/api/v1/archivos/{guid}` usado como `<Avatar src>` -- un `<img src>` NUNCA manda el
> Authorization Bearer (la app no usa cookies para el access token), asi que la foto jamas
> habria cargado (401 silencioso, Avatar cae a las iniciales). Se creo `AvatarUsuario`
> (`shared/components/AvatarUsuario.tsx`), que descarga el archivo autenticado como blob
> (reutiliza `descargarArchivoBlob` ya existente) y usa un object URL -- mismo patron que ya
> advertia el comentario de `archivos.ts` sobre nunca usar `<img src>`/`<a href>` directo.
> (4) **Carga de trabajo por elementos**: seccion nueva (tabla + PieChart) que replica el
> reporte "Carga de trabajo" del GT que el usuario tenia como referencia (Pendiente/EnProceso/
> Terminado/Retrasos/PromDuracionDias/Total/EficienciaEntrega por colaborador, mas pie de
> distribucion de carga total) -- nuevo `CargaTrabajoDetalleResponse` y
> `ArmarDesgloseCargaTrabajoAsync`, con el mismo periodo (Anio/Mes) que el resto del dashboard
> en vez de un selector de rango Inicio/Fin independiente (simplificacion deliberada para no
> duplicar el filtro global). (5) **Rankings**: cada pestaña de metrica ahora muestra tambien
> un BarChart (Top10 y Bottom10) ademas de la tabla, clickeable para abrir el mismo dialogo de
> detalle. (6) **Tooltips explicativos** en KPIs del resumen ejecutivo, columnas de carga de
> trabajo, pestañas de ranking y las 6 dimensiones de evaluacion mensual (componente
> `EtiquetaConTooltip`), con la formula en lenguaje llano. (7) **Administracion > Usuarios**:
> gap real confirmado -- el formulario ya podia asignar Puesto (que carga Area) pero no
> exponia Area de forma directa, y no existia forma de subir foto de perfil (el modulo
> completo de foto era de solo lectura hasta ahora). Se agrego combo Area (filtra las opciones
> de Puesto) en alta/edicion, y subida de foto en edicion (`POST /api/v1/usuarios/{id}/foto`,
> `SubirFotoUsuarioCommand` -- mismo patron que `SubirArchivoCommand` de WorkItem pero
> reemplaza la foto anterior en vez de acumular, valida solo imagen vs la lista general de
> `ConstantesArchivos.ExtensionesPermitidas`). `UsuarioResponse` ahora expone `UrlFoto`
> (join con `tblArchivoVinculo`/`tblArchivo` filtrado a `Entidad='Usuario'`).
> **Verificado**: `dotnet build`/`dotnet test` (53/53) y `tsc -b`/`vite build` limpios tras
> cada cambio; prueba real en el navegador con datos migrados -- empleado del mes confirmado
> igual con el filtro Empleado activo y sin el (antes cambiaba), Ana Viramontes (Administrador)
> ya no aparece en ninguna metrica, tabla+pie de carga por elementos con datos reales, dialogo
> de edicion de Usuario muestra el combo Area y el boton "Cambiar foto" correctamente. **No
> verificado**: la subida de foto en si de punta a punta (requiere seleccionar un archivo real
> desde el dialogo nativo del SO, fuera del alcance de la automatizacion de navegador
> disponible en esta sesion) -- el endpoint se probo por codigo/compilacion y sigue el mismo
> patron ya probado de adjuntos de WorkItem.
> **Correccion misma sesion, mismo dia (2)**: el usuario reporto que un desarrollador siempre
> sobrecargado "cumple con las metricas" -- investigando encontramos la causa real: RN-REQ-08
> (presupuesto automatico via `tblMatrizPresupuesto`, complejidad x nivel del asignado) ya
> existia pero **nunca se disparaba en la practica** porque `NuevoItemModal.tsx` no capturaba
> Complejidad (solo el modal de Editar la tenia). Decision del usuario: Complejidad pasa a ser
> **obligatoria al crear** (no en editar, ahi se queda opcional como antes) y **Puntos de
> historia se congela automatico igual que los minutos** (antes era 100% manual y la columna
> `tblMatrizPresupuesto.Puntos`, aunque sembrada por la migracion, nunca se leia). Cambios:
> `IWorkItemRepository.ObtenerMinutosMatrizAsync` -> `ObtenerPresupuestoMatrizAsync` (nuevo
> record `PresupuestoMatriz(Minutos, Puntos)`); `CrearWorkItemHandler.CalcularPresupuestoAsync`
> devuelve ambos y `CrearWorkItemValidator` exige `IdComplejidad`; `ActualizarWorkItemCommand`
> recalcula Puntos junto con Minutos solo cuando cambia complejidad/asignado (antes Puntos
> viajaba tal cual del request); se quito `PuntosHistoria` de `WorkItemCrearRequest`/
> `WorkItemActualizarRequest` (ya no se acepta a mano). Frontend: `NuevoItemModal.tsx` gano
> combo Complejidad requerido con leyenda explicando que fija horas+puntos automatico;
> `ModalEditarWorkItem.tsx` cambio el TextField de Puntos a solo lectura. **Pruebas de
> integracion rotas por el cambio** (8 en `GTE.Api.Tests`, todas las que crean un WorkItem via
> HTTP sin mandar complejidad): se agrego un helper compartido
> `FabricaApiAutenticada.ObtenerOCrearComplejidadAsync()` (reutiliza cualquier fila activa o
> crea una propia) y se uso en los 4 archivos de prueba afectados.
> **Verificado**: `dotnet build`/`dotnet test` (53/53) limpios; `tsc -b` limpio; en vivo contra
> LocalDB real (usuario lgarcia): crear sin complejidad -> 400 "La complejidad es obligatoria."
> con el mensaje correcto; crear con Complejidad=Media(4) y asignado Luis Garcia -> devolvio
> `minutosPresupuesto=840` y `puntosHistoria=7.00` automaticos (datos reales de la migracion);
> editar sin tocar complejidad/asignado conserva ambos valores; editar cambiando complejidad
> sin el permiso `WI.ModificarComplejidad` sigue bloqueado con 403 (la regla de permisos
> existente no se toco). **Pendiente real**: revisar con el negocio si conviene tambien exigir
> Complejidad en el modal de Editar para los WorkItems legacy que quedaron sin ella.
>
> **Correccion misma sesion, mismo dia (3)**: el usuario confirmo "si que los exija" (Complejidad
> tambien obligatoria al editar). Al revisar donde mas se reutiliza `CrearWorkItemCommand` para
> implementarlo, se encontro una **regresion real introducida en la correccion (2) de arriba**:
> 4 flujos internos crean WorkItems SIN pasar `IdComplejidad`
> (`ConvertirSolicitudCommand`/Triage, `VincularCorrectivoIncidenteCommand`, `EscalarTicketCommand`,
> `CalidadCommands` al crear un bug desde una ejecucion fallida) -- con `IdComplejidad` obligatorio
> a nivel de comando, los 4 habrian empezado a fallar con 400 en cuanto se desplegara. Se
> corrigio ANTES de que llegara a produccion: `CrearWorkItemCommand` ya NO exige `IdComplejidad`
> por FluentValidation (esos 4 flujos no tienen momento de captura humana); en vez de eso,
> `CrearWorkItemHandler.Handle` ahora rellena un default (`ObtenerComplejidadPorDefectoAsync`,
> la complejidad activa de menor `Orden`) cuando viene nula -- asi RN-REQ-08 siempre tiene con
> que calcular presupuesto sin bloquear ni tocar esas 4 pantallas. El modal de alta manual
> (`NuevoItemModal.tsx`) sigue exigiendola como campo obligatorio en la UI (una persona la elige
> a conciencia en el camino principal), pero el backend ya no depende de eso para garantizar
> que el presupuesto se calcule.
>
> Para Editar si se implemento la exigencia dura pedida: `ActualizarWorkItemValidator` exige
> `IdComplejidad` (ahi NO hay flujos internos, se verifico que solo el controller construye
> `ActualizarWorkItemCommand`). Para no bloquear retroactivamente los items legacy que se creen
> sin ella, se aplico el mismo criterio que RN-REQ-04 (fecha compromiso): **fijarla por primera
> vez (de vacia a un valor) queda libre; solo RE-CLASIFICAR una complejidad ya elegida sigue
> exigiendo el permiso `WI.ModificarComplejidad`**.
> **Verificado en vivo contra LocalDB real**: crear sin complejidad (simulando un flujo interno)
> -> 200 OK con complejidad default (`Basica`, Orden 1) y presupuesto calculado (120 min / 1
> punto); editar GTE-0012 (legacy, `idComplejidad=null`) sin mandar complejidad -> 400; el mismo
> item capturando complejidad por primera vez como `lgarcia` (sin `WI.ModificarComplejidad`) ->
> 200 OK con 840 min / 7 puntos calculados, SIN pedir el permiso especial. `dotnet test`
> 53/53 (una corrida intermedia broto con timeouts/500 por congestion de LocalDB tras tantas
> corridas seguidas en la misma sesion -- se resolvio con `sqllocaldb stop/start`, no era un
> problema de codigo); `tsc -b` limpio.
> **Actualización anterior 13:** 2026-08-04 (Correccion de 3 huecos reportados por el usuario probando la sesion anterior en vivo: (1) **"Mis solicitudes" (`PortalPage.tsx`) no tenia buscador ni orden** -- se me habia pasado esta lista al hacer el lote general de buscadores/orden (Revision de solicitudes SI ya lo tenia, confirmado de nuevo en vivo: si no se ve, es cache del navegador, refrescar). Mismo patron `useOrdenTabla`/`EncabezadoOrdenable` que el resto. (2) **Pegar imagenes en la Descripcion de una Solicitud era un callejon sin salida real**: la limitacion de "no se puede pegar antes de guardar" (documentada como aceptable, igual que WorkItem) se volvia permanente porque una Solicitud NUNCA se podia editar despues de creada -- sin edicion, jamas habia un momento para agregar imagenes. Se construyo edicion real de Solicitud (`PUT /api/v1/solicitudes/{id}`, `ActualizarSolicitudCommand`, nueva regla: el propio solicitante siempre puede editar la suya, cualquier otro necesita `SOL.Triage`, solo mientras el estatus siga en Enviada/EnAnalisis/Aprobada) y se extendio el mecanismo de adjuntos -- ya generico por diseño (`ArchivoNuevo(Entidad, IdEntidad, ...)`, solo WorkItem lo consumia -- a Solicitud (`POST/GET /api/v1/solicitudes/{id}/archivos`, mismo patron que WorkItem). `EditorEnriquecido` gano un prop generico `onSubirImagen` (con prioridad sobre `idWorkItemParaAdjuntos`) para no atarlo a una sola entidad. Con la Solicitud ya guardada, "Editar" abre el mismo editor con `idSolicitud` real -- pegar imagenes ya funciona. Se exponen `idTipoSolicitud`/`idPrioridad`/`idUsuarioSolicitante` en `SolicitudResponse` (antes solo el nombre resuelto) para poder precargar el formulario de edicion sin adivinar por nombre. (3) **Dialogo "Convertir en elementos de trabajo" con controles apretados**: una sola fila con Tipo/Titulo/Prioridad/Asignado/Compromiso + icono de borrar en un dialogo de 900px dejaba Titulo con el resto de espacio sobrante (a veces <150px) y el resto de campos en 120-150px. Rediseñado: dialogo mas ancho (900px -> ~1100px), cada item en su propia tarjeta (`Paper`) con Titulo en fila propia a ancho completo y el resto de campos envolviendo con `flex-wrap` y mas espacio cada uno. Verificado en vivo con datos reales (no solo tsc/build): edicion de una Solicitud real con pegado de una imagen real via evento `paste` sintetico -- `POST /solicitudes/{id}/archivos` y `PUT /solicitudes/{id}` ambos 200, la descripcion guardada trae `<img data-guid="...">`; dialogo Convertir confirmado en 1095px con Titulo en ~975px. 53/53 pruebas backend, `tsc`/build limpios.)
> **Actualización anterior 11:** 2026-08-04 (Sesion grande sobre un lote de pendientes de UX/negocio pedidos directo por el usuario: proyectos administrados, QA obligatorio por categoria, fecha compromiso mas estricta, buscadores/orden en catalogos y bandejas, filtro de agente en Mesa de ayuda, total de horas registradas, editor enriquecido en Nueva Solicitud, ver detalle en Revision de solicitudes, catalogos Area/Puesto y el editor de Workflows (P21). Detalle completo:
> (1) **Proyecto administrado** (`tblProyecto.Administrado`, script 22, mismo patron de bit flag que `EsMantenimiento`): checkbox nuevo en Administracion > Proyectos. En un proyecto marcado, crear un WorkItem exige `WI.CrearEnAdministrado` y cancelarlo (accion CANCELAR desde Pendiente, ya gateada por `WI.Eliminar` -- ese es el "eliminar" real del sistema, no hay borrado fisico de WorkItems) exige ademas `WI.EliminarEnAdministrado`; en proyectos NO administrados (default) nada cambia. Los demas usuarios en un proyecto administrado solo pueden cambiar estatus (INICIAR/TERMINAR/etc. siguen con sus propias reglas, sin tocar). Permisos nuevos sembrados en script 23, asignados solo a Administrador -- el equipo decide despues si algun otro rol los necesita, via Administracion > Roles (la matriz los toma automatico).
> (2) **QA obligatorio por categoria de proyecto** (RN-QA-06 nueva): en proyectos categoria Desarrollo (`tblCategoriaProyecto.Id=1`), terminar un WorkItem directo desde En Proceso (saltando En Pruebas) exige el permiso nuevo `WI.SaltarPruebas`; TI (2) y Mantenimiento (3) quedan libres, igual que antes. Logica en `CambiarEstatusWorkItemHandler`, no en `tblTransicionConfig` (esa tabla no puede expresar una condicion sobre el proyecto, solo sobre la transicion).
> (3) **RN-REQ-04 mas estricto**: antes solo se bloqueaba mover la fecha compromiso al pasado; ahora CUALQUIER cambio a una fecha compromiso YA capturada (a otra fecha, o borrarla) exige `WI.ModificarCompromiso` -- fijarla por primera vez (de vacia a una fecha) sigue libre. Un cambio de una linea en `ActualizarWorkItemCommand` (la condicion ya no mira si la fecha nueva es pasada, solo si la anterior existia).
> (4) **Buscadores y orden en catalogos/bandejas**: hook nuevo `useOrdenTabla` + componente `EncabezadoOrdenable` (`frontend/gte-web/src/shared/hooks`, `.../shared/components`) para orden client-side en tablas que ya traen el arreglo completo -- aplicado en Administracion (Proyectos, Equipos, Usuarios, Ambientes, Horarios/festivos, y los dos catalogos nuevos Areas/Puestos) y en QA/Releases. Para Tickets/Incidentes/Triage (paginados, el cliente nunca tiene el 100% del dataset) se replico el patron server-side que ya usaba la Bandeja de trabajo: `ordenarPor`/`ordenDescendente` en el filtro, switch de `OrderBy`/`OrderByDescending` en el QueryService, mismo `TableSortLabel` en la UI. Buscador de texto agregado donde faltaba (Equipos, Ambientes, Proyectos, Horarios, QA, Releases) con simple `.filter()` en memoria.
> (5) **Mesa de ayuda: filtro Agente**: mismo patron que ya tenia la Bandeja de trabajo (filtro Asignado) -- se inicializa con el usuario firmado al entrar a la pantalla, sin pisar una eleccion posterior a "Todos". El backend ya soportaba `idAsignado` en el filtro, solo faltaba exponerlo en `BandejaTicketsPage.tsx`.
> (6) **Total de horas registradas**: suma client-side (sin cambios de backend, el endpoint `/tiempo` ya traia todo) mostrada en la pestaña Tiempo del Detalle de WorkItem (fila "Total" al pie de la tabla) y dentro del propio `ModalTiempo` ("Total ya registrado: Xh Ym", mismo query de React Query, sin round-trip extra si ya esta en cache).
> (7) **Nueva Solicitud con editor enriquecido**: el campo Descripcion de `PortalPage.tsx` (antes `TextField` plano) ahora usa `EditorEnriquecido` (mismo componente TipTap que Descripcion de WorkItem/Hallazgos/Comentarios) -- negritas, listas, etc. `CrearSolicitudHandler` ahora sanitiza con `ISanitizadorHtml` (antes guardaba el texto tal cual, gap real encontrado al revisar el handler). **Fuera de alcance deliberado, igual que en NuevoItemModal de WorkItem**: pegar una imagen antes de guardar la Solicitud sigue bloqueado (no existe `idSolicitud` todavia en ese punto) -- no se construyo un endpoint de adjuntos para Solicitud en esta pasada porque no habria ningun consumidor real (sin pantalla de edicion post-alta), habria quedado codigo muerto.
> (8) **Revision de solicitudes: ver detalle**: icono nuevo por fila en `TriagePage.tsx` que abre un dialogo de solo lectura (folio, solicitante, tipo, prioridad, fecha deseada, descripcion via `ContenidoEnriquecido`, justificacion) usando los datos que la bandeja YA trae, sin round-trip nuevo. El tooltip del titulo (que concatenaba Descripcion+Justificacion como texto plano) se corrigio para no mostrar tags HTML crudos ahora que Descripcion puede venir enriquecida (helper nuevo `htmlATextoPlano` en `shared/editor/textoPlano.ts`).
> (9) **Catalogos Area y Puesto**: existian las tablas (`tblArea`, `tblPuesto`) y se leian para los combos de Usuarios, pero sin CRUD ni pantalla propia (gap documentado en la seccion 3.4 de este archivo). Se clono el patron de Ambientes (Domain/Application/Infrastructure/WebApi + `AreasTab.tsx`/`PuestosTab.tsx`) para alta/edicion/baja logica completas, con buscador+orden desde el dia 1. Sin script SQL de esquema (las tablas ya existian).
> (10) **Editor de Workflows** (P21, `/admin/workflows`, permiso `ADM.Workflows` -- ya sembrado desde el script 02 pero sin ningun consumidor hasta ahora): pantalla nueva, ruta aparte (no pestana de Administracion, para respetar la URL pedida). Backend nuevo completo (`GTE.Domain/Workflow`, `GTE.Application/Workflow`, `WorkflowQueryService`, `WorkflowRepository`, `WorkflowController`): lista de procesos (`tblProceso`) -> grafo completo de un proceso (`tblTransicion` + metadatos de `tblTransicionConfig`, unidos en memoria por no tener FK real) -> edicion en lote de etiqueta/permiso/motivo/accion principal/orden. **Deliberadamente NO crea ni elimina transiciones** (el grafo estructural sigue siendo por script SQL, coherente con "no tocar CambiarST" de la seccion 9.3 del InterfloClaude.md) -- solo edita los metadatos de UI de transiciones que ya existen. **Trampa evitada**: no hay forma de leer dinamicamente "la tabla de catalogo de estatus de este proceso" sin SQL interpolado (`tblProceso.TablaEstatus` es solo texto descriptivo) -- se opto por un mapeo explicito en codigo (switch de 11 casos, uno por proceso) en vez de reflection o SQL dinamico, ver `WorkflowQueryService.ObtenerCatalogoEstatusAsync`.
> **Verificado**: 53/53 pruebas backend en verde (`dotnet test`, incluye correr los scripts 22/23 contra LocalDB real -- una prueba existente, `VerticalCompleto_CrearIniciarSuspenderRegistrarTerminar`, tuvo que cambiar su proyecto de prueba a categoria TI en vez de Desarrollo porque probaba RN-REQ-03, no la regla nueva RN-QA-06); `tsc -b` + `vite build` limpios (incluye el `predev`/`prebuild` de sync del manual). Prueba manual real en el navegador contra la API+SPA levantadas con datos migrados reales: edicion de Workflows (cambio de etiqueta/permiso en la transicion APROBAR de Solicitud, guardado, recargado y confirmado persistido, despues revertido); checkbox Administrado en un proyecto (persistido); Nueva Solicitud con texto en negritas -> confirmado en la respuesta real del POST que el HTML llego sanitizado (`<strong>` preservado) y se ve igual en el dialogo de Ver detalle de Triage; filtro Agente de Mesa de ayuda confirmado por el querystring real (`idAsignado=1` enviado solo, sin tocar nada); registro de tiempo con el total apareciendo en ambos lugares; alta de un Area y un Puesto vinculado. Sin pruebas automatizadas nuevas para las piezas de frontend (buscadores/orden, editor de Workflows) mas alla de `tsc`/`vite build`. Ver seccion 2 para el detalle por modulo)
> **Actualización anterior 10:** 2026-08-04 (**Manual de usuario (Ayuda): se reemplazo el contenido fijo en `ManualUsuarioPage.tsx` por el manual real y completo que vive en `Doctos/ManualUsuarioGTE.html`** -- HTML autocontenido con su propio sidebar/buscador/estilos, mucho mas extenso que el acordeon anterior (cubre los 27 modulos documentados, incluye seccion "Proximamente" y glosario). En vez de copiar el contenido a mano dentro del componente (lo que garantiza que se desactualice), `ManualUsuarioPage.tsx` ahora es un iframe apuntando a `/manual-usuario.html`, servido desde `public/` de Vite; un script nuevo (`frontend/gte-web/scripts/sync-manual.mjs`) copia `Doctos/ManualUsuarioGTE.html` a `public/manual-usuario.html` en cada `predev`/`prebuild` (enganchado en `package.json`), asi que `publicar.bat` (que ya corre `npm run build`) siempre empaqueta la version mas reciente del manual sin ningun paso manual adicional. El archivo generado en `public/manual-usuario.html` se agrego a `.gitignore` -- la unica fuente de verdad editable (a mano o con IA) sigue siendo `Doctos/ManualUsuarioGTE.html`. **Verificado**: `predev` corrio solo al levantar `npm run dev` y genero el archivo correctamente; `http://localhost:<puerto>/manual-usuario.html` sirvio el manual completo en el navegador. **No verificado end-to-end**: la ruta `/ayuda` dentro de la SPA autenticada (requiere login real, sin credenciales de prueba a mano en esta sesion) -- el cambio en si es minimo (un `<iframe>` apuntando a una ruta estatica ya confirmada), pero falta el click-through real logueado. **Nota**: `DiagramaFlujoSolicitud.tsx` (el diagrama SVG que usaba el acordeon viejo) quedo sin ninguna referencia -- se dejo el archivo tal cual por pedido explicito de no borrar nada en esta sesion, pendiente de que alguien decida si se borra o se reutiliza)
> **Actualización anterior 9:** 2026-08-03 (Sesion larga de ajustes sobre Bandeja de trabajo/Nuevo elemento + Mi Dia extendido a Tickets/Incidentes/Solicitudes/Triage + accion Copiar + edicion de Proyecto + bloqueo de alta en proyecto cerrado (RN-PRY-02 nueva). Detalle completo: (1) **Bandeja de trabajo** (`BandejaPage.tsx`): el filtro Asignado se inicializa con el usuario firmado al entrar a la pantalla (solo si no habia uno ya elegido en la sesion, no pisa una eleccion posterior a "Todos"); se quito la columna Presupuesto de `TablaBandeja.tsx` (sin uso real, quedo pedido explicito); se regreso "Registrar tiempo" al menu de acciones de cada fila (`MenuAcciones.tsx`) -- el modal `ModalTiempo` ya existia pero solo estaba enganchado en Mi Dia y el Detalle, no ahi. (2) **Nuevo elemento de trabajo** (`NuevoItemModal.tsx`): Prioridad nace en Media (id 3, fijo por el enumerado `tblPrioridad`) y Asignado en el usuario firmado por default (antes ambos nacian vacios); si el Proyecto elegido es de categoria "TI", la Fecha compromiso se precarga con hoy (solo si el campo sigue vacio, no pisa una fecha ya escrita) -- exigio agregar `CategoriaProyecto` a `ProyectoItemResponse`/`CatalogosQueryService` (antes el catalogo de `/catalogos/bandeja` solo traia id/clave/nombre por proyecto). (3) **Filtro "solo mis proyectos"** en los 9 combos de Proyecto que salen de `/catalogos/bandeja` (bandeja, nuevo elemento, triage, incidentes, tickets, backlog, QA, releases): un proyecto aparece si el usuario es su Responsable o pertenece al equipo asignado (join contra `TblEquipoMiembro`, mismo patron que ya arma `sesion.equipos` en el login) -- resuelto en el Handler via `IProveedorUsuarioActual`, no en el QueryService. Deliberadamente EXCLUIDO de Ambientes/Costeo/OKR (`obtenerProyectos()` sin filtro, siguen viendo los 47 proyectos completos via `/api/v1/proyectos` -- son pantallas de administracion, no de trabajo personal). **Hallazgo real de la prueba en vivo**: la mayoria de proyectos migrados del GT no tienen Responsable ni Equipo capturado en BD -- los 8 proyectos de categoria "TI" probados tienen AMBOS campos NULL; decision del negocio fue dejar el filtro como esta (el problema real es que a esos proyectos les falta el dato, no que el filtro este mal), ver nota en seccion 3.4. (4) **Accion "Copiar"** en el menu de acciones de la bandeja: abre el mismo modal de alta prellenado con Proyecto/Tipo/Prioridad/Titulo ("Copia de ...")/Descripcion/Asignado del elemento original -- hace `GET /api/v1/workitems/{folio}` para traer Descripcion e IdPrioridad numerico (la fila de la bandeja no los trae); NO copia Estatus/Folio/Fecha compromiso, siempre nacen limpios como en una alta normal. (5) **Mi Dia** (`GET /api/v1/mi-dia`) ahora tambien trae `TicketsAsignados` (agente, no Cerrado, reutiliza `ITicketQueryService.ObtenerBandejaAsync` tal cual sin tocarlo), `IncidentesRelevantes` (proyectos donde el usuario es responsable, no Cerrado -- `IIncidenteQueryService.ObtenerRelevantesAsync` nuevo, ya que `TblIncidente` no tiene asignacion directa a un usuario), `SolicitudesPendientes` (las que el usuario levanto, en Enviada/EnAnalisis/Aprobada, reutiliza `ISolicitudQueryService.ObtenerMiasAsync`) y `TriagePendientes` (contador GLOBAL de solicitudes esperando revision en todo el sistema, solo si el usuario tiene `SOL.Triage` -- no existe hoy forma de personalizarlo por proyecto/equipo, es un aviso con link a Triage, no una lista propia). Cada seccion nueva de Mi Dia solo se muestra si tiene contenido, a diferencia de Vencidas/Para hoy/Proximas que siempre se muestran con "(0)". (6) **Edicion de Proyecto** en Administracion: el backend YA tenia todo listo desde antes (`PUT /api/v1/proyectos/{id}`, `ActualizarProyectoCommand`, `actualizarProyecto` en `administracion.ts`) pero `ProyectosTab.tsx` nunca lo exponia en la UI (documentado por error como resuelto en una fila vieja de esta seccion) -- se agrego boton "Editar" + modal, mismo patron `proyectoEditar`/`abrirEditar`/`guardarEdicion` que ya usa `UsuariosTab.tsx`. (7) **RN-PRY-02 (nueva)**: un proyecto en estatus Cerrado o Cancelado ya no admite WorkItems nuevos -- `CrearWorkItemHandler` valida `IdEstatusProyecto` (campo agregado a `ProyectoResumen`/`WorkItemRepository.ObtenerProyectoAsync`, mismo query existente, sin round-trip extra) y rechaza con 400 "El proyecto esta cerrado; no admite elementos nuevos.". Verificado en vivo contra la API real (no solo `tsc`/`dotnet build`) con `aviramontes`: Mi Dia mostro 2 tickets reales asignados; Copiar trajo Descripcion+Asignado reales de un WorkItem migrado; edicion de Proyecto persistio un cambio de Responsable via `PUT` directo (confirmado y revertido); bloqueo de proyecto cerrado probado creando un proyecto de prueba (`TESTCERR`, autorizado y cancelado via el motor, queda en LocalDB a proposito, ver seccion 3.4) y confirmando el 400 al intentar crear un WorkItem ahi. Ver seccion 2, filas "Bandeja de trabajo (ajustes 2026-08-03)", "Mi Dia: Tickets/Incidentes/Solicitudes/Triage" y "Administracion: edicion de Proyecto + bloqueo de proyecto cerrado")
> **Actualización anterior 8:** 2026-08-03 (Buscador tipo LIKE en todos los combos de catalogo dinamico de la SPA: nuevo componente compartido `ComboBuscable`/`ComboBuscableMultiple` (`frontend/gte-web/src/shared/components/ComboBuscable.tsx`), wrapper de MUI Autocomplete con un tipo normalizado `{valor, etiqueta}` que desacopla el componente de la forma real de cada catalogo -- se uso en los ~70 combos que vienen de un catalogo dinamico del backend, en los 22 archivos de `src/features/` que antes usaban `Select`+`MenuItem` (admin, trabajo, workitem, triage, operacion, soporte, portafolio, planeacion, calidad, entregas, solicitudes). El filtro "contiene, insensible a mayusculas" es el default nativo de Autocomplete, sin `filterOptions` custom. **Quedaron deliberadamente fuera** (decision explicita del negocio) unos 10 `Select` de listas cortas y fijas escritas en el JSX, sin catalogo de backend (dia de la semana en Horarios, Alcance global/horario en Horarios, Aplica a proyecto/equipo y Trimestre Q1-Q4 en OKRs, Destino de cierre de sprint en Backlog, Tipo de artefacto y Tipo despliegue/rollback en Releases, Resultado de ejecucion en QA) -- buscar entre 2-7 opciones fijas no aporta. El caso multiple (Estatus en BarraFiltros/BandejaTickets con sentinel `-1`="Todos", y "Elementos" en ReleasesPage con resumen "N seleccionado(s)" via prop `resumenSimple`) se cubre con `ComboBuscableMultiple`. Verificado en vivo con `npm run dev`: `tsc --noEmit` en 0 errores (con `noUnusedLocals`/`noUnusedParameters`, confirma que no quedaron imports de `Select`/`MenuItem`/`FormControl`/`InputLabel` sueltos) y prueba interactiva real en el navegador -- filtro Proyecto de la Bandeja de trabajo filtra en vivo al escribir "GTE", y el multiple de Estatus filtra opciones y pinta el chip "En Proceso" seleccionado, sin regresion visible. Ver seccion 2, fila "Combos con buscador")
> **Actualización anterior 7:** 2026-08-03 (Notificacion al convertir una Solicitud, segunda pasada: ademas de avisar al solicitante (ver abajo), `ConvertirSolicitudHandler` ahora notifica tambien al **usuario asignado** de cada item del desglose que traiga `IdAsignado` -- "Se te asigno el elemento de trabajo {folio}" con el titulo del item, ligado a la entidad `WorkItem`/ruta `/wi/{folio}` (no todos los items traen asignado, el triage puede dejarlos sin asignar). Verificado en vivo en LocalDB con dos cuentas reales: convertida una Solicitud con Asignado=Luis Garcia, la notificacion aparece de inmediato en la campana de `lgarcia`. Ver seccion 2)
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
| **Indicadores ejecutivos (P18)** | Lead/Cycle Time, DORA (Deployment Frequency/Change Failure Rate/MTTR reales, Lead Time for Changes sin datos), Entrega a tiempo, Eficiencia, Retrabajo, Productividad, SLA/CSAT, semáforo de proyectos con costo, OKR, KPIs personalizados (snapshot nocturno via Hangfire), top riesgos, burndown de sprint activo; widgets ocultables y reordenables persistidos por usuario | Indicadores ejecutivos |
| **Administracion** | Proyectos (alta/edicion + cambio de estatus por el motor, folio al autorizar, RN-PRY-01 bloquea el cierre con WorkItems abiertos), equipos con miembros y % dedicacion, usuarios (alta/edicion/baja logica, RN-ADM-01 valida ciclos de jerarquia con CTE recursivo), roles (asignar/retirar con alcance global o por proyecto, matriz rol-permiso guardada en lote), horarios (tramos con turnos partidos, dias festivos) ambientes (por proyecto o globales), y **areas/puestos (2026-08-04, catalogos nuevos con CRUD)** | Administracion (8 pestañas) |
| **Comentarios y adjuntos** | Hilos de comentarios sobre WorkItem con formato basico (negritas, listas, etc.), @menciones con autocompletado (TipTap + catalogo de usuarios) y pegado de imagenes desde el portapapeles; adjuntos con subida/descarga por streaming autenticado (`IAlmacenArchivos` en disco, GUID + SHA-256), validacion de extension/tamano, baja logica solo por el propio autor. HTML sanitizado en el backend (`HtmlSanitizer`) antes de guardarse | franja de Comentarios bajo el detalle + pestaña Adjuntos, en Detalle de WorkItem |
| **Rich text en Descripcion y Hallazgos (2026-08-02)** | El mismo tratamiento de Comentarios (TipTap + pegado de imagenes + sanitizado backend) se extendio a la Descripcion del WorkItem (solo en edicion/detalle, ver nota de alcance abajo) y a la captura de Hallazgos (`PanelRevisiones`, campo "Que se encontro"). Piezas compartidas extraidas a `frontend/gte-web/src/shared/editor/` (`ImagenProtegida`, `ContenidoEnriquecido`, `EditorEnriquecido`, `textoPlano.ts`) para no duplicar el patron entre Comentarios/Descripcion/Hallazgos; `EditorComentario`/`PanelComentarios` se refactorizaron para consumir las mismas piezas en vez de mantener su propia copia. Backend: `CrearWorkItemCommand`/`ActualizarWorkItemCommand`/`CrearRevisionCommand` ahora sanitizan con `ISanitizadorHtml` igual que Comentarios (`SanitizadorHtmlGanss` ya soportaba el mismo set de tags, no necesito cambios ahi). Compatibilidad con datos legado (Descripcion/Comentarios de Hallazgo eran texto plano antes de esto): `normalizarHtmlLegado` detecta si el valor YA es HTML (contiene una etiqueta) y si no, escapa y convierte saltos de linea a `<br>` antes de mostrarlo/editarlo -- transparente, no requiere migracion de datos. **Fuera de alcance deliberado**: el modal de ALTA (`NuevoItemModal.tsx`) sigue con Descripcion en texto plano -- el WorkItem no existe todavia en ese punto, no hay a que adjuntar imagenes pegadas (mismo motivo por el que no se puede comentar antes de crear); se vuelve rich text recien en la edicion. `CriteriosAceptacion` y el "Motivo" de reapertura de hallazgo quedan como texto plano (no se pidio ampliarlos) | Modal editar WorkItem + pestaña Descripcion del Detalle; modal "Reportar hallazgo" + listado de Revisiones |
| **Notificaciones y tiempo real** | Campana con notificaciones In-App (`tblNotificacion`) que llegan en vivo por SignalR (`NotificacionesHub`, un solo hub para notificaciones + refresco de tablero); disparadores: Solicitud aprobada/rechazada/devuelta/**convertida (2026-08-03, notifica al solicitante Y a cada usuario asignado de los items generados)** y @mencion en un comentario (notifica al mencionado). El tablero Kanban se refresca solo cuando cualquier WorkItem cambia de estatus, sin importar quien lo haya movido | campana en la barra superior (todas las pantallas) |
| **Mesa de ayuda (Tickets y SLA)** | Primer módulo de Fase 4, construido 2026-08-02 verificado extremo a extremo en LocalDB (no solo compilado): alta de ticket (folio TKT-año, estatus inicial Nuevo, SLA resuelto por prioridad con fechas límite de respuesta/resolución vía `ICalendarioLaboral`), bandeja de agentes (permiso `TKT.Atender`) con las 7 transiciones del proceso `Ticket` (asignar, iniciar atención, esperar usuario, reanudar, resolver, cerrar, reabrir) vía el motor de workflow existente, escalamiento a WorkItem tipo Soporte (acción de negocio fuera del motor -- no hay transición `ESCALAR` en `tblTransicion`, así que no aparece en el listado dinámico de acciones), y encuesta de satisfacción 1-5 del solicitante al Resuelto/Cerrado. El esquema de BD (`tblTicket`, `tblSla`, `tblEstatusTicket`, `tblCategoriaTicket`, `tblEncuestaSatisfaccion`, el proceso `Ticket` en `tblProceso`/`tblTransicion`, el permiso `TKT.Atender`) ya existía desde el despliegue inicial (script 06/01/02/09); esta sesión sembró `tblTransicionConfig` + categorías + SLA por defecto (script 12) y construyó las 4 capas de código + 3 pantallas nuevas. **Fuera de alcance de esta pasada** (ver también §3.4): sin pruebas automatizadas nuevas (solo verificación manual real en LocalDB); RN-SUP-02/03 (alertas de SLA al 80%/100%, cierre automático a 5 días hábiles) requieren Hangfire (A4, no construido); Base de conocimiento y "Derivar a ticket" desde Triage de Solicitudes quedan pendientes | Mis tickets (`/tickets`, portal), Mesa de ayuda (`/soporte`, bandeja), Detalle de ticket (`/tickets/:folio`) |
| **Mesa de ayuda: filtro de estatus, autoasignación y cierre por el ingeniero (2026-08-03)** | Tres ajustes pedidos por el negocio sobre el módulo anterior, verificados en vivo en LocalDB con dos cuentas reales (`aviramontes`/Administrador con `TKT.Atender`, `lgarcia`/Desarrollador sin el permiso): (1) **Filtro de Estatus en la bandeja de agentes** (`BandejaTicketsPage.tsx`) -- antes `ObtenerBandejaAsync` ocultaba `Cerrado` sin forma de verlos desde la UI aunque el backend ya soportaba `estatus=-1` (todos); se agregó el mismo selector multiple con "Todos" que ya existía en la Bandeja de trabajo de WorkItems (`BarraFiltros.tsx`), catálogo `EstatusTicket` nuevo en `CatalogosBandejaResponse`/`CatalogosQueryService`, default `[-1]` para mostrar todo de entrada. (2) **Autoasignación al registrar un ticket**: si quien lo crea ya tiene `TKT.Atender` (un ingeniero registrando su propio caso atendido en el momento, no un usuario autoreportándose), `CrearTicketHandler` ejecuta el mismo grafo de `ASIGNAR` (motor de workflow + `AsignarAsync`) para que el ticket nazca en Asignado con el propio ingeniero como responsable; un usuario sin el permiso sigue naciendo en Nuevo sin asignar, sin cambio de comportamiento. (3) **RESOLVER exige Solucion y MinutosSolucion**: dos columnas nuevas en `tblTicket` (script `16_2026-08-03_ALTER_tblTicket.sql`, corrido en LocalDB, pendiente en dev/preprod/prod), validadas en `CambiarEstatusTicketHandler` (rechaza la transición sin ambos datos, mismo patrón que RN-OPS-02 de Incidentes) y capturadas en un diálogo nuevo (Solución multilínea + minutos) tanto en la Bandeja como en el Detalle de ticket; con eso capturado, CERRAR (que ya existía) queda disponible con sentido de negocio completo. Sin pruebas automatizadas nuevas (solo verificación manual real en LocalDB, incluyendo el caso negativo de un usuario sin `TKT.Atender`) | Mesa de ayuda (`/soporte`), Detalle de ticket (`/tickets/:folio`) |
| **Ticket: Usuario solicitante y Locacion (2026-08-03)** | Dos catálogos nuevos, `tblUsuarioSolicitante` (Usuario/Nombre/Correo — gente que puede no tener cuenta de GTE) y `tblLocacion` (Locacion/Descripcion/Activo), creados a mano en producción sin script ni bitácora; se les agregó la bitácora estándar (scripts 17/18), un script de reproducibilidad para ambientes nuevos (19, no-op en producción) y dos columnas nuevas en `tblTicket` (`IdUsuarioSolicitante`, `IdLocacion`, script 20 — constraint del segundo FK renombrado a `FK_tblTicket_tblUsuarioSolicitanteCatalogo` para no chocar con el FK ya existente `IdSolicitante->tblUsuario`, que por historia se llama igual que el nombre "obvio"). El modal "Nuevo ticket" (`PortalTicketsPage.tsx`) muestra estos dos catálogos como Select opcionales SOLO si quien lo llena tiene `TKT.Atender`; se resuelven por nombre en `TicketResponse`/`TicketQueryService` y se muestran en el Detalle de ticket. Verificado en vivo en LocalDB con datos sembrados y con las dos cuentas reales (aviramontes los ve y los captura, lgarcia no los ve en el formulario). **Scripts 17, 18, 19 y 20 corridos en producción y confirmados funcionando** (2026-08-03) — nota real de despliegue: `tblLocacion.IdLocacion` en producción no tenía PRIMARY KEY (creada a mano sin ella), lo cual tronó el script 20 al crear el FK con "Could not create constraint or index" (Msg 1750); el equipo la corrigió directo en producción antes de re-correr el script | Mis tickets (`/tickets`), Detalle de ticket (`/tickets/:folio`) |
| **Solicitud/WorkItem: Usuario solicitante (2026-08-03)** | Mismo patrón que en Tickets, extendido a Solicitudes: `tblSolicitud.IdUsuarioSolicitante` (capturado opcionalmente al crear, SOLO visible para quien tiene `SOL.Triage` — un Líder registrando a nombre de otra persona) y `tblWorkItem.IdUsuarioSolicitante` (copiado automáticamente por `ConvertirSolicitudHandler` al convertir, mismo mecanismo con que ya se copian `IdSolicitante`/`IdSolicitud`) — script 21, mismo cuidado de nombrar el FK nuevo `FK_tblSolicitud_tblUsuarioSolicitanteCatalogo`/`FK_tblWorkItem_tblUsuarioSolicitanteCatalogo` para no chocar con el FK ya existente por rol (`FK_tblWorkItem_tblUsuarioSolicitante` es `IdSolicitante->tblUsuario`, verificado ANTES de escribir el script esta vez). Indicador "*" con tooltip en la bandeja de Triage cuando hay Usuario solicitante. Verificado en vivo extremo a extremo en LocalDB con datos reales migrados: Solicitud → Triage (Tomar/Aprobar a proyecto GTE) → Convertida en WorkItem, visible en su Detalle junto al Solicitante interno; confirmado que un usuario sin `SOL.Triage` no ve el campo. **Pendiente**: correr el script 21 en producción | Mis solicitudes (`/solicitudes`), Revisión de solicitudes (`/triage`), Detalle de WorkItem (`/wi/:folio`) |
| **Incidentes** | Segundo módulo de Fase 4, construido 2026-08-02, verificado extremo a extremo en LocalDB (no solo compilado): alta de incidente (folio INC-año, estatus inicial Detectado) dentro de un proyecto con severidad S1-S4, bandeja + detalle con las 5 transiciones del proceso `Incidente` (atender, mitigar, resolver, cerrar -- sin reapertura, un incidente siempre concluye en Cerrado) vía el motor de workflow existente, RN-OPS-02 (cerrar con severidad S1/S2 exige causa raíz capturada, validado y probado en vivo: el cierre se rechaza sin causa raíz y procede tras capturarla), RN-OPS-03 (cambio de severidad como acción de negocio aparte -- no es una transición de `tblTransicion` -- con motivo obligatorio), vincular WorkItem correctivo (crea un WorkItem tipo Corrección igual patrón que el escalamiento de Tickets, probado: creó `HELPDESK-3395`), y vincular un release ya existente como causante (reutiliza `GET /api/v1/releases?idProyecto=X`, insumo futuro de DORA Change Failure Rate). El esquema de BD (`tblIncidente`, `tblEstatusIncidente`, `tblSeveridad`, el proceso `Incidente` en `tblProceso`/`tblTransicion`, el permiso `INC.Gestionar`, `tblProyecto.IdResponsable`) ya existía desde el despliegue inicial; esta sesión sembró `tblTransicionConfig` (script 13) y construyó las 4 capas de código + 2 pantallas nuevas. **Fuera de alcance de esta pasada**: RN-OPS-01 completo (notificación a "todos los canales" -- solo existe InApp -- y escalamiento automático a 30 min sin atención, necesita Hangfire/A4; tampoco se notifica "al líder" por falta de una consulta usuarios-por-rol ya establecida) -- sí se implementó la notificación InApp inmediata al responsable del proyecto en incidentes S1; disponibilidad/% uptime mensual (reporte, Fase 5); monitoreo con health checks (Hangfire + catálogo de sistemas, no existe); `tblBitacoraCambio` ("qué cambió ayer") sigue sin UI, es bitácora general de PROD no específica de Incidentes; sin pruebas automatizadas nuevas | Incidentes (`/operacion/incidentes`, bandeja), Detalle de incidente (`/operacion/incidentes/:folio`) |
| **Portafolio: Costeo real y OKRs (A5, parcial)** | Construido 2026-08-02, verificado extremo a extremo en LocalDB: catálogo de tarifas por nivel con vigencia por fecha (alta/edición/baja lógica), presupuesto por proyecto/año, y reporte de costo real (`tblRegistroTiempo` × tarifa vigente del nivel del usuario a la fecha del registro, resuelta con `OUTER APPLY` en la vista `vwCostoRegistroTiempo` — nuevo patrón de vigencia, no existía uno previo en el código) comparado contra el presupuesto, con desglose por usuario. Probado en vivo contra datos históricos migrados reales del GT (proyecto PLANTILLA ANGULAR, usuario con 20h registradas × tarifa Junior $150/h = $3,000 exacto). OKRs: objetivos trimestrales por proyecto o equipo con resultados clave (meta/valor actual editado a mano, vínculo opcional a `ClaveKpi` para cuando exista el job de snapshot). Dos permisos nuevos sembrados (`POR.GestionarCosteo`, `POR.GestionarOkr`, módulo "Portafolio" en `tblPermiso`, script 14) — a diferencia de Tickets/Incidentes, este submódulo no tenía permiso previo. **Refinamiento 2026-08-02 (mismo día): ver tarifas/presupuesto/costo real ahora exige permiso aparte de administrarlos.** En vez de sembrar un tercer permiso redundante, se reutilizó `RPT.Costos` (ya sembrado en script 02, módulo "Indicadores", reservado para el futuro Dashboard Ejecutivo — su descripción "Ver reportes de costos y rentabilidad" calzaba exacto). Las 3 consultas de lectura (`ObtenerTarifasNivelQuery`, `ObtenerPresupuestosProyectoQuery`, `ObtenerCostoProyectoQuery`) exigen `RPT.Costos` **o** `POR.GestionarCosteo` (quien administra el catálogo también puede verlo); los Commands de alta/edición/baja siguen exigiendo solo `POR.GestionarCosteo` — ver no habilita editar. En el frontend, la pestaña Costeo se oculta completa si el usuario no tiene ninguno de los dos permisos (`PortafolioPage.tsx`, con mensaje "No tienes permiso para ver esta sección" en vez de dejar caer en una pestaña oculta al navegar directo a la URL — bug encontrado y corregido en el mismo repaso), y los botones de alta/edición/baja de tarifas y presupuesto se ocultan si falta específicamente `POR.GestionarCosteo` (`CosteoTab.tsx`, prop `puedeGestionar`). Verificado en vivo con dos cuentas reales: `aviramontes` (Administrador, tiene ambos permisos) ve y edita todo sin regresión; `lgarcia` (rol Desarrollador, sin ninguno de los dos) ve "No tienes permiso..." en la pantalla, el ítem "Portafolio" ni aparece en el menú lateral, y una llamada directa a `GET /api/v1/costeo/tarifas` con su token responde `403 FORBIDDEN`. **Fuera de alcance de esta pasada**: Riesgos (matriz probabilidad×impacto, ya tiene workflow sembrado en el motor) y la jerarquía Portafolio/Programa quedan para otra sesión (ver A5 en 3.2); "avance automático" de OKR ligado a KPIs depende del job nocturno de `tblKpiValor` (Hangfire/A4); sin pruebas automatizadas nuevas | Portafolio (`/portafolio`, pestañas Costeo/OKR) |
| **Catalogo de reglas de negocio por proyecto (2026-08-24)** | Modulo nuevo, pedido del negocio en esta sesion. **AJUSTE posterior del mismo dia -- la clave se forma SOLA**: `RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO DE 3 DIGITOS}` (`RN-GTE-001`, `RN-XX-001`). Ya no se captura ni viaja en el request de alta. El consecutivo lo entrega **`dbo.spGenerarFolio`** con la serie `RN-{CLAVE}` y 3 digitos, NO un `MAX()+1`: el SP toma `ROWLOCK/UPDLOCK/HOLDLOCK` y es el motor de folios del sistema, asi que dos altas simultaneas no pueden sacar el mismo numero -- verificado con 6 peticiones en paralelo que devolvieron `RN-GTE-004` a `009`, todas distintas. La clave del proyecto se normaliza (mayusculas, solo A-Z0-9, tope 12) porque viene de captura libre: el proyecto de pruebas `xx` genera `RN-XX-001`. Si al limpiarla no queda nada usable cae a la serie `GEN` en vez de fallar el alta. El patron de validacion se amplio a `RN-[A-Z0-9]{2,12}-\d{2,4}` para aceptar el formato nuevo **y el heredado**. **RENUMERACION de las 40 reglas de GTE (decision del equipo el mismo dia: se prefiere uniformidad)**: pasaron de las claves historicas del Documento Maestro a `RN-GTE-001` .. `RN-GTE-040`, script `45_2026-08-24_UPDATE_bdsGTE_RenumeraReglasGTE.sql`. **El modulo NO se perdio**: sigue en el ambito de cada regla (`tblAmbitoRegla`: "Requerimientos", "Calidad (QA)"...), que es donde el catalogo lo muestra y agrupa; lo que se perdio es el mnemonico dentro de la clave. La numeracion sigue el **orden de las secciones 3.x del Documento Maestro**, asi que las reglas de un mismo modulo quedan consecutivas. Se actualizaron **en el mismo movimiento** las 82 ocurrencias `RN-*` de comentarios en `src/` y las 62 del Documento Maestro (verificado: cero claves con el formato viejo quedaron en ninguno de los dos), y el script reescribe ademas las **referencias cruzadas dentro de los enunciados** guardados en BD -- varias reglas se citan entre si por clave y habrian quedado apuntando a algo inexistente. La serie de folio `RN-GTE` queda en 40 para que la siguiente regla nueva salga `RN-GTE-041` (verificado en vivo). **`PENDIENTES.md` y los commits anteriores NO se reescribieron** (son historial, no fuente de verdad), asi que citan las claves viejas; esta es la tabla de equivalencia para leerlos: `RN-ADM-01..04` = `RN-GTE-001..004`; `RN-PRY-01..03` = `005..007`; `RN-REQ-01..09` = `008..016`; `RN-PLA-01..05` = `017..021`; `RN-DEV-01..03` = `022..024`; `RN-QA-01..06` = `025..030`; `RN-REL-01..04` = `031..034`; `RN-OPS-01..03` = `035..037`; `RN-SUP-01..03` = `038..040`. Hay reintentos (5) por si la serie de `tblFolio` queda desfasada de las claves reales, con la UNIQUE de BD como garantia final. **Problema que resuelve**: las reglas de negocio vivian en tres lugares que nadie sincronizaba (Documento Maestro, comentarios `///` sueltos en el codigo y este mismo PENDIENTES), y ninguna persona de negocio podia consultarlas sin abrir el repo. **Modelo (se descartaron dos alternativas antes de llegar a este, dejar constancia para no repetir la discusion)**: (1) se descarto un motor de reglas ejecutable -- eso es §7.1 del Documento Maestro sobre `tblReglaAutomatizacion`, que sigue pendiente y sin una sola linea de codigo; (2) se descarto un modelo de alcance por criterio (categoria de proyecto, programa, portafolio, proceso, con exclusiones y precedencias) porque el negocio piensa las reglas SIEMPRE desde un sistema concreto. El modelo definitivo es: toda regla **nace en un proyecto dueno** (`tblReglaNegocio.IdProyecto`, NOT NULL), puede declararse como **afectando a otros proyectos** enlistados **uno por uno** a mano (`tblReglaNegocioImpacto`, nada derivado), y dentro de su proyecto se ubica en **UN flujo de operacion O UNA caracteristica del sistema** (`tblAmbitoRegla`, FK simple nullable, catalogo PROPIO de cada proyecto). Al consultar un proyecto se ven sus reglas propias y las heredadas con la etiqueta de su proyecto de origen; **las heredadas se leen pero solo se editan en su dueno**, que es lo que garantiza que nunca existan dos versiones del mismo enunciado. Enunciado versionado en `tblReglaNegocioVersion` (mismo patron que `tblArticuloVersion`): sube version SOLO si cambio el texto, renombrar o mover de flujo no ensucia el historial. **Integridad sin trigger**: una regla no puede impactarse a si misma; se logro desnormalizando `IdProyectoDueno` en la fila de impacto + FK compuesta contra `UQ_tblReglaNegocio_IdProyecto` + `CHECK IdProyectoAfectado <> IdProyectoDueno`. Permisos nuevos `RGN.Ver`/`RGN.Administrar` (script 43, solo rol Administrador), ambos con alcance por proyecto: `RGN.Ver` respeta `tblUsuarioRol` porque las reglas de negocio de un sistema son informacion sensible del cliente interno. **Semilla (script 44)**: GTE se dio de alta como **un proyecto mas** (`tblProyecto.Clave = 'GTE'`, quedo con `IdProyecto = 2`) y se sembraron sus **40 reglas**: 25 Vigente, 1 Implementada parcialmente (`RN-PRY-02`), 14 Documentada sin implementar. **DOS DISCREPANCIAS REALES que el catalogo saco a la luz al sembrarlo, pendientes de decision del equipo**: (a) **`RN-PLA-05` existe en el codigo** (`PlaneacionQueryService`, "la columna Terminado solo muestra el mes en curso", marcada "nueva") **y NO esta en el Documento Maestro** -- hay que documentarla ahi; (b) **`RN-QA-06` significa cosas distintas en cada fuente**: el Documento Maestro la usa para "no rechazar sin hallazgo pendiente" y el codigo (`CambiarEstatusWorkItemCommand`) para "en proyectos Desarrollo, saltar En Pruebas exige `WI.SaltarPruebas`". Se sembro **la version del codigo** porque es la que corre, y la del documento se sembro dentro de `RN-QA-04`, que es donde esta implementada; el equipo decide si renumera o corrige el documento. **Las 14 "Documentada sin implementar" REQUIEREN REVISION**: son las claves que no aparecen en el codigo (`RN-ADM-03/04`, `RN-DEV-01/02/03`, `RN-PLA-01`, `RN-PRY-03`, `RN-REL-04`, `RN-REQ-06/07/09`, `RN-SUP-01/02/03`) y algunas podrian estar implementadas sin la etiqueta -- no se marcaron como vigentes sin evidencia. **Defensa anti-podredumbre**: `CatalogoReglasSincronizadoTests` (GTE.Domain.Tests, prueba PURA sin BD) escanea las claves `RN-*` de `src/` y falla si alguna no esta en el script de semilla versionado. Se compara contra el SCRIPT y no contra la BD a proposito: corre en cualquier maquina sin LocalDB. **Trampa del propio mecanismo**: el escaneo no distingue una referencia real de un ejemplo en un comentario -- para ilustrar el formato hay que usar el marcador `RN-XXX-NN`, que no coincide con el patron (la prueba fallo exactamente por eso al escribirla, con tres claves de ejemplo inventadas). **Verificado extremo a extremo contra `ALIEN\SQLEXPRESS01`** (no solo compilado): scripts 43 y 44 aplicados y corridos 2-3 veces (segunda corrida solo SKIP y 0 filas, idempotencia confirmada); las 4 defensas de integridad probadas con SQL real y rollback (auto-impacto -> CHECK, dueno desalineado -> FK compuesta, clave duplicada en el mismo proyecto -> UNIQUE, vigencia invertida -> CHECK); por HTTP con token de desarrollo real: catalogo de GTE devuelve 40 propias / 0 heredadas, se creo una regla en el proyecto 1 declarando impacto al 2 y **GTE la vio aparecer como Heredada con el chip de su proyecto origen y la descripcion del impacto**, derogarla la quito de las heredadas del secundario en el mismo movimiento, editar solo el nombre NO subio version y cambiar el enunciado SI (v1 -> v2), auto-impacto responde **400** con mensaje (no 500 por constraint) y un ambito de otro proyecto tambien **400**; la bitacora registro CREAR/ACTUALIZAR/AGREGAR_IMPACTO/DEROGAR con el usuario del token. En el navegador (SPA de desarrollo apuntada temporalmente a la instancia de prueba): `/reglas-negocio` lista "Reglas propias (40)" con selector de proyecto y filtros, y `/reglas-negocio/34` muestra ficha, panel de proyectos afectados, reglas relacionadas e historial. Los datos de prueba se borraron: la base quedo en 40 reglas y 9 flujos. `dotnet build` 0 errores, 34/34 pruebas de Domain, `tsc -b`/`oxlint`/`vite build` limpios. **Fuera de alcance a proposito**: el catalogo DOCUMENTA, no controla -- dar de alta una regla no la vuelve ejecutable; sigue pendiente el motor §7.1; los proyectos distintos de GTE arrancan con catalogo vacio (no se inventaron reglas de sistemas que no conozco); sin pruebas de integracion nuevas en `GTE.Api.Tests` (la suite asume LocalDB, que no existe en esta maquina). **Matiz al "Entorno real" documentado abajo (el `localhost` de `appsettings.json` SI es correcto, no lo cambien)**: el servicio conecta a `ALIEN\SQLEXPRESS01/bdsGTE` con el login `svc_gte` -- verificado consultando `sys.dm_exec_sessions` en esa instancia, donde se ven sus conexiones con `program_name = 'GTE.WebApi'`. Lo que NO funciona es `Server=localhost` desde una **sesion interactiva normal**: ahi resuelve a la otra instancia (`SQLEXPRESS`), que no tiene `bdsGTE` (`DB_ID('bdsGTE')` devuelve NULL ahi). O sea: `appsettings.json` esta bien como esta, pero para `sqlcmd` o para levantar una instancia de depuracion a mano hay que escribir `Server=localhost\SQLEXPRESS01`. **INCIDENTE DE DESPLIEGUE del 2026-08-24, la trampa mas cara de esta sesion**: tras desplegar, el modulo nuevo seguia sin verse en el navegador aunque el backend, la BD, el permiso y el bundle nuevo estaban todos correctos. Causa: `index.html` se servia **sin `Cache-Control`** (solo ETag/Last-Modified) y el despliegue **dejo el bundle viejo en `wwwroot/assets`**. Un navegador con el `index.html` viejo en cache seguia pidiendo el bundle anterior, que seguia existiendo y respondia 200, asi que cargaba la SPA ANTERIOR **sin un solo error en consola** -- sintoma: modulo invisible, todo lo demas funcionando. Diagnostico decisivo (guardarlo para la proxima): `curl http://localhost:5090/ | grep -o 'index-[A-Za-z0-9_-]*\.js'` para ver a que bundle apunta el HTML servido, y `curl .../assets/<bundle>.js | grep -c '<ruta-del-modulo-nuevo>'` para ver si ese bundle trae el codigo. Arreglo de raiz ya en el codigo (`Program.cs`): `StaticFileOptions.OnPrepareResponse` marca `no-cache, no-store, must-revalidate` para `.html` y `public, max-age=31536000, immutable` para el resto (los assets llevan hash de contenido en el nombre), y **las mismas opciones se le pasan a `MapFallbackToFile`**, que es el camino que sirve `index.html` en los deep links -- sin eso el problema vuelve por la puerta de atras. Ademas el despliegue debe dejar `wwwroot` espejeado (`robocopy /MIR`) para que no sobrevivan bundles huerfanos. **Esa correccion de `Program.cs` esta compilada pero NO desplegada todavia**: requiere un publish+despliegue nuevo (elevado) para tomar efecto | Reglas de negocio (`/reglas-negocio`), Detalle de regla (`/reglas-negocio/:id`) |
| **Base de conocimiento (P23), incluida su publicacion anonima (2026-08-23)** | Tercer modulo de Fase 4. Articulos versionados y terminos de Glosario sobre `tblArticuloConocimiento`/`tblArticuloVersion` (tablas que ya existian desde el script 06 del despliegue inicial, sin codigo hasta ahora). Un termino de Glosario NO es una entidad aparte: es la misma fila con `EsGlosario = 1`, asi que hereda editor, imagenes, adjuntos y versionado sin ramas de codigo propias; lo unico distinto es la presentacion (el filtro "Glosario" pinta un indice alfabetico agrupado por letra en vez de tarjetas de resultado, porque son definiciones cortas y no articulos largos). **Versionado**: `tblArticuloVersion` guarda TODAS las versiones incluida la vigente y `VersionActual` apunta a ella; una edicion genera version nueva SOLO si el contenido cambio (renombrar o mover los switches no ensucia el historial). Se puede abrir una version anterior para verla sin restaurarla. **Contenido enriquecido y archivos**: reutiliza `EditorEnriquecido` (Tiptap) tal cual -- formato basico y pegado de imagenes del portapapeles -- y la tabla generica `tblArchivoVinculo` con el discriminador `Entidad = 'ArticuloConocimiento'`, asi que NO hizo falta esquema de adjuntos propio (solo un `SubirArchivoArticuloCommand`/`ObtenerArchivosArticuloQuery` calcados de los de WorkItem). Permiso nuevo `CON.Administrar` (script 42, sembrado solo al rol Administrador): escribir lo exige, LEER no -- P23 esta marcada como "Todos" en la seccion 5.1 del Documento Maestro. **Publicacion anonima (pedido de negocio de esta misma sesion)**: columna nueva `tblArticuloConocimiento.EsPublico` (BIT NOT NULL DEFAULT 0, script 42) y un controlador aparte `ConocimientoPublicoController` con `[AllowAnonymous]` -- segunda excepcion deliberada al FallbackPolicy despues de health/version/auth, con la razon escrita en el propio controlador como exige CLAUDE.md. Los limites de esa excepcion, todos implementados: (1) el default es privado y marcar publico es una accion explicita del autor; (2) un articulo interno responde 404 igual que uno inexistente, para que nadie enumere el contenido privado probando ids; (3) solo lectura; (4) DTOs propios (`ArticuloPublico*`) que NO exponen autores, numero de version ni historial; (5) **las imagenes del texto SI se ven pero los adjuntos NO se descargan** (decision explicita del negocio en esta sesion) -- el endpoint publico de imagenes sirve un GUID solo si esta vinculado a un articulo publico Y ademas aparece incrustado dentro de su HTML (`Contenido.Contains(guid)`, LIKE parametrizado, nunca SQL interpolado), asi un PDF o un archivo interno no se sirve sin sesion aunque alguien adivine su GUID; no existe endpoint publico de adjuntos; (6) limitador de tasa nuevo (`AddRateLimiter`, politica `publico`, 120 req/min por IP) porque son las unicas rutas expuestas a internet -- el resto de la API exige token y vive en la red interna. Dar de baja un articulo apaga `EsPublico` en el mismo movimiento, para que un cambio futuro en el filtro publico no pueda re-exponerlo. En el frontend las 2 rutas publicas viven FUERA de `GuardiaSesion` (las unicas del SPA que cargan sin sesion) y con `LayoutPublico` propio: reusar el shell interno le revelaria el menu de los 20 modulos de GTE a cualquier visitante. `ContenidoEnriquecido` se extendio con una prop opcional (`urlPublicaImagen`) en vez de duplicarse: en modo publico resuelve la imagen a una URL directa (se puede porque la ruta es anonima y no hay token que exponer, que es la razon de ser del blob autenticado en el modo interno). **Verificado extremo a extremo (no solo compilado)** contra el SQL Server real `ALIEN\SQLEXPRESS01` (la base NO esta en LocalDB -- ver nota de entorno abajo). Script 42 aplicado y corrido dos veces (la segunda solo SKIP y 0 filas afectadas, idempotencia confirmada); la columna real quedo `EsPublico bit NOT NULL`, igual que el scaffold editado a mano. Pruebas reales con datos creados por la propia API: (a) el listado publico anonimo devuelve SOLO el articulo marcado publico (1 de 2); (b) el detalle publico de un articulo interno responde **404**, indistinguible de uno inexistente; (c) los DTOs publicos llegan sin autor ni version; (d) **la regla de imagenes**: la imagen INCRUSTADA en un articulo publico se sirve `200 image/png`, mientras que un PDF adjunto al MISMO articulo publico pero NO incrustado responde **404**, y una imagen incrustada en un articulo PRIVADO tambien **404**; (e) la baja logica dejo `Activo=0` y `EsPublico=0` en la misma operacion y el articulo desaparecio del publico; (f) RBAC: una cuenta sin `CON.Administrar` LEE (200 en lista y detalle) pero no escribe (403 en POST y DELETE, con el mensaje del permiso); (g) versionado: editar el contenido subio a v2 y un PUT identico NO creo version nueva (siguio en 2 versiones); (h) titulo duplicado -> 409; (i) el limitador de tasa corto exacto en 120 (120x200 + 10x429 en 130 peticiones); (j) el sanitizado elimino `<script>`, `onerror`, `href="javascript:"` e `<iframe>` dejando solo `<p>ok</p><img><a>clic</a>`; (k) la bitacora registro CREAR/ACTUALIZAR/ELIMINAR/ADJUNTAR con el usuario del token. En el navegador: `/publico/conocimiento` lista el articulo publico sin sesion, el detalle renderiza la imagen resuelta por la URL publica (`cargada: true`) y NO tiene boton Editar, ni panel de Adjuntos, ni historial de versiones; `/conocimiento` sigue cayendo en la pantalla de login. Compilacion y pruebas: backend `0 Errores`, `tsc -b`/`oxlint` limpios, 23 Domain + 2 Application pasan. **Datos de prueba que quedaron en esa base de desarrollo** (borrarlos si molestan): articulos 1 "Procedimiento interno de respaldos" (privado) y 2 "SLA (Service Level Agreement)" (publico, glosario, con `captura.png` incrustada y `manual.pdf` adjunto), los articulos 3 y 4 ya dados de baja, y el usuario sin roles `pruebaconocimiento` que se creo solo al pedir un token de desarrollo. **Entorno real (confirmado con el usuario 2026-08-23, dejar de adivinar esto)**: el API que vale es el **servicio de Windows `GTE`** (`C:\Servicios\GTE\GTE.WebApi.exe`, como LocalSystem, ambiente Production) en el puerto **5090**; la base es la instancia **`SQLEXPRESS01`**, alcanzable como `Server=localhost` en el puerto 1433 (es la forma que usa `appsettings.json` y la que funciona para el servicio -- verificado: el servicio en 5090 devuelve los articulos sembrados en `ALIEN\SQLEXPRESS01`, o sea es la MISMA base). Hay una segunda instancia de SQL en la maquina (`SQLEXPRESS`) que NO se usa. La segunda instancia del API en el puerto 5088, que se levantaba a mano, **ya no se usa**: se quito la entrada `api` del `.claude/launch.json` (clavaba la cadena a `(localdb)\MSSQLLocalDB`, que no existe aqui) y el frontend apunta a 5090 por default (`.env.development` + el default de `http.ts`; `.env.production` va vacio porque en produccion Kestrel sirve el SPA desde wwwroot en el mismo origen). Aun asi existe `appsettings.Local.json` (gitignored) como mecanismo de override por maquina: CLAUDE.md ya lo documentaba pero nunca se cargaba, y ahora si se lee via `AddJsonFile` en `Program.cs` -- "Local" no es un ASPNETCORE_ENVIRONMENT, asi que la convencion `appsettings.{Environment}.json` no lo tomaba. **INCIDENTE de esta sesion, leer antes de tocar configuracion**: ese `appsettings.Local.json` de desarrollo se colo en el `dotnet publish` (el SDK web lo barre con el glob `appsettings*.json`), se copio a `C:\Servicios\GTE` y, al tener la precedencia mas alta, le gano a `appsettings.json` y repunto el SERVICIO al nombre `ALIEN\SQLEXPRESS01`, forma con la que el servicio NO logra conectar (con `localhost` si; la causa exacta de esa diferencia no se investigo a fondo -- SQL Browser esta arriba y la instancia escucha en 1433, asi que apunta a resolucion de nombre de instancia en el contexto de LocalSystem, no a permisos de BD, ya que es la misma base). Resultado: toda llamada a BD trono y el login empezo a responder `500 INTERNAL_ERROR` incluso con contraseña incorrecta -- **sintoma delator para la proxima vez: la instancia local con el MISMO codigo respondia `400` limpio, o sea el 500 era de conexion, no de credenciales**. Se arreglo borrando el archivo de `C:\Servicios\GTE` (respaldo en el scratchpad de esa sesion) y reiniciando el servicio. Quedaron DOS defensas: (1) `CopyToPublishDirectory="Never"` sobre `appsettings.Local.json` y `appsettings.*.Local.json` en `GTE.WebApi.csproj` -- verificado con un `dotnet publish` real, el paquete ya solo lleva `appsettings.json`/`.Development`/`.Production` y el bin de desarrollo si conserva el Local; y (2) el `AddJsonFile` de `Program.cs` corre SOLO si `IsDevelopment()`, para que aunque alguien lo copie a mano a un servidor no pise la config de produccion. **Leccion general**: un archivo de configuracion por maquina jamas debe poder ganarle a la config de produccion, y "gitignored" no implica "no se publica". **Fuera de alcance de esta pasada**: migracion del Glosario del GT (falta el equivalente de `tblGlosarioTag`, ver B3); sugerencia de articulos al capturar ticket (IA, Fase 5); busqueda por texto es `LIKE` sobre titulo y contenido (si crece el volumen toca evaluar Full-Text, con indice de apoyo ya creado para el filtro publico/glosario); sin pruebas automatizadas nuevas | Base de conocimiento (`/conocimiento`), Detalle de articulo (`/conocimiento/:id`), y las 2 publicas sin sesion: `/publico/conocimiento` y `/publico/conocimiento/:id` |
| **Manual de usuario (Ayuda) (actualizado 2026-08-04)** | Pagina de ayuda dentro de la SPA: `ManualUsuarioPage.tsx` ahora es un `<iframe>` que incrusta `Doctos/ManualUsuarioGTE.html` (el manual real y completo, HTML autocontenido con su propio sidebar/buscador/estilos, 27 secciones incluyendo Proximamente y Glosario), servido como `/manual-usuario.html` desde `public/` de Vite. `frontend/gte-web/scripts/sync-manual.mjs` copia el HTML de `Doctos/` a `public/` en cada `predev`/`prebuild` -- se edita SOLO `Doctos/ManualUsuarioGTE.html` (a mano o con IA) y el build/publish siempre lo refleja, sin tocar codigo. Sin permiso (disponible para cualquier usuario autenticado). El acordeon viejo (contenido fijo en el componente) se reemplazo por completo; `DiagramaFlujoSolicitud.tsx` quedo sin referencias, no se borro (ver nota arriba) | Ayuda (visible para todos en el menu) |
| **Menu lateral (2026-08-02)** | La navegacion se movio de una barra horizontal arriba a un panel lateral fijo del lado izquierdo (`Drawer` de MUI, `variant="permanent"`, `anchor="left"`), con la opcion activa resaltada segun la ruta actual. En pantallas chicas se colapsa a un boton de menu al inicio de la barra superior (junto al logo) que abre un cajon deslizable (`variant="temporary"`) que se cierra solo al navegar. La barra superior conservo el logo, la campana de notificaciones y el chip de usuario (con el nombre truncado en pantallas chicas para no empujar el boton de menu fuera de la vista). Se probo primero con `anchor="right"` (pedido inicial) y se corrigio a `anchor="left"` (decision final) -- ver leccion tecnica en la seccion 5 sobre por que el lado derecho encimaba el menu con el contenido | Panel lateral izquierdo (todas las pantallas) |
| **Combos con buscador (2026-08-03)** | Todo `Select`+`MenuItem` de MUI que representa un catalogo dinamico del backend (proyecto, usuario, estatus, prioridad, severidad, release, ambiente, etc.) se reemplazo por dos componentes nuevos y reutilizables en `frontend/gte-web/src/shared/components/ComboBuscable.tsx`: `ComboBuscable` (single) y `ComboBuscableMultiple` (multiple), ambos wrapper de MUI Autocomplete con el catalogo normalizado a `{valor, etiqueta}` -- el filtro "contiene" insensible a mayusculas es el default nativo de Autocomplete, sin `filterOptions` custom. Cubre ~70 combos en 22 archivos de `src/features/`. Las opciones "Todos"/"Sin asignar"/"Sin especificar" viven como una entrada mas del arreglo con `valor: ""`, igual que el `MenuItem value=""` que reemplazan. El multiple con chips (Estatus en BarraFiltros/BandejaTickets, sentinel `-1`="Todos") y el multiple con resumen de texto (prop `resumenSimple`, "Elementos" en ReleasesPage) usan el mismo `ComboBuscableMultiple`. **Fuera de alcance por decision explicita**: unos 10 `Select` de listas cortas y fijas escritas en el JSX sin catalogo de backend (dia de semana y Alcance en Horarios, Aplica-a y Trimestre en OKRs, Destino de cierre de sprint en Backlog, Tipo de artefacto y Tipo despliegue/rollback en Releases, Resultado de ejecucion en QA) quedan como `Select` simple -- pocas opciones fijas, buscar no aporta. Verificado con `tsc --noEmit` (0 errores, `noUnusedLocals`/`noUnusedParameters` activos) y probado en vivo en el navegador (filtro Proyecto y Estatus multiple de la Bandeja de trabajo) | Todos los combos de catalogo en toda la SPA |
| **Bandeja de trabajo: ajustes de filtro/columna/menu (2026-08-03)** | Tres ajustes puntuales sobre el modulo WorkItems ya existente: Asignado se inicializa con el usuario firmado al entrar (`BandejaPage.tsx`, no pisa una eleccion posterior a "Todos" mientras la pantalla siga montada); columna Presupuesto retirada de la tabla (`TablaBandeja.tsx`); "Registrar tiempo" de vuelta en el menu de acciones de cada fila (`MenuAcciones.tsx`, reutilizando `ModalTiempo` ya existente). Mismo archivo `MenuAcciones.tsx` gano la accion **"Copiar"**: abre `NuevoItemModal` (nueva prop `copiaDe`) prellenado con Proyecto/Tipo/Prioridad/Titulo ("Copia de ...")/Descripcion/Asignado del original -- hace `GET /workitems/{folio}` porque la fila de la bandeja no trae Descripcion ni IdPrioridad numerico; Estatus/Folio/Fecha compromiso siempre nacen limpios. `NuevoItemModal` tambien gano defaults propios sin copiaDe: Prioridad en Media (id 3) y Asignado en el usuario firmado; y si el Proyecto es categoria "TI", Fecha compromiso se precarga con hoy (solo si sigue vacia) -- requirio agregar `CategoriaProyecto` a `ProyectoItemResponse`/`CatalogosQueryService`. Ademas, los 9 combos de Proyecto que salen de `/catalogos/bandeja` (bandeja, nuevo elemento, triage, incidentes, tickets, backlog, QA, releases) ahora solo listan proyectos donde el usuario es Responsable o es de su equipo (join contra `TblEquipoMiembro`, resuelto en el Handler via `IProveedorUsuarioActual`) -- deliberadamente NO aplica a Ambientes/Costeo/OKR, que siguen viendo todos los proyectos via `/api/v1/proyectos` sin filtro. Verificado en vivo contra la API real con `aviramontes`: filtro Asignado, ausencia de columna Presupuesto, menu con Registrar tiempo/Copiar, Copiar trayendo Descripcion+Asignado reales, y Prioridad/Asignado por default confirmados en el DOM | Trabajo (bandeja + menu de acciones + Nuevo/Copiar) |
| **Mi Dia: Tickets, Incidentes, Solicitudes y Triage (2026-08-03)** | Extiende la fila "Mi Dia" de arriba: ademas de WorkItems, `GET /api/v1/mi-dia` ahora trae `TicketsAsignados` (agente, no Cerrado -- reutiliza `ITicketQueryService.ObtenerBandejaAsync` sin tocarlo), `IncidentesRelevantes` (proyectos donde el usuario es Responsable, no Cerrado -- `IIncidenteQueryService.ObtenerRelevantesAsync` nuevo, ya que `TblIncidente` no tiene asignacion directa a un usuario), `SolicitudesPendientes` (las que el usuario levanto, en Enviada/EnAnalisis/Aprobada -- reutiliza `ISolicitudQueryService.ObtenerMiasAsync`) y `TriagePendientes` (contador GLOBAL de solicitudes esperando revision en todo el sistema, solo si el usuario tiene `SOL.Triage` -- no existe forma de personalizarlo por proyecto/equipo hoy, es un aviso con link a `/triage`, no una lista propia). Cada seccion nueva solo se muestra si tiene contenido (a diferencia de Vencidas/Para hoy/Proximas que siempre muestran "(0)"), para no ensuciarle la pantalla a quien no le aplica. Verificado en vivo contra la API real: `aviramontes` vio "Tickets asignados (2)" con datos y links reales a `/tickets/{folio}`; Incidentes/Solicitudes/Triage no aparecieron por no tener datos, confirmando el comportamiento de seccion oculta | Mi Dia |
| **Administracion: edicion de Proyecto + bloqueo de proyecto cerrado (2026-08-03)** | El backend de edicion de Proyecto (`PUT /api/v1/proyectos/{id}`, `ActualizarProyectoCommand`, `actualizarProyecto` en `administracion.ts`) ya existia completo desde antes pero `ProyectosTab.tsx` nunca lo exponia en la UI -- se agrego boton "Editar" + modal (Nombre/Categoria/Equipo/Responsable/fechas/Mantenimiento; Clave e IdPrograma quedan de solo lectura por diseno del backend), mismo patron `entidadEditar`/`abrirEditar`/`guardarEdicion` que ya usa `UsuariosTab.tsx`. **RN-PRY-02 (nueva)**: un proyecto en estatus Cerrado o Cancelado ya no admite WorkItems nuevos -- `CrearWorkItemHandler` valida `IdEstatusProyecto` (campo agregado a `ProyectoResumen`/`WorkItemRepository.ObtenerProyectoAsync`, mismo query existente, sin round-trip extra) contra `EstatusProyecto.Cerrado`/`.Cancelado` (ya existian como constantes) y rechaza con 400 "El proyecto esta cerrado; no admite elementos nuevos." (mismo estilo `BusinessException` que la validacion de proyecto inactivo ya existente). Verificado en vivo contra la API real: `PUT` de edicion persistio un cambio de Responsable (confirmado y revertido); bloqueo probado de punta a punta creando un proyecto de prueba, autorizandolo y cancelandolo via el motor de workflow, y confirmando el 400 al intentar crear un WorkItem ahi | Administracion > Proyectos |
| **Proyectos administrados y QA obligatorio por categoria (2026-08-04)** | `tblProyecto.Administrado` (BIT, script 22) marcable desde Administracion > Proyectos: en un proyecto administrado, crear un WorkItem exige `WI.CrearEnAdministrado` y cancelarlo (accion CANCELAR desde Pendiente, el "eliminar" real del sistema -- no hay borrado fisico) exige ademas `WI.EliminarEnAdministrado`; el resto de usuarios solo cambia estatus. RN-QA-06 nueva: en proyectos categoria Desarrollo, terminar un WorkItem directo desde En Proceso (sin pasar por En Pruebas) exige `WI.SaltarPruebas`; TI y Mantenimiento siguen libres. RN-REQ-04 ampliado: cualquier cambio a una fecha compromiso YA capturada exige `WI.ModificarCompromiso` (antes solo se bloqueaba moverla al pasado); fijarla la primera vez sigue libre. Los tres permisos nuevos (script 23) solo se sembraron para Administrador -- el equipo asigna a otros roles desde Administracion > Roles si lo decide. Verificado: 53/53 pruebas backend en verde tras correr los scripts 22/23 en LocalDB (una prueba existente se ajusto a categoria TI porque probaba una regla distinta, RN-REQ-03) | Administracion > Proyectos, cualquier WorkItem |
| **Buscadores y orden en catalogos y bandejas (2026-08-04)** | Hook `useOrdenTabla` + componente `EncabezadoOrdenable` (`frontend/gte-web/src/shared/hooks`, `.../shared/components`) para orden client-side en tablas sin paginacion server-side: Administracion (Proyectos, Equipos, Usuarios, Ambientes, Horarios/festivos, Areas, Puestos), QA y Releases; buscador de texto en memoria donde faltaba. Para Tickets/Incidentes/Triage (paginados) se agrego `ordenarPor`/`ordenDescendente` al filtro y un switch `OrderBy`/`OrderByDescending` en el QueryService, mismo patron server-side que ya tenia la Bandeja de trabajo. Solo `tsc`/`vite build`, sin pruebas automatizadas nuevas | Administracion, QA, Releases, Mesa de ayuda, Incidentes, Revision de solicitudes |
| **Mesa de ayuda: filtro por Agente (2026-08-04)** | `BandejaTicketsPage.tsx` inicializa el filtro Agente (`idAsignado`) con el usuario firmado al entrar, sin pisar una eleccion posterior a "Todos" -- mismo patron que ya tenia el filtro Asignado de la Bandeja de trabajo. El backend ya soportaba `idAsignado` en el filtro, solo faltaba exponerlo. Verificado en vivo: el querystring real trae `idAsignado=<idDelUsuarioFirmado>` sin tocar nada | Mesa de ayuda (`/soporte`) |
| **Nueva Solicitud con editor enriquecido + ver detalle en Revision (2026-08-04)** | Descripcion de `PortalPage.tsx` (antes texto plano) ahora usa `EditorEnriquecido` (mismo TipTap de Descripcion de WorkItem/Comentarios/Hallazgos); `CrearSolicitudHandler` sanitiza con `ISanitizadorHtml` (gap real, antes guardaba tal cual). Pegar imagenes antes de guardar (alta) sigue bloqueado, igual que WorkItem (no existe `idSolicitud` todavia) -- **ya no es un callejon sin salida**, ver fila "Edicion de Solicitud + adjuntos" mas abajo: guardar primero y editar despues si resuelve. En Revision de solicitudes (`TriagePage.tsx`), icono nuevo "Ver detalle" por fila abre un dialogo de solo lectura con los datos que la bandeja ya trae (sin round-trip nuevo), renderizando Descripcion con `ContenidoEnriquecido`; el tooltip del titulo se corrigio para no mostrar tags HTML crudos (`htmlATextoPlano` nuevo en `shared/editor/textoPlano.ts`). Verificado en vivo end-to-end: una Solicitud creada con texto en negritas confirma `<strong>` en la respuesta real del POST (sanitizado, no escapado) y se ve en negritas en el dialogo de Ver detalle | Solicitudes (`/solicitudes`), Revision de solicitudes (`/triage`) |
| **Edicion de Solicitud + adjuntos genericos + buscador en Mis solicitudes (2026-08-04)** | `PUT /api/v1/solicitudes/{id}` (`ActualizarSolicitudCommand`) edita Titulo/Descripcion/Tipo/Prioridad/FechaDeseada/JustificacionNegocio/UsuarioSolicitante mientras el estatus siga en Enviada/EnAnalisis/Aprobada; el propio solicitante siempre puede, cualquier otro necesita `SOL.Triage` (mismo gate "ajeno" que el resto del sistema). El mecanismo de adjuntos ya era generico por diseno (`ArchivoNuevo(Entidad, IdEntidad, ...)`, tabla de vinculo polimorfica) pero solo WorkItem lo consumia -- se agrego el par `SubirArchivoSolicitudCommand`/`ObtenerArchivosSolicitudQuery` + rutas `POST/GET /api/v1/solicitudes/{id}/archivos`, mismo patron exacto que WorkItem. `EditorEnriquecido` gano el prop generico `onSubirImagen` (prioridad sobre `idWorkItemParaAdjuntos`) para no acoplarlo a una sola entidad -- callers de WorkItem sin cambios. `SolicitudResponse` ahora expone `idTipoSolicitud`/`idPrioridad`/`idUsuarioSolicitante` (antes solo el nombre resuelto) para poder precargar el formulario de edicion sin adivinar el id buscando por nombre. `PortalPage.tsx` (Mis solicitudes) gano buscador y orden por columna (`useOrdenTabla`/`EncabezadoOrdenable`, mismo patron que el resto de catalogos) -- se habia quedado fuera del lote general de la sesion anterior. Verificado en vivo con un `paste` sintetico de una imagen real sobre una Solicitud Aprobada ya guardada: `POST /solicitudes/{id}/archivos` 200, `PUT /solicitudes/{id}` 200 con `descripcion` conteniendo `<img data-guid="...">` | Mis solicitudes (`/solicitudes`) |
| **Dialogo "Convertir en elementos de trabajo" mas ancho (2026-08-04)** | Los 5 controles (Tipo/Titulo/Prioridad/Asignado/Compromiso) + icono de borrar vivian en una sola fila dentro de un dialogo de 900px (`maxWidth="md"`), dejando Titulo con el espacio sobrante (a veces <150px) y el resto en 120-150px cada uno -- ilegible con titulos largos. Rediseñado en `TriagePage.tsx`: dialogo `maxWidth="lg"` (~1100px), cada item en su propia tarjeta (`Paper`) con Titulo en fila propia a ancho completo y el resto de campos envolviendo con `flexWrap` a ~200px cada uno. Verificado en vivo: dialogo real midio 1095px con Titulo en ~975px (antes competia por espacio con otros 4 controles) | Revision de solicitudes (`/triage`), dialogo Convertir |
| **Registrar tiempo: total (2026-08-04)** | Suma client-side (el endpoint `/tiempo` ya traia todo, sin cambios de backend) mostrada como fila "Total" al pie de la pestaña Tiempo del Detalle de WorkItem, y como "Total ya registrado: Xh Ym" dentro del propio `ModalTiempo` (mismo query de React Query, sin round-trip extra si ya esta en cache) | pestaña Tiempo del Detalle de WorkItem, modal Registrar tiempo |
| **Editor de Workflows (P21, 2026-08-04)** | Pantalla nueva en `/admin/workflows` (ruta aparte, no pestaña de Administracion), permiso `ADM.Workflows` (sembrado desde el script 02, sin ningun consumidor hasta ahora). Backend nuevo completo (`GTE.Domain/Workflow`, `GTE.Application/Workflow`, `WorkflowQueryService`, `WorkflowRepository`, `WorkflowController`): lista de procesos (`tblProceso`) -> grafo completo de un proceso (`tblTransicion` + metadatos de `tblTransicionConfig`, unidos en memoria por no tener FK real) -> edicion en lote de etiqueta/permiso/motivo/accion principal/orden. **Deliberadamente NO crea ni elimina transiciones** (el grafo estructural sigue siendo por script SQL, "no tocar CambiarST" de la seccion 9.3 del InterfloClaude.md) -- solo edita los metadatos de UI de transiciones que ya existen en el grafo; para los 6 procesos sin fila de config todavia (Release, Ausencia, Riesgo, Sprint, Aprobacion, Proyecto) el editor muestra defaults (etiqueta = la accion tal cual) y crea la fila al guardar. Sin forma de leer dinamicamente el catalogo de estatus de un proceso sin SQL interpolado (`tblProceso.TablaEstatus` es solo texto descriptivo) -- se opto por un mapeo explicito de 11 casos en codigo en vez de reflection o SQL dinamico. Verificado en vivo: edicion de etiqueta/permiso en la transicion APROBAR de Solicitud, guardado, recargado y confirmado persistido, despues revertido | Workflows (`/admin/workflows`) |
| **Catalogos Area y Puesto (2026-08-04)** | `tblArea`/`tblPuesto` existian y se leian para los combos de Usuarios, pero sin CRUD ni pantalla propia (gap documentado en la seccion 3.4 de este archivo, ahora resuelto). Se clono el patron de Ambientes (Domain/Application/Infrastructure/WebApi + `AreasTab.tsx`/`PuestosTab.tsx`) para alta/edicion/baja logica completas, con buscador+orden desde el dia 1 (`useOrdenTabla`). Sin script SQL de esquema (las tablas ya existian). Verificado en vivo: alta de un Area y un Puesto vinculado a esa Area, ambos visibles de inmediato en su tabla | Administracion > Areas, Administracion > Puestos |
| **Flujo de registro de ausencias (2026-08-27)** | La BD ya tenia todo desde los scripts 01/02 de `01_Libera` (`tblAusencia`, `tblTipoAusencia`, `tblEstatusAusencia`) y el proceso `Ausencia` sembrado en `tblProceso`/`tblTransicion` (APROBAR/RECHAZAR/CANCELAR desde Solicitada), ademas de dos consumidores de lectura ya vivos (`PlaneacionRepository.ObtenerAusenciasAprobadasAsync` para capacidad de sprint y `ReportesQueryService` para la columna Ausencia del reporte de actividad); lo que faltaba era el flujo. Se construyo `GTE.Domain/Ausencias` (EstatusAusencia/AccionesAusencia/PermisosAusencia + `IAusenciaRepository`), `GTE.Application/Ausencias` (Crear/Actualizar/CambiarEstatus + queries mias/bandeja/detalle/acciones/catalogos/conteo), `AusenciaRepository` + `AusenciaQueryService`, `AusenciasController` (8 endpoints) y la pantalla `/ausencias` con pestañas "Mis ausencias" y "Por aprobar". **Aprobacion por permiso `ADM.Ausencias`, no por jefe directo**: el Documento Maestro (A12) preveia al jefe directo, pero `tblUsuarioRol` admite varios equipos/proyectos por persona y no modela una jefatura unica, asi que resolver "el jefe" seria ambiguo -- decision explicita, migrable despues si el negocio lo pide. Reglas propias del backend (las que el grafo no conoce): traslape con otra ausencia vigente (Solicitada/Aprobada) de la misma persona -> 409 con la lista de periodos que chocan (el front la pinta sin re-consultar); edicion solo mientras siga Solicitada; CANCELAR la ejecuta el dueño o quien gestione; RECHAZAR exige motivo. Notificacion A12 in-app en las dos direcciones: al solicitar, a todos los que tienen `ADM.Ausencias`; al aprobar/rechazar, a la persona. El permiso nuevo entra por `DataBase/Scripts/05_Scripts/01_2026-08-27_INSERT_bdsGTE_PermisoAusencias.sql` (siembra + asignacion al rol Administrador). **Sin verificar en vivo todavia**: exige correr ese script y la unica BD alcanzable es la real (el API lo sirve el servicio de Windows con los DLL anteriores). Backend y frontend compilan limpio | Ausencias (`/ausencias`) |
| **R15 Detalle de actividades terminadas (2026-08-28)** | Los reportes R01-R03 agregan por persona o proyecto; faltaba el detalle renglon por renglon, que es lo que se entrega como evidencia de trabajo del periodo. Nuevo reporte con filtros de equipo, asignado, proyecto, tipo y folio, atajos Hoy/Mes/Año, chips de totales y exportacion a Excel gemela (los minutos salen como horas decimales para que Excel los sume). Por cada actividad Terminada en el rango (filtrado por `FechaFin`): titulo, descripcion, tiempo invertido (`VwBandejaTrabajo.MinutosInvertidos`), fechas, **tiempo de espera** (creacion -> inicio) y **tiempo de resolucion** (creacion -> fin), ambos en dias naturales Y en tiempo habil. **Se agrego `ICalendarioLaboral.CalcularMinutosLaboralesLoteAsync`**: el metodo existente abre una conexion por llamada y un detalle de 300 renglones habria hecho 600 conexiones; el lote agrupa por horario y carga tramos/festivos una vez. Sigue pasando todo por el motor unico, no se introdujo un segundo motor de tiempo. El tiempo habil usa el horario **del asignado**; si no tiene horario configurado la columna sale vacia en vez de tronar el reporte. Tope de 5000 renglones con aviso en pantalla para que un rango abierto no tumbe la pagina ni el Excel. **DOS relojes de tiempo, no uno** (hallazgo de la sesion, valia la pena separarlos): la columna "En proceso" es tiempo habil en estatus En Proceso (`vwTiempoInvertido` suma `tblHistorialEstatus.MinutosLaborales`, materializados por `spCambiarEstatus`), mientras que "Registrado" es lo que la persona capturo a mano en `tblRegistroTiempo` (lo que reporta R02). Miden cosas distintas y no tienen por que coincidir, asi que se muestran ambas + su diferencia (roja solo cuando se registro DE MENOS, el caso accionable) + un contador "con tiempo capturado X de Y", sin el cual un total bajo no distingue "nadie captura" de "pocos capturan mucho". Atajos de rango Hoy/Semana/Mes/Año (Semana arranca en LUNES; `getDay()` da 0 en domingo y una resta ingenua dejaba el rango en un solo dia). **Tres secciones en el mismo reporte** (decision del equipo 2026-08-28): WorkItems, Tickets (Resuelto+Cerrado, con % de SLA y espera a primera respuesta) e Incidentes (Resuelto+Cerrado, con indisponibilidad y tiempo hasta la deteccion). Las tres usan el MISMO criterio de tiempo, el reloj de estatus: En Atencion es el analogo de En Proceso. No se mezclan en una sola tabla porque **no son simetricas**: un ticket no tiene equipo ni proyecto y un incidente ademas no tiene asignado, asi que filtrar por equipo (o por asignado en incidentes) VACIA esa seccion y el front explica por que, en vez de devolver datos que no honran el filtro. La indisponibilidad de un incidente es impacto, no esfuerzo: va en su propia columna y nunca se suma a los tiempos de trabajo. **BUG corregido en "A tiempo"**: comparaba los `datetime` completos, y como `FechaCompromiso` se captura a medianoche y `FechaFin` trae hora real, TODA tarea entregada el dia del compromiso salia como incumplida; ahora compara solo la parte de fecha (mismo bug que ya se habia corregido en `vwBandejaTrabajo.EsVencida`, script 27 de `02_Libera`). El SLA de tickets NO se toco: ahi `FechaLimiteResolucion` si es un instante con hora exacta y comparar con hora es lo correcto. **Verificado en produccion el 2026-08-28** tras dos fallos de EF en tiempo de ejecucion (ver seccion 5, "Dos fallos de EF que el compilador NO ve") | Reportes > R15 |
| **Notas de version visibles para el usuario (2026-08-28)** | El historial de lo liberado se escribia a mano al pie de `Directory.Build.props`, **dentro de un comentario XML con vinetas `--`, lo cual rompia el `[xml]` que lee `publicar.bat` y tumbo una publicacion** (error `"+" no es una cadena de version valida` en el restore). Se saco de ahi (el archivo ya solo tiene el `<Version>`, con una advertencia escrita) y se convirtio en modulo: `tblNotaVersion` + `tblNotaVersionDetalle` + `tblTipoCambioVersion` (enumerado de ID fijo 1 Proyecto / 2 Mejora / 3 Defecto, **el mismo eje del estandar de versionado**, para que la nota justifique por que subio el digito que subio). Guardado atomico del arbol con patron `uiId`; el `Orden` lo renumera el backend segun la posicion en el arreglo. Bajas: la nota es logica, los renglones fisica (son contenido en edicion, no historia). `GET /api/v1/notas-version` **no exige permiso a proposito** (cualquier usuario autenticado debe poder ver que trae la version que usa); redactar y publicar exige `ADM.NotasVersion`. El usuario lo abre desde el sello de version de la barra superior, que ahora es clickeable (se respeto la logica del descuadre ambar: si bundle y API no coinciden, el tooltip sigue avisando del despliegue a medias). **Las 3 entidades y su config en `DbContextGTE` se escribieron A MANO, sin re-scaffold**, porque habia trabajo en curso de otro equipo sobre la misma BD -- queda anotado en el `DbContext`; conviene cotejar el mapeo contra `INFORMATION_SCHEMA` la proxima vez que se toque. Scripts en `06_Scripts`: `01` esquema + permiso (ya corrido), `02` carga de las notas 1.18-1.21 migradas del texto viejo y reescritas en voz de usuario, **como borrador** (la redaccion es una interpretacion de peticiones y hay que revisarla antes de publicar). **Sin verificar en vivo**; backend y frontend compilan limpio | Administracion > Notas de version, sello de version de la barra superior |

**Inventario:** 156 endpoints en 19 controladores + 1 hub de SignalR · 20 pantallas ·
18 scripts SQL (mínimo; recuento aproximado entre sesiones paralelas) · ~100 tablas
(+1, `tblRefreshToken`) · 53 pruebas (sin pruebas nuevas para Tickets, Incidentes,
Portafolio ni el lote de pendientes de 2026-08-04, ver filas correspondientes arriba).

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
- ~~Base de conocimiento (`tblArticuloConocimiento`, `tblArticuloVersion`)~~
  **Construida 2026-08-23** (P23). Ver fila "Base de conocimiento (P23)" en la sección 2.
  Pendiente dentro de este sub-alcance: la migración del Glosario del GT con sus imágenes
  y tags de redirección (falta el equivalente de `tblGlosarioTag` para las relaciones
  bidireccionales, ver B3); sugerencia de artículos al capturar ticket (IA-, Fase 5).

**Fase 5 — Ejecutivo, automatizaciones e IA**
- ~~Dashboard ejecutivo: KPIs, OKRs, DORA metrics, costo y rentabilidad por proyecto,
  retrabajo, CSAT. `tblKpiDefinicion`/`tblKpiValor` y `spSnapshotKpi` ya existen.~~
  **Resuelto 2026-08-13** ("Indicadores ejecutivos", P18). Ver detalle de sesión arriba.
  Pendiente real dentro de este sub-alcance: pruebas automatizadas, DORA "Lead Time for
  Changes" (necesita integración Git, resto de Fase 3), y verificar el arrastre de widgets
  con mouse real (no se pudo automatizar en esta sesión, mismo tipo de limitación ya
  documentada para el kanban).
- ~~14 reportes del catálogo (§13 del diseño) y vistas `vwBI*` para Power BI.~~
  **Resuelto 2026-08-13.** Ver detalle de sesión arriba. Pendiente real dentro de este
  sub-alcance: pruebas automatizadas, y R06 Riesgos/R11 KPIs normalmente vacíos hasta que
  exista captura real (A5 sin CRUD de riesgos; KPIs personalizados sin más definiciones que
  las 2 semilla de `spSnapshotKpi`).
- 23 automatizaciones de fábrica (§7.2) sobre `tblReglaAutomatizacion` con constructor
  visual de reglas. Hangfire ya instalado (adelantado desde este bloque por el Dashboard
  P18, ver detalle de sesión arriba) -- reusar la misma infraestructura, no duplicarla.
- 11 funciones de IA (§14), empezando por IA-01: sugerir el desglose en historias al
  aprobar una solicitud.

### 3.4 Detalles menores conocidos

- ~~**El servicio de Windows no comparte el almacen de archivos con las instancias de
  desarrollo**~~ **Resuelto 2026-08-24 (era problema de despliegue, no de codigo; afectaba
  a TODA la app).** `AlmacenArchivos:Ruta` estaba VACIO, y con ese valor
  `AlmacenArchivosDisco` cae al fallback `AppContext.BaseDirectory + "ArchivosGte"`: cada
  proceso guardaba los binarios junto a su propio ejecutable (el servicio en
  `C:\Servicios\GTE\ArchivosGte`, una instancia de desarrollo en su
  `bin\Debug\net8.0\ArchivosGte`). Como la BD es la MISMA, los METADATOS de
  `tblArchivo`/`tblArchivoVinculo` se compartian pero los BINARIOS no: un adjunto subido
  por una instancia respondia 500 al descargarse desde la otra (el registro existe, el
  archivo no). Sintoma observado: la imagen incrustada en un articulo de la base de
  conocimiento se veia bien en la instancia local y el navegador la bloqueaba con
  `ERR_BLOCKED_BY_ORB` contra el servicio -- **ojo con ese error, enganna: la respuesta
  real era un 500 en JSON y el navegador lo bloqueo por no ser una imagen**. Aplicaba
  igual a adjuntos e imagenes pegadas de WorkItems, Solicitudes, Comentarios y Revisiones.
  **Arreglo aplicado** (se mantuvo ADR-07, filesystem con GUID; NO se migro a VARBINARY):
  `AlmacenArchivos:Ruta` = `D:\GTE\Archivos` tanto en el `appsettings.json` del REPO (para
  que viaje en cada `dotnet publish`) como en el del servicio (para que tome efecto sin
  esperar un redespliegue). Se eligio `D:` porque tiene ~560 GB libres contra ~30 GB de
  `C:`, y sobre todo porque NO debe vivir dentro de `C:\Servicios\GTE` (ese directorio se
  sobreescribe en cada publicacion y se llevaria los archivos) ni dentro del repo, que
  esta en la carpeta sincronizada de Google Drive. Se consolidaron ahi los binarios de
  los dos almacenes y se verifico que las 5 filas activas de `tblArchivo` tienen su
  archivo presente. **Hallazgo colateral que justifica la mudanza**: un archivo guardado
  en el almacen de desarrollo (dentro de Google Drive) aparecio en disco como
  `...1546.pdf` mientras `tblArchivo.RutaRelativa` decia `...1546` sin extension -- el
  codigo nombra los archivos SOLO por GUID (`guid.ToString("N")`, sin extension; la
  extension es metadato en BD), asi que algo del sincronizador le agrego la extension y
  eso rompe la descarga, porque `ObtenerAsync` abre la ruta sin extension. Se renombro.
  **Leccion: el almacen de archivos nunca debe vivir en una carpeta sincronizada.**
  Pendiente real: incluir `D:\GTE\Archivos` en la politica de respaldos (los respaldos de
  BD y de archivos son SEPARADOS, asi que pueden quedar inconsistentes entre si -- es la
  desventaja conocida de ADR-07 frente a guardar los binarios en la BD), y cuando haya
  servidor dedicado mover la ruta al share de red que preve el Documento Maestro (§1.2).
- **Proyectos migrados sin Responsable ni Equipo (dato faltante, no bug de codigo):**
  detectado al probar el filtro "solo mis proyectos" (2026-08-03) -- la mayoria de
  proyectos migrados del GT no tienen `IdResponsable` ni `IdEquipo` capturados en BD
  (los 8 proyectos de categoria "TI" probados los tienen ambos en `NULL`). Efecto real:
  ningun usuario los ve en los combos de Proyecto de Nuevo elemento/Copiar/filtros
  (`/catalogos/bandeja`), aunque sigan teniendo WorkItems activos asignados a personas
  concretas (la asignacion de trabajo es por tarea, no por proyecto, ver hallazgo de
  checksums de la migracion en B3 arriba). Decision del negocio: el filtro se queda
  como esta: el pendiente real es capturar Responsable/Equipo en esos proyectos
  (via el nuevo boton Editar de Administracion > Proyectos), no relajar el filtro.
  **Actualizacion 2026-09-09:** desde este dia hay una tercera via de acceso que si
  cuenta para el filtro -- un rol acotado al proyecto, dado en Admin > Proyectos >
  Accesos. No es una relajacion del filtro: es un acceso capturado a mano, proyecto
  por proyecto, que hasta ahora se otorgaba y el combo ignoraba. Los proyectos
  migrados sin Responsable ni Equipo siguen invisibles hasta que se les capture el
  dato o se le de acceso explicito a alguien.
- **Dato de prueba en LocalDB:** proyecto `TESTCERR` ("Proyecto prueba cierre"),
  llevado a Cancelado a proposito para probar RN-PRY-02, con un WorkItem
  `TESTCERR-0001` creado antes de cerrar el proyecto -- queda como esta en LocalDB,
  no en produccion, mismo criterio que otros registros de prueba ya mencionados en
  este documento (ej. `REESTRUCTURACION TI-0097` en B3).
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
  UI), gestion de Ausencias/vacaciones (mencionada en el Documento Maestro §3.1 pero no en el
  alcance de esta sesion), Repositorios Git (tabla `tblRepositorio` lista, sin API), y "Version
  del sistema". ~~CRUD de Areas/Puestos (catalogos simples, sin pantalla propia)~~ **Resuelto
  2026-08-04**, ver fila "Catalogos Area y Puesto" en la seccion 2. El catalogo de
  Nivel/Horario sigue solo de lectura para los selects de Usuarios, falta un alta propia si se
  necesita crear valores nuevos desde la UI en vez de por SQL.
- ~~`QaPage`: el alta de caso solo captura un paso; falta editar casos para agregar más~~
  **Resuelto por el rediseño 2026-08-21 (commit `52e2bec`, "las pruebas y sus hallazgos
  viven en el WorkItem").** `QaPage` ya no existe; alta y edición de casos soportan N
  pasos de punta a punta: `PanelPruebas.tsx` (alta, pestaña Pruebas del WorkItem) y
  `CasosPruebaTab.tsx` (edición del catálogo, Admin), ambos sobre `PasoCaso[]` real
  (tabla hija `tblCasoPruebaPaso`, no texto libre) vía `ActualizarCasoPruebaCommand`.
- `tblEtiqueta` sin uso (etiquetas libres para WorkItems).
- ~~Cadena de aprobación de releases fija (`QA`, `Líder`, `Negocio`); el diseño la quiere
  configurable por proyecto~~ **Resuelto 2026-08-24.** Tabla nueva
  `tblCadenaAprobacionProyecto` (script
  `41_2026-08-24_SCRIPT_bdsGTE_CadenaAprobacionProyecto.sql`, 1-a-N por proyecto,
  reemplazo completo en cada guardado). `CambiarEstatusReleaseHandler` usa la cadena
  configurada del proyecto si existe; si no, sigue con el default fijo
  `RolesAprobacion.Cadena` (ningun proyecto existente cambia de comportamiento). Editor
  en Admin > Workflows (gateado por `ADM.Workflows`, permiso ya sembrado sin consumidor
  hasta ahora): elegir proyecto, lista ordenada de roles con mover arriba/abajo/quitar/
  agregar, "Restaurar default" limpia la config (vuelve a `QA -> Lider -> Negocio`).
  **Verificado**: `dotnet build`/`dotnet test` (58/58) y `tsc -b`/`oxlint` limpios. **No
  verificado en vivo en navegador** (sin LocalDB disponible en esta sesion) -- pendiente
  real: probar el flujo end-to-end (configurar cadena custom, solicitar aprobacion de un
  release real, confirmar que crea las firmas en el orden configurado).
- RN-PLA-01 (avisar si el sprint se compromete por encima de la velocidad histórica +20%)
  no implementado; la capacidad sí se compara contra horas.
- `spImportarJira` planeado, no escrito.
- ~~Suplantación auditada (permiso `ADM.Suplantar` ya sembrado) sin implementar~~
  **Resuelto 2026-08-24.** "Iniciar sesion como" en Admin > Usuarios (oculto si no tienes
  `ADM.Suplantar` o si el objetivo eres tu mismo): pide re-autenticacion con la PROPIA
  contraseña del suplantador (no la del suplantado) en `IniciarSuplantacionCommand`, emite
  un JWT cuya identidad efectiva (`preferred_username`, la que evalua RBAC via
  `AuditContext.Usuario`) es la del suplantado, con un claim extra `actor_real` que
  `AuditMiddleware` vuelca a `AuditContext.UsuarioReal` -- doble identidad real en
  `tblBitacora` (columna nueva `UsuarioReal`, script
  `40_2026-08-24_SCRIPT_bdsGTE_BitacoraSuplantacion.sql`). Banner visible "Actuando como
  X · admin: Y" con boton "Salir" en la barra superior mientras dura la suplantacion
  (`estaSuplantando()` en `sesion.ts`, token real guardado aparte en sessionStorage).
  "Salir"/`TerminarSuplantacionCommand` solo deja rastro en bitacora (el JWT no tiene
  estado que revocar) y hace recarga completa de la SPA para descartar cache/estado
  cargado con la identidad suplantada. Sin suplantacion anidada (bloqueada con
  `BusinessException` si `AuditContext.EsSuplantacion` ya es true). **Verificado**:
  `dotnet build`/`dotnet test` (58/58) y `tsc -b`/`oxlint` limpios. **No verificado en
  vivo en navegador** (sin entorno de LocalDB disponible en esta sesion para probar el
  flujo end-to-end con dos usuarios reales) -- pendiente real: probarlo manualmente antes
  de considerarlo cerrado del todo en produccion.
- ~~Tema oscuro y revisión de accesibilidad (WCAG AA) pendientes~~ **Alcance acotado
  resuelto 2026-08-24** (decisión explícita con el usuario: dark mode funcional + pase de
  accesibilidad acotado, NO una auditoría WCAG AA formal completa). Toggle de tema
  claro/oscuro persistente (`localStorage`, sin tabla nueva -- no hay preferencias de
  usuario en BD todavía) en la barra superior (`App.tsx`); `primary`/`secondary` quedan
  fijos en ambos modos (MUI ya resuelve `contrastText` legible) y `palette.mode` deja que
  MUI calcule background/paper/texto/`divider` del modo oscuro con sus defaults (ya
  pensados para contraste WCAG AA), salvo `background.default` en claro que se conserva
  igual que antes para no regresionar. Accesibilidad: se agregaron `aria-label` a los
  `IconButton` de la barra superior que no lo tenían (menu, notificaciones, nuevo toggle
  de tema) -- no se auditó el resto de la app boton por boton (fuera del alcance
  acotado). Sin overrides de `outline` que rompan el foco de teclado (verificado con
  grep, cero coincidencias). **Fuera de alcance, señalado explícitamente**: ~56
  colores hexadecimales fijos en 9 archivos (concentrados en
  `DashboardEjecutivoPage.tsx`, `CatalogoReportesPage.tsx` y `DiagramaFlujoSolicitud.tsx`
  -- paletas de categorías/estatus para graficas y diagramas) no se tocaron: remapearlos
  de forma segura exige verlos en vivo en ambos modos, y esta sesión no pudo levantar
  LocalDB para probarlo (ver abajo). Revisarlos es el pendiente real si se quiere
  profundizar la accesibilidad en modo oscuro. **Verificado**: `tsc -b`/`oxlint` limpios.
  **No verificado en vivo**: la base `bdsGTE` no existe en la LocalDB de este entorno
  (crearla exige correr ~40 scripts de `DataBase/Scripts` en orden, fuera de alcance
  razonable solo para una verificación visual) -- se pudo confirmar que la app carga sin
  romperse (pantalla de login) pero no se llegó a ver la barra superior autenticada con
  el toggle real. Pendiente real: probarlo con LocalDB poblada (login real) y confirmar
  visualmente contraste en ambos modos.
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
- ~~**Catálogo de Complejidades sin semilla de datos**~~ **Resuelto 2026-08-24.** Script
  `39_2026-08-24_INSERT_bdsGTE_ComplejidadSeed.sql`: siembra Baja/Media/Alta (Orden 1-3)
  para ambientes nuevos sin migración del GT (la migración real ya siembra sus propios
  nombres reales `Basica`/`Media`/etc. desde `bdsApollo`, y no corre en estos ambientes).
  Es un default genérico editable por administración, no un catálogo fijo de negocio.
  **Deliberadamente NO siembra `tblMatrizPresupuesto`** (Complejidad x Nivel -> Minutos/
  Puntos, RN-REQ-08): son valores reales de negocio que este script no debe inventar: sin
  esas filas, el presupuesto automático simplemente no se calcula para WorkItems con estas
  complejidades hasta que administración capture la matriz real.
- **Edición de WorkItem, fuera de alcance deliberado de esta entrega:** el modal de alta
  (`NuevoItemModal.tsx`) sigue sin captura de complejidad ni puntos de historia (no se pidió
  ampliarlo); no se introdujo deshabilitado de campos individuales por permiso (el backend
  revalida cada regla y su 403 se muestra tal cual, consistente con el resto de la app).
- ~~**Notificaciones, sin canales Correo/Teams/WhatsApp**~~ **Correo resuelto 2026-08-24**
  (Teams/WhatsApp siguen sin implementación -- decisión explícita: sin credenciales de
  ningún tipo disponibles en este entorno para esos dos). `CanalCorreoSmtp : ICanalNotificacion`
  via `System.Net.Mail.SmtpClient` (sin agregar dependencia nueva al proyecto);
  `ServicioNotificaciones` resuelve el correo de cada destinatario (`tblUsuario.Correo`) y
  llama al canal ademas del InApp de siempre. **Sin credenciales SMTP en este entorno**:
  `Smtp:Habilitado=false` por default en `appsettings.json` -- el canal existe pero no
  envia nada hasta que alguien configure host/usuario/password reales; un fallo de envio
  se atrapa y solo deja warning en log (no tumba el flujo de negocio que disparo la
  notificacion). **Verificado**: `dotnet build`/`dotnet test` (58/58) limpios. **No
  verificado en vivo** (sin servidor SMTP real disponible para probar un envio real) --
  pendiente real: configurar credenciales SMTP reales y confirmar que llega un correo.
  Sigue fuera de alcance (sin cambios): `tblPlantillaNotificacion` (mensajes siguen armados
  inline en cada disparador), disparador de Solicitud convertida ni de Release liberado,
  grupo por equipo en el broadcast de tablero (`Clients.All`), eliminar/editar
  notificaciones (solo alta + marcar leída) y preferencias de canal/evento por usuario (el
  Documento Maestro las menciona en el perfil pero no hay tabla para ellas -- decisión
  explícita de no construirlas en esta pasada, se manda correo a todo destinatario con
  correo capturado, sin opt-out).

---

## 4. Decisiones firmes (no cambiar sin acuerdo del equipo)

| ADR | Decisión |
|---|---|
| 02 | .NET 8 (retargeteado desde .NET 9 el 2026-08-01) + React. Stack ya validado. El backend en .NET 8 alineado a estándares |
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

- **Rutas de máquina de desarrollo dentro de `appsettings.json` se publican al servidor y rompen funciones completas en silencio** (2026-08-26): `AlmacenArchivos:Ruta = D:\GTE\Archivos` viajó al servidor de despliegue, que no tiene unidad `D:`, y TODA subida de archivos empezó a fallar con `INTERNAL_ERROR` — en cuatro pantallas distintas a la vez, lo que hacía parecer que el bug era de cada módulo. **Lección doble**: (1) un valor de configuración específico de una máquina no debe quedar como default en el archivo que se publica — o va vacío (con fallback en código) o el instalador está obligado a fijarlo; (2) cuando el MISMO error aparece en varias pantallas sin relación entre sí, la causa es un recurso compartido (almacén, BD, permiso), no cada pantalla. Ahora hay chequeo al arranque, `GET /api/v1/version/almacen` para diagnosticar sin entrar al servidor, y el instalador lo comprueba con una sonda de escritura.
- **Una pista de UI que MIENTE es peor que no tener pista** (2026-08-26, misma causa raíz que el punto anterior): `asistente-instalacion.html` tenía la ruta del almacén detrás de un checkbox opcional cuya ayuda decía "Si no lo marcas, se usa la carpeta local junto al ejecutable". Falso: si no se marcaba, el asistente no escribía la variable de entorno y ganaba el `D:` de `appsettings.json`. El fallback a carpeta local solo ocurre cuando la ruta está VACÍA, y nunca lo estaba. Quien instalara con carpeta local hacía exactamente lo que decía la pista y se llevaba una instalación rota. **Lección**: al documentar un default en la UI, verificar contra el código qué pasa REALMENTE en la rama "no configurado" — no asumir que "no marcar" equivale a "vacío".
- **En PowerShell el backslash NO escapa dentro de cadenas** (2026-08-26, bug propio detectado al probar): se escribió `$ruta -like '\\*'` para detectar un share UNC, pensando en semántica tipo C. En PowerShell (donde el escape es la backtick) eso son CUATRO barras literales, así que ningún UNC entraba y se trataba como carpeta local. El correcto es `-like '\*'`. Se comprobó con una tabla explícita de rutas (`\servidor\...`, `C:\...`, texto suelto). **Trampa adicional del mismo día**: `Join-Path` valida la unidad y lanza antes que el `catch` útil, tapando el error real ("no existe la unidad Z") con un "la ruta no puede ser nula" que no le dice nada a quien instala — usar `[IO.Path]::Combine` cuando la ruta puede ser inválida a propósito. **Y**: no probar bloques de PowerShell copiándolos a un heredoc de Bash, que se come las barras y produce falsos positivos; invocar el archivo real.
- **EF y columnas `bit`**: el `DEFAULT 1` de la base no aplica en los INSERT de EF. Toda
  alta debe fijar `Activo = true` explícitamente.
- **EF y proyecciones intermedias**: filtrar u ordenar sobre un DTO/record ya proyectado da
  error 500 (LINQ no traducible). Patrón correcto: unir entidades sin proyectar, filtrar por
  columnas reales, proyectar al final con `Expression<Func<T,TResult>>` (ver
  `PlaneacionQueryService`).
- **EF y SPs con valor de retorno**: `ExecuteSqlRaw` no sirve; usar `DbCommand` con
  `ParameterDirection.ReturnValue` (ver `MotorWorkflow`).
- **Dos fallos de EF que el compilador NO ve y solo aparecen contra datos reales** (2026-08-28,
  R15: los dos tumbaron el reporte en producción, uno tras otro, después de haberlo dado por
  bueno porque "compilaba limpio"):
  1. **Constructor dentro del `Select` de un `GroupBy` que entra a un join → no traduce.** Se
     usó `new RelojEstatusDTO(g.Key, g.Sum(...))` (record posicional) como subconsulta de un
     LEFT JOIN: EF pierde el tipo, castea las llaves a `object` y lanza *The LINQ expression
     could not be translated* al EJECUTAR. Correcto: tipo de **referencia** con propiedades
     settables e **inicializador de objeto** (`new X { A = ..., B = ... }`), o un tipo anónimo.
  2. **`SUM()` de un LEFT JOIN llega NULL y revienta al MATERIALIZAR.** Un item sin filas del
     lado derecho hace que el SUM sea NULL; leerlo en un `int` no nullable da *Nullable object
     must have a value*, con la consulta ya traducida y ejecutada. Correcto: la propiedad
     destino va como `int?` (o el SUM casteado a `(int?)`) y se coalesce al proyectar. Ademas,
     probar `r.Minutos != null` en vez de `r == null`: EF aplana el LEFT JOIN en columnas, no
     entrega un objeto nulo.
  **Lección de método**, más allá de los dos casos: `dotnet build` y `oxlint` en verde NO son
  evidencia de que una consulta EF funciona. `ToQueryString()` (que no abre conexión) cubre la
  traducción pero es CIEGO a la materialización, porque nunca lee filas. La regresión de los dos
  patrones está en `GTE.Api.Tests/TraduccionConsultasR15Tests.cs`, que corre sin BD y por eso es
  útil en cualquier máquina; la segunda trampa se cubre exigiendo `COALESCE` en el SQL generado.
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
- **No hay forma segura de leer "la tabla de catalogo de estatus de este proceso" dinamicamente** (2026-08-04, construyendo el editor de Workflows): `tblProceso.TablaEstatus` guarda el nombre como texto (ej. `"dbo.tblEstatusTicket"`), pero armar el SELECT con ese texto es SQL interpolado (`FromSqlRaw($"SELECT ... FROM {tabla}")`), prohibido por regla dura del proyecto aunque el texto venga de una fila propia (no de un usuario) -- el riesgo no es solo inyeccion, es que EF no puede validar tipos/columnas contra una tabla desconocida en tiempo de compilacion. Solucion: mapeo explicito en codigo (switch de un caso por proceso, 11 en total) contra los DbSets ya scaffoldeados (`TblEstatusWorkItem`, `TblEstatusTicket`, etc.), ver `WorkflowQueryService.ObtenerCatalogoEstatusAsync`. Mas repetitivo que reflection/SQL dinamico, pero tipado y sin riesgo.
- **Un permiso nuevo insertado ANTES del gate de una regla de negocio existente puede tapar el 400/403 que una prueba esperaba** (2026-08-04): al agregar RN-QA-06 (WI.SaltarPruebas) en `CambiarEstatusWorkItemHandler`, se coloco el chequeo antes de `ValidarCierreAsync` (donde vive RN-REQ-03, "sin avance registrado"). Una prueba existente (`VerticalCompleto_CrearIniciarSuspenderRegistrarTerminar`) esperaba `400 BadRequest` de RN-REQ-03 pero recibio `403 Forbidden` de la regla nueva, porque su proyecto de prueba era categoria Desarrollo y el usuario (`lgarcia`, sin `WI.SaltarPruebas`) nunca llegaba al segundo chequeo. No era un bug del codigo nuevo -- la regla nueva SI debia bloquear ese escenario -- sino un fixture de prueba que ahora colisionaba con una regla que no existia cuando se escribio. Arreglo: cambiar la categoria del proyecto de prueba a TI (la prueba no busca cubrir RN-QA-06, busca RN-REQ-03). **Leccion**: al agregar un gate nuevo en un flujo con multiples validaciones encadenadas, correr toda la suite de pruebas de ese modulo (no solo compilar) para encontrar este tipo de colision de orden.
- **Después de correr `dotnet test` recién agregado un ALTER de columna, si el error es `Invalid column name` en LocalDB: correr el script SQL contra LocalDB antes de asumir que el código está mal** — las pruebas de integración (`GTE.Api.Tests`) apuntan a la misma `bdsGTE` de LocalDB que usa `dotnet run`, pero el `dotnet build`/`dotnet test` NO corre los scripts de `DataBase/Scripts` (no hay migraciones EF en este proyecto, es deliberado — ver sección 4). `sqlcmd`/`Invoke-Sqlcmd` desde una ruta con `:` en Git Bash falla con "Acceso denegado" (interpreta `C:` como un flag) — usar PowerShell para invocar `sqlcmd` con rutas Windows.

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
