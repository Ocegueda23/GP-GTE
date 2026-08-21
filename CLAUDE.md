# GTE - Reglas del repositorio

Este repo implementa la plataforma GTE. La fuente de decisiones es
`Doctos/GTE-DocumentoMaestro.md`; las convenciones generales del ecosistema estan en el
InterfloClaude.md global (secciones 7 a 13 aplican aqui con las adaptaciones de abajo).

## Repositorio (ADR-09)

Definitivo: https://github.com/Ocegueda23/GP-GTE (rama `main`, remoto `origin`).
Excepcion deliberada al estandar del ecosistema, que usa Gitea self-hosted; aplica
SOLO a este proyecto. No migrar sin decision del equipo.

## Stack (ADR-02 del Documento Maestro)

- Backend: .NET 8 (LTS; retargeteado desde .NET 9 el 2026-08-01 -- ver PENDIENTES.md
  seccion 4 y 5, el servidor real de destino no tenia el runtime 9.0 instalado y ya de
  paso alinea a GTE con el estandar del resto del ecosistema, Frente B), ASP.NET Core,
  EF Core 8, MediatR 12.x, FluentValidation, AutoMapper 13.0.1, Serilog. (MediatR y
  AutoMapper quedan fijados en estas versiones por licencia libre; no subir de major sin
  decision del equipo.)
- Frontend: React 18 + TypeScript + Vite (frontend/gte-web).
- BD: SQL Server - bdsGTE, LA UNICA BASE DEL SISTEMA (ADR-03: independencia total; el
  motor de estatus tblProceso/tblTransicion/spCambiarEstatus y los folios spGenerarFolio
  viven dentro de bdsGTE).

## Reglas duras

- Flujo estricto: Controller -> Command/Query MediatR (AppService) -> Repository (escritura)
  / QueryService (lectura) -> DbContext. Domain Services sin EF.
- Un solo DbContext (DbContextGTE); scaffold en Infrastructure/Modelos/bdsGTE. PROHIBIDO
  agregar conexiones o referencias a otras bases de datos.
- Ciclo de vida del contexto via FabricaContexto.ConectarContexto<T>(); los repositorios
  no reciben DbContext por DI ni implementan IDisposable.
- DTOs Request/Response separados por feature (sufijos Request/Response/DTO); nunca
  exponer entidades del scaffold.
- Toda respuesta usa ApiResponse<T>; excepciones de dominio via GlobalExceptionMiddleware.
- Auditoria SIEMPRE del token (AuditContext llenado por AuditMiddleware), nunca del payload.
  Sin identidad, AuditContext.Usuario queda VACIO (usar TieneIdentidad): no usar centinelas
  con texto porque pueden coincidir con una cuenta real y confundir la auditoria.
- Toda la API exige autenticacion por FallbackPolicy. Un endpoint solo se abre con
  [AllowAnonymous] y con una razon escrita (hoy: health, version y auth/configuracion).
  Bitacora con contexto de vida corta (RegistrarBitacoraAsync de RepositoryBase).
- Cambios de estatus SOLO via IMotorWorkflow (dbo.spCambiarEstatus, motor propio); el
  front manda la accion, nunca el estatus destino. El estatus inicial lo fija el backend.
- Todo calculo de tiempo laborable pasa por ICalendarioLaboral (motor unico).
- Metodos en espanol (Obtener/Crear/Actualizar/Eliminar...); sin emojis ni simbolos
  decorativos en codigo, comentarios ni commits.
- El esquema de BD lo gobiernan los scripts idempotentes de DataBase/Scripts
  (nomenclatura y plantillas de InterfloClaude.md seccion 10); NO usar migraciones EF.
  Tras cada cambio de esquema: re-scaffold y revision de computadas/tipos.
- Bajas: logicas (Activo = 0); borradores: hard delete. TRAMPA EF: el default de BD de
  las columnas bit (Activo DEFAULT 1) NO aplica de forma confiable en INSERTs de EF;
  toda alta fija Activo = true explicitamente en la entidad.
- Sin SQL interpolado; sin SQL almacenado en datos.
- TRAMPA EF (§7.8): no filtrar ni ordenar sobre proyecciones intermedias complejas
  (records/DTOs anidados). Patron correcto: unir entidades sin proyectar, filtrar y
  ordenar por columnas reales, y proyectar al final con Expression<Func<T,TResult>>
  (ver ConsultaBase/ProyeccionTarjeta en PlaneacionQueryService).

## Frontend

- Estado servidor con TanStack Query; UI con Zustand por feature; actualizaciones inmutables.
- El cliente HTTP lee ApiResponse<T> (response para el dato, code/success para el flujo).
- Pre-validar con Zod como espejo, pero el backend es la fuente de verdad.
- Botones de estatus desde GET workflow/acciones; nunca decidir transiciones en el front.

<!-- INICIO: estructura-proyecto-auto -->
## Estructura del proyecto (generado automaticamente)
_Ultima actualizacion: 2026-08-21 -- generado con /analizar-proyecto_

### Resumen
GTE (Gestor Tecnologico Empresarial) es la plataforma de gestion del departamento de
desarrollo de software de Interflo: portafolio, backlog/sprints, workflow de WorkItems,
soporte/tickets, calidad, releases, costeo, OKRs, dashboards ejecutivos y reportes.
Sucesora del antiguo GT (WinForms).

### Stack tecnologico
- Backend: .NET 8 (ASP.NET Core, EF Core 8 solo scaffold, MediatR 12.x, FluentValidation,
  AutoMapper 14.0.0, Serilog, Hangfire 1.8.14 para jobs, ClosedXML para export Excel).
- Frontend: React 19 + TypeScript + Vite 8, MUI 9, TanStack Query 5, Zustand 5, React
  Router 7, Tiptap 3 (editor enriquecido), dnd-kit (drag&drop), recharts, SignalR client
  (notificaciones/tiempo real), axios, oxlint.
- BD: SQL Server, base unica `bdsGTE`. Sin migraciones EF: el esquema vive en
  `DataBase/Scripts` (scripts idempotentes) y se re-scaffolda a mano.
- Gestor de paquetes: NuGet (backend) y npm (frontend, `frontend/gte-web`).

### Como correrlo
```
dotnet run --project src/GTE.WebApi         # API en http://localhost:5088 (ver .claude/launch.json)
cd frontend/gte-web && npm run dev          # SPA en http://localhost:5173
dotnet test                                  # GTE.Domain.Tests / GTE.Application.Tests / GTE.Api.Tests
cd frontend/gte-web && npm run lint          # oxlint
cd frontend/gte-web && npm run build         # tsc -b && vite build
```
Cadena de conexion local en `appsettings.Development.json` (o `appsettings.Local.json`,
gitignored). Autenticacion dev sin tenant: `POST /api/v1/auth/desarrollo/token` con
`Jwt:Desarrollo:Habilitado=true` (ya activo en Development).

### Estructura de carpetas
```
src/
  GTE.Domain/            Entidades de negocio, constantes, excepciones de dominio (sin EF)
  GTE.Application/       Casos de uso MediatR (Commands/Queries por feature), DTOs
                         Request/Response, interfaces de puertos (repos/servicios)
  GTE.Infrastructure/    Scaffold EF (Modelos/bdsGTE), DbContextGTE, FabricaContexto,
                         Repositories (escritura), Services (*QueryService de lectura,
                         MotorWorkflow, CalendarioLaboral, GeneradorFolios, SnapshotKpiJob)
  GTE.WebApi/            Controllers, ApiResponse, GlobalExceptionMiddleware,
                         AuditMiddleware, Hubs (SignalR), Program.cs, AutoMapperProfile
tests/
  GTE.Domain.Tests/      Reglas de negocio puras
  GTE.Application.Tests/ Handlers/behaviors (pipeline de validacion)
  GTE.Api.Tests/         Integracion contra LocalDB real (se omiten si no existe)
frontend/gte-web/src/
  features/<modulo>/     Una carpeta por feature (admin, planeacion, workitem, soporte,
                         calidad, entregas, portafolio, dashboard, reportes, sesion...)
  shared/api/            Un modulo por dominio, llama al backend y devuelve tipos TS
  shared/components/     Componentes reutilizables (avatar, combo buscable, encabezados)
  shared/editor/         Editor enriquecido (Tiptap) y sanitizacion de contenido
  shared/hooks/, shared/tiempoReal/  Hooks compartidos y conexion SignalR
DataBase/Scripts/        Scripts SQL idempotentes versionados (01_Libera, 02_Libera,
                         Migracion) -- unica fuente de verdad del esquema
Doctos/                  GTE-DocumentoMaestro.md (diseno), PENDIENTES.md (estado y
                         continuidad), MANUAL_INSTALACION_GTE.md
```

### Modulos/componentes clave
- `src/GTE.Infrastructure/Services/MotorWorkflow.cs` -- unico punto de cambio de estatus,
  llama `dbo.spCambiarEstatus`.
- `src/GTE.Infrastructure/Services/CalendarioLaboral.cs` -- unico motor de tiempo laborable.
- `src/GTE.Infrastructure/Persistence/FabricaContexto.cs` -- ciclo de vida de DbContextGTE
  (los repos piden un contexto y lo disponen con `using`, no reciben DbContext por DI).
- `src/GTE.Infrastructure/Repositories/RepositoryBase.cs` -- base de repos de escritura:
  expone `Fabrica`/`Auditoria` y `RegistrarBitacoraAsync` (bitacora con contexto propio,
  sobrevive a rollback de la transaccion de negocio).
- `src/GTE.WebApi/Middleware/GlobalExceptionMiddleware.cs` -- mapea excepciones de dominio
  (NotFound/Validation/Business/Conflict/Forbidden) al envelope `ApiResponse<T>`.
- `src/GTE.WebApi/Middleware/AuditMiddleware.cs` -- llena `AuditContext` desde el token en
  cada request (nunca desde el payload).
- `src/GTE.Application/Common/AuditContext.cs` / `ComportamientoValidacion.cs` -- contexto
  de auditoria y pipeline behavior de FluentValidation en MediatR.
- `src/GTE.Infrastructure/Services/PlaneacionQueryService.cs` -- referencia del patron
  correcto de proyeccion EF (ConsultaBase/ProyeccionTarjeta, ver TRAMPA EF abajo).
- `frontend/gte-web/src/shared/api/http.ts` -- cliente axios que desenvuelve `ApiResponse<T>`.
- `frontend/gte-web/src/shared/tiempoReal/useConexionTiempoReal.ts` -- hook de conexion
  SignalR para notificaciones en vivo.

### Convenciones y patrones
- Capas estrictas: Controller -> Command/Query MediatR -> Repository (escritura) /
  QueryService (lectura) -> DbContext. Domain sin dependencias a EF.
- Un DbContext (`DbContextGTE`), una base (`bdsGTE`); nombres de tabla scaffoldeados como
  `Tbl*`/`Vw*` en `GTE.Infrastructure/Modelos/bdsGTE`.
- Metodos y nombres en espanol (Obtener/Crear/Actualizar/Eliminar/Retirar...).
- DTOs sufijados Request/Response/DTO, uno por feature bajo `Application/DTOs`; nunca se
  exponen las entidades del scaffold hacia el controller.
- Toda respuesta HTTP usa `ApiResponse<T>` (`Code`/`Success`/`UserMessage`/`Response`).
- Bajas logicas via `Activo = 0`; los borradores (drafts) se eliminan con hard delete.
- Sin migraciones EF: cambios de esquema entran por scripts numerados e idempotentes en
  `DataBase/Scripts/<Tanda>_Libera/NN_<fecha>_<CATEGORIA>_<Objeto>.sql`, seguidos de
  re-scaffold (ver comando en `README.md`).
- Tests xUnit por capa (`GTE.Domain.Tests`, `GTE.Application.Tests`, `GTE.Api.Tests`); las
  pruebas de integracion asumen LocalDB (`(localdb)\MSSQLLocalDB`, BD `bdsGTE`) y se
  omiten solas si no esta disponible.
- Frontend organizado por feature (`features/<modulo>`) con su propia store Zustand y
  llamadas centralizadas en `shared/api/<dominio>.ts`; validacion Zod es solo espejo del
  backend.

### Integraciones externas
- SQL Server local/LocalDB -- unica base de datos del sistema (`bdsGTE`).
- Microsoft Entra ID (JWT Bearer) en produccion para autenticacion; modo desarrollo emite
  tokens propios sin tenant.
- SignalR (`NotificacionesHub`) para notificaciones en tiempo real hacia el frontend.
- Hangfire con storage propio en `bdsGTE` (schema `HangFire`) para jobs recurrentes
  (snapshot nocturno de KPIs via `dbo.spSnapshotKpi`).
- ClosedXML para exportar reportes a Excel (elegido sobre EPPlus por licencia).
- Windows Service hosting (`Microsoft.Extensions.Hosting.WindowsServices`) para el
  despliegue del API como servicio.

### Notas para futuras modificaciones
- La fuente de verdad de arquitectura/decisiones es `Doctos/GTE-DocumentoMaestro.md`; el
  estado y continuidad entre sesiones vive en `Doctos/PENDIENTES.md` (leerlo antes de
  retomar trabajo, se actualiza al cerrar cada bloque).
- TRAMPA EF (parrafo largo, ver `CLAUDE.md` seccion "Reglas duras" y
  `PlaneacionQueryService`): no filtrar/ordenar sobre proyecciones intermedias tipo
  record/DTO anidado; unir sin proyectar, filtrar/ordenar por columnas reales, proyectar
  al final con `Expression<Func<T,TResult>>`.
- TRAMPA EF de columnas bit: el `DEFAULT 1` de BD no aplica de forma confiable en INSERTs
  de EF -- toda alta fija `Activo = true` explicitamente en la entidad.
- El repositorio real es GitHub (`Ocegueda23/GP-GTE`), excepcion deliberada al estandar
  Gitea del resto del ecosistema Interflo -- no migrar sin decision de equipo.
- Hay archivos sueltos sin versionar en la raiz del repo que no siguen la convencion de
  `DataBase/Scripts`: `DataBase/bdsGTE.sql` y `DataBase/script.sql` (dumps UTF-16 de SSMS
  "Generate Scripts", no idempotentes) y una carpeta `publicado/` con artefactos de
  publicacion/despliegue (incluye un `.bak`). Ambos aparecen como `??` en `git status`;
  no son parte del flujo de scripts versionado y no deberian tratarse como fuente de
  verdad del esquema.
- `MediatR` y `AutoMapper` quedan fijados en sus versiones actuales por licencia libre
  (AutoMapper 15+ es comercial); no subir de major sin decision del equipo.
- El backend fue retargeteado de .NET 9 a .NET 8 el 2026-08-01 (el servidor de destino no
  tenia el runtime 9 instalado); todo el codigo nuevo debe apuntar a `net8.0`.
<!-- FIN: estructura-proyecto-auto -->
