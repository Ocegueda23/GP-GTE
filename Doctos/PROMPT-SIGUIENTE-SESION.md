# Prompt para la siguiente sesión

Copiar el bloque de abajo tal cual en el chat nuevo. Sustituir la sección
**Objetivo** si se decide atacar otro frente (ver alternativas al final).

---

## Prompt (listo para copiar)

Trabajo en **GTE (Gestor Tecnológico Empresarial)**, la plataforma que sustituye al
Gestor de Proyectos WinForms de Interflo. El repositorio es `C:\CODE\GTE`
(GitHub: `Ocegueda23/GP-GTE`, rama `main`).

**Antes de escribir código, lee en este orden:**

1. `Doctos/PENDIENTES.md` — estado actual, pendientes por prioridad, decisiones firmes
   y trampas técnicas ya resueltas. Es la fuente al día.
2. `CLAUDE.md` (raíz del repo) — reglas duras de arquitectura y estilo.
3. `Doctos/GTE-DocumentoMaestro.md` — solo las secciones del módulo que vayamos a tocar
   (es largo; no lo leas completo).

**Estado al abrir este chat: todo lo de la sesión anterior está commiteado y empujado a
`main`** (commits `9fcd523`, `7c473fe`, `470188b`). No debería haber nada pendiente en
`git status` salvo `run-api-dev.cmd` (script local de conveniencia, deliberadamente fuera
del repo). Confírmalo de todas formas al abrir, no lo asumas ciegamente.

En la sesión anterior se cerraron tres bloques completos, todos verificados de punta a
punta (build limpio, `dotnet test` en 49/49, y prueba manual real en el navegador):

- **Comentarios y adjuntos (A1)**: hilos de comentarios sobre WorkItem con formato básico,
  @menciones con autocompletado (TipTap) e imágenes pegadas del portapapeles; adjuntos con
  subida/descarga por streaming autenticado. HTML sanitizado en el backend antes de
  guardarse.
- **Edición de WorkItem en la UI (A2)**: modal de edición sobre el endpoint `PUT
  /workitems/{id}` ya existente (título, descripción, prioridad, complejidad, asignado,
  compromiso, puntos), con el botón oculto cuando el usuario no podría guardar nada.
- **Notificaciones + SignalR (A3 + A6)**: campana de notificaciones In-App que llega en
  vivo (un solo `NotificacionesHub`), disparada al aprobar/rechazar/devolver una solicitud
  y al mencionar a alguien en un comentario; el tablero Kanban también se refresca solo
  cuando cualquier WorkItem cambia de estatus. Verificado con **dos sesiones reales
  simultáneas** en el navegador (push a un usuario específico + refresco de tablero de
  otra pestaña).

**Objetivo de esta sesión: a decidir con el usuario** entre las alternativas de abajo, o
lo que él prefiera.

**Matices críticos (no negociables, aplican a cualquier frente que se elija):**

- Toda la API exige token (401 sin él). Si agregas un endpoint público necesita
  `[AllowAnonymous]` y una razón escrita.
- El frontend **nunca** decide transiciones de estatus: pide las acciones válidas al motor
  y envía acciones, jamás un estatus destino.
- El esquema lo gobiernan los scripts idempotentes de `DataBase/Scripts` con la
  nomenclatura y plantilla del estándar; **no uses migraciones de EF**. Si cambias el
  esquema, corre el script y aplica el delta a mano en el scaffold (correr el scaffolder
  completo sin filtro reescribe `DbContextGTE.cs` entero y lo deja solo con las tablas que
  filtres — ver la lección en `PENDIENTES.md` sección 5).
- Toda alta debe fijar `Activo = true` explícitamente: el `DEFAULT` de la base no aplica
  en los INSERT de EF.
- No filtres ni ordenes sobre proyecciones intermedias complejas en EF (da error 500);
  une entidades, filtra por columnas reales y proyecta al final.
- Métodos en español; sin emojis ni caracteres decorativos en código, comentarios ni
  mensajes de commit.
- GTE no usa Entra ID ni ningún proveedor de identidad externo (decisión firme,
  2026-08-01): la autenticación, los accesos y los roles se manejan enteramente dentro de
  `bdsGTE`.
- **Si el módulo nuevo necesita avisarle algo a un usuario**, ya existe el mecanismo:
  inyecta `IServicioNotificaciones` (Application) y llama `NotificarAsync(idsUsuarios,
  titulo, mensaje, entidad, idEntidad, url)` después de que la acción de negocio tenga
  éxito — no reinventes el alta de `tblNotificacion` a mano. `ICanalNotificacion` sigue
  sin implementación (solo queda reservado para Correo/Teams cuando haya credenciales
  reales); no lo uses todavía.
- **Verifica de verdad antes de afirmar que algo funciona**: compila, corre
  `dotnet test` (hoy 49 pruebas en verde) y prueba el flujo por API o en el navegador.
  Para features de tiempo real (SignalR) o drag-and-drop (dnd-kit), el Browser pane SÍ
  puede probarlas con eventos sintéticos bien construidos — ver las lecciones nuevas en
  `PENDIENTES.md` sección 5 antes de asumir que "no se puede verificar aquí".
  Si algo de verdad no se puede verificar en este entorno, dilo explícitamente.

Cómo levantar la base, la API y el SPA está en la sección 1 de `Doctos/PENDIENTES.md`.

**Al terminar**: actualiza `Doctos/PENDIENTES.md` con lo que quedó hecho y lo que falta,
y haz commit y push a `main` (confirmando antes con el usuario).

---

## Alternativas de objetivo

Si se prefiere otro frente, sustituir la sección **Objetivo** por uno de estos
(están descritos con detalle en `PENDIENTES.md`):

| Frente | Por qué elegirlo |
|---|---|
| **Migración de datos del GT** (B3) | Necesario para el corte real; el mapeo está en el Documento Maestro §15.4 |
| **Despliegue** (B4) | Ya no bloquea en la parte de identidad (B2 resuelto); falta pipeline CI/CD, `appsettings.Production.json`, usuario de BD de mínimo privilegio, hosting (IIS/Kestrel/Docker) |
| **Hangfire** (A4) | Vigilancia de SLA, snapshot de KPIs (`spSnapshotKpi` ya existe), recordatorios de compromiso, despacho del outbox `tblEventoDominio`; complementa las notificaciones ya resueltas |
| **Portafolio** (A5) | Riesgos, hitos, OKRs, presupuesto/costo real por proyecto — módulo nuevo sin dependencias pendientes |
| **Integración Git** (resto de Fase 3) | Traza commits y PRs contra los WorkItems, tras la abstracción `IProveedorGit` |
| **Fase 4 (Operación y Soporte)** | Incidentes, Mesa de ayuda, Base de conocimiento (incluye migrar el Glosario Interflo del GT) |
