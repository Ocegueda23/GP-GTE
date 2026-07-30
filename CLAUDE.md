# GTE - Reglas del repositorio

Este repo implementa la plataforma GTE. La fuente de decisiones es
`Doctos/GTE-DocumentoMaestro.md`; las convenciones generales del ecosistema estan en el
InterfloClaude.md global (secciones 7 a 13 aplican aqui con las adaptaciones de abajo).

## Stack (ADR-02 del Documento Maestro)

- Backend: .NET 9, ASP.NET Core, EF Core 9, MediatR 12.x, FluentValidation,
  AutoMapper 13.0.1, Serilog. (MediatR y AutoMapper quedan fijados en estas versiones
  por licencia libre; no subir de major sin decision del equipo.)
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

## Frontend

- Estado servidor con TanStack Query; UI con Zustand por feature; actualizaciones inmutables.
- El cliente HTTP lee ApiResponse<T> (response para el dato, code/success para el flujo).
- Pre-validar con Zod como espejo, pero el backend es la fuente de verdad.
- Botones de estatus desde GET workflow/acciones; nunca decidir transiciones en el front.
